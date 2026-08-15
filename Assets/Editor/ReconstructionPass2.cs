// ReconstructionPass2.cs — corrections pass (reconstruction_2_plan.md, S-1..S-6).
// Editor-only. Authors scenes/prefabs; changes no runtime gameplay C#.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ReconPass2
{
    // ==================================================================== S-1a
    [MenuItem("Tools/Recon2/S-1a Fix Overlay Defaults")]
    public static void S1a_OverlayDefaults()
    {
        ReconBuild.ResetLog();
        const string path = "Assets/Prefabs/MenuOverlay.prefab";

        var root = PrefabUtility.LoadPrefabContents(path);
        var hmc = root.GetComponent<HamburgerMenuController>();
        var panelT = root.transform.Find("Panel");

        ReconBuild.Log("prefab root active           = " + root.activeSelf + "  (must stay TRUE: hosts the controller)");
        ReconBuild.Log("Panel found                  = " + (panelT != null));
        ReconBuild.Log("Panel active BEFORE          = " + (panelT != null && panelT.gameObject.activeSelf));

        if (panelT != null) panelT.gameObject.SetActive(false);

        // Re-point the controller at the Panel and confirm it survived.
        if (hmc != null && panelT != null)
        {
            var so = new SerializedObject(hmc);
            so.FindProperty("panel").objectReferenceValue = panelT.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            var chk = new SerializedObject(hmc).FindProperty("panel").objectReferenceValue;
            ReconBuild.Log("HamburgerMenuController.panel = " + (chk != null ? chk.name : "NULL <<<"));
        }
        ReconBuild.Log("Panel active AFTER           = " + (panelT != null && panelT.gameObject.activeSelf));

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ReconBuild.Log("SAVED " + path);
        ReconBuild.DumpLog("S-1a Overlay Defaults");
    }

    // ==================================================================== S-1b
    /// Instantiates PopupOverlay into a throwaway scene under a real Canvas so
    /// layout actually resolves, then measures every child. Prefab-isolation mode
    /// gives a degenerate Stage rect, which is why this cannot be judged statically.
    [MenuItem("Tools/Recon2/S-1b Diagnose Popup Close")]
    public static void S1b_DiagnosePopupClose()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PopupOverlay.prefab");
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var canvas = inst.GetComponent<Canvas>();
        var crt = inst.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(1920, 1080);   // simulate a 16:9 screen
        Canvas.ForceUpdateCanvases();

        var stage = inst.transform.Find("Stage") as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(stage);
        Canvas.ForceUpdateCanvases();

        var sc = new Vector3[4]; stage.GetWorldCorners(sc);
        ReconBuild.Log(string.Format("Stage rect size {0}  worldX {1:0.0}..{2:0.0}  worldY {3:0.0}..{4:0.0}",
            stage.rect.size, sc[0].x, sc[2].x, sc[0].y, sc[2].y));

        foreach (Transform t in stage)
        {
            var rt = t as RectTransform;
            var g = t.GetComponent<Graphic>();
            var img = t.GetComponent<Image>();
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            bool inside = c[0].x >= sc[0].x - 0.5f && c[2].x <= sc[2].x + 0.5f &&
                          c[0].y >= sc[0].y - 0.5f && c[2].y <= sc[2].y + 0.5f;
            string spr = img != null ? (img.sprite != null ? img.sprite.name : "NULL SPRITE") : "-";
            float a = g != null ? g.color.a : -1f;
            // Stage-local box, derived from anchors -- reliable even when the canvas
            // has no screen and GetWorldCorners degenerates.
            float sw = stage.rect.width, sh = stage.rect.height;
            float x0 = rt.anchorMin.x * sw, x1 = rt.anchorMax.x * sw;
            float y0 = rt.anchorMin.y * sh, y1 = rt.anchorMax.y * sh;
            bool box = x0 >= -1 && y0 >= -1 && x1 <= sw + 1 && y1 <= sh + 1;
            ReconBuild.Log(string.Format(
                "  [{0,2}] {1,-20} rect{2,-20} stageBox x {3,7:0}..{4,-7:0} y {5,7:0}..{6,-7:0} " +
                "active {7}/{8} imgEnabled {9} scale{10} alpha {11:0.00} sprite={12} {13}",
                t.GetSiblingIndex(), t.name, rt.rect.size.ToString(), x0, x1, y0, y1,
                t.gameObject.activeSelf, t.gameObject.activeInHierarchy,
                img != null ? img.enabled.ToString() : "-",
                t.localScale.ToString("0.##"), a, spr, box ? "" : "<<< OUTSIDE STAGE"));
        }

        ReconBuild.DumpLog("S-1b Popup Close Diagnosis");
    }

    // ============================================================ S-1b apply
    [MenuItem("Tools/Recon2/S-1b Fix Popup Close")]
    public static void S1b_FixPopupClose()
    {
        ReconBuild.ResetLog();
        const string path = "Assets/Prefabs/UI/PopupOverlay.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var stage = root.transform.Find("Stage");

        // Re-measured against _ArtDrop/Pop-Up Overlay/Pop-Up_Baked.jpg.
        var close = stage.Find("Close Button");
        if (close != null)
        {
            ReconBuild.AnchorPx(close.gameObject, 1817, 45, 67, 63);
            close.SetAsLastSibling();
            var im = close.GetComponent<Image>();
            im.color = Color.white;
            im.enabled = true;
            close.localScale = Vector3.one;
            ReconBuild.Log("Close Button: anchors re-applied (1817,45,67,63), forced last sibling, colour opaque, scale 1");
        }

        // Whole-prefab draw order: Board -> content -> buttons.
        string[] order = { "Board", "Logo", "Pawl Running", "Butterfly", "Results Heading",
                           "Results Caption", "Score Value", "Pts Label",
                           "View Best Corridor", "Play Again", "Add to Hall of Fame", "Close Button" };
        for (int i = 0; i < order.Length; i++)
        {
            var t = stage.Find(order[i]);
            if (t != null) t.SetSiblingIndex(i);
        }
        ReconBuild.Log("Draw order enforced: Scrim -> Stage[Board -> content -> buttons]");
        foreach (Transform t in stage) ReconBuild.Log("   " + t.GetSiblingIndex() + ": " + t.name);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();
        ReconBuild.Log("SAVED " + path);
        ReconBuild.DumpLog("S-1b Fix Popup Close");
    }
}
#endif
