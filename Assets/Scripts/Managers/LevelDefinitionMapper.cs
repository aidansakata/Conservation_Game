using System.Collections.Generic;
using UnityEngine;

public static class LevelDefinitionMapper
{
    public static LevelDefinition FromJson(LevelJson j)
    {
        if (j.schemaVersion != 1)
            Debug.LogWarning($"[LevelDefinitionMapper] Unknown schemaVersion {j.schemaVersion}; proceeding with best-effort mapping.");

        var d = new LevelDefinition();
        d.width = j.width;
        d.height = j.height;
        d.budget = j.budget;

        d.EnsureSize(d.width, d.height, defaultTileType: "", defaultCost: 1);

        void CopyList(List<int> src, List<int> dst, string name)
        {
            // Ignore missing OR empty lists to avoid spammy length warnings
            if (src == null || src.Count == 0) return;

            if (src.Count != dst.Count)
                Debug.LogWarning($"[LevelDefinitionMapper] {name} length {src.Count} != expected {dst.Count}");

            int n = Mathf.Min(src.Count, dst.Count);
            for (int i = 0; i < n; i++) dst[i] = src[i];
        }

        void CopyStringList(List<string> src, List<string> dst, string name)
        {
            if (src == null || src.Count == 0) return;
            if (src.Count != dst.Count)
                Debug.LogWarning($"[LevelDefinitionMapper] {name} length {src.Count} != expected {dst.Count}");
            int n = Mathf.Min(src.Count, dst.Count);
            for (int i = 0; i < n; i++) dst[i] = src[i];
        }

        // required
        CopyStringList(j.tileTypes, d.tileTypes, nameof(j.tileTypes));
        CopyList(j.costData, d.costData, nameof(j.costData));

        // map JSON "utilities" -> LevelDefinition.ecoData1
        CopyList(j.utilities, d.ecoData1, nameof(j.utilities));

        // optional flags (present in your JSON)
        CopyList(j.optimalData, d.optimalData, nameof(j.optimalData));

        // optional scalar
        d.optUtil = j.optUtil;

        // DO NOT copy ecoData2/3/locked/displayValues anymore

        return d;
    }
}
