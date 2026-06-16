using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// Loads the server level catalog once and buckets entries by level number.
// Composite ids have the form level-<N>_landscape_<n>; the real ids are sparse
// and overlap across levels, so we never assume contiguous or 1..100 numbering.
public static class CatalogService
{
    private static readonly Regex IdPattern = new Regex(@"^level-(\d+)_landscape_(\d+)$");

    // level number -> list of composite ids belonging to that level
    private static readonly Dictionary<int, List<string>> _byLevel = new Dictionary<int, List<string>>();

    private static bool _loaded;

    public static bool IsLoaded => _loaded;
    public static int MinLevel { get; private set; }
    public static int MaxLevel { get; private set; }

    // Load-once / cached. Runs as a coroutine on the calling MonoBehaviour.
    public static IEnumerator EnsureLoaded(string baseApiUrl, Action onReady, Action<string> onError)
    {
        if (_loaded)
        {
            onReady?.Invoke();
            yield break;
        }

        string error = null;

        yield return LevelJsonLoader.LoadCatalog(baseApiUrl,
            (json) =>
            {
                try { Parse(json); }
                catch (Exception ex) { error = ex.Message; }
            },
            (err) => { error = err; });

        if (_loaded && error == null)
            onReady?.Invoke();
        else
            onError?.Invoke(error ?? "Catalog load failed");
    }

    private static void Parse(string json)
    {
        var catalog = JsonUtility.FromJson<LevelCatalog>(json);
        if (catalog == null || catalog.levels == null)
            throw new Exception("Catalog JSON had no 'levels' array");

        _byLevel.Clear();

        foreach (var entry in catalog.levels)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
            if (!TryParseLevel(entry.id, out int level)) continue;

            if (!_byLevel.TryGetValue(level, out var list))
            {
                list = new List<string>();
                _byLevel[level] = list;
            }
            list.Add(entry.id);
        }

        if (_byLevel.Count == 0)
            throw new Exception("Catalog parsed but contained no valid level-<N>_landscape_<n> ids");

        int min = int.MaxValue, max = int.MinValue;
        foreach (var level in _byLevel.Keys)
        {
            if (level < min) min = level;
            if (level > max) max = level;
        }
        MinLevel = min;
        MaxLevel = max;
        _loaded = true;

        Debug.Log($"[CatalogService] Loaded catalog: {_byLevel.Count} levels (Min={MinLevel}, Max={MaxLevel}).");
    }

    // Extracts the level number N from a composite id "level-N_landscape_n".
    public static bool TryParseLevel(string id, out int level)
    {
        level = 0;
        if (string.IsNullOrEmpty(id)) return false;
        var m = IdPattern.Match(id);
        if (!m.Success) return false;
        return int.TryParse(m.Groups[1].Value, out level);
    }

    // Returns a random composite id within the given level, or null (with a warning)
    // if the catalog has no entries for that level.
    public static string PickRandomLandscapeId(int level)
    {
        if (_byLevel.TryGetValue(level, out var list) && list.Count > 0)
            return list[UnityEngine.Random.Range(0, list.Count)];

        Debug.LogWarning($"[CatalogService] No landscapes found for level {level}.");
        return null;
    }
}
