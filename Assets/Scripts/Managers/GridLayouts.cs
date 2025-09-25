using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides per‑level definitions at runtime.
/// Level 1 is 8×8 (top‑left copies a 7×7 prototype block).
/// Other levels currently stub to Level 1.
/// </summary>
public static class GridLayouts
{
    public static LevelDefinition GetLayout(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1: return CreateLevel1();
            case 2: return CreateLevel2();
            case 3: return CreateLevel3();
            case 4: return CreateLevel4();
            case 5: return CreateLevel5();
            default:
                Debug.LogError($"GridLayouts: no layout for level {levelNumber}");
                return null;
        }
    }

    private static LevelDefinition CreateLevel1()
    {
        int w = 8, h = 8;
        var def = new LevelDefinition
        {
            width = w,
            height = h,
            budget = 25000,
            tileTypes = new List<int>(new int[w * h]),
            costData = new List<int>(new int[w * h]),
            ecoData1 = new List<int>(new int[w * h]),
            ecoData2 = new List<int>(new int[w * h]),
            ecoData3 = new List<int>(new int[w * h]),
            lockedData = new List<int>(new int[w * h]),
            displayValues = new List<int>(new int[w * h])
        };

        // Your 7×7 prototype arrays:
        int[,] cost7 = {
            {0,8,8,7,7,6,6},
            {0,0,7,6,6,6,5},
            {0,4,5,6,5,4,4},
            {2,2,3,4,4,4,0},
            {2,3,1,2,2,0,0},
            {2,2,0,0,6,5,0},
            {0,0,0,3,4,5,7},
        };
        int[,] eco7 = {
            {0,52,51,58,54,57,81},
            {0, 0,52,55,53,58,52},
            {0,44,60,56,59,54,51},
            {55,58,60,55,58,43, 0},
            {57,59,46,60,56, 0, 0},
            {51,56, 0, 0,59,42, 0},
            {0, 0, 0,49,59,55,94},
        };
        int[,] lock7 = {
            {1,0,0,0,0,0,0},
            {1,1,0,0,0,0,0},
            {1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1},
            {0,0,0,0,0,1,1},
            {0,0,1,1,0,0,1},
            {1,1,1,0,0,0,0},
        };

        // Copy the 7×7 block into the top‑left corner of the 8×8
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
            {
                int idx = y * w + x;
                def.tileTypes[idx] = 1;                         // e.g. 1 = forest
                def.costData[idx] = cost7[y, x] * 1000;
                def.ecoData1[idx] = eco7[y, x];
                def.lockedData[idx] = lock7[y, x];
                def.displayValues[idx] = eco7[y, x];
            }

        // leave the rest at zero and/or unlocked

        return def;
    }

    // Implement distinct sizes for levels 2 and 3; leave 4 and 5 as stubs for now.
    private static LevelDefinition CreateLevel2()
    {
        int w = 10, h = 10;
        var def = new LevelDefinition
        {
            width = w,
            height = h,
            budget = 25000,
            tileTypes = new List<int>(new int[w * h]),
            costData = new List<int>(new int[w * h]),
            ecoData1 = new List<int>(new int[w * h]),
            ecoData2 = new List<int>(new int[w * h]),
            ecoData3 = new List<int>(new int[w * h]),
            lockedData = new List<int>(new int[w * h]),
            displayValues = new List<int>(new int[w * h])
        };

        return def;
    }

    private static LevelDefinition CreateLevel3()
    {
        int w = 12, h = 12;
        var def = new LevelDefinition
        {
            width = w,
            height = h,
            budget = 25000,
            tileTypes = new List<int>(new int[w * h]),
            costData = new List<int>(new int[w * h]),
            ecoData1 = new List<int>(new int[w * h]),
            ecoData2 = new List<int>(new int[w * h]),
            ecoData3 = new List<int>(new int[w * h]),
            lockedData = new List<int>(new int[w * h]),
            displayValues = new List<int>(new int[w * h])
        };

        return def;
    }

    private static LevelDefinition CreateLevel4() => CreateLevel1();
    private static LevelDefinition CreateLevel5() => CreateLevel1();
}
