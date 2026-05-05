#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class EnsureScenesInBuild
{
    [MenuItem("Tools/Scenes/Ensure MainMenu, LevelSelect, Game-Interface in Build")]
    public static void Ensure()
    {
        var wanted = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/LevelSelect.unity", "Assets/Scenes/Game-Interface.unity" };
        var current = EditorBuildSettings.scenes.Select(s => s.path).ToList();
        bool changed = false;
        foreach (var path in wanted)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"Scene not found on disk: {path}");
                continue;
            }
            if (!current.Contains(path))
            {
                current.Add(path);
                changed = true;
                Debug.Log($"Added to Build Settings: {path}");
            }
        }
        if (changed)
        {
            EditorBuildSettings.scenes = current.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            Debug.Log("Build Settings updated.");
        }
        else
        {
            Debug.Log("Build Settings already contain the required scenes.");
        }
    }
}
#endif


