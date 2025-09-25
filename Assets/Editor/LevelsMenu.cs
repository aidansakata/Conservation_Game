#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class LevelsMenu
{
    [MenuItem("Tools/Levels/Open StreamingAssets/Levels")]
    public static void OpenLevelsFolder()
    {
        string path = Path.Combine(Application.dataPath, "StreamingAssets/Levels");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
        EditorUtility.RevealInFinder(path);
    }
}
#endif
