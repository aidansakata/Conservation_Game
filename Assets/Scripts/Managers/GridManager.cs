using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Text.RegularExpressions; // Required for manual parsing

public class GridManager : MonoBehaviour
{
    public static GridManager instance; // Singleton for easy access

    public enum Difficulty { Easy = 0, Medium = 1, Hard = 2 }

    [Header("Tilemap Setup")]
    [Tooltip("Assign your hex Tilemap here (Grid layout: Hexagonal).")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private List<TileTypeEntry> tileTypeEntries;
    private Dictionary<string, TileBase> _tileTypeMap;

    [Header("LevelDefinitions (nested serializable objects)")]
    [SerializeField] private LevelDefinition level1Easy8x8;   // width=8,  height=8
    [SerializeField] private LevelDefinition level2Med10x10;  // width=10, height=10
    [SerializeField] private LevelDefinition level3Hard12x12; // width=12, height=12
    [SerializeField] private LevelDefinition level4;          // optional
    [SerializeField] private LevelDefinition level5;          // optional

    [Header("Value Overlay (optional)")]
    [SerializeField] private bool showCellValues = true;
    [SerializeField] private TextMeshPro valueLabelPrefab;      // world-space TMP (3D) prefab
    [SerializeField] private Transform labelsParent;            // empty parent for labels (optional)

    private readonly List<TextMeshPro> spawnedLabels = new();
    private GameTiles tilesManager;
    private LevelDefinition def;
    private int currentLevelNumber;
    private bool _isLoading;

    // --- LOGIC ENGINE ---
    private HexagonGrid logicGrid;
    private HashSet<(int col, int row)> _revealedHints = new HashSet<(int col, int row)>();

    [Header("Submit Popup")]
    [SerializeField] private Sprite popupBoardSprite;
    [SerializeField] private Sprite popupLogoSprite;
    [SerializeField] private Sprite popupPawlSprite;
    [SerializeField] private Sprite popupPlayAgainSprite;
    [SerializeField] private Sprite popupBestCorridorSprite;
    [SerializeField] private Sprite popupHallOfFameSprite;
    [SerializeField] private Sprite popupCloseSprite;
    [SerializeField] private Sprite popupButterflySprite;
    [SerializeField] private Canvas popupCanvas;
    [SerializeField] private TextMeshProUGUI budgetWarningText;

    private GameObject _submitPopup;
    private TextMeshProUGUI _resultText;
    private TextMeshProUGUI _scoreText;
    private int _currentOptUtil = 1;
    private LevelDefinition _currentDef;
    private Coroutine _budgetWarningCoroutine;

    void Awake()
    {
        instance = this;
        tilesManager = GameTiles.instance ?? FindObjectOfType<GameTiles>();
        if (tilesManager != null && tilesManager.Tilemap == null && tilemap != null)
            tilesManager.Tilemap = tilemap;

        _tileTypeMap = new Dictionary<string, TileBase>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var entry in tileTypeEntries)
            if (entry != null && !string.IsNullOrEmpty(entry.typeName) && entry.tile != null)
                _tileTypeMap[entry.typeName.Trim().ToLower()] = entry.tile;
    }

    void Start()
    {
        if (popupCanvas == null)
        {
            var popupCanvasGO = new GameObject("PopupCanvas");
            var pc = popupCanvasGO.AddComponent<Canvas>();
            pc.renderMode = RenderMode.ScreenSpaceOverlay;
            pc.sortingOrder = 100;
            popupCanvasGO.AddComponent<CanvasScaler>();
            popupCanvasGO.AddComponent<GraphicRaycaster>();
            popupCanvas = pc;
        }

        CreateSubmitPopup();

        // Prefer string-based selected id when present (set by LevelSelectController)
        if (!string.IsNullOrEmpty(GameState.SelectedLevelId))
        {
            var cfg = Resources.Load<ApiConfig>("ApiConfig");
            var baseUrl = cfg != null ? cfg.baseApiUrl : "";
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _isLoading = true;
                StartCoroutine(LevelJsonLoader.LoadLevelJsonById(baseUrl, GameState.SelectedLevelId, (json) => {
                    try
                    {
                        ProcessLoadedJson(json);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"JSON load failed for id '{GameState.SelectedLevelId}': {ex.Message}. Falling back to numeric level {GameState.SelectedLevel}.");
                        LoadLevel(GameState.SelectedLevel);
                    }
                    finally
                    {
                        _isLoading = false;
                    }
                }, (err) => {
                    Debug.LogWarning($"JSON load failed for id '{GameState.SelectedLevelId}': {err}. Falling back to numeric level {GameState.SelectedLevel}.");
                    _isLoading = false;
                    LoadLevel(GameState.SelectedLevel);
                }));

                return; // avoid running numeric flow below
            }
            else
            {
                Debug.LogWarning("ApiConfig not found or baseApiUrl empty. Falling back to numeric level.");
            }
        }

        currentLevelNumber = Mathf.Clamp(GameState.SelectedLevel, 1, 5);
        LoadLevel(GameState.SelectedLevel);
    }

    public void LoadLevel(int levelNumber)
    {
        _isLoading = true;
        currentLevelNumber = Mathf.Clamp(levelNumber, 1, 5);
        GameState.SelectedLevel = currentLevelNumber;

        // --- NEW VARIATION LOGIC START ---
        if (currentLevelNumber == 1)
        {
            var cfg = Resources.Load<ApiConfig>("ApiConfig");
            string baseUrl = (cfg != null) ? cfg.baseApiUrl.Trim().TrimEnd('/') : "http://127.0.0.1:4000";

            int variation = Random.Range(1, 101);
            string levelId = $"landscape_{variation}";

            Debug.Log($"[GridManager] Requesting variation: {levelId} from {baseUrl}");

            StartCoroutine(LevelJsonLoader.LoadLevelJsonById(baseUrl, levelId,
                (json) =>
                {
                    try { ProcessLoadedJson(json); }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"JSON parse failed for {levelId}: {ex.Message}. Falling back.");
                        LoadLevelFallback(currentLevelNumber);
                    }
                },
                (err) =>
                {
                    Debug.LogWarning($"Failed to load {levelId} from {baseUrl}: {err}. Falling back.");
                    LoadLevelFallback(currentLevelNumber);
                }
            ));
            return;
        }

        StartCoroutine(LevelJsonLoader.LoadLevelJson(
            currentLevelNumber,
            onLoaded: (json) =>
            {
                try { ProcessLoadedJson(json); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"JSON parse failed for level {currentLevelNumber}: {ex.Message}. Falling back.");
                    LoadLevelFallback(currentLevelNumber);
                }
            },
            onError: (err) =>
            {
                Debug.Log($"No JSON found for level {currentLevelNumber} ({err}). Falling back.");
                LoadLevelFallback(currentLevelNumber);
            }
        ));
    }

    private void ProcessLoadedJson(string json)
    {
        var lj = JsonUtility.FromJson<LevelJson>(json);
        var d = LevelDefinitionMapper.FromJson(lj);

        if (d.ecoData1 == null || d.ecoData1.Count == 0 || (d.ecoData1.Count > 0 && d.ecoData1[1] == 0))
        {
            Debug.LogWarning("EcoData1 appears empty/zero. Attempting Manual Parse...");
            d.ecoData1 = ManualParseList(json, "\"utilities\"");
            Debug.Log($"Manual Parse Result: {d.ecoData1.Count} items found. Index 1 value: {(d.ecoData1.Count > 1 ? d.ecoData1[1] : -1)}");
        }

        PrepareDefinition(d, currentLevelNumber);
        LoadComplete(d, currentLevelNumber);
    }

    private List<int> ManualParseList(string json, string fieldName)
    {
        List<int> result = new List<int>();
        int startIdx = json.IndexOf(fieldName);
        if (startIdx == -1) return result;

        int arrayStart = json.IndexOf('[', startIdx);
        int arrayEnd = json.IndexOf(']', arrayStart);
        if (arrayStart == -1 || arrayEnd == -1) return result;

        string content = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
        string[] parts = content.Split(',');

        foreach (var p in parts)
        {
            if (int.TryParse(p.Trim(), out int val))
                result.Add(val);
        }
        return result;
    }

    private void LoadLevelFallback(int levelNumber)
    {
        LevelDefinition candidate = null;
        try { candidate = GridLayouts.GetLayout(levelNumber); }
        catch { }

        if (candidate == null)
        {
            candidate = levelNumber switch
            {
                1 => level1Easy8x8,
                2 => level2Med10x10,
                3 => level3Hard12x12,
                4 => level4,
                5 => level5,
                _ => null
            };
        }

        if (candidate == null)
        {
            Debug.LogError($"GridManager: No LevelDefinition available for level {levelNumber}");
            return;
        }

        PrepareDefinition(candidate, levelNumber);
        LoadComplete(candidate, levelNumber);
        Debug.Log($"[GridManager] Painted {candidate.width}x{candidate.height} (level {levelNumber}).");
    }

    public void ReloadCurrent()
    {
        _revealedHints.Clear();
        LoadLevel(currentLevelNumber);
    }

    public void ResetCurrentLevel()
    {
        if (_currentDef == null || tilesManager == null) return;

        foreach (var tile in tilesManager.tiles.Values)
        {
            if (tile.Purchased)
            {
                tile.Purchased = false;
                tile.TilemapMember.SetTileFlags(tile.LocalPlace, TileFlags.None);
                tile.TilemapMember.SetColor(tile.LocalPlace, Color.white);
            }
            else
            {
                tile.TilemapMember.SetTileFlags(tile.LocalPlace, TileFlags.None);
                tile.TilemapMember.SetColor(tile.LocalPlace, Color.white);
            }
        }

        tilesManager.score = 0;
        tilesManager.budget = _currentDef.budget;
        tilesManager.boughtTiles.Clear();
        _revealedHints.Clear();

        if (_submitPopup != null)
            _submitPopup.SetActive(false);

        Debug.Log("[GridManager] Level reset. Landscape unchanged.");
    }

    public void NextLevel() => LoadLevel(Mathf.Clamp(currentLevelNumber + 1, 1, 5));

    public void StartLevelById(string levelId)
    {
        if (_isLoading) return;
        _isLoading = true;

        var cfg = Resources.Load<ApiConfig>("ApiConfig");
        string baseUrl = (cfg != null) ? cfg.baseApiUrl : "http://localhost:4000/levels/";

        StartCoroutine(LevelJsonLoader.LoadLevelJsonById(baseUrl, levelId, (json) =>
        {
            try { ProcessLoadedJson(json); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"JSON parse failed for level id {levelId}: {ex.Message}. Falling back.");
                _isLoading = false;
                LoadLevelFallback(GameState.SelectedLevel);
            }
        }, (err) =>
        {
            Debug.LogWarning($"JSON load failed ({levelId}): {err}; falling back");
            _isLoading = false;
            LoadLevelFallback(GameState.SelectedLevel);
        }));
    }

    private void LoadComplete(LevelDefinition d, int levelNumber)
    {
        def = d;
        _currentDef = d;
        _currentOptUtil = d.optUtil > 0 ? d.optUtil : 1;

        int ecoCount = (def.ecoData1 != null) ? def.ecoData1.Count : 0;
        Debug.Log($"[LoadComplete] Level Data Loaded. EcoData1 Count: {ecoCount}, Budget (Tile Limit): {d.budget}");

        if (ecoCount == 0) Debug.LogError("CRITICAL: EcoData1 is EMPTY. Tiles will have 0 Value.");

        PaintTiles(def);
        ApplyToWorldTiles(def);
        if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
        else ClearValueLabels();
        if (levelNumber != -1) ConfigureCameraForLevel(levelNumber);

        InitializeLogicGrid(def);
        _isLoading = false;
    }

    private void InitializeLogicGrid(LevelDefinition d)
    {
        logicGrid = new HexagonGrid(d.width);

        for (int y = 0; y < d.height; y++)
        {
            for (int x = 0; x < d.width; x++)
            {
                int jsonRow = y;
                int idx = jsonRow * d.width + x;

                if (idx < 0 || idx >= d.CellCount) continue;

                var hex = logicGrid.GetHexByCoords(x, y);

                if (hex != null)
                {
                    hex.Optimal = (idx < d.optimalData.Count) ? d.optimalData[idx] : 0;
                    hex.Utility = (idx < d.ecoData1.Count) ? d.ecoData1[idx] : 0;

                    string tileTypeStr = (idx < d.tileTypes.Count) ? d.tileTypes[idx] : "";
                    hex.Type = (!string.IsNullOrEmpty(tileTypeStr)) ? tileTypeStr.Trim().ToLower() : "forest";
                }
            }
        }
        Debug.Log("Logic Grid Initialized.");
    }

    public void OnHintClicked()
    {
        if (logicGrid == null || tilesManager == null) return;

        List<(int col, int row)> purchased = new List<(int, int)>();
        foreach (var kv in tilesManager.boughtTiles)
        {
            purchased.Add((kv.Key.x, kv.Key.y));
        }

        try
        {
            var hintTuple = logicGrid.GetHint(purchased, _revealedHints);
            if (hintTuple.col == -1 && hintTuple.row == -1)
            {
                Debug.Log("All hints revealed.");
                return;
            }
            _revealedHints.Add((hintTuple.col, hintTuple.row));
            Vector3Int hintPos = new Vector3Int(hintTuple.col, hintTuple.row, 0);

            if (tilesManager.tiles.TryGetValue(hintPos, out WorldTile tile))
            {
                // UNLOCK FLAGS: Required to allow color changes via script
                tile.TilemapMember.SetTileFlags(tile.LocalPlace, TileFlags.None);

                // HIGHLIGHT: Set to Yellow
                tile.TilemapMember.SetColor(tile.LocalPlace, new Color(0f, 0f, 0.5f));

                if (valueLabelPrefab != null)
                {
                    var label = Instantiate(valueLabelPrefab, labelsParent ? labelsParent : transform);
                    label.transform.position = tile.TilemapMember.GetCellCenterWorld(tile.LocalPlace) + new Vector3(0, 0, -1);
                    label.text = "HINT";
                    label.color = Color.yellow;
                    label.fontSize = 6;
                    Destroy(label.gameObject, 3f);
                }
                Debug.Log($"Hint highlighted at: {hintPos}");
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("No hint available (or grid error): " + e.Message);
        }
    }

    public void OnSubmitClicked()
    {
        if (_submitPopup == null)
        {
            Debug.LogError("[GridManager] _submitPopup is null. Was CreateSubmitPopup() called?");
            return;
        }

        if (logicGrid == null || tilesManager == null) return;

        List<(int col, int row)> purchased = new List<(int, int)>();
        foreach (var kv in tilesManager.boughtTiles)
        {
            purchased.Add((kv.Key.x, kv.Key.y));
        }

        var (isConnected, visited) = logicGrid.isValidCorridor(purchased);

        float rawScore = tilesManager != null ? tilesManager.score : 0f;
        float pct = Mathf.Round((rawScore / _currentOptUtil) * 100f);

        Debug.Log($"[Submit] isConnected={isConnected}, rawScore={rawScore}, optUtil={_currentOptUtil}, optimalDataCount={(_currentDef?.optimalData != null ? _currentDef.optimalData.Count : -1)}");
        if (_submitPopup != null) _submitPopup.SetActive(true);

        if (isConnected)
        {
            if (_resultText != null)
                _resultText.text = "Your corridor was tested by Pawl";
            if (_scoreText != null)
                _scoreText.text = $"{(int)rawScore} pts";
        }
        else
        {
            if (_resultText != null)
                _resultText.text = "Your corridor was tested by Pawl";
            if (_scoreText != null)
            {
                _scoreText.text = "Path not connected.\nConnect both habitats to score.";
                _scoreText.fontSize = 22;
            }
        }
    }

    private void PrepareDefinition(LevelDefinition d, int levelNumber)
    {
        if (d.width <= 0 || d.height <= 0)
        {
            if (levelNumber == 1) { d.width = 8; d.height = 8; }
            if (levelNumber == 2) { d.width = 10; d.height = 10; }
            if (levelNumber == 3) { d.width = 12; d.height = 12; }
        }

        d.EnsureSize(d.width, d.height, defaultTileType: "forest", defaultCost: 1);

        bool looksEmpty = d.tileTypes == null || d.tileTypes.Count == 0 || d.tileTypes.Count != d.width * d.height;

        if (looksEmpty)
        {
            switch (levelNumber)
            {
                case 1: d.Fill((x, y, w, h) => ((x + y) % 2 == 0) ? "habitat" : "forest"); break;
                case 2: d.Fill((x, y, w, h) => (x == w / 2) ? "forest" : "habitat"); break;
                case 3: d.Fill((x, y, w, h) => (x == y || x == (w - 1 - y)) ? "grassland" : "habitat"); break;
                default: d.Fill((x, y, w, h) => "forest"); break;
            }
        }
    }

    private void PaintTiles(LevelDefinition d)
    {
        int total = d.width * d.height;
        if (total <= 0) { tilemap.ClearAllTiles(); return; }

        var tileArray = new TileBase[total];

        for (int y = 0; y < d.height; y++)
            for (int x = 0; x < d.width; x++)
            {
                int jsonRow = y;
                int idx = jsonRow * d.width + x;
                string tileTypeStr = (idx >= 0 && idx < d.tileTypes.Count) ? d.tileTypes[idx] : null;
                int visualIdx = y * d.width + x;

                string tileKey = (tileTypeStr != null) ? tileTypeStr.Trim().ToLower() : "forest";
                TileBase visualTile = _tileTypeMap.TryGetValue(tileKey, out var t) ? t : null;

                tileArray[visualIdx] = visualTile;
            }

        tilemap.ClearAllTiles();
        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, d.width, d.height, 1), tileArray);
        tilemap.RefreshAllTiles();
        tilemap.CompressBounds();
    }

    private void ApplyToWorldTiles(LevelDefinition d)
    {
        if (tilesManager == null)
            tilesManager = GameTiles.instance ?? FindObjectOfType<GameTiles>();
        if (tilesManager == null) return;

        if (tilesManager.Tilemap == null && tilemap != null)
            tilesManager.Tilemap = tilemap;

        tilesManager.RebuildFromTilemap(tilemap ?? tilesManager.Tilemap);

        tilesManager.budget = d.budget;
        tilesManager.score = 0;
        tilesManager.boughtTiles.Clear();

        foreach (var kv in tilesManager.tiles)
        {
            var wTile = kv.Value;
            int jsonRow = (d.height - 1) - wTile.LocalPlace.y;
            int idx = jsonRow * d.width + wTile.LocalPlace.x;

            if (idx < 0 || idx >= d.CellCount) continue;

            wTile.Cost = (d.costData != null && idx < d.costData.Count) ? d.costData[idx] : 1;
            wTile.ecoVal = (d.ecoData1 != null && idx < d.ecoData1.Count) ? d.ecoData1[idx] : 0;
            wTile.ecoVal2 = (d.ecoData2 != null && idx < d.ecoData2.Count) ? d.ecoData2[idx] : 0;
            wTile.ecoVal3 = (d.ecoData3 != null && idx < d.ecoData3.Count) ? d.ecoData3[idx] : 0;

            if (d.lockedData != null && idx < d.lockedData.Count && d.lockedData[idx] > 0)
            {
                wTile.Purchased = wTile.Locked = true;
                tilesManager.boughtTiles[wTile.LocalPlace] = wTile;
                tilesManager.score += wTile.ecoVal + wTile.ecoVal2 + wTile.ecoVal3;
            }

            string tileTypeStr = (d.tileTypes != null && idx < d.tileTypes.Count)
                ? d.tileTypes[idx].Trim().ToLower()
                : "forest";
            if (tileTypeStr == "habitat" || tileTypeStr == "road")
                wTile.Locked = true;
            else
                wTile.Locked = false;
        }
    }

    private void SpawnValueLabels(LevelDefinition d)
    {
        ClearValueLabels();
        bool hasDisplay = d.displayValues != null && d.displayValues.Count == d.CellCount;
        for (int y = 0; y < d.height; y++)
            for (int x = 0; x < d.width; x++)
            {
                int jsonRow = (d.height - 1) - y;
                int idx = jsonRow * d.width + x;
                if (idx < 0 || idx >= d.CellCount) continue;
                int val = hasDisplay ? d.displayValues[idx] : (idx < d.ecoData1.Count ? d.ecoData1[idx] : 0);
                var cell = new Vector3Int(x, y, 0);
                var world = tilemap.GetCellCenterWorld(cell);
                var label = Instantiate(valueLabelPrefab, labelsParent ? labelsParent : transform);
                label.transform.position = world;
                label.text = val.ToString();
                label.alignment = TextAlignmentOptions.Center;
                spawnedLabels.Add(label);
            }
    }

    private void ClearValueLabels()
    {
        for (int i = spawnedLabels.Count - 1; i >= 0; i--)
            if (spawnedLabels[i]) Destroy(spawnedLabels[i].gameObject);
        spawnedLabels.Clear();
    }

    private void ConfigureCameraForLevel(int levelNumber)
    {
        var cam = Camera.main;
        if (cam == null) return;

        switch (levelNumber)
        {
            case 1:
                cam.transform.position = new Vector3(46f, 23f, -37.3f);
                cam.orthographicSize = 15f;
                break;
            case 2:
                cam.transform.position = new Vector3(49f, 25.3f, -37.3f);
                cam.orthographicSize = 18f;
                break;
            case 3:
                cam.transform.position = new Vector3(52f, 27f, -37.3f);
                cam.orthographicSize = 22f;
                break;
            default:
                break;
        }
    }

    private void CreateSubmitPopup()
    {
        if (popupCanvas == null)
        {
            Debug.LogError("[GridManager] popupCanvas not assigned in Inspector. Submit popup will not work.");
            return;
        }

        // Root overlay — full screen dark tint
        _submitPopup = new GameObject("SubmitPopup_Overlay");
        _submitPopup.transform.SetParent(popupCanvas.transform, false);
        var overlayRect = _submitPopup.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImg = _submitPopup.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);

        // Board panel — slightly lower to give logo room above top edge
        var boardGO = new GameObject("Board");
        boardGO.transform.SetParent(_submitPopup.transform, false);
        var boardRect = boardGO.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(750f, 520f);
        boardRect.anchoredPosition = new Vector2(0f, -20f);
        var boardImg = boardGO.AddComponent<Image>();
        boardImg.sprite = popupBoardSprite;
        boardImg.type = Image.Type.Simple;
        boardImg.preserveAspect = true;

        // Logo — overlaps top edge of board, shifted right toward center
        var logoGO = new GameObject("Logo");
        logoGO.transform.SetParent(boardGO.transform, false);
        var logoRect = logoGO.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 1f);
        logoRect.sizeDelta = new Vector2(380f, 150f);
        logoRect.anchoredPosition = new Vector2(80f, 300f);
        var logoImg = logoGO.AddComponent<Image>();
        logoImg.sprite = popupLogoSprite;
        logoImg.preserveAspect = true;

        // Pawl running character — right side
        var pawlGO = new GameObject("Pawl");
        pawlGO.transform.SetParent(boardGO.transform, false);
        var pawlRect = pawlGO.AddComponent<RectTransform>();
        pawlRect.anchorMin = new Vector2(0.5f, 0.5f);
        pawlRect.anchorMax = new Vector2(0.5f, 0.5f);
        pawlRect.pivot = new Vector2(0.5f, 0.5f);
        pawlRect.sizeDelta = new Vector2(320f, 300f);
        pawlRect.anchoredPosition = new Vector2(185f, 0f);
        var pawlImg = pawlGO.AddComponent<Image>();
        pawlImg.sprite = popupPawlSprite;
        pawlImg.preserveAspect = true;

        // Butterfly decoration
        var butterflyGO = new GameObject("Butterfly");
        butterflyGO.transform.SetParent(boardGO.transform, false);
        var bfRect = butterflyGO.AddComponent<RectTransform>();
        bfRect.anchorMin = new Vector2(0.5f, 1f);
        bfRect.anchorMax = new Vector2(0.5f, 1f);
        bfRect.pivot = new Vector2(0.5f, 0.5f);
        bfRect.sizeDelta = new Vector2(70f, 70f);
        bfRect.anchoredPosition = new Vector2(330f, 130f);
        var bfImg = butterflyGO.AddComponent<Image>();
        bfImg.sprite = popupButterflySprite;
        bfImg.preserveAspect = true;

        // Close button — outside top-right corner of board
        var closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(boardGO.transform, false);
        var closeRect = closeGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(55f, 55f);
        closeRect.anchoredPosition = new Vector2(40f, 40f);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.sprite = popupCloseSprite;
        closeImg.preserveAspect = true;
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(() => _submitPopup.SetActive(false));

        // Results box — tan card matching Javier's design
        var resultsBoxGO = new GameObject("ResultsBox");
        resultsBoxGO.transform.SetParent(boardGO.transform, false);
        var rbRect = resultsBoxGO.AddComponent<RectTransform>();
        rbRect.anchorMin = new Vector2(0.5f, 0.5f);
        rbRect.anchorMax = new Vector2(0.5f, 0.5f);
        rbRect.pivot = new Vector2(0.5f, 0.5f);
        rbRect.sizeDelta = new Vector2(310f, 280f);
        rbRect.anchoredPosition = new Vector2(-180f, 10f);
        var rbImg = resultsBoxGO.AddComponent<Image>();
        rbImg.color = new Color(0.98f, 0.93f, 0.78f, 1f);

        // Results label — parented to ResultsBox
        var resultsLabelGO = new GameObject("ResultsLabel");
        resultsLabelGO.transform.SetParent(resultsBoxGO.transform, false);
        var rlRect = resultsLabelGO.AddComponent<RectTransform>();
        rlRect.anchorMin = new Vector2(0.5f, 0.5f);
        rlRect.anchorMax = new Vector2(0.5f, 0.5f);
        rlRect.pivot = new Vector2(0.5f, 0.5f);
        rlRect.sizeDelta = new Vector2(280f, 50f);
        rlRect.anchoredPosition = new Vector2(0f, 100f);
        var rlTmp = resultsLabelGO.AddComponent<TextMeshProUGUI>();
        rlTmp.text = "Results";
        rlTmp.fontSize = 32;
        rlTmp.fontStyle = FontStyles.Bold;
        rlTmp.color = new Color(0.25f, 0.12f, 0.05f, 1f);
        rlTmp.alignment = TextAlignmentOptions.Center;

        // Result text — parented to ResultsBox, stored for runtime update
        var resultGO = new GameObject("ResultText");
        resultGO.transform.SetParent(resultsBoxGO.transform, false);
        var rtRect = resultGO.AddComponent<RectTransform>();
        rtRect.anchorMin = new Vector2(0.5f, 0.5f);
        rtRect.anchorMax = new Vector2(0.5f, 0.5f);
        rtRect.pivot = new Vector2(0.5f, 0.5f);
        rtRect.sizeDelta = new Vector2(260f, 70f);
        rtRect.anchoredPosition = new Vector2(0f, 30f);
        _resultText = resultGO.AddComponent<TextMeshProUGUI>();
        _resultText.text = "";
        _resultText.fontSize = 20;
        _resultText.color = new Color(0.25f, 0.12f, 0.05f, 1f);
        _resultText.alignment = TextAlignmentOptions.Center;

        // Score text — parented to ResultsBox, stored for runtime update
        var scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(resultsBoxGO.transform, false);
        var stRect = scoreGO.AddComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0.5f, 0.5f);
        stRect.anchorMax = new Vector2(0.5f, 0.5f);
        stRect.pivot = new Vector2(0.5f, 0.5f);
        stRect.sizeDelta = new Vector2(260f, 90f);
        stRect.anchoredPosition = new Vector2(0f, -70f);
        _scoreText = scoreGO.AddComponent<TextMeshProUGUI>();
        _scoreText.text = "";
        _scoreText.fontSize = 52;
        _scoreText.fontStyle = FontStyles.Bold;
        _scoreText.color = new Color(0.25f, 0.12f, 0.05f, 1f);
        _scoreText.alignment = TextAlignmentOptions.Center;

        // Three buttons at bottom using Javier sprites
        CreatePopupButton(boardGO.transform, "BestCorridorBtn", popupBestCorridorSprite,
            new Vector2(-230f, -220f), new Vector2(215f, 55f), OnShowBestCorridorClicked);

        CreatePopupButton(boardGO.transform, "PlayAgainBtn", popupPlayAgainSprite,
            new Vector2(5f, -215f), new Vector2(215f, 65f), OnPlayAgainClicked);

        CreatePopupButton(boardGO.transform, "HallOfFameBtn", popupHallOfFameSprite,
            new Vector2(240f, -220f), new Vector2(225f, 55f), OnHallOfFameClicked);

        _submitPopup.SetActive(false);
    }

    private void CreatePopupButton(Transform parent, string name, Sprite sprite,
        Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.type = Image.Type.Simple;
        img.SetNativeSize();
        rect.sizeDelta = size;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
    }

    private void OnPlayAgainClicked()
    {
        if (_submitPopup != null) _submitPopup.SetActive(false);
        ReloadCurrent();
    }

    private void OnShowBestCorridorClicked()
    {
        if (_submitPopup != null) _submitPopup.SetActive(false);
        if (_currentDef == null || tilesManager == null) return;

        int correct = 0, wrong = 0, missed = 0;
        foreach (var kv in tilesManager.tiles)
        {
            var tile = kv.Value;
            Debug.Log($"[BestCorridor tile] pos=({tile.LocalPlace.x},{tile.LocalPlace.y}) Purchased={tile.Purchased}");
            int idx = tile.LocalPlace.y * _currentDef.width + tile.LocalPlace.x;
            bool isOptimal = _currentDef.optimalData != null
                             && idx >= 0 && idx < _currentDef.optimalData.Count
                             && _currentDef.optimalData[idx] == 1;
            bool isSelected = tile.Purchased;

            if (isOptimal || isSelected)
            {
                tile.TilemapMember.SetTileFlags(tile.LocalPlace, TileFlags.None);

                if (isOptimal && isSelected)
                {
                    tile.TilemapMember.SetColor(tile.LocalPlace, new Color(0.6f, 0f, 1f, 1f));
                    Debug.Log($"[Purple] tile at LocalPlace=({tile.LocalPlace.x},{tile.LocalPlace.y}) Purchased={tile.Purchased} isOptimal={isOptimal}");
                    correct++;
                }
                else if (isOptimal && !isSelected)
                {
                    tile.TilemapMember.SetColor(tile.LocalPlace, new Color(0f, 0.4f, 1f, 1f));
                    missed++;
                }
                else
                {
                    tile.TilemapMember.SetColor(tile.LocalPlace, new Color(1f, 0f, 0.15f, 1f));
                    wrong++;
                }
            }
            else
            {
                tile.TilemapMember.SetTileFlags(tile.LocalPlace, TileFlags.None);
                tile.TilemapMember.SetColor(tile.LocalPlace, Color.white);
            }
        }
        Debug.Log($"[BestCorridor] Correct: {correct}, Wrong: {wrong}, Missed: {missed}");
    }

    private void OnHallOfFameClicked()
    {
        if (_submitPopup != null) _submitPopup.SetActive(false);
        Debug.Log("Hall of Fame clicked — not yet implemented.");
    }

    private void OnResultsClicked()
    {
        if (_submitPopup != null) _submitPopup.SetActive(false);
        Debug.Log("Results clicked — not yet implemented.");
    }

    public void ShowBudgetWarning()
    {
        if (budgetWarningText == null) return;
        budgetWarningText.text = "Not enough patches remaining!";
        budgetWarningText.gameObject.SetActive(true);
        if (_budgetWarningCoroutine != null) StopCoroutine(_budgetWarningCoroutine);
        _budgetWarningCoroutine = StartCoroutine(HideBudgetWarning());
    }

    private IEnumerator HideBudgetWarning()
    {
        yield return new WaitForSeconds(2f);
        if (budgetWarningText != null)
            budgetWarningText.gameObject.SetActive(false);
    }
}

[System.Serializable]
public class TileTypeEntry
{
    public string typeName;
    public TileBase tile;
}