// ReconstructionBuilder.cs
// Editor-only tooling for the scene reconstruction pass (reconstruction_1_plan.md).
//
// WHY THIS EXISTS: mcp-unity's update_component cannot assign UnityEngine.Object
// references (Sprite, TMP_FontAsset, onClick targets, serialized component refs).
// It reports success and silently leaves the field null. Anchors work fine over
// mcp-unity; object references do not. Plan step P-4 permits an Editor script as
// the import mechanism; this is that script, scoped to also cover object-reference
// assignment and onClick wiring.
//
// This file authors scenes. It does not change any runtime/gameplay C#.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ReconBuild
{
    public const string ArtDrop = "_ArtDrop";
    public const string ReconRoot = "Assets/Art/Reconstruction";
    public const string UIButtonPrefab = "Assets/Prefabs/UI/UIButton.prefab";

    // ---------------------------------------------------------------- logging
    static readonly List<string> _log = new List<string>();
    public static void Log(string s) { _log.Add(s); Debug.Log("[Recon] " + s); }
    public static void ResetLog() { _log.Clear(); }
    public static void DumpLog(string header)
    {
        Debug.Log("[Recon] ===== " + header + " =====\n" + string.Join("\n", _log));
    }

    // ---------------------------------------------------------------- import
    /// Copies every PNG/JPG in "<ArtDrop>/<srcFolder>/images" into
    /// Assets/Art/Reconstruction/<dstName>/ and applies UI sprite import settings.
    /// Baked reference images live at the folder ROOT, not in images/, so they are
    /// never copied. That is the plan's "never import baked references" rule.
    public static void ImportFolder(string srcFolder, string dstName)
    {
        string src = Path.Combine(ArtDrop, srcFolder, "images");
        if (!Directory.Exists(src)) { Log("IMPORT: no images/ folder for " + srcFolder + " (expected for HamburgerMenu)"); return; }

        string dst = ReconRoot + "/" + dstName;
        Directory.CreateDirectory(dst);

        int n = 0;
        foreach (var f in Directory.GetFiles(src))
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;
            string name = Path.GetFileName(f);
            if (name.ToLowerInvariant().Contains("baked")) { Log("IMPORT: REFUSED baked ref " + name); continue; }
            string target = dst + "/" + name;
            File.Copy(f, target, true);
            n++;
        }
        AssetDatabase.Refresh();

        foreach (var f in Directory.GetFiles(dst))
        {
            if (f.EndsWith(".meta")) continue;
            ApplySpriteImport(f.Replace("\\", "/"));
        }
        AssetDatabase.Refresh();
        Log(string.Format("IMPORT: {0} -> {1} ({2} files)", srcFolder, dst, n));
    }

    public static void ApplySpriteImport(string assetPath)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.Compressed;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
    }

    public static Sprite Sprite(string assetPath)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (s == null) Log("!! MISSING SPRITE: " + assetPath);
        return s;
    }

    // ---------------------------------------------------------------- skeleton
    public static GameObject Find(string name)
    {
        foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
            if (go.name == name) return go;
        return null;
    }

    public static RectTransform Anchor(GameObject go, float ax0, float ay0, float ax1, float ay1)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax0, ay0);
        rt.anchorMax = new Vector2(ax1, ay1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    /// Anchors from a top-left-origin pixel box in the 1920x1080 baked reference.
    public static RectTransform AnchorPx(GameObject go, float x, float y, float w, float h)
    {
        return Anchor(go, x / 1920f, 1f - (y + h) / 1080f, (x + w) / 1920f, 1f - y / 1080f);
    }

    public static GameObject Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        GameObject go;
        if (t != null) go = t.gameObject;
        else { go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); }
        if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
        return go;
    }

    /// Builds Canvas -> Letterbox / Stage -> Background. Returns the Stage transform.
    /// Reuses the existing Canvas so render mode / camera binding survive (R-6 needs this).
    public static RectTransform BuildSkeleton(string backgroundSpritePath, bool forceOverlay)
    {
        var canvasGo = Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvasGo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
        if (canvasGo.GetComponent<GraphicRaycaster>() == null) canvasGo.AddComponent<GraphicRaycaster>();

        // CanvasScaler FIRST -- every anchor fraction assumes a 1920x1080 space.
        var cs = canvasGo.GetComponent<CanvasScaler>();
        if (cs == null) cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        Log(string.Format("CanvasScaler -> {0}x{1} match {2} (renderMode {3})",
            cs.referenceResolution.x, cs.referenceResolution.y, cs.matchWidthOrHeight, canvas.renderMode));

        // Letterbox
        var lb = Child(canvasGo.transform, "Letterbox");
        Anchor(lb, 0, 0, 1, 1);
        var lbImg = lb.GetComponent<Image>(); if (lbImg == null) lbImg = lb.AddComponent<Image>();
        lbImg.sprite = null;
        lbImg.color = new Color(26f / 255f, 26f / 255f, 26f / 255f, 1f);
        lbImg.raycastTarget = false;
        lb.transform.SetSiblingIndex(0);

        // Stage
        var stage = Child(canvasGo.transform, "Stage");
        var srt = Anchor(stage, 0, 0, 1, 1);
        srt.pivot = new Vector2(0.5f, 0.5f);
        var arf = stage.GetComponent<AspectRatioFitter>(); if (arf == null) arf = stage.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 1.7778f;
        stage.transform.SetSiblingIndex(1);

        // Background
        if (!string.IsNullOrEmpty(backgroundSpritePath))
        {
            var bg = Child(stage.transform, "Background");
            Anchor(bg, 0, 0, 1, 1);
            var bi = bg.GetComponent<Image>(); if (bi == null) bi = bg.AddComponent<Image>();
            bi.sprite = Sprite(backgroundSpritePath);
            bi.color = Color.white;
            bi.raycastTarget = false;
            bg.transform.SetSiblingIndex(0);
        }
        return srt;
    }

    // ---------------------------------------------------------------- elements
    public static GameObject Img(RectTransform stage, string name, string spritePath, float x, float y, float w, float h)
    {
        var go = Child(stage, name);
        AnchorPx(go, x, y, w, h);
        var im = go.GetComponent<Image>(); if (im == null) im = go.AddComponent<Image>();
        if (!string.IsNullOrEmpty(spritePath)) im.sprite = Sprite(spritePath);
        im.color = Color.white;
        im.raycastTarget = false;
        return go;
    }

    public static TextMeshProUGUI Text(RectTransform stage, string name, string content,
        float x, float y, float w, float h, float sizeMin, float sizeMax,
        TextAlignmentOptions align, Color col)
    {
        var go = Child(stage, name);
        AnchorPx(go, x, y, w, h);
        var t = go.GetComponent<TextMeshProUGUI>(); if (t == null) t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.enableAutoSizing = true;
        t.fontSizeMin = sizeMin;
        t.fontSizeMax = sizeMax;
        t.alignment = align;
        t.color = col;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }

    // ---------------------------------------------------------------- UIButton
    public static void EnsureUIButtonPrefab()
    {
        Directory.CreateDirectory("Assets/Prefabs/UI");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(UIButtonPrefab) != null) { Log("UIButton.prefab already exists"); return; }

        var root = new GameObject("UIButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.4f, 0.4f);
        rt.anchorMax = new Vector2(0.6f, 0.6f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = root.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;

        var btn = root.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = img;

        var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        var lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

        var tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = "Label";
        tmp.alignment = TextAlignmentOptions.Center;   // horizontal centre + vertical middle
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 44f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.margin = new Vector4(12f, 6f, 12f, 6f);

        PrefabUtility.SaveAsPrefabAsset(root, UIButtonPrefab);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Log("CREATED " + UIButtonPrefab);
    }

    /// Instantiates UIButton under stage. Overrides ONLY plate sprite, label string, anchors.
    public static GameObject Btn(RectTransform stage, string name, string platePath, string label,
        float x, float y, float w, float h, bool darkLabel = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIButtonPrefab);
        var existing = stage.Find(name);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage);
        go.name = name;
        AnchorPx(go, x, y, w, h);
        var img = go.GetComponent<Image>();
        if (!string.IsNullOrEmpty(platePath)) img.sprite = Sprite(platePath);
        else { img.sprite = null; img.color = new Color(1, 1, 1, 0); }
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        tmp.text = label;
        if (darkLabel) tmp.color = new Color(0.23f, 0.11f, 0.03f, 1f);
        return go;
    }

    // ---------------------------------------------------------------- wiring
    public static void Wire(GameObject buttonGo, UnityAction call, string desc)
    {
        var b = buttonGo.GetComponent<Button>();
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddPersistentListener(b.onClick, call);
        Log("WIRE " + buttonGo.name + " -> " + desc);
    }

    public static void WireInt(GameObject buttonGo, UnityAction<int> call, int arg, string desc)
    {
        var b = buttonGo.GetComponent<Button>();
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddIntPersistentListener(b.onClick, call, arg);
        Log("WIRE " + buttonGo.name + " -> " + desc + "(" + arg + ")");
    }

    public static void Unwired(GameObject buttonGo, string why)
    {
        Log("UNWIRED " + buttonGo.name + " : " + why);
    }

    // ---------------------------------------------------------------- verify
    public class VerifyResult
    {
        public int notUnderStage, pointAnchors, nonZeroOffsets, buttonsNoLabel, iconOnly, nullRefs, refsChecked, elements;
        public List<string> detail = new List<string>();
    }

    public static VerifyResult Verify(bool expectStage = true)
    {
        var r = new VerifyResult();
        var stage = Find("Stage");
        var canvasGo = Find("Canvas");

        // CanvasScaler
        var cs = canvasGo != null ? canvasGo.GetComponent<CanvasScaler>() : null;
        if (cs != null)
            Log(string.Format("CHECK CanvasScaler: {0}x{1} match {2}  -> {3}",
                cs.referenceResolution.x, cs.referenceResolution.y, cs.matchWidthOrHeight,
                (cs.referenceResolution == new Vector2(1920, 1080) && Mathf.Approximately(cs.matchWidthOrHeight, 0.5f)) ? "PASS" : "FAIL"));

        var arf = stage != null ? stage.GetComponent<AspectRatioFitter>() : null;
        Log(string.Format("CHECK Stage AspectRatioFitter: {0} ratio {1} -> {2}",
            arf != null ? arf.aspectMode.ToString() : "MISSING",
            arf != null ? arf.aspectRatio.ToString("0.0000") : "-",
            (arf != null && arf.aspectMode == AspectRatioFitter.AspectMode.FitInParent && Mathf.Abs(arf.aspectRatio - 1.7778f) < 0.001f) ? "PASS" : "FAIL"));

        if (stage == null) { Log("CHECK Stage: MISSING"); return r; }

        foreach (var g in stage.GetComponentsInChildren<Graphic>(true))
        {
            if (g.transform == stage.transform) continue;
            r.elements++;
            var rt = g.rectTransform;
            if (rt.anchorMin == rt.anchorMax) { r.pointAnchors++; r.detail.Add("POINT ANCHOR: " + HPath(rt)); }
            if (rt.offsetMin != Vector2.zero || rt.offsetMax != Vector2.zero)
            { r.nonZeroOffsets++; r.detail.Add(string.Format("OFFSETS {0} min{1} max{2}", HPath(rt), rt.offsetMin, rt.offsetMax)); }
        }

        // Graphics anywhere in the scene that are NOT under Stage
        foreach (var g in UnityEngine.Object.FindObjectsOfType<Graphic>(true))
        {
            if (g.transform == stage.transform) continue;
            if (g.transform.IsChildOf(stage.transform)) continue;
            if (g.gameObject.name == "Letterbox") continue;           // Letterbox is skeleton, by design
            if (IsInsideOverlayPrefab(g.transform)) continue;          // MenuOverlay has its own canvas
            r.notUnderStage++;
            r.detail.Add("NOT UNDER STAGE: " + HPath(g.transform));
        }

        foreach (var b in stage.GetComponentsInChildren<Button>(true))
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null && !string.IsNullOrEmpty(t.text)) continue;
            // A button carrying a sprite and no text is an icon button (hamburger, close,
            // patch tile). That is by design, not a missing label -- counted separately.
            var im = b.GetComponent<Image>();
            if (im != null && im.sprite != null) { r.iconOnly++; r.detail.Add("ICON-ONLY (no label, by design): " + HPath(b.transform)); }
            else { r.buttonsNoLabel++; r.detail.Add("BUTTON NO LABEL: " + HPath(b.transform)); }
        }

        // Serialized object references on every project MonoBehaviour in the scene
        foreach (var mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            var t = mb.GetType();
            var ms = MonoScript.FromMonoBehaviour(mb);
            var p = ms != null ? AssetDatabase.GetAssetPath(ms) : "";
            if (!p.StartsWith("Assets/Scripts/")) continue;   // project scripts only, not TMP/UGUI internals
            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.name == "m_Script") continue;
                r.refsChecked++;
                if (it.objectReferenceValue == null)
                { r.nullRefs++; r.detail.Add(string.Format("NULL REF: {0}.{1} on {2}", t.Name, it.name, mb.gameObject.name)); }
            }
        }

        // Stage itself is driven by AspectRatioFitter; report its live anchors.
        var stageRt = stage.GetComponent<RectTransform>();
        Log(string.Format("CHECK Stage live anchors: min{0} max{1} offMin{2} offMax{3} rect{4}",
            stageRt.anchorMin, stageRt.anchorMax, stageRt.offsetMin, stageRt.offsetMax, stageRt.rect.size));

        Log("---- ELEMENTS (live) ----");
        foreach (var g in stage.GetComponentsInChildren<Graphic>(true))
        {
            if (g.transform == stage.transform) continue;
            var rt = g.rectTransform;
            string extra = "";
            var im = g as Image;
            if (im != null) extra = "sprite=" + (im.sprite != null ? im.sprite.name : "NONE");
            var tm = g as TextMeshProUGUI;
            if (tm != null) extra = "text=\"" + (tm.text.Length > 34 ? tm.text.Substring(0, 34).Replace("\n", " ") + "..." : tm.text.Replace("\n", " ")) + "\"";
            Log(string.Format("  {0,-26} min({1:0.0000},{2:0.0000}) max({3:0.0000},{4:0.0000}) off{5}{6}  {7}",
                g.gameObject.name, rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y,
                rt.offsetMin, rt.offsetMax, extra));
        }
        Log("---- BUTTONS ----");
        foreach (var b in stage.GetComponentsInChildren<Button>(true))
        {
            int n = b.onClick.GetPersistentEventCount();
            string tgt = n == 0 ? "UNWIRED" : "";
            for (int i = 0; i < n; i++)
            {
                var o = b.onClick.GetPersistentTarget(i);
                tgt += (o != null ? o.GetType().Name : "NULL") + "." + b.onClick.GetPersistentMethodName(i) + "()  ";
            }
            var lb = b.GetComponentInChildren<TextMeshProUGUI>(true);
            Log(string.Format("  {0,-26} label=\"{1}\"  -> {2}", b.gameObject.name, lb != null ? lb.text : "<none>", tgt));
        }

        Log(string.Format("CHECK elements under Stage: {0}", r.elements));
        Log(string.Format("CHECK not parented to Stage: {0} -> {1}", r.notUnderStage, r.notUnderStage == 0 ? "PASS" : "FAIL"));
        Log(string.Format("CHECK point anchors:         {0} -> {1}", r.pointAnchors, r.pointAnchors == 0 ? "PASS" : "FAIL"));
        Log(string.Format("CHECK non-zero offsets:      {0} -> {1}", r.nonZeroOffsets, r.nonZeroOffsets == 0 ? "PASS" : "FAIL"));
        Log(string.Format("CHECK buttons w/o label:     {0} -> {1}   (icon-only, exempt: {2})", r.buttonsNoLabel, r.buttonsNoLabel == 0 ? "PASS" : "FAIL", r.iconOnly));
        Log(string.Format("CHECK serialized refs: {0} checked, {1} null", r.refsChecked, r.nullRefs));
        foreach (var d in r.detail) Log("   " + d);
        Log("CHECK baked refs imported: " + CountBakedImported() + " -> " + (CountBakedImported() == 0 ? "PASS" : "FAIL"));
        return r;
    }

    static bool IsInsideOverlayPrefab(Transform t)
    {
        while (t != null) { if (t.name == "MenuOverlay" || t.name == "PopupOverlay") return true; t = t.parent; }
        return false;
    }

    public static int CountBakedImported()
    {
        int n = 0;
        foreach (var g in AssetDatabase.FindAssets("Baked"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.StartsWith("Assets/") && (p.EndsWith(".png") || p.EndsWith(".jpg"))) n++;
        }
        return n;
    }

    public static string HPath(Transform t)
    {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    public static void SaveActive()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Log("SAVED " + SceneManager.GetActiveScene().path);
    }
}
#endif
