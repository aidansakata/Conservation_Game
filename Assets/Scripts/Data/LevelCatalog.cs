using System;
using System.Collections.Generic;

// Mirrors the server catalog at {baseApiUrl}/levels/catalog.json.
// Top-level shape is a wrapped object { "levels": [ ... ] }, which JsonUtility
// can deserialize directly (no bare-array wrapper workaround needed).
[Serializable]
public class LevelCatalog
{
    public List<CatalogEntry> levels;
}

[Serializable]
public class CatalogEntry
{
    public string id;     // composite id, e.g. "level-3_landscape_7"
    public int width;
    public int height;
    public int budget;
    public string path;   // server-relative path to the per-level json
}
