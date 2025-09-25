using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelJson
{
    public int schemaVersion = 1;
    public int width;
    public int height;
    public int budget;
    public List<int> tileTypes;
    public List<int> costData;
    public List<int> ecoData1;
    public List<int> ecoData2;
    public List<int> ecoData3;
    public List<int> lockedData;
    public List<int> displayValues;
    public List<int> optimalData;
    public List<Vector2Int> startCluster;
    public List<Vector2Int> endCluster;
    public List<Vector2Int> optimalPath;
}
