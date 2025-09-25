#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class LevelPreflight
{
    static LevelPreflight()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;

        // Best effort: try to read selected level number from GameState if available.
        int levelNumber = 1;
        try
        {
            var t = typeof(GameState);
            var f = t.GetField("SelectedLevel");
            if (f != null) levelNumber = (int)f.GetValue(null);
        }
        catch { /* ignore and use default 1 */ }

        string path = Path.Combine(Application.dataPath, "StreamingAssets/Levels", $"level-{levelNumber}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Preflight] Missing JSON for level {levelNumber} at {path}. If you expect JSON loading, ensure CI sync ran or copy a sample file locally.");
        }
    }
}
#endif
