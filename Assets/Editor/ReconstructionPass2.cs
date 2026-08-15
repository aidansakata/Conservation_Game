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

    // ==================================================================== S-3
    [MenuItem("Tools/Recon2/S-3 SinglePatch Title + Hexagon")]
    public static void S3_SinglePatch()
    {
        ReconBuild.ResetLog();
        const string CLEAN = "Assets/Art/Reconstruction/SinglePatch/title-board-clean.png";
        ReconBuild.ApplySpriteImport(CLEAN);
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene("Assets/Scenes/Single Patch.unity");
        var stage = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");

        // S-3a: point the sign at the cleaned copy. Original file left untouched.
        var tb = stage.Find("Title Board").GetComponent<Image>();
        var before = tb.sprite != null ? tb.sprite.name : "none";
        tb.sprite = ReconBuild.Sprite(CLEAN);
        ReconBuild.Log("Title Board sprite: " + before + " -> " + (tb.sprite != null ? tb.sprite.name : "NULL <<<"));

        // S-3b: shadow box re-derived from the ALPHA channel (max alpha 146, never
        // opaque, so colour matching was impossible). Sprite alpha-centroid (247.0,54.7)
        // aligned to the hexagon's horizontal centre and base.
        var shadow = stage.Find("Patch Shadow").gameObject;
        ReconBuild.AnchorPx(shadow, 116, 755, 472, 119);
        ReconBuild.Log("Patch Shadow: (126,751,472,119) -> (116,755,472,119)");

        // Hexagon extent re-confirmed by two-axis edge detection on the baked ref.
        var patch = stage.Find("Patch Image").gameObject;
        ReconBuild.AnchorPx(patch, 128, 288, 469, 522);
        ReconBuild.Log("Patch Image: (128,288,469,522) confirmed (scale 2.057 x / 2.016 y of native 228x259)");

        // Confirm the dynamic name is centred over the cleaned region.
        var nameT = stage.Find("Tile Name").GetComponent<RectTransform>();
        float cx = (nameT.anchorMin.x + nameT.anchorMax.x) * 0.5f * 1920f;
        float cy = (1f - (nameT.anchorMin.y + nameT.anchorMax.y) * 0.5f) * 1080f;
        // cleaned patch rect in sprite space x105..292 y88..146; sign placed at (163,0)
        ReconBuild.Log(string.Format(
            "Tile Name centre = ({0:0.0},{1:0.0}); cleaned region centre = ({2:0.0},{3:0.0}) -> offset ({4:0.0},{5:0.0})",
            cx, cy, 163 + (105 + 292) / 2f, (88 + 146) / 2f,
            cx - (163 + (105 + 292) / 2f), cy - (88 + 146) / 2f));

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("S-3 SinglePatch");
    }

    // ==================================================================== S-4
    [MenuItem("Tools/Recon2/S-4 Hamburger in HowToPlay + LevelSelect")]
    public static void S4_Hamburger()
    {
        ReconBuild.ResetLog();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MenuOverlay.prefab");

        var jobs = new (string scene, string btn)[] {
            ("Assets/Scenes/HowToPlay.unity",   "Hamburger Button"),
            ("Assets/Scenes/LevelSelect.unity", "Hamburger-menu"),
        };

        foreach (var (scenePath, btnName) in jobs)
        {
            EditorSceneManager.OpenScene(scenePath);
            ReconBuild.Log("--- " + scenePath);

            var existing = Object.FindObjectOfType<HamburgerMenuController>(true);
            if (existing == null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = "MenuOverlay";
                inst.transform.SetParent(null);            // scene root, matching Patches / Game-Interface
                ReconBuild.Log("   INSTANCED MenuOverlay at scene root");
            }
            else ReconBuild.Log("   MenuOverlay already present");

            var hmc = Object.FindObjectOfType<HamburgerMenuController>(true);
            var panel = new SerializedObject(hmc).FindProperty("panel").objectReferenceValue;
            ReconBuild.Log("   panel -> " + (panel != null ? panel.name : "NULL <<<") +
                           "   panel active = " + (panel != null && ((GameObject)panel).activeSelf));

            var stage = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
            var btnT = stage.Find(btnName);
            if (btnT == null) { ReconBuild.Log("   !! button '" + btnName + "' not found"); continue; }
            ReconBuild.Wire(btnT.gameObject, new UnityAction(hmc.Toggle), "HamburgerMenuController.Toggle");

            ReconBuild.Verify();
            ReconBuild.SaveActive();
        }
        ReconBuild.DumpLog("S-4 Hamburger");
    }

    // ==================================================================== S-5
    const float VP_X = 490f, VP_Y = 98f, VP_W = 963f, VP_H = 889f;   // measured playfield

    [MenuItem("Tools/Recon2/S-5 Setup Grid Registration")]
    public static void S5_Setup()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");

        var canvasGo = ReconBuild.Find("Canvas");
        var canvas = canvasGo.GetComponent<Canvas>();
        var stage = ReconBuild.FindDeep(canvasGo.transform, "Stage");
        ReconBuild.Log("Canvas renderMode=" + canvas.renderMode + " worldCamera=" +
                       (canvas.worldCamera != null ? canvas.worldCamera.name : "none") + " (untouched)");

        // ---- S-5a: GridViewport marker over the board playfield ----
        var vpGo = ReconBuild.Child(stage, "GridViewport");
        ReconBuild.AnchorPx(vpGo, VP_X, VP_Y, VP_W, VP_H);
        foreach (var g in vpGo.GetComponents<Graphic>()) Object.DestroyImmediate(g);
        var vpRt = vpGo.GetComponent<RectTransform>();
        ReconBuild.Log(string.Format("GridViewport px({0},{1},{2},{3})  anchors min({4:0.0000},{5:0.0000}) max({6:0.0000},{7:0.0000})  no Graphic",
            VP_X, VP_Y, VP_W, VP_H, vpRt.anchorMin.x, vpRt.anchorMin.y, vpRt.anchorMax.x, vpRt.anchorMax.y));

        // ---- S-5c: LetterboxCamera behind everything ----
        var mainCam = Camera.main != null ? Camera.main : ReconBuild.Find("Main Camera").GetComponent<Camera>();
        var lbGo = ReconBuild.Find("LetterboxCamera");
        if (lbGo == null) lbGo = new GameObject("LetterboxCamera");
        var lb = lbGo.GetComponent<Camera>(); if (lb == null) lb = lbGo.AddComponent<Camera>();
        lb.orthographic = true;
        lb.rect = new Rect(0, 0, 1, 1);
        lb.clearFlags = CameraClearFlags.SolidColor;
        lb.backgroundColor = new Color(26f / 255f, 26f / 255f, 26f / 255f, 1f);
        lb.cullingMask = 0;                       // Nothing
        lb.depth = mainCam.depth - 1f;            // strictly lower than Main Camera
        var al = lbGo.GetComponent<AudioListener>(); if (al != null) Object.DestroyImmediate(al);
        lbGo.transform.position = new Vector3(mainCam.transform.position.x, mainCam.transform.position.y, mainCam.transform.position.z);
        ReconBuild.Log(string.Format("LetterboxCamera rect(0,0,1,1) clear=SolidColor #1A1A1A cullingMask=Nothing depth={0} (Main Camera depth={1})",
            lb.depth, mainCam.depth));

        // ---- S-5b: the fitter, on its own object so Main Camera's GO is untouched ----
        var fitGo = ReconBuild.Find("GridViewportFitter");
        if (fitGo == null) fitGo = new GameObject("GridViewportFitter");
        var fit = fitGo.GetComponent<GridViewportFitter>();
        if (fit == null) fit = fitGo.AddComponent<GridViewportFitter>();

        var tilemap = Object.FindObjectOfType<UnityEngine.Tilemaps.Tilemap>(true);
        var so = new SerializedObject(fit);
        so.FindProperty("targetCamera").objectReferenceValue = mainCam;
        so.FindProperty("viewportRect").objectReferenceValue = vpRt;
        so.FindProperty("canvasRect").objectReferenceValue = canvasGo.GetComponent<RectTransform>();
        so.FindProperty("tilemap").objectReferenceValue = tilemap;
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach (var f in new[] { "targetCamera", "viewportRect", "canvasRect", "tilemap" })
        {
            var v = new SerializedObject(fit).FindProperty(f).objectReferenceValue;
            ReconBuild.Log("   fitter." + f + " = " + (v != null ? v.name : "NULL <<<"));
        }

        ReconBuild.Log("Main Camera BEFORE: rect=" + mainCam.rect + " orthoSize=" + mainCam.orthographicSize +
                       " pos=" + mainCam.transform.position + " clearFlags=" + mainCam.clearFlags +
                       " cullingMask=" + mainCam.cullingMask);

        ReconBuild.SaveActive();
        ReconBuild.DumpLog("S-5 Setup");
    }

    [MenuItem("Tools/Recon2/S-5d Clear Authored Tilemap")]
    public static void S5d_ClearTilemap()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");
        var tm = Object.FindObjectOfType<UnityEngine.Tilemaps.Tilemap>(true);
        if (tm == null) { ReconBuild.Log("no Tilemap found"); ReconBuild.DumpLog("S-5d"); return; }
        tm.CompressBounds();
        ReconBuild.Log("authored cellBounds BEFORE: " + tm.cellBounds + "  tiles=" + CountTiles(tm));
        tm.ClearAllTiles();
        tm.CompressBounds();
        ReconBuild.Log("authored cellBounds AFTER : " + tm.cellBounds + "  tiles=" + CountTiles(tm));
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("S-5d Clear Tilemap");
    }

    static int CountTiles(UnityEngine.Tilemaps.Tilemap tm)
    {
        int n = 0; var b = tm.cellBounds;
        foreach (var p in b.allPositionsWithin) if (tm.GetTile(p) != null) n++;
        return n;
    }

    /// Renders the scene to a RenderTexture at a given size so camera.rect is honoured,
    /// then writes a PNG. The Scene view uses its own camera and would not show this.
    [MenuItem("Tools/Recon2/S-5e Capture Aspects")]
    public static void S5e_Capture()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");
        string dir = System.Environment.GetEnvironmentVariable("TEMP") + "/recon_shots";
        System.IO.Directory.CreateDirectory(dir);

        var cams = Object.FindObjectsOfType<Camera>(true);
        System.Array.Sort(cams, (a, b) => a.depth.CompareTo(b.depth));

        var sizes = new (string name, int w, int h)[] {
            ("16x9",  1600, 900), ("16x10", 1600, 1000),
            ("4x3",   1200, 900), ("ultrawide", 2560, 1080),
        };

        foreach (var (name, w, h) in sizes)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            foreach (var c in cams) { c.targetTexture = rt; }
            // let [ExecuteAlways] re-fit for this target size
            foreach (var f in Object.FindObjectsOfType<GridViewportFitter>(true)) f.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            foreach (var c in cams) if (c.enabled && c.gameObject.activeInHierarchy) c.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            var mc = Camera.main;
            ReconBuild.Log(string.Format("{0,-10} {1}x{2}  MainCam rect={3} orthoSize={4:0.000} pos={5}",
                name, w, h, mc.rect, mc.orthographicSize, mc.transform.position));

            foreach (var c in cams) c.targetTexture = null;
            rt.Release(); Object.DestroyImmediate(rt);
        }
        ReconBuild.Log("PNGs written to " + dir);
        ReconBuild.DumpLog("S-5e Capture");
    }

    /// Real screen capture. Works in Play mode (and in Edit mode for the Game view),
    /// unlike the RenderTexture route, which does not resize a Screen Space - Camera canvas.
    [MenuItem("Tools/Recon2/S-5e Screenshot Now")]
    public static void S5e_Shot()
    {
        string dir = System.Environment.GetEnvironmentVariable("TEMP") + "/recon_shots";
        System.IO.Directory.CreateDirectory(dir);
        string tag = Application.isPlaying ? "play" : "edit";
        string path = dir + "/real_" + tag + "_" + Screen.width + "x" + Screen.height + ".png";
        ScreenCapture.CaptureScreenshot(path);
        var mc = Camera.main;
        Debug.Log(string.Format("[Recon] SHOT {0}  Screen={1}x{2}  MainCam rect={3} orthoSize={4:0.000} pos={5} " +
                                "clearFlags={6} cullingMask={7}",
            path, Screen.width, Screen.height, mc.rect, mc.orthographicSize, mc.transform.position,
            mc.clearFlags, mc.cullingMask));
    }

    /// Reports what the fitter computes for a hypothetical screen size, and where the
    /// board playfield lands in the same normalized space -- a numeric registration test
    /// that does not depend on rendering.
    [MenuItem("Tools/Recon2/S-5e Numeric Aspect Check")]
    public static void S5e_Numeric()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");
        var canvasRt = ReconBuild.Find("Canvas").GetComponent<RectTransform>();
        var stage = ReconBuild.FindDeep(canvasRt.transform, "Stage") as RectTransform;
        var vp = ReconBuild.FindDeep(canvasRt.transform, "GridViewport") as RectTransform;

        ReconBuild.Log("Simulating Stage's AspectRatioFitter (FitInParent 1.7778) by hand:");
        ReconBuild.Log("screen        stageRect(px)          GridViewport normalized rect");
        foreach (var (name, w, h) in new (string, float, float)[] {
            ("16:9", 1920, 1080), ("16:10", 1920, 1200), ("4:3", 1440, 1080), ("21:9 ultra", 2560, 1080) })
        {
            // CanvasScaler ScaleWithScreenSize, match 0.5 -> canvas size in reference units
            float logW = 1920f, logH = 1080f;
            float scale = Mathf.Pow(w / logW, 0.5f) * Mathf.Pow(h / logH, 0.5f);
            float cw = w / scale, chh = h / scale;
            // Stage fits 1.7778 inside canvas
            float sw, sh;
            if (cw / chh > 1.7778f) { sh = chh; sw = chh * 1.7778f; }
            else { sw = cw; sh = cw / 1.7778f; }
            // GridViewport is a fixed fraction of Stage
            float u0 = (cw - sw) * 0.5f + VP_X / 1920f * sw;
            float u1 = (cw - sw) * 0.5f + (VP_X + VP_W) / 1920f * sw;
            float v1 = (chh - sh) * 0.5f + (1f - VP_Y / 1080f) * sh;
            float v0 = (chh - sh) * 0.5f + (1f - (VP_Y + VP_H) / 1080f) * sh;
            ReconBuild.Log(string.Format("{0,-11} canvas {1,6:0}x{2,-6:0} stage {3,6:0}x{4,-6:0}  rect=({5:0.0000},{6:0.0000},{7:0.0000},{8:0.0000})",
                name, cw, chh, sw, sh, u0 / cw, v0 / chh, (u1 - u0) / cw, (v1 - v0) / chh));
        }
        ReconBuild.DumpLog("S-5e Numeric Aspect Check");
    }

    /// Decisive, cheap check: for a Screen Space - Camera canvas Unity renders the UI into
    /// the camera's pixelRect. If canvas.pixelRect follows the driven sub-rect, the whole UI
    /// collapses into the board region and the approach cannot target that camera.
    [MenuItem("Tools/Recon2/S-5 Canvas Coupling Check")]
    public static void S5_CanvasCoupling()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");
        var canvas = ReconBuild.Find("Canvas").GetComponent<Canvas>();
        var mc = Camera.main;
        var saved = mc.rect;
        mc.rect = new Rect(0.2552f, 0.0861f, 0.5016f, 0.8231f);   // force the driven sub-rect
        Canvas.ForceUpdateCanvases();
        ReconBuild.Log("Screen                = " + Screen.width + "x" + Screen.height);
        ReconBuild.Log("Main Camera.rect      = " + mc.rect);
        ReconBuild.Log("Main Camera.pixelRect = " + mc.pixelRect);
        ReconBuild.Log("Canvas.renderMode     = " + canvas.renderMode + "  worldCamera=" +
                       (canvas.worldCamera != null ? canvas.worldCamera.name : "none"));
        ReconBuild.Log("Canvas.pixelRect      = " + canvas.pixelRect);
        var crt = canvas.GetComponent<RectTransform>();
        ReconBuild.Log("Canvas RectTransform  = " + crt.rect.size);
        bool collapsed = Mathf.Abs(canvas.pixelRect.width - mc.pixelRect.width) < 2f &&
                         Mathf.Abs(canvas.pixelRect.width - Screen.width) > 2f;
        ReconBuild.Log(collapsed
            ? ">>> UI COLLAPSED: canvas follows the camera sub-rect. Cannot drive this camera."
            : ">>> UI INDEPENDENT: canvas still spans the full screen.");
        mc.rect = saved;
        Canvas.ForceUpdateCanvases();
        ReconBuild.DumpLog("S-5 Canvas Coupling");
    }

    /// Leaves S-5 in a safe, fully-wired state. The fitter is authored and referenced but
    /// DISABLED, because driving Main Camera's rect collapses the UI (measured: canvas.pixelRect
    /// == camera.pixelRect). Enabling it requires a change this pass is not allowed to make.
    [MenuItem("Tools/Recon2/S-5 Finalize Safe State")]
    public static void S5_Finalize()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");

        var mc = Camera.main;
        mc.rect = new Rect(0, 0, 1, 1);
        mc.orthographicSize = 28f;
        mc.transform.position = new Vector3(48f, 27f, -37.3f);
        ReconBuild.Log("Main Camera restored to authored state: rect=" + mc.rect +
                       " orthoSize=" + mc.orthographicSize + " pos=" + mc.transform.position);
        ReconBuild.Log("   clearFlags=" + mc.clearFlags + " cullingMask=" + mc.cullingMask + " (never modified)");

        var fit = Object.FindObjectOfType<GridViewportFitter>(true);
        if (fit != null)
        {
            fit.enabled = false;
            ReconBuild.Log("GridViewportFitter present and fully wired, but DISABLED (see report).");
            foreach (var f in new[] { "targetCamera", "viewportRect", "canvasRect", "tilemap" })
            {
                var v = new SerializedObject(fit).FindProperty(f).objectReferenceValue;
                ReconBuild.Log("   " + f + " = " + (v != null ? v.name : "NULL <<<"));
            }
        }

        var lb = ReconBuild.Find("LetterboxCamera");
        if (lb != null)
        {
            var c = lb.GetComponent<Camera>();
            ReconBuild.Log("LetterboxCamera depth=" + c.depth + " rect=" + c.rect +
                           " clear=" + c.clearFlags + " mask=" + c.cullingMask +
                           " (Main Camera depth=" + mc.depth + ")");
        }

        var vp = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "GridViewport") as RectTransform;
        if (vp != null)
            ReconBuild.Log("GridViewport anchors min(" + vp.anchorMin.x.ToString("0.0000") + "," + vp.anchorMin.y.ToString("0.0000") +
                           ") max(" + vp.anchorMax.x.ToString("0.0000") + "," + vp.anchorMax.y.ToString("0.0000") +
                           ") offsets " + vp.offsetMin + vp.offsetMax);

        // S-5d: safe -- PaintTiles() clears and repaints from level data before
        // RebuildFromTilemap() reads the tilemap (GridManager 356-357, 529-530, 544).
        var tm = Object.FindObjectOfType<UnityEngine.Tilemaps.Tilemap>(true);
        if (tm != null)
        {
            tm.CompressBounds();
            int before = CountTiles(tm);
            tm.ClearAllTiles();
            tm.CompressBounds();
            ReconBuild.Log("S-5d authored tiles cleared: " + before + " -> " + CountTiles(tm) +
                           "   cellBounds now " + tm.cellBounds);
        }

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("S-5 Finalize");
    }

    // ============================================ hamburger wiring repair
    /// HowToPlay and LevelSelect had NO persisted m_OnClick call on the hamburger.
    /// Patches / Game-Interface do, because their MenuOverlay instance already existed
    /// when the button was wired. Wiring a nested prefab instance to a component on a
    /// brand-new prefab instance needs RecordPrefabInstancePropertyModifications, or the
    /// override is dropped when the scene is written.
    [MenuItem("Tools/Recon2/FIX Hamburger Wiring")]
    public static void FixHamburgerWiring()
    {
        ReconBuild.ResetLog();
        var jobs = new (string scene, string btn)[] {
            ("Assets/Scenes/HowToPlay.unity",   "Hamburger Button"),
            ("Assets/Scenes/LevelSelect.unity", "Hamburger-menu"),
        };
        foreach (var (scenePath, btnName) in jobs)
        {
            var sc = EditorSceneManager.OpenScene(scenePath);
            ReconBuild.Log("--- " + scenePath);

            var hmc = Object.FindObjectOfType<HamburgerMenuController>(true);
            if (hmc == null) { ReconBuild.Log("   !! no HamburgerMenuController"); continue; }

            var stage = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
            var btnT = stage.Find(btnName);
            if (btnT == null) { ReconBuild.Log("   !! button '" + btnName + "' missing"); continue; }
            var btn = btnT.GetComponent<Button>();

            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(hmc.Toggle));

            // The step that was missing: register the change as a prefab-instance override.
            if (PrefabUtility.IsPartOfPrefabInstance(btn))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(btn);
                ReconBuild.Log("   RecordPrefabInstancePropertyModifications(Button) applied");
            }
            EditorUtility.SetDirty(btn);
            EditorSceneManager.MarkSceneDirty(sc);

            int n = btn.onClick.GetPersistentEventCount();
            for (int i = 0; i < n; i++)
            {
                var t = btn.onClick.GetPersistentTarget(i);
                ReconBuild.Log(string.Format("   in-memory call[{0}] -> {1}.{2}()  targetIsSceneObject={3}",
                    i, t != null ? t.GetType().Name : "NULL", btn.onClick.GetPersistentMethodName(i),
                    t != null && !EditorUtility.IsPersistent(t)));
            }
            EditorSceneManager.SaveScene(sc);
            ReconBuild.Log("   SAVED " + scenePath);
        }
        AssetDatabase.SaveAssets();
        ReconBuild.DumpLog("FIX Hamburger Wiring");
    }

    /// Play-mode test for the ACTIVE scene: raycasts at the icon through the real
    /// GraphicRaycaster, dispatches a pointer click, and reads the panel state.
    [MenuItem("Tools/Recon2/PLAYTEST Hamburger (active scene)")]
    public static void PlaytestHamburger()
    {
        ReconBuild.ResetLog();
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        ReconBuild.Log("SCENE: " + scene + "   isPlaying=" + Application.isPlaying);

        var hmc = Object.FindObjectOfType<HamburgerMenuController>(true);
        var panelProp = hmc != null ? new SerializedObject(hmc).FindProperty("panel").objectReferenceValue : null;
        var panel = panelProp as GameObject;
        ReconBuild.Log("  controller=" + (hmc != null ? "present" : "MISSING") +
                       "  panel=" + (panel != null ? panel.name : "NULL"));
        ReconBuild.Log("  1) ON LOAD panel.activeSelf = " + (panel != null && panel.activeSelf) + "   (want False)");

        var stage = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
        Button icon = null;
        foreach (var b in stage.GetComponentsInChildren<Button>(true))
            if (b.name.ToLower().Contains("hamburger")) { icon = b; break; }
        if (icon == null) { ReconBuild.Log("  !! hamburger button not found"); ReconBuild.DumpLog("PLAYTEST " + scene); return; }

        int n = icon.onClick.GetPersistentEventCount();
        for (int i = 0; i < n; i++)
        {
            var t = icon.onClick.GetPersistentTarget(i);
            ReconBuild.Log(string.Format("  wiring: call[{0}] -> {1}.{2}()  isPersistentAsset={3}",
                i, t != null ? t.GetType().Name : "NULL", icon.onClick.GetPersistentMethodName(i),
                t != null && EditorUtility.IsPersistent(t)));
        }
        if (n == 0) ReconBuild.Log("  wiring: NO PERSISTENT CALLS <<<");

        // Real raycast through the GraphicRaycaster at the icon's screen centre.
        var rt = icon.GetComponent<RectTransform>();
        var cam = ReconBuild.Find("Canvas").GetComponent<Canvas>().worldCamera;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var ped = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = screenPt };
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(ped, hits);
            ReconBuild.Log("  raycast at " + screenPt + " -> " + hits.Count + " hits; top = " +
                           (hits.Count > 0 ? hits[0].gameObject.name : "none"));
            bool reaches = hits.Count > 0 && (hits[0].gameObject == icon.gameObject || hits[0].gameObject.transform.IsChildOf(icon.transform));
            ReconBuild.Log("  2) icon is topmost hit = " + reaches + "   (want True)");
            if (hits.Count > 0)
                UnityEngine.EventSystems.ExecuteEvents.Execute(hits[0].gameObject, ped, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        }
        else ReconBuild.Log("  !! EventSystem.current is null");

        ReconBuild.Log("  3) AFTER icon click panel.activeSelf = " + (panel != null && panel.activeSelf) + "   (want True)");

        // Close via the X inside THIS overlay. Must be scoped to the panel subtree:
        // GridManager.cs:736 creates a second runtime GameObject also named "CloseButton"
        // for the submit popup, so an unscoped search picks the wrong one in Game-Interface.
        Button close = null;
        if (panel != null)
            foreach (var b in panel.GetComponentsInChildren<Button>(true))
                if (b.name == "CloseButton") { close = b; break; }
        int dupes = 0;
        foreach (var b in Object.FindObjectsOfType<Button>(true)) if (b.name == "CloseButton") dupes++;
        ReconBuild.Log("  CloseButton objects in scene = " + dupes + " (scoped to overlay panel: " +
                       (close != null ? ReconBuild.HPath(close.transform) : "NOT FOUND") + ")");
        if (close != null)
        {
            close.onClick.Invoke();
            ReconBuild.Log("  4) AFTER X click  panel.activeSelf = " + (panel != null && panel.activeSelf) + "   (want False)");
        }
        else ReconBuild.Log("  !! CloseButton not found");

        ReconBuild.DumpLog("PLAYTEST " + scene);
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
