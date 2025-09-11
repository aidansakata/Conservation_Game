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

    void Awake()
    {
        tilesManager = GameTiles.instance ?? FindObjectOfType<GameTiles>();
        if (tilesManager != null && tilesManager.Tilemap == null && tilemap != null)
            tilesManager.Tilemap = tilemap;
    }

    void Start()
    {
        currentLevelNumber = Mathf.Clamp(GameState.SelectedLevel, 1, 5);
        LoadLevel(currentLevelNumber);
    }

    public void LoadLevel(int levelNumber)
    {
        currentLevelNumber = Mathf.Clamp(levelNumber, 1, 5);
        GameState.SelectedLevel = currentLevelNumber;

        // 1) Try runtime provider first (if present in your project).
        LevelDefinition candidate = null;
        try
        {
            // If you have a GridLayouts provider in your project, use it.
            candidate = GridLayouts.GetLayout(currentLevelNumber);
        }
        catch { /* provider not present → ignore */ }

        // 2) Fallback to nested inspector assignments.
        if (candidate == null)
        {
            candidate = currentLevelNumber switch
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
            Debug.LogError($"GridManager: No LevelDefinition available for level {currentLevelNumber}");
            return;
        }

        // Ensure arrays sized; if empty, populate with a distinct demo pattern.
        PrepareDefinition(candidate, currentLevelNumber);
        def = candidate;

        PaintTiles(def);
        ApplyToWorldTiles(def);

        if (showCellValues && valueLabelPrefab != null) SpawnValueLabels(def);
        else ClearValueLabels();

        ConfigureCameraForLevel(currentLevelNumber);

        Debug.Log($"[GridManager] Painted {def.width}x{def.height} (level {currentLevelNumber}).");
    }

    public void ReloadCurrent() => LoadLevel(currentLevelNumber);
    public void NextLevel() => LoadLevel(Mathf.Clamp(currentLevelNumber + 1, 1, 5));

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
