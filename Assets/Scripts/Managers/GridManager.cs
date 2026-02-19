using UnityEngine;
using UnityEngine.Tilemaps;
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
    [Tooltip("Index 0 = blank/clear, 1 = first terrain tile, etc.")]
    [SerializeField] private List<Tile> typeToTile;

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

    void Awake()
    {
        instance = this;
        tilesManager = GameTiles.instance ?? FindObjectOfType<GameTiles>();
        if (tilesManager != null && tilesManager.Tilemap == null && tilemap != null)
            tilesManager.Tilemap = tilemap;
    }

    void Start()
    {
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
        LoadLevel(currentLevelNumber);
    }

    public void LoadLevel(int levelNumber)
    {
        _isLoading = true;
        currentLevelNumber = Mathf.Clamp(levelNumber, 1, 5);
        GameState.SelectedLevel = currentLevelNumber;

        // --- NEW VARIATION LOGIC START ---
        // If we are loading Level 1, we want to pick a random "landscape_X"
        if (currentLevelNumber == 1)
        {
            var cfg = Resources.Load<ApiConfig>("ApiConfig");

            // 1. Get Base URL (default to 127.0.0.1)
            string baseUrl = (cfg != null) ? cfg.baseApiUrl.Trim().TrimEnd('/') : "http://127.0.0.1:4000";

            // 2. Pick Random Variation
            int variation = Random.Range(1, 101);

            // 3. Construct ID WITHOUT Extension
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
            return; // Exit here so we don't run the standard loader below
        }
        // --- NEW VARIATION LOGIC END ---

        // Standard JSON load
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

    // --- NEW HELPER: Centralized JSON Processing ---
    private void ProcessLoadedJson(string json)
    {
        var lj = JsonUtility.FromJson<LevelJson>(json);
        var d = LevelDefinitionMapper.FromJson(lj);

        // --- MANUAL DATA RECOVERY ---
        // If EcoData is 0/Empty (due to serialization bugs), parse it manually from the text.
        if (d.ecoData1 == null || d.ecoData1.Count == 0 || (d.ecoData1.Count > 0 && d.ecoData1[1] == 0))
        {
            Debug.LogWarning("EcoData1 appears empty/zero. Attempting Manual Parse...");
            d.ecoData1 = ManualParseList(json, "\"ecoData1\"");
            Debug.Log($"Manual Parse Result: {d.ecoData1.Count} items found. Index 1 value: {(d.ecoData1.Count > 1 ? d.ecoData1[1] : -1)}");
        }
        // ----------------------------

        PrepareDefinition(d, currentLevelNumber);
        LoadComplete(d, currentLevelNumber);
    }

    // Parses a JSON integer array by regex to bypass JsonUtility case-sensitivity
    private List<int> ManualParseList(string json, string fieldName)
    {
        List<int> result = new List<int>();
        // Find "fieldName": [ ... ]
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
        // 1) Try runtime provider first.
        LevelDefinition candidate = null;
        try
        {
            candidate = GridLayouts.GetLayout(levelNumber);
        }
        catch { /* provider not present -> ignore */ }

        // 2) Fallback to nested inspector assignments.
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

        // Ensure arrays sized; if empty, populate with a distinct demo pattern.
        PrepareDefinition(candidate, levelNumber);
        LoadComplete(candidate, levelNumber);

        Debug.Log($"[GridManager] Painted {candidate.width}x{candidate.height} (level {levelNumber}).");
    }

    // --- HELPER METHODS ---
    public void ReloadCurrent() => LoadLevel(currentLevelNumber);
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

    // --- CENTRAL LOADING POINT ---
    private void LoadComplete(LevelDefinition d, int levelNumber)
    {
        def = d;

        // DEBUG: Verify Data Integrity
        int ecoCount = (def.ecoData1 != null) ? def.ecoData1.Count : 0;

        Debug.Log($"[LoadComplete] Level Data Loaded. EcoData1 Count: {ecoCount}, Budget (Tile Limit): {d.budget}");

        if (ecoCount == 0) Debug.LogError("CRITICAL: EcoData1 is EMPTY. Tiles will have 0 Value.");

        // 1. Initialize Unity Visuals
        PaintTiles(def);
        ApplyToWorldTiles(def);
        if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
        else ClearValueLabels();
        if (levelNumber != -1) ConfigureCameraForLevel(levelNumber);

        // 2. Initialize Logic Engine
        InitializeLogicGrid(def);

        _isLoading = false;
    }

    // --- BRIDGE: Unity Data -> Logic Grid ---
    private void InitializeLogicGrid(LevelDefinition d)
    {
        // Create the logic object
        logicGrid = new HexagonGrid(d.width);

        // Loop through data and populate the Logic Hexes
        for (int y = 0; y < d.height; y++)
        {
            for (int x = 0; x < d.width; x++)
            {
                // CONVERSION: Unity (0,0) is Bottom-Left. JSON (0,0) is Top-Left.
                int jsonRow = (d.height - 1) - y;
                int idx = jsonRow * d.width + x;

                if (idx < 0 || idx >= d.CellCount) continue;

                // Get the logic hex
                var hex = logicGrid.GetHexByCoords(x, y);

                if (hex != null)
                {
                    // Populate Hex data
                    hex.Optimal = (idx < d.optimalData.Count) ? d.optimalData[idx] : 0;
                    hex.Utility = (idx < d.ecoData1.Count) ? d.ecoData1[idx] : 0;

                    // Identify Type string for logic checks (habitat vs others)
                    int tId = (idx < d.tileTypes.Count) ? d.tileTypes[idx] : 0;

                    if (tId == 0) hex.Type = "habitat";
                    else hex.Type = "terrain";
                }
            }
        }
        Debug.Log("Logic Grid Initialized.");
    }

    // --- BRIDGE: Button Functions ---

    public void OnHintClicked()
    {
        if (logicGrid == null || tilesManager == null) return;

        List<(int col, int row)> purchased = new List<(int, int)>();

        foreach (var kv in tilesManager.boughtTiles)
        {
            Vector3Int pos = kv.Key;
            purchased.Add((pos.x, pos.y));
        }

        try
        {
            var hintTuple = logicGrid.GetHint(purchased); // Returns (col, row)
            Vector3Int hintPos = new Vector3Int(hintTuple.col, hintTuple.row, 0);

            Debug.Log($"Hint suggested at: {hintPos}");

            if (tilesManager.tiles.TryGetValue(hintPos, out WorldTile tile))
            {
                if (valueLabelPrefab != null)
                {
                    var label = Instantiate(valueLabelPrefab, labelsParent ? labelsParent : transform);
                    label.transform.position = tile.TilemapMember.GetCellCenterWorld(tile.LocalPlace);
                    label.text = "HINT";
                    label.color = Color.yellow;
                    label.fontSize = 6;
                    Destroy(label.gameObject, 3f);
                }
                tile.TilemapMember.SetColor(tile.LocalPlace, Color.yellow);
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("No hint available (or grid error): " + e.Message);
        }
    }

    public void OnSubmitClicked()
    {
        if (logicGrid == null || tilesManager == null) return;

        List<(int col, int row)> purchased = new List<(int, int)>();
        foreach (var kv in tilesManager.boughtTiles)
        {
            Vector3Int pos = kv.Key;
            purchased.Add((pos.x, pos.y));
        }

        var (isConnected, visited) = logicGrid.isValidCorridor(purchased);

        if (isConnected)
        {
            Debug.Log($"WIN! Corridor Connected. Score: {tilesManager.score}");
        }
        else
        {
            Debug.Log("FAIL. Path not connected.");
        }
    }

    // ---------- internal ----------

    private void PrepareDefinition(LevelDefinition d, int levelNumber)
    {
        if (d.width <= 0 || d.height <= 0)
        {
            if (levelNumber == 1) { d.width = 8; d.height = 8; }
            if (levelNumber == 2) { d.width = 10; d.height = 10; }
            if (levelNumber == 3) { d.width = 12; d.height = 12; }
        }

        d.EnsureSize(d.width, d.height, defaultTileType: 1, defaultCost: 1);

        bool looksEmpty = true;
        int n = d.CellCount;
        for (int i = 0; i < n; i++) { if (d.tileTypes[i] != 0 && d.tileTypes[i] != 1) { looksEmpty = false; break; } }

        if (looksEmpty)
        {
            switch (levelNumber)
            {
                case 1: d.Fill((x, y, w, h) => ((x + y) % 2 == 0) ? 1 : 2); break;
                case 2: d.Fill((x, y, w, h) => (x == w / 2) ? 2 : 1); break;
                case 3: d.Fill((x, y, w, h) => (x == y || x == (w - 1 - y)) ? 3 : 1); break;
                default: d.Fill((x, y, w, h) => 1); break;
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
                int jsonRow = (d.height - 1) - y;
                int idx = jsonRow * d.width + x;
                int tId = (idx >= 0 && idx < d.tileTypes.Count ? d.tileTypes[idx] : 0);
                int visualIdx = y * d.width + x;

                if (tId < 0 || tId >= typeToTile.Count) tileArray[visualIdx] = null;
                else tileArray[visualIdx] = typeToTile[tId];
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
        if (tilesManager == null)
        {
            Debug.LogError("GridManager: No GameTiles instance present in scene.");
            return;
        }

        if (tilesManager.Tilemap == null && tilemap != null)
            tilesManager.Tilemap = tilemap;

        tilesManager.RebuildFromTilemap(tilemap ?? tilesManager.Tilemap);

        // --- UPDATED LOGIC ---
        // 1. Budget = Number of tiles allowed (from JSON 'budget')
        tilesManager.budget = d.budget;
        tilesManager.score = 0;
        tilesManager.boughtTiles.Clear();

        Debug.Log($"[ApplyToWorldTiles] Budget set to {tilesManager.budget} tiles.");

        foreach (var kv in tilesManager.tiles)
        {
            var wTile = kv.Value;
            int jsonRow = (d.height - 1) - wTile.LocalPlace.y;
            int idx = jsonRow * d.width + wTile.LocalPlace.x;

            if (idx < 0 || idx >= d.CellCount) continue;

            // 2. Cost = 1 (Each tile costs 1 "unit" of budget)
            wTile.Cost = 1;

            // 3. Eco Value = Data from JSON (Score gained when bought)
            wTile.ecoVal = (d.ecoData1 != null && idx < d.ecoData1.Count) ? d.ecoData1[idx] : 0;
            wTile.ecoVal2 = (d.ecoData2 != null && idx < d.ecoData2.Count) ? d.ecoData2[idx] : 0;
            wTile.ecoVal3 = (d.ecoData3 != null && idx < d.ecoData3.Count) ? d.ecoData3[idx] : 0;

            if (d.lockedData != null && idx < d.lockedData.Count && d.lockedData[idx] > 0)
            {
                wTile.Purchased = wTile.Locked = true;
                tilesManager.boughtTiles[wTile.LocalPlace] = wTile;
                tilesManager.score += wTile.ecoVal + wTile.ecoVal2 + wTile.ecoVal3;
            }
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
}