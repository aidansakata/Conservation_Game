// NavigationPass4.cs — pass 4 navigation wiring (reconstruction_4_plan.md, N-1..N-9).
// Editor-only tooling. Performs inspector/asset work; the only runtime C# touched this
// pass is SceneLoader / MainMenuController / HamburgerMenuController, edited directly.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class NavigationPass4
{
    static Button Find(Transform stage, string name)
    {
        var t = ReconBuild.FindDeep(stage, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static Transform Stage() => ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");

    /// Replaces every persistent listener on a button, then records the change as a
    /// prefab-instance override -- without this the override is dropped on save, which
    /// is exactly how pass 2's hamburger wiring was lost.
    static void Wire(Button b, UnityAction call, string desc)
    {
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddPersistentListener(b.onClick, call);
        Persist(b);
        ReconBuild.Log("   WIRE " + b.name + " -> " + desc);
    }

    static void WireInt(Button b, UnityAction<int> call, int arg, string desc)
    {
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddIntPersistentListener(b.onClick, call, arg);
        Persist(b);
        ReconBuild.Log("   WIRE " + b.name + " -> " + desc + "(" + arg + ")");
    }

    static void Persist(Button b)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(b))
            PrefabUtility.RecordPrefabInstancePropertyModifications(b);
        EditorUtility.SetDirty(b);
    }

    // =================================================================== N-2 / N-4 / N-6
    [MenuItem("Tools/Nav4/N-2,4,6 Wire Scene Buttons")]
    public static void WireScenes()
    {
        ReconBuild.ResetLog();

        // ---- N-2: HowToPlay "Next" -> Patches (and set the breadcrumb) ----
        EditorSceneManager.OpenScene("Assets/Scenes/HowToPlay.unity");
        ReconBuild.Log("--- HowToPlay (N-2)");
        var mm = Object.FindObjectOfType<MainMenuController>(true);
        if (mm == null)
        {
            // HowToPlay was authored fresh in pass 1 and has no controller object. Reuse the
            // existing MainMenuController rather than introducing a new script (constraint 9).
            var go = new GameObject("HowToPlayUI");
            mm = go.AddComponent<MainMenuController>();
            ReconBuild.Log("   CREATED 'HowToPlayUI' hosting the existing MainMenuController (no new script)");
        }
        var st = Stage();
        var next = Find(st, "Next Button");
        if (next != null) Wire(next, new UnityAction(mm.GoToPatchesFromHowToPlay),
                               "MainMenuController.GoToPatchesFromHowToPlay  [sets PatchesReturnScene=HowToPlay]");
        else ReconBuild.Log("   !! Next Button not found");
        var htPlay = Find(st, "Play Button");
        if (htPlay != null) Wire(htPlay, new UnityAction(mm.GoToLevelSelect),
                                 "MainMenuController.GoToLevelSelect  [loads LevelSelect]");
        else ReconBuild.Log("   !! Play Button not found");
        ReconBuild.SaveActive();

        // ---- N-4: Patches "Play" -> LevelSelect ----
        EditorSceneManager.OpenScene("Assets/Scenes/Patches.unity");
        ReconBuild.Log("--- Patches (N-4)");
        st = Stage();
        var ui = ReconBuild.Find("LevelSelectUI");
        var lsc = ui.GetComponent<LevelSelectController>();
        var play = Find(st, "Play");
        if (play != null) Wire(play, new UnityAction(lsc.BackToLevelSelect),
                               "LevelSelectController.BackToLevelSelect  [loads LevelSelect]");
        var nextP = Find(st, "Next");
        ReconBuild.Log("   N-5 Patches 'Next' left UNWIRED by instruction (persistent calls: "
                       + (nextP != null ? nextP.onClick.GetPersistentEventCount().ToString() : "n/a") + ")");
        ReconBuild.SaveActive();

        // ---- N-6: LevelSelect 1-10 -> SelectLevel(n) ----
        EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity");
        ReconBuild.Log("--- LevelSelect (N-6)");
        st = Stage();
        var lui = ReconBuild.Find("LevelSelectUI").GetComponent<LevelSelectController>();
        for (int n = 1; n <= 10; n++)
        {
            var b = Find(st, "Level " + n);
            if (b == null) { ReconBuild.Log("   !! Level " + n + " not found"); continue; }
            WireInt(b, new UnityAction<int>(lui.SelectLevel), n, "LevelSelectController.SelectLevel");
        }
        ReconBuild.SaveActive();

        AssetDatabase.SaveAssets();
        ReconBuild.DumpLog("N-2/N-4/N-6 wiring");
    }

    // =================================================================== N-7
    [MenuItem("Tools/Nav4/N-7 Hamburger Home")]
    public static void N7_Home()
    {
        ReconBuild.ResetLog();
        const string P = "Assets/Prefabs/MenuOverlay.prefab";
        var root = PrefabUtility.LoadPrefabContents(P);
        var hmc = root.GetComponent<HamburgerMenuController>();
        var stage = ReconBuild.FindDeep(root.transform, "Stage");

        var item = stage.Find("Menu 01");
        if (item == null) { ReconBuild.Log("!! 'Menu 01' not found"); PrefabUtility.UnloadPrefabContents(root); return; }

        var lbl = item.GetComponentInChildren<TextMeshProUGUI>(true);
        ReconBuild.Log("relabel '" + lbl.text + "' -> 'Home'");
        lbl.text = "Home";
        item.gameObject.name = "Home";

        var btn = item.GetComponent<Button>();
        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(btn.onClick, i);
        UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(hmc.GoHome));
        ReconBuild.Log("wired in the PREFAB ASSET -> HamburgerMenuController.GoHome (all instances inherit)");

        for (int i = 2; i <= 4; i++)
        {
            var t = stage.Find("Menu 0" + i);
            if (t == null) continue;
            var b2 = t.GetComponent<Button>();
            ReconBuild.Log("   Menu 0" + i + " left unwired/unlabelled (persistent calls: "
                           + b2.onClick.GetPersistentEventCount() + ")");
        }

        PrefabUtility.SaveAsPrefabAsset(root, P);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        ReconBuild.Log("SAVED " + P);
        ReconBuild.DumpLog("N-7 Hamburger Home");
    }

    // =================================================================== N-8
    [MenuItem("Tools/Nav4/N-8 Report Outline State")]
    public static void N8_Report()
    {
        ReconBuild.ResetLog();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TypographyPass3.MatValueLabel);
        ReconBuild.Log("material: " + TypographyPass3.MatValueLabel);
        ReconBuild.Log("   shader = " + mat.shader.name);
        foreach (var p in new[] { "_OutlineWidth", "_OutlineColor", "_FaceColor" })
        {
            if (!mat.HasProperty(p)) { ReconBuild.Log("   " + p + " : property ABSENT on this shader"); continue; }
            if (p == "_OutlineWidth") ReconBuild.Log("   _OutlineWidth = " + mat.GetFloat(p));
            else ReconBuild.Log("   " + p + " = " + mat.GetColor(p));
        }
        var vl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ValueLabel.prefab");
        var tmp = vl.GetComponent<TextMeshPro>();
        ReconBuild.Log("ValueLabel.prefab font=" + (tmp.font != null ? tmp.font.name : "NULL")
                       + "  sharedMaterial=" + (tmp.fontSharedMaterial != null ? tmp.fontSharedMaterial.name : "NULL"));
        ReconBuild.DumpLog("N-8 outline state BEFORE");
    }

    [MenuItem("Tools/Nav4/N-8 Bake Outline")]
    public static void N8_Bake()
    {
        ReconBuild.ResetLog();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TypographyPass3.MatValueLabel);
        // Baked at 0.3 to match exactly what GridManager already writes at runtime, so the
        // authored state and the running state are identical and nothing changes visually.
        // I did not tune away from 0.3: judging legibility against the city/farmland tiles
        // needs a correct Game-Interface render, and grid registration is still blocked from
        // pass 2 (S-5), so any other value would be an unverified change to shipped behaviour.
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat("_OutlineWidth", 0.3f);
        mat.SetColor("_OutlineColor", Color.black);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        ReconBuild.Log("baked _OutlineWidth=0.3  _OutlineColor=black  keyword OUTLINE_ON");
        ReconBuild.Log("   GridManager's runtime write (0.3) still happens and now only re-sets this same asset");
        ReconBuild.DumpLog("N-8 bake");
    }
}
#endif
