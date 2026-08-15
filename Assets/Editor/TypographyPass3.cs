// TypographyPass3.cs — pass 3 typography (reconstruction_3_plan.md, F-1..F-6).
// Editor-only. Asset + inspector work; changes no runtime C#.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TypographyPass3
{
    public const string FontDir = "Assets/Fonts";
    public const string AlphakindTTF = FontDir + "/Alphakind.ttf";
    public const string JungleTTF = FontDir + "/JungleRanger.ttf";
    public const string AlphakindSDF = FontDir + "/Alphakind SDF.asset";
    public const string JungleSDF = FontDir + "/JungleRanger SDF.asset";

    // ASCII printable + the curly quotes actually used in project strings.
    // U+2013 EN DASH and U+2014 EM DASH are deliberately included in the request so
    // the generator REPORTS them as missing -- neither font contains them.
    static string TargetSet()
    {
        var sb = new StringBuilder();
        for (int c = 32; c <= 126; c++) sb.Append((char)c);
        sb.Append('‘').Append('’').Append('“').Append('”');
        sb.Append('–').Append('—');
        return sb.ToString();
    }

    [MenuItem("Tools/Type3/F-1 Generate TMP Font Assets")]
    public static void F1_Generate()
    {
        ReconBuild.ResetLog();
        Gen(AlphakindTTF, AlphakindSDF, "Alphakind");
        Gen(JungleTTF, JungleSDF, "JungleRanger");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ReconBuild.DumpLog("F-1 Font Assets");
    }

    static void Gen(string ttfPath, string assetPath, string label)
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) { ReconBuild.Log("!! could not load " + ttfPath); return; }

        // 90pt sampling / 9px padding / 1024^2 is TMP's own default for a display face
        // and leaves headroom; atlas occupancy is reported below.
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                                               1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (fa == null) { ReconBuild.Log("!! CreateFontAsset failed for " + label); return; }
        fa.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        string set = TargetSet();
        uint[] missing;
        fa.TryAddCharacters(set.Select(c => (uint)c).ToArray(), out missing, true);

        // Freeze the atlas so builds do not rely on runtime glyph generation.
        fa.atlasPopulationMode = AtlasPopulationMode.Static;

        if (System.IO.File.Exists(assetPath)) AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(fa, assetPath);
        // Atlas texture and material must live inside the font asset.
        foreach (var t in fa.atlasTextures)
        {
            t.name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(t, fa);
        }
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();

        int have = fa.characterTable.Count;
        var miss = missing == null ? new uint[0] : missing;
        ReconBuild.Log(string.Format("{0}: source={1}  glyphs in atlas={2}  atlas={3}x{4}  renderMode=SDFAA  padding=9  samplingPointSize=90",
            label, System.IO.Path.GetFileName(ttfPath), have,
            fa.atlasWidth, fa.atlasHeight));
        if (miss.Length > 0)
        {
            var names = miss.Select(u => string.Format("U+{0:X4}", u));
            ReconBuild.Log("   MISSING from this font: " + string.Join(", ", names));
        }
        else ReconBuild.Log("   MISSING: none");
    }

    // ---------------------------------------------------------------- F-2
    public const string MatUI = FontDir + "/Alphakind SDF - UI.mat";
    public const string MatBody = FontDir + "/JungleRanger SDF - Body.mat";
    public const string MatValueLabel = FontDir + "/Alphakind SDF - ValueLabel.mat";

    [MenuItem("Tools/Type3/F-2 Material Presets")]
    public static void F2_Materials()
    {
        ReconBuild.ResetLog();
        var alpha = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlphakindSDF);
        var jungle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JungleSDF);
        MakePreset(alpha, MatUI, "shared: all Alphakind UI text");
        MakePreset(jungle, MatBody, "shared: all Jungle Ranger body text");
        MakePreset(alpha, MatValueLabel, "DEDICATED: ValueLabel.prefab ONLY (GridManager outline write lands here)");

        // The en dash U+2013 used by SinglePatchController's score strings exists in
        // neither font. A fallback keeps it from rendering as a blank box.
        var lib = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        foreach (var fa in new[] { alpha, jungle })
        {
            if (fa == null || lib == null) continue;
            if (fa.fallbackFontAssetTable == null) fa.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!fa.fallbackFontAssetTable.Contains(lib)) fa.fallbackFontAssetTable.Add(lib);
            EditorUtility.SetDirty(fa);
            ReconBuild.Log("fallback on " + fa.name + " -> LiberationSans SDF (covers U+2013 en dash)");
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        ReconBuild.DumpLog("F-2 Material Presets");
    }

    static void MakePreset(TMP_FontAsset fa, string path, string note)
    {
        if (fa == null) { ReconBuild.Log("!! font asset missing for " + path); return; }
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
        var m = new Material(fa.material);
        m.name = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(m, path);
        ReconBuild.Log("CREATED " + path + "   (" + note + ")");
    }

    // ---------------------------------------------------------------- F-3
    [MenuItem("Tools/Type3/F-3 UIButton Prefab")]
    public static void F3_UIButton()
    {
        ReconBuild.ResetLog();
        const string P = "Assets/Prefabs/UI/UIButton.prefab";
        var root = PrefabUtility.LoadPrefabContents(P);
        var lab = root.GetComponentInChildren<TextMeshProUGUI>(true);
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlphakindSDF);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatUI);
        lab.font = fa;
        lab.fontSharedMaterial = mat;
        // Measured cap heights of baked button text: 41px (HowToPlay PLAY), 43px
        // (Patches Back), 44px (Game-Interface SUBMIT). Mean 42.7px.
        // Alphakind capHeight/em = 0.700  ->  42.7 / 0.700 = 61.0
        lab.fontSize = 61f;
        lab.enableAutoSizing = true;
        lab.fontSizeMin = 24f;
        lab.fontSizeMax = 61f;
        lab.color = Color.white;
        lab.alignment = TextAlignmentOptions.Center;
        PrefabUtility.SaveAsPrefabAsset(root, P);
        PrefabUtility.UnloadPrefabContents(root);
        ReconBuild.Log("UIButton label: font=Alphakind SDF  material=Alphakind SDF - UI");
        ReconBuild.Log("   fontSize=61 (measured cap 42.7px / 0.700 capHeight-per-em), autosize 24..61, white, centered");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        ReconBuild.DumpLog("F-3 UIButton");
    }

    // ---------------------------------------------------------------- F-4/F-5
    static readonly Color BodyBrown = new Color(153f/255f, 77f/255f, 12f/255f, 1f);
    static readonly Color HeadOrange = new Color(216f/255f, 135f/255f, 44f/255f, 1f);
    static readonly Color TitleBrown = new Color(70f/255f, 31f/255f, 0f, 1f);
    static readonly Color LeafGreen = new Color(169f/255f, 217f/255f, 42f/255f, 1f);
    static readonly Color ResultsTan = new Color(206f/255f, 150f/255f, 65f/255f, 1f);
    static readonly Color ScoreBrown = new Color(116f/255f, 52f/255f, 8f/255f, 1f);
    static readonly Color DarkBrown = new Color(0.30f, 0.16f, 0.06f, 1f);

    static TMP_FontAsset _alpha, _jungle;
    static Material _mUI, _mBody;

    static void Load()
    {
        _alpha = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlphakindSDF);
        _jungle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JungleSDF);
        _mUI = AssetDatabase.LoadAssetAtPath<Material>(MatUI);
        _mBody = AssetDatabase.LoadAssetAtPath<Material>(MatBody);
    }

    /// alpha=true -> Alphakind + UI preset; false -> JungleRanger + Body preset.
    static void Set(Transform stage, string path, bool alpha, float size, Color col,
                    float minS, float lineSpacing = 0f)
    {
        var t = ReconBuild.FindDeep(stage, path);
        if (t == null) { ReconBuild.Log("   !! not found: " + path); return; }
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp == null) { ReconBuild.Log("   !! no TMP on: " + path); return; }
        tmp.font = alpha ? _alpha : _jungle;
        tmp.fontSharedMaterial = alpha ? _mUI : _mBody;
        tmp.fontSize = size;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minS;
        tmp.fontSizeMax = size;
        tmp.color = col;
        if (lineSpacing != 0f) tmp.lineSpacing = lineSpacing;
        ReconBuild.Log(string.Format("   {0,-22} {1,-13} size={2,-5} min={3,-4} colour=({4:0.00},{5:0.00},{6:0.00})",
            path, alpha ? "Alphakind" : "JungleRanger", size, minS, col.r, col.g, col.b));
    }

    [MenuItem("Tools/Type3/F-4 Apply Per-Scene Text")]
    public static void F4_Apply()
    {
        ReconBuild.ResetLog();
        Load();

        // ---- HowToPlay ----
        EditorSceneManager.OpenScene("Assets/Scenes/HowToPlay.unity");
        var st = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
        ReconBuild.Log("HowToPlay:");
        // body cap 27px -> 27/0.70 = 38.6 ; line pitch 40px -> +5.6% line spacing
        Set(st, "Body", false, 39f, BodyBrown, 16f, 5.6f);
        ReconBuild.SaveActive();

        // ---- Patches ----
        EditorSceneManager.OpenScene("Assets/Scenes/Patches.unity");
        st = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
        ReconBuild.Log("Patches:");
        foreach (var k in new[] { "City", "Grassland", "Forest", "Road", "Farmland", "Habitat" })
        {
            Set(st, k + " Heading", true, 36f, HeadOrange, 16f);       // cap 25px / 0.70
            Set(st, k + " Desc", false, 25f, BodyBrown, 12f, 2.0f);    // line pitch 24px
        }
        ReconBuild.SaveActive();

        // ---- Single Patch ----
        EditorSceneManager.OpenScene("Assets/Scenes/Single Patch.unity");
        st = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
        ReconBuild.Log("Single Patch:");
        Set(st, "Tile Name", true, 64f, Color.white, 22f);             // cap 45px on wooden sign
        Set(st, "Tile Score", false, 63f, DarkBrown, 22f);             // cap 44px
        Set(st, "Tile Description", false, 47f, BodyBrown, 16f, 0f);   // line pitch 46px
        ReconBuild.SaveActive();

        // ---- LevelSelect ----
        EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity");
        st = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
        ReconBuild.Log("LevelSelect:");
        Set(st, "Title", false, 79f, TitleBrown, 24f);                 // cap 55px, JungleRanger (measured)
        Set(st, "Caption 1", false, 44f, BodyBrown, 16f);
        Set(st, "Caption 2", false, 33f, BodyBrown, 14f);
        // numerals live on UIButton instances -> override size only, font comes from the prefab
        foreach (var b in st.GetComponentsInChildren<Button>(true))
        {
            if (!b.name.StartsWith("Level ")) continue;
            var lb = b.GetComponentInChildren<TextMeshProUGUI>(true);
            lb.font = _jungle; lb.fontSharedMaterial = _mBody;         // measured: numerals are JungleRanger
            lb.fontSize = 64f; lb.fontSizeMax = 64f; lb.fontSizeMin = 24f; lb.color = Color.white;
        }
        ReconBuild.Log("   Level 1-10 numerals   JungleRanger  size=64 (cap 45px, measured)");
        ReconBuild.SaveActive();

        // ---- Game-Interface ----
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");
        st = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
        ReconBuild.Log("Game-Interface:");
        Set(st, "Level Label", true, 66f, Color.white, 22f);           // cap 46px, wooden board
        Set(st, "Pts Label", true, 30f, LeafGreen, 12f);
        Set(st, "Patches Label", true, 29f, LeafGreen, 12f);
        var bw = ReconBuild.FindDeep(st, "Budget Warning");
        if (bw != null)
        {
            var t = bw.GetComponent<TextMeshProUGUI>();
            t.font = _alpha; t.fontSharedMaterial = _mUI; t.fontSize = 44f;
            t.enableAutoSizing = true; t.fontSizeMin = 18f; t.fontSizeMax = 44f;
            ReconBuild.Log("   Budget Warning         Alphakind     size=44");
        }
        // F-5 legacy UnityEngine.UI.Text -- cannot take a TMP asset, gets the raw Font
        var rawFont = AssetDatabase.LoadAssetAtPath<Font>(AlphakindTTF);
        foreach (var nm in new[] { "Score Value", "Budget Value" })
        {
            var t = ReconBuild.FindDeep(st, nm);
            if (t == null) { ReconBuild.Log("   !! legacy Text missing: " + nm); continue; }
            var ui = t.GetComponent<Text>();
            ui.font = rawFont;
            ui.color = Color.white;
            ui.resizeTextForBestFit = true;
            ui.resizeTextMinSize = 20;
            ui.resizeTextMaxSize = nm == "Score Value" ? 74 : 66;   // caps 52px / 46px
            ReconBuild.Log(string.Format("   [F-5 legacy Text] {0,-14} Font=Alphakind.ttf  bestFit {1}..{2}",
                nm, ui.resizeTextMinSize, ui.resizeTextMaxSize));
        }
        ReconBuild.SaveActive();

        // ---- PopupOverlay.prefab ----
        ApplyPrefab("Assets/Prefabs/UI/PopupOverlay.prefab", new[] {
            ("Results Heading", false, 77f, ResultsTan, 24f),
            ("Results Caption", false, 43f, BodyBrown, 16f),
            ("Score Value",     false, 100f, ScoreBrown, 30f),
            ("Pts Label",       false, 50f, ResultsTan, 18f) });

        // ---- MenuOverlay.prefab (placeholders, styled consistently) ----
        var mo = PrefabUtility.LoadPrefabContents("Assets/Prefabs/MenuOverlay.prefab");
        ReconBuild.Log("MenuOverlay.prefab:");
        foreach (var lb in mo.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!lb.transform.parent.name.StartsWith("Menu 0")) continue;
            lb.font = _alpha; lb.fontSharedMaterial = _mUI;
            lb.fontSize = 50f; lb.enableAutoSizing = true; lb.fontSizeMin = 20f; lb.fontSizeMax = 50f;
            lb.color = Color.white; lb.alignment = TextAlignmentOptions.Left;
            ReconBuild.Log("   " + lb.transform.parent.name + "  Alphakind  size=50 (cap 35px)");
        }
        PrefabUtility.SaveAsPrefabAsset(mo, "Assets/Prefabs/MenuOverlay.prefab");
        PrefabUtility.UnloadPrefabContents(mo);

        // ---- ValueLabel.prefab : DEDICATED material (F-2 trap) ----
        var vl = PrefabUtility.LoadPrefabContents("Assets/Prefabs/ValueLabel.prefab");
        var vtmp = vl.GetComponent<TextMeshPro>();
        var vmat = AssetDatabase.LoadAssetAtPath<Material>(MatValueLabel);
        vtmp.font = _alpha;
        vtmp.fontSharedMaterial = vmat;
        PrefabUtility.SaveAsPrefabAsset(vl, "Assets/Prefabs/ValueLabel.prefab");
        PrefabUtility.UnloadPrefabContents(vl);
        ReconBuild.Log("ValueLabel.prefab: font=Alphakind SDF  material=" + MatValueLabel + "  (dedicated, no other user)");

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        ReconBuild.DumpLog("F-4/F-5 Apply");
    }

    static void ApplyPrefab(string path, (string name, bool alpha, float size, Color col, float minS)[] items)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        ReconBuild.Log(System.IO.Path.GetFileName(path) + ":");
        var stage = ReconBuild.FindDeep(root.transform, "Stage");
        foreach (var it in items) Set(stage, it.name, it.alpha, it.size, it.col, it.minS);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---------------------------------------------------------------- audit
    [MenuItem("Tools/Type3/F-6 Audit All Text")]
    public static void F6_Audit()
    {
        ReconBuild.ResetLog();
        string[] scenes = {
            "Assets/Scenes/MainMenu.unity","Assets/Scenes/HowToPlay.unity","Assets/Scenes/Patches.unity",
            "Assets/Scenes/Single Patch.unity","Assets/Scenes/LevelSelect.unity","Assets/Scenes/Game-Interface.unity" };
        int liberation = 0, overrides = 0, noLabel = 0, overflow = 0, total = 0;
        foreach (var s in scenes)
        {
            EditorSceneManager.OpenScene(s);
            ReconBuild.Log("--- " + System.IO.Path.GetFileNameWithoutExtension(s));
            foreach (var t in Object.FindObjectsOfType<TextMeshProUGUI>(true))
            {
                total++;
                string fn = t.font != null ? t.font.name : "NULL";
                if (fn.Contains("Liberation")) { liberation++; ReconBuild.Log("   LIBERATION STILL: " + ReconBuild.HPath(t.transform)); }
                var btn = t.GetComponentInParent<Button>();
                if (btn != null && PrefabUtility.IsPartOfPrefabInstance(t))
                {
                    var mods = PrefabUtility.GetPropertyModifications(t);
                    if (mods != null && mods.Any(m => m.propertyPath == "m_fontAsset"))
                    { overrides++; ReconBuild.Log("   FONT OVERRIDE on button label: " + ReconBuild.HPath(t.transform)); }
                }
                t.ForceMeshUpdate();
                if (t.isTextOverflowing || (t.preferredHeight > t.rectTransform.rect.height + 1f && !t.enableAutoSizing))
                { overflow++; ReconBuild.Log("   OVERFLOW: " + ReconBuild.HPath(t.transform) + " pref=" + t.preferredHeight.ToString("0.0") + " rect=" + t.rectTransform.rect.height.ToString("0.0")); }
            }
            foreach (var b in Object.FindObjectsOfType<Button>(true))
            {
                var lb = b.GetComponentInChildren<TextMeshProUGUI>(true);
                var img = b.GetComponent<Image>();
                if ((lb == null || string.IsNullOrEmpty(lb.text)) && !(img != null && img.sprite != null)) { noLabel++; ReconBuild.Log("   BUTTON NO LABEL: " + ReconBuild.HPath(b.transform)); }
            }
        }
        ReconBuild.Log("");
        ReconBuild.Log("TMP elements total .................. " + total);
        ReconBuild.Log("still using LiberationSans SDF ...... " + liberation + "   -> " + (liberation == 0 ? "PASS" : "FAIL"));
        ReconBuild.Log("per-instance font overrides ......... " + overrides + "   -> " + (overrides == 0 ? "PASS" : "FAIL"));
        ReconBuild.Log("buttons with no label ............... " + noLabel + "   -> " + (noLabel == 0 ? "PASS" : "FAIL"));
        ReconBuild.Log("text overflowing its rect ........... " + overflow + "   -> " + (overflow == 0 ? "PASS" : "FAIL"));
        ReconBuild.DumpLog("F-6 Audit");
    }
}
#endif
