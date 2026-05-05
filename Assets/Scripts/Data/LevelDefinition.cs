using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class LevelDefinition
{
    public int width = 8;
    public int height = 8;
    public int budget = 0;

    // Per cell arrays: size must be width * height
    public List<string> tileTypes = new List<string>();
    public List<int> costData = new List<int>();
    public List<int> ecoData1 = new List<int>();
    public List<int> ecoData2 = new List<int>();
    public List<int> ecoData3 = new List<int>();
    public List<int> lockedData = new List<int>();
    public List<int> displayValues = new List<int>();
    public List<int> optimalData = new List<int>();

    // Optional clusters and/or paths
    public List<Vector2Int> startCluster = new List<Vector2Int>();
    public List<Vector2Int> endCluster = new List<Vector2Int>();
    public List<Vector2Int> optimalPath = new List<Vector2Int>();

    public int CellCount => Mathf.Max(0, width * height);
    public int Idx(int x, int y) => y * width + x;

    /// Ensure all cell lists are sized to width * height and filled with defaults.
    public void EnsureSize(int w, int h, string defaultTileType = "", int defaultCost = 1)
    {
        width = Mathf.Max(1, w);
        height = Mathf.Max(1, h);
        int n = width * height;

        EnsureList(ref tileTypes, n, defaultTileType);
        EnsureList(ref costData, n, defaultCost);
        EnsureList(ref ecoData1, n, 0);
        EnsureList(ref ecoData2, n, 0);
        EnsureList(ref ecoData3, n, 0);
        EnsureList(ref lockedData, n, 0);
        EnsureList(ref displayValues, n, 0);
        EnsureList(ref optimalData, n, 0);
    }

    public string GetTileType(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return "";
        var i = Idx(x, y);
        return (i >= 0 && i < tileTypes.Count) ? tileTypes[i] : "";
    }

    public void SetTileType(int x, int y, string t)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        var i = Idx(x, y);
        if (i >= 0 && i < tileTypes.Count) tileTypes[i] = t;
    }

    /// Fill tileTypes via a rule (x,y,w,h) -> type.
    public void Fill(Func<int, int, int, int, string> rule)
    {
        EnsureSize(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tileTypes[Idx(x, y)] = rule(x, y, width, height);
    }

    // ---------- helpers ----------
    private static void EnsureList(ref List<int> list, int n, int fill)
    {
        if (list == null) list = new List<int>(n);
        if (list.Count > n) list.RemoveRange(n, list.Count - n);
        while (list.Count < n) list.Add(fill);
    }

    private static void EnsureList(ref List<string> list, int n, string fill)
    {
        if (list == null) list = new List<string>(n);
        if (list.Count > n) list.RemoveRange(n, list.Count - n);
        while (list.Count < n) list.Add(fill);
    }
}
