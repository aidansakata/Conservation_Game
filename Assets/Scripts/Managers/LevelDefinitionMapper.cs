using System.Collections.Generic;
using UnityEngine;

public static class LevelDefinitionMapper
{
    public static LevelDefinition FromJson(LevelJson j)
    {
        if (j.schemaVersion != 1)
            Debug.LogWarning($"[LevelDefinitionMapper] Unknown schemaVersion {j.schemaVersion}; proceeding with best-effort mapping.");

        var def = new LevelDefinition();
        def.width = j.width;
        def.height = j.height;
        def.budget = j.budget;

        def.EnsureSize(def.width, def.height, defaultTileType: 0, defaultCost: 1);

        void CopyList(List<int> src, List<int> dst, string name)
        {
            if (src == null) return;
            if (src.Count != dst.Count)
                Debug.LogWarning($"[LevelDefinitionMapper] {name} length {src.Count} != expected {dst.Count}");
            int n = Mathf.Min(src.Count, dst.Count);
            for (int i = 0; i < n; i++) dst[i] = src[i];
        }

        CopyList(j.tileTypes, def.tileTypes, nameof(j.tileTypes));
        CopyList(j.costData, def.costData, nameof(j.costData));
        CopyList(j.ecoData1, def.ecoData1, nameof(j.ecoData1));
        if (j.ecoData2 != null) CopyList(j.ecoData2, def.ecoData2, nameof(j.ecoData2));
        if (j.ecoData3 != null) CopyList(j.ecoData3, def.ecoData3, nameof(j.ecoData3));
        if (j.lockedData != null) CopyList(j.lockedData, def.lockedData, nameof(j.lockedData));
        if (j.displayValues != null) CopyList(j.displayValues, def.displayValues, nameof(j.displayValues));
        if (j.optimalData != null) CopyList(j.optimalData, def.optimalData, nameof(j.optimalData));

        // Optional vector lists (only if LevelJson defines them)
        def.startCluster = j.startCluster ?? def.startCluster;
        def.endCluster = j.endCluster ?? def.endCluster;
        def.optimalPath = j.optimalPath ?? def.optimalPath;

        return def;
    }
}
