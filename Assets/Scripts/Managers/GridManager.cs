using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using TMPro; // for value labels

public class GridManager : MonoBehaviour
{
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

    void Awake()
    {
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
                        var j = JsonUtility.FromJson<LevelJson>(json);
                        var defLocal = LevelDefinitionMapper.FromJson(j);
                        PrepareDefinition(defLocal, -1);
                        def = defLocal;

                        PaintTiles(def);
                        ApplyToWorldTiles(def);

                        if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
                        else ClearValueLabels();
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
            string baseUrl = (cfg != null) ? cfg.baseApiUrl : "http://127.0.0.1:4000";

            // CLEANUP: Trim whitespace and trailing slash, but DO NOT add "/levels/"
            // LevelJsonLoader.cs line 97 adds "/levels/" automatically.
            baseUrl = baseUrl.Trim().TrimEnd('/');

            // 2. Pick Random Variation (Restored!)
            int variation = Random.Range(1, 101);

            // 3. Construct ID WITHOUT Extension
            // LevelJsonLoader.cs adds ".json" automatically.
            string levelId = $"landscape_{variation}";

            Debug.Log($"[GridManager] Requesting variation: {levelId} from {baseUrl}");

            StartCoroutine(LevelJsonLoader.LoadLevelJsonById(baseUrl, levelId,
                (json) =>
                {
                    try
                    {
                        var lj = JsonUtility.FromJson<LevelJson>(json);
                        var d = LevelDefinitionMapper.FromJson(lj);
                        PrepareDefinition(d, currentLevelNumber);
                        def = d;

                        PaintTiles(def);
                        ApplyToWorldTiles(def);

                        if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
                        else ClearValueLabels();

                        ConfigureCameraForLevel(currentLevelNumber);
                        _isLoading = false;
                        Debug.Log($"[GridManager] Painted {levelId} ({def.width}x{def.height}).");
                    }
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

        // Try JSON-first for other levels (standard behavior)
        StartCoroutine(LevelJsonLoader.LoadLevelJson(
            currentLevelNumber,
            onLoaded: (json) =>
            {
                try
                {
                    var lj = JsonUtility.FromJson<LevelJson>(json);
                    var d = LevelDefinitionMapper.FromJson(lj);
                    PrepareDefinition(d, currentLevelNumber);
                    def = d;

                    PaintTiles(def);
                    ApplyToWorldTiles(def);

                    if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
                    else ClearValueLabels();

                    ConfigureCameraForLevel(currentLevelNumber);
                    _isLoading = false;
                    Debug.Log($"[GridManager] Painted (from JSON) {def.width}x{def.height} (level {currentLevelNumber}).");
                }
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
        def = candidate;

        PaintTiles(def);
        ApplyToWorldTiles(def);

        if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
        else ClearValueLabels();

        ConfigureCameraForLevel(levelNumber);

        _isLoading = false;

        Debug.Log($"[GridManager] Painted {def.width}x{def.height} (level {levelNumber}).");
    }

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
            try
            {
                var j = JsonUtility.FromJson<LevelJson>(json);
                var defLocal = LevelDefinitionMapper.FromJson(j);
                PrepareDefinition(defLocal, -1);
                def = defLocal;

                PaintTiles(def);
                ApplyToWorldTiles(def);

                if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
                else ClearValueLabels();

                _isLoading = false;
            }
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

    // ---------- internal ----------

    private void PrepareDefinition(LevelDefinition d, int levelNumber)
    {
        // If width/height are zero, set reasonable defaults.
        if (d.width <= 0 || d.height <= 0)
        {
            if (levelNumber == 1) { d.width = 8; d.height = 8; }
            if (levelNumber == 2) { d.width = 10; d.height = 10; }
            if (levelNumber == 3) { d.width = 12; d.height = 12; }
        }

        d.EnsureSize(d.width, d.height, defaultTileType: 1, defaultCost: 1);

        // If tileTypes look empty (all zeros/ones from Ensure), stamp a distinct pattern
        // so each level *visibly* differs without hand-editing.
        bool looksEmpty = true;
        int n = d.CellCount;
        for (int i = 0; i < n; i++) { if (d.tileTypes[i] != 0 && d.tileTypes[i] != 1) { looksEmpty = false; break; } }

        if (looksEmpty)
        {
            switch (levelNumber)
            {
                case 1: // checker: 1 vs 2
                    d.Fill((x, y, w, h) => ((x + y) % 2 == 0) ? 1 : 2);
                    break;
                case 2: // vertical river stripe: 2 through middle, 1 elsewhere
                    d.Fill((x, y, w, h) => (x == w / 2) ? 2 : 1);
                    break;
                case 3: // road diagonals: 3; background 1
                    d.Fill((x, y, w, h) => (x == y || x == (w - 1 - y)) ? 3 : 1);
                    break;
                default:
                    d.Fill((x, y, w, h) => 1);
                    break;
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
                int idx = d.Idx(x, y);
                int tId = (idx < d.tileTypes.Count ? d.tileTypes[idx] : 0);
                if (tId <= 0 || tId >= typeToTile.Count)
                {
                    tileArray[idx] = null; // empty
                }
                else
                {
                    tileArray[idx] = typeToTile[tId];
                }
            }

        var region = new BoundsInt(0, 0, 0, d.width, d.height, 1);
        tilemap.ClearAllTiles();
        tilemap.SetTilesBlock(region, tileArray);
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

        tilesManager.budget = d.budget;
        tilesManager.score = 0;
        tilesManager.boughtTiles.Clear();

        foreach (var kv in tilesManager.tiles)
        {
            var wTile = kv.Value;
            int idx = wTile.LocalPlace.y * d.width + wTile.LocalPlace.x;

            if (idx < 0 || idx >= d.CellCount) continue;

            wTile.Cost = (idx < d.costData.Count ? d.costData[idx] : 1);
            wTile.ecoVal = (idx < d.ecoData1.Count ? d.ecoData1[idx] : 0);
            wTile.ecoVal2 = (idx < d.ecoData2.Count ? d.ecoData2[idx] : 0);
            wTile.ecoVal3 = (idx < d.ecoData3.Count ? d.ecoData3[idx] : 0);

            if (idx < d.lockedData.Count && d.lockedData[idx] > 0)
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
                int idx = d.Idx(x, y);
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