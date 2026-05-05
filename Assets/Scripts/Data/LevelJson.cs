using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelJson
{
    public int schemaVersion = 1;
    public int width;
    public int height;
    public int budget;

    // arrays (length = width * height)
    public List<string> tileTypes;
    public List<int> costData;

    // JSON now uses "utilities" → we map that into LevelDefinition.ecoData1 in the mapper
    public List<int> utilities;

    // optional (present in your JSON)
    public List<int> optimalData;

    // optional metadata (safe if missing in JSON)
    public int optUtil;
}
