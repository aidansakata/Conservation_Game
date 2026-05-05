#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class PopulateDemoLevelsOnGridManager
{
    [MenuItem("Tools/Levels/Populate Demo Data On Selected GridManager")]
    public static void Populate()
    {
        var go = Selection.activeGameObject;
        if (!go) { Debug.LogError("Select a GridManager GameObject in Grid_Scene."); return; }

        var gm = go.GetComponent<GridManager>();
        if (!gm) { Debug.LogError("Selected object has no GridManager component."); return; }

        // reflect into private fields (they are [SerializeField])
        var t = typeof(GridManager);

        void Fill(string fieldName, int w, int h, System.Func<int, int, int, int, string> rule)
        {
            var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (f == null) { Debug.LogWarning($"Field not found: {fieldName}"); return; }
            var def = f.GetValue(gm) as LevelDefinition ?? new LevelDefinition();
            def.width = w; def.height = h;
            def.EnsureSize(w, h, defaultTileType: "forest", defaultCost: 1);
            def.Fill(rule);
            f.SetValue(gm, def);
        }

        Fill("level1Easy8x8", 8, 8, (x, y, w, h) => ((x + y) % 2 == 0) ? "forest" : "grassland");
        Fill("level2Med10x10", 10, 10, (x, y, w, h) => (x == w / 2) ? "grassland" : "forest");
        Fill("level3Hard12x12", 12, 12, (x, y, w, h) => (x == y || x == (w - 1 - y)) ? "farmland" : "forest");

        EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        Debug.Log("Demo data populated on GridManager�s nested LevelDefinitions.");
    }
}
#endif
