// ButtonSizeFix.cs — scoped follow-up to pass 3: button label sizing.
// Editor-only. Asset + inspector work; changes no runtime C#.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ButtonSizeFix
{
    /// STEP 1. Renders "PLAY" in Alphakind at exactly 61pt inside a 1920x1080 reference
    /// canvas and measures the cap height TMP actually produces, so we can tell whether
    /// the 0.700 ratio was wrong or the 42.7px art measurement was.
    [MenuItem("Tools/BtnFix/STEP1 Diagnose 61pt")]
    public static void Step1()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var canvasGo = new GameObject("C", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasGo.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        var crt = canvasGo.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(1920, 1080);

        var go = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvasGo.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        foreach (var fontPath in new[] { TypographyPass3.AlphakindSDF, TypographyPass3.JungleSDF })
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = fa;
            tmp.enableAutoSizing = false;
            tmp.fontSize = 61f;
            tmp.text = "PLAY";
            tmp.ForceMeshUpdate();

            var fi = fa.faceInfo;
            ReconBuild.Log("=== " + fa.name + " ===");
            ReconBuild.Log(string.Format("   faceInfo: pointSize={0}  scale={1}  capLine={2}  ascentLine={3}  descentLine={4}  unitsPerEm(sampling)",
                fi.pointSize, fi.scale, fi.capLine, fi.ascentLine, fi.descentLine));
            ReconBuild.Log(string.Format("   TMP capLine/pointSize ratio = {0:0.0000}   (pass-3 table claimed 0.7000 from OS/2 sCapHeight)",
                fi.capLine / (float)fi.pointSize));

            // Exact glyph-body cap height: 'P' sits on the baseline with no descender,
            // so its glyph metric height IS the cap height. No SDF padding involved.
            uint P = 'P';
            TMP_Character ch;
            if (fa.characterLookupTable.TryGetValue(P, out ch))
            {
                float gh = ch.glyph.metrics.height;
                float scale = 61f / fi.pointSize;
                ReconBuild.Log(string.Format("   glyph 'P' metrics.height={0:0.##} at sampling pointSize {1}  ->  at 61pt = {2:0.##} canvas px",
                    gh, fi.pointSize, gh * scale));
                ReconBuild.Log(string.Format("   EFFECTIVE capHeight/pt = {0:0.0000}", gh * scale / 61f));
            }

            // Cross-check against the actual generated mesh (quad corners include SDF
            // padding, so this should read slightly LARGER than the glyph body).
            var ti = tmp.textInfo;
            float top = float.MinValue, bot = float.MaxValue, baseline = 0f;
            for (int i = 0; i < ti.characterCount; i++)
            {
                var c = ti.characterInfo[i];
                if (!c.isVisible) continue;
                top = Mathf.Max(top, c.topLeft.y);
                bot = Mathf.Min(bot, c.bottomLeft.y);
                baseline = c.baseLine;
            }
            ReconBuild.Log(string.Format("   mesh quad top-baseline = {0:0.##} px (incl. SDF padding), full quad height {1:0.##}",
                top - baseline, top - bot));
            ReconBuild.Log(string.Format("   rendered text bounds W x H = {0:0.##} x {1:0.##}", tmp.preferredWidth, tmp.preferredHeight));
        }
        ReconBuild.DumpLog("STEP1 61pt diagnosis");
    }

    // Measured cap heights in the baked art, per button:
    //   class A (short labels)  35,36,38,38,39,42 px  -> mean 38.0
    //   class B (long labels)   24,25,25,27,27,27,27  -> mean 25.9
    // Alphakind EFFECTIVE capHeight/pt = 0.745 (measured), not the 0.700 OS/2 claimed.
    //   class A  38.0 / 0.745 = 51pt      class B  25.9 / 0.745 = 35pt
    // Sizes are the IoU optimum of the rendered label composited onto the baked art,
    // swept per class. The peaks are sharp (class B: .61 .67 .83 .67 .49 across 33-37pt),
    // so these are real optima rather than noise.
    const float SIZE_A = 50f, MIN_A = 24f;   // short labels, all scenes   (mean IoU .805)
    const float SIZE_B = 35f, MIN_B = 22f;   // long labels, menus         (mean IoU .827)
    const float SIZE_C = 32f, MIN_C = 20f;   // long labels, in-game HUD   (mean IoU .793)

    // Padding by INSETTING ANCHOR FRACTIONS -- offsets stay zero.
    // Tightest text inset in the art is 36/269 = 0.134 of plate width; 0.12 leaves the
    // long class-B labels room while still clearing the plate bevel. Vertically 0.18..0.82
    // is 69px on a 108px plate, enough for a 51pt line box (60.7px).
    const float PAD_X0 = 0.12f, PAD_X1 = 0.88f, PAD_Y0 = 0.18f, PAD_Y1 = 0.82f;

    // Three measured classes. Game-Interface's long labels are genuinely ~8% smaller in
    // the art than the menus', so they get their own class rather than being averaged in.
    static readonly (string scene, string button, float size, float min, string cls)[] OVERRIDES = {
        ("Assets/Scenes/MainMenu.unity",      "How to Play Button",  SIZE_B, MIN_B, "B menu"),
        ("Assets/Scenes/MainMenu.unity",      "Hall of Fame Button", SIZE_B, MIN_B, "B menu"),
        ("Assets/Scenes/Single Patch.unity",  "How to Play Button",  SIZE_B, MIN_B, "B menu"),
        ("Assets/Scenes/Single Patch.unity",  "About Button",        SIZE_B, MIN_B, "B menu"),
        ("Assets/Scenes/Game-Interface.unity","How to Play Button",  SIZE_C, MIN_C, "C hud"),
        ("Assets/Scenes/Game-Interface.unity","Reset Button",        SIZE_C, MIN_C, "C hud"),
        ("Assets/Scenes/Game-Interface.unity","Hint Button",         SIZE_C, MIN_C, "C hud"),
    };

    [MenuItem("Tools/BtnFix/STEP3 Apply Sizes + Padding")]
    public static void Step3()
    {
        ReconBuild.ResetLog();

        // --- prefab: class A default + padded label rect ---
        const string P = "Assets/Prefabs/UI/UIButton.prefab";
        var root = PrefabUtility.LoadPrefabContents(P);
        var lab = root.GetComponentInChildren<TextMeshProUGUI>(true);
        var lrt = lab.rectTransform;
        lrt.anchorMin = new Vector2(PAD_X0, PAD_Y0);
        lrt.anchorMax = new Vector2(PAD_X1, PAD_Y1);
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        lab.enableAutoSizing = true;
        lab.fontSize = SIZE_A; lab.fontSizeMax = SIZE_A; lab.fontSizeMin = MIN_A;
        lab.margin = Vector4.zero;         // padding now comes from the anchors
        PrefabUtility.SaveAsPrefabAsset(root, P);
        PrefabUtility.UnloadPrefabContents(root);
        ReconBuild.Log(string.Format("UIButton.prefab  size={0} (class A, cap {1:0.0}px)  autosize {2}..{0}",
            SIZE_A, SIZE_A * 0.745f, MIN_A));
        ReconBuild.Log(string.Format("   Label rect anchors x {0:0.00}..{1:0.00}  y {2:0.00}..{3:0.00}  offsets 0 (padding via anchors)",
            PAD_X0, PAD_X1, PAD_Y0, PAD_Y1));
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        // --- class B: SIZE override only, font + material still inherited ---
        foreach (var g in OVERRIDES.GroupBy(x => x.scene))
        {
            EditorSceneManager.OpenScene(g.Key);
            var stage = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
            ReconBuild.Log("--- " + System.IO.Path.GetFileNameWithoutExtension(g.Key));
            foreach (var o in g)
            {
                var t = ReconBuild.FindDeep(stage, o.button);
                if (t == null) { ReconBuild.Log("   !! not found: " + o.button); continue; }
                var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
                tmp.enableAutoSizing = true;
                tmp.fontSize = o.size; tmp.fontSizeMax = o.size; tmp.fontSizeMin = o.min;
                if (PrefabUtility.IsPartOfPrefabInstance(tmp))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);
                EditorUtility.SetDirty(tmp);
                ReconBuild.Log(string.Format("   {0,-22} class {1}  size={2} (cap {3:0.0}px)  font/material INHERITED",
                    o.button, o.cls, o.size, o.size * 0.745f));
            }
            ReconBuild.SaveActive();
        }

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        ReconBuild.DumpLog("STEP3 sizes + padding");
    }

    /// STEP 4. Per-button: resolved size, rendered text bounds vs the inset anchor box,
    /// and whether anything overflows or hits the auto-size floor.
    [MenuItem("Tools/BtnFix/STEP4 Verify Buttons")]
    public static void Step4()
    {
        ReconBuild.ResetLog();
        var targets = new (string scene, string btn, string longest, float artCap)[] {
            ("Assets/Scenes/MainMenu.unity","Play Button","PLAY",36f),
            ("Assets/Scenes/MainMenu.unity","How to Play Button","How to Play",27f),
            ("Assets/Scenes/MainMenu.unity","Hall of Fame Button","Hall of Fame",27f),
            ("Assets/Scenes/Patches.unity","Back","Back",39f),
            ("Assets/Scenes/Patches.unity","Play","PLAY",38f),
            ("Assets/Scenes/Patches.unity","Next","Next",35f),
            ("Assets/Scenes/HowToPlay.unity","Play Button","PLAY",38f),
            ("Assets/Scenes/HowToPlay.unity","Next Button","Next",35f),
            ("Assets/Scenes/Game-Interface.unity","How to Play Button","How to Play",25f),
            ("Assets/Scenes/Game-Interface.unity","Reset Button","Reset Game",24f),
            ("Assets/Scenes/Game-Interface.unity","Hint Button","Pawl’s Hint",25f),
            ("Assets/Scenes/Game-Interface.unity","Submit Button","SUBMIT",42f),
            ("Assets/Scenes/Single Patch.unity","How to Play Button","How to Play",27f),
            ("Assets/Scenes/Single Patch.unity","About Button","About",27f),
        };
        int bleed = 0, overflow = 0, atMin = 0, fontOverride = 0;
        string cur = null; Transform stage = null;
        ReconBuild.Log(string.Format("{0,-14} {1,-20} {2,-13} {3,6} {4,7} {5,7} {6,8} {7,8}  {8}",
            "scene", "button", "longest", "pt", "capPx", "artCap", "textW", "boxW", "verdict"));
        foreach (var (scene, btn, longest, artCap) in targets)
        {
            if (scene != cur) { EditorSceneManager.OpenScene(scene); cur = scene; stage = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage"); }
            var t = ReconBuild.FindDeep(stage, btn);
            if (t == null) { ReconBuild.Log("   !! missing " + btn); continue; }
            var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);

            if (PrefabUtility.IsPartOfPrefabInstance(tmp))
            {
                var mods = PrefabUtility.GetPropertyModifications(tmp);
                if (mods != null && mods.Any(m => m.propertyPath == "m_fontAsset" || m.propertyPath == "m_sharedMaterial"))
                { fontOverride++; ReconBuild.Log("   FONT/MATERIAL OVERRIDE on " + btn); }
            }

            string old = tmp.text;
            tmp.text = longest;
            tmp.ForceMeshUpdate();
            var box = tmp.rectTransform.rect;
            float w = tmp.textBounds.size.x, h = tmp.textBounds.size.y;
            float pt = tmp.fontSize;
            float cap = pt * 0.745f;
            bool over = w > box.width + 0.5f || h > box.height + 0.5f;
            bool floorHit = pt <= tmp.fontSizeMin + 0.01f;
            if (over) { bleed++; overflow++; }
            if (floorHit) atMin++;
            ReconBuild.Log(string.Format("{0,-14} {1,-20} {2,-13} {3,6:0.0} {4,7:0.0} {5,7:0} {6,8:0.0} {7,8:0.0}  {8}",
                System.IO.Path.GetFileNameWithoutExtension(scene), btn, longest, pt, cap, artCap, w, box.width,
                over ? "BLEEDS <<<" : (floorHit ? "at min <<<" : "inside")));
            tmp.text = old; tmp.ForceMeshUpdate();
        }
        ReconBuild.Log("");
        ReconBuild.Log("labels bleeding past their inset box .... " + bleed + "  -> " + (bleed == 0 ? "PASS" : "FAIL"));
        ReconBuild.Log("labels at the auto-size floor ........... " + atMin + "  -> " + (atMin == 0 ? "PASS" : "FAIL"));
        ReconBuild.Log("per-instance FONT/MATERIAL overrides .... " + fontOverride + "  -> " + (fontOverride == 0 ? "PASS" : "FAIL"));
        ReconBuild.DumpLog("STEP4 verify");
    }
}
#endif
