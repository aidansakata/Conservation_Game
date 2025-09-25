# Level JSON Schema (derived from LevelDefinition)

```
{
  "width": number,
  "height": number,
  "budget": number,
  "tileTypes": number[],      // length = width * height
  "costData": number[],       // length = width * height
  "ecoData1": number[],       // length = width * height
  "ecoData2": number[],       // optional
  "ecoData3": number[],       // optional
  "lockedData": number[],     // 0/1 flags
  "displayValues": number[],  // optional
  "optimalData": number[],    // optional per-cell flags
  "startCluster": { "x": number, "y": number }[], // optional
  "endCluster":   { "x": number, "y": number }[], // optional
  "optimalPath":  { "x": number, "y": number }[]  // optional
}
```

Sample file: `level-1.json` (see next file)
