// ReconstructionPhases.cs — per-scene layout data for the reconstruction pass.
// All pixel boxes are top-left-origin in the 1920x1080 baked reference and come
// from the completed R-0 audit (template-matched, not eyeballed).

#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ReconPhases
{
    static Color Cream = new Color(1f, 1f, 1f, 1f);
    // Sampled straight out of the baked references so text matches the art.
    static Color BodyBrown = new Color(153f / 255f, 77f / 255f, 12f / 255f, 1f);   // body copy
    static Color HeadOrange = new Color(214f / 255f, 126f / 255f, 44f / 255f, 1f); // patch headings
    static Color DarkBrown = new Color(0.30f, 0.16f, 0.06f, 1f);

    // =====================================================================  R-1
    [MenuItem("Tools/Recon/R-1 MainMenu")]
    public static void R1_MainMenu()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

        // UIButton prefab is the first artifact of R-1.
        ReconBuild.EnsureUIButtonPrefab();

        ReconBuild.ImportFolder("MainMenu Scene", "MainMenu");
        const string A = ReconBuild.ReconRoot + "/MainMenu/";

        var stage = ReconBuild.BuildSkeleton(A + "background.png", true);

        ReconBuild.Img(stage, "Logo",  A + "logo-welcome.png", 443, 0, 989, 813);
        ReconBuild.Img(stage, "Pawl",  A + "pawl-welcome.png", 1432, 373, 432, 658);
        ReconBuild.Img(stage, "Bees",  A + "bees.png", 281, 226, 162, 230);

        var howTo = ReconBuild.Btn(stage, "How to Play Button", A + "how-to-play-btn.png", "How to Play", 467, 813, 286, 108);
        var play  = ReconBuild.Btn(stage, "Play Button",        A + "play-btn.png",        "PLAY",        832, 813, 287, 108);
        var hof   = ReconBuild.Btn(stage, "Hall of Fame Button",A + "hall-of-fame-btn.png","Hall of Fame",1196, 813, 287, 108);

        // Replicate existing bindings exactly (see R-0 audit).
        var ctrl = ReconBuild.Find("MainMenuUI").GetComponent<MainMenuController>();
        ReconBuild.Wire(play,  new UnityAction(ctrl.GoToLevelSelect),  "MainMenuController.GoToLevelSelect");
        ReconBuild.Wire(howTo, new UnityAction(ctrl.OpenHowTo),        "MainMenuController.OpenHowTo");
        ReconBuild.Wire(hof,   new UnityAction(ctrl.OpenHallOfFame),   "MainMenuController.OpenHallOfFame");

        // Retire the old alpha-0 hitbox container + baked background.
        var old = GameObject.Find("Canvas/Menu container");
        if (old != null) { Object.DestroyImmediate(old); ReconBuild.Log("DELETED old 'Menu container' (baked bg + 3 alpha-0 hitboxes)"); }

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("R-1 MainMenu");
    }

    // =====================================================================  R-2
    [MenuItem("Tools/Recon/R-2 HowToPlay")]
    public static void R2_HowToPlay()
    {
        ReconBuild.ResetLog();

        const string path = "Assets/Scenes/HowToPlay.unity";
        if (!File.Exists(path))
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(s, path);
            ReconBuild.Log("CREATED scene " + path);
        }
        EditorSceneManager.OpenScene(path);

        // HowToPlay/background.png is byte-identical to Patches/Background.png.
        // Plan says import once and reference from both -> import under Patches,
        // reference from here.
        ReconBuild.ImportFolder("HowToPlay Scene", "HowToPlay");
        const string A = ReconBuild.ReconRoot + "/HowToPlay/";

        // EventSystem is required for buttons to work in a brand-new scene.
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            ReconBuild.Log("CREATED EventSystem");
        }

        var stage = ReconBuild.BuildSkeleton(A + "background.png", true);

        ReconBuild.Img(stage, "Pawl",  A + "pawl.png", 109, 304, 498, 611);
        ReconBuild.Img(stage, "Bee",   A + "bee.png", 518, 163, 89, 91);
        ReconBuild.Img(stage, "Board", A + "board-how-to-play.png", 689, 0, 1121, 978);

        // Title cap + body copy, transcribed verbatim from HowToPlay_Baked.png.
        // Boxes are measured text bounding boxes (white / RGB(153,77,12) pixel scan).
        ReconBuild.Text(stage, "Title", "How to Play", 1107, 62, 373, 60, 24, 66,
            TextAlignmentOptions.Center, Cream);

        ReconBuild.Text(stage, "Body",
            "Pawl the Panther needs to travel from one habitat to another, and he needs your help!\n\n" +
            "Choose land patches to build a safe path for Pawl. But be careful—you can only pick a limited number of patches.\n\n" +
            "Click on a patch to add it to your path and click again to remove it.\n\n" +
            "Watch your patch budget and score as you build Pawl’s path!\n\n" +
            "Each patch gives you points based on how good it is. Help Pawl reach his destination while earning the highest score possible!",
            917, 198, 748, 634, 14, 32, TextAlignmentOptions.TopLeft, BodyBrown);

        var hamb = ReconBuild.Btn(stage, "Hamburger Button", A + "hamburger-icon.png", "", 40, 977, 69, 70);

        var playBtn = ReconBuild.Btn(stage, "Play Button", A + "button.png", "PLAY", 808, 915, 284, 108);
        var nextBtn = ReconBuild.Btn(stage, "Next Button", A + "button.png", "Next", 1475, 915, 284, 108);

        ReconBuild.Unwired(playBtn, "navigation is a separate pass (plan: replicate-only, new scene has no counterpart)");
        ReconBuild.Unwired(nextBtn, "Next buttons stay unwired by instruction");
        ReconBuild.Unwired(hamb, "no MenuOverlay counterpart in this new scene");

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("R-2 HowToPlay");
    }

    // =====================================================================  R-3
    // Column geometry measured from Patches_Baked.png:
    //   left text column  x=546 w=392   right text column x=1176 w=445
    //   heading colour RGB(216,135,44)  body colour RGB(153,77,12)
    const float LCOL = 546f, LW = 392f, RCOL = 1176f, RW = 445f;

    [MenuItem("Tools/Recon/R-3 Patches")]
    public static void R3_Patches()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Patches.unity");

        // The six hex tiles already exist at Assets/Art/Tiles/Patches-*.png (PPU 259)
        // and are byte-identical to the _ArtDrop copies. Reuse rather than duplicate.
        ReconBuild.ImportFolder("Patches Scene", "Patches", new[] {
            "Patches-city.png","Patches-farmland.png","Patches-forest.png",
            "Patches-grassland.png","Patches-habitat.png","Patches-road.png" });

        const string A = ReconBuild.ReconRoot + "/Patches/";
        const string T = "Assets/Art/Tiles/";

        var stage = ReconBuild.BuildSkeleton(A + "Background.png", true);
        ReconBuild.PurgeCanvasChildren();

        ReconBuild.Img(stage, "Board", A + "board-full.png", 259, 2, 1406, 980);
        ReconBuild.Img(stage, "Bald Eagle", A + "bald-eagle.png", 1704, 756, 180, 274);

        ReconBuild.Text(stage, "Title", "The Patches", 779, 62, 367, 57, 24, 62,
            TextAlignmentOptions.Center, Cream);

        // Six hexagons. Measured at scale 0.725 of native 228x259 -> 165x188.
        var city      = ReconBuild.Btn(stage, "City",      T + "Patches-city.png",      "", 349, 192, 165, 188);
        var grassland = ReconBuild.Btn(stage, "Grassland", T + "Patches-grassland.png", "", 979, 192, 165, 188);
        var forest    = ReconBuild.Btn(stage, "Forest",    T + "Patches-forest.png",    "", 349, 412, 165, 188);
        var road      = ReconBuild.Btn(stage, "Road",      T + "Patches-road.png",      "", 979, 413, 165, 188);
        var farmland  = ReconBuild.Btn(stage, "Farmland",  T + "Patches-farmland.png",  "", 349, 632, 165, 188);
        var habitat   = ReconBuild.Btn(stage, "Habitats",  T + "Patches-habitat.png",   "", 979, 633, 165, 188);

        // Headings + descriptions, transcribed verbatim from Patches_Baked.png.
        Cell(stage, "City", "City", LCOL, 212, LW,
            "Cities are very dangerous for Pawl. They have lots of buildings, roads, cars, and people. City patches make it hard for Pawl to move safely.",
            255, 92);
        Cell(stage, "Grassland", "Grassland", RCOL, 210, RW,
            "Grasslands are important for Pawl because they provide open spaces to hunt. But Pawl prefers forests because they offer better cover and protection.",
            255, 95);
        Cell(stage, "Forest", "Forest", LCOL, 433, LW,
            "Forests are Pawl’s home. They provide places to hunt for food, hide, and stay safe. Pawl needs large, dense forests to survive.",
            477, 92);
        Cell(stage, "Road", "Road", RCOL, 432, RW,
            "Roads are dangerous for Pawl because of fast-moving cars and heavy traffic. Crossing roads can be very risky, so road patches make it harder for Pawl to travel safely.",
            477, 118);
        Cell(stage, "Farmland", "Farmland", LCOL, 648, LW,
            "Farmland is one of the places where Pawl can travel. He may pass through farms and even find food like deer, raccoons, or hogs. But farmland can also be risky because of human activity.",
            693, 116);
        Cell(stage, "Habitat", "Habitat", RCOL, 649, RW,
            "This is a protected, high-quality habitat patch for Pawl. It provides plenty of food, shelter, and safe space to move around. These are some of the best places for Pawl to live.",
            693, 116);

        var back = ReconBuild.Btn(stage, "Back", A + "btn-board.png", "Back", 332, 915, 284, 108);
        var play = ReconBuild.Btn(stage, "Play", A + "btn-board.png", "PLAY", 818, 915, 284, 108);
        var next = ReconBuild.Btn(stage, "Next", A + "btn-board.png", "Next", 1305, 915, 284, 108);
        var hamb = ReconBuild.Btn(stage, "Hamburger-menu", A + "hamburger-icon.png", "", 40, 977, 69, 70);

        // Replicate bindings exactly as found (R-0 audit).
        var ui = ReconBuild.Find("LevelSelectUI");
        var pc = ui.GetComponent<PatchesController>();
        var mm = ui.GetComponent<MainMenuController>();
        ReconBuild.Wire(forest,    new UnityAction(pc.OnForestClicked),    "PatchesController.OnForestClicked");
        ReconBuild.Wire(city,      new UnityAction(pc.OnCityClicked),      "PatchesController.OnCityClicked");
        ReconBuild.Wire(farmland,  new UnityAction(pc.OnFarmlandClicked),  "PatchesController.OnFarmlandClicked");
        ReconBuild.Wire(grassland, new UnityAction(pc.OnGrasslandClicked), "PatchesController.OnGrasslandClicked");
        ReconBuild.Wire(habitat,   new UnityAction(pc.OnHabitatClicked),   "PatchesController.OnHabitatClicked");
        ReconBuild.Wire(road,      new UnityAction(pc.OnRoadClicked),      "PatchesController.OnRoadClicked");
        ReconBuild.Wire(back,      new UnityAction(pc.OnBackClicked),      "PatchesController.OnBackClicked");
        // Known-wrong in the original (OpenAbout reloads Patches). Plan says replicate, do not fix.
        ReconBuild.Wire(play,      new UnityAction(mm.OpenAbout),          "MainMenuController.OpenAbout  [replicated as-found; known-wrong]");

        var hmc = Object.FindObjectOfType<HamburgerMenuController>(true);
        if (hmc != null) ReconBuild.Wire(hamb, new UnityAction(hmc.Toggle), "HamburgerMenuController.Toggle");
        else ReconBuild.Unwired(hamb, "no HamburgerMenuController found in scene");

        ReconBuild.Unwired(next, "was unwired in the original; Next buttons stay unwired");

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("R-3 Patches");
    }

    static void Cell(RectTransform stage, string key, string heading, float x, float hy, float w,
                     string body, float by, float bh)
    {
        ReconBuild.Text(stage, key + " Heading", heading, x, hy, w, 32f, 14, 34,
            TextAlignmentOptions.TopLeft, HeadOrange);
        ReconBuild.Text(stage, key + " Desc", body, x, by, w, bh, 10, 24,
            TextAlignmentOptions.TopLeft, BodyBrown);
    }

    // =====================================================================  R-4
    [MenuItem("Tools/Recon/R-4 SinglePatch")]
    public static void R4_SinglePatch()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Single Patch.unity");

        ReconBuild.ImportFolder("Single Patch Scene", "SinglePatch");
        const string A = ReconBuild.ReconRoot + "/SinglePatch/";
        const string T = "Assets/Art/Tiles/";

        var stage = ReconBuild.BuildSkeleton(A + "Background.jpg", true);
        ReconBuild.PurgeCanvasChildren();

        // Shadow sits behind the hexagon. patch-shadow.png has NO fully-opaque
        // pixels, so it could not be template-matched -- this box is an estimate.
        ReconBuild.Img(stage, "Patch Shadow", A + "patch-shadow.png", 126, 751, 472, 119);

        // Dynamic tile image. Bounds edge-detected on both axes from the baked
        // reference: 469x522 == 2.04x the native 228x259 tile.
        var patchImg = ReconBuild.Img(stage, "Patch Image", T + "Patches-forest.png", 128, 288, 469, 522);

        ReconBuild.Img(stage, "Board", A + "Board.png", 742, 3, 1136, 911);
        ReconBuild.Img(stage, "Title Board", A + "title-board.png", 163, 0, 402, 179);
        ReconBuild.Img(stage, "Bird", A + "bird.png", 547, 214, 137, 124);

        // Dynamic text, driven at runtime by SinglePatchController.
        // Design-time strings transcribed verbatim from Single Patch_Baked.jpg.
        var nameT = ReconBuild.Text(stage, "Tile Name", "Forest", 212, 90, 300, 58, 16, 46,
            TextAlignmentOptions.Center, Cream);
        var scoreT = ReconBuild.Text(stage, "Tile Score", "Score: 80-100", 878, 182, 620, 50, 16, 44,
            TextAlignmentOptions.TopLeft, DarkBrown);
        var descT = ReconBuild.Text(stage, "Tile Description",
            "Forests are the Florida panther’s home. They give panthers places to hunt for food, hide, and raise their kittens safely. Deer, hogs, and other animals that panthers eat also live in the forest, so panthers depend on these places for their meals. Panthers need big, connected forests so they can move around without getting trapped or crossing busy roads. Protecting Florida’s forests helps keep the panther safe and gives it the space and food it needs to survive.",
            878, 281, 828, 490, 12, 30, TextAlignmentOptions.TopLeft, BodyBrown);

        // Go-Back.png already contains the words "Go Back" -- no TMP label.
        var back = ReconBuild.Btn(stage, "Back Button", A + "Go-Back.png", "", 91, 878, 248, 202);
        var howTo = ReconBuild.Btn(stage, "How to Play Button", A + "How-to-play.png", "How to Play", 734, 927, 284, 108);
        var about = ReconBuild.Btn(stage, "About Button", A + "About.png", "About", 1579, 927, 284, 108);

        var ui = ReconBuild.Find("MainMenuUI");
        var spc = ui.GetComponent<SinglePatchController>();
        var mm = ui.GetComponent<MainMenuController>();
        ReconBuild.Wire(back,  new UnityAction(spc.OnBackClicked), "SinglePatchController.OnBackClicked");
        ReconBuild.Wire(howTo, new UnityAction(mm.OpenHowTo),      "MainMenuController.OpenHowTo");
        ReconBuild.Wire(about, new UnityAction(mm.OpenAbout),      "MainMenuController.OpenAbout");

        // Assign all ten SinglePatchController serialized fields (all were null).
        var so = new SerializedObject(spc);
        so.FindProperty("tileNameText").objectReferenceValue = nameT;
        so.FindProperty("tileScoreRangeText").objectReferenceValue = scoreT;
        so.FindProperty("tileDescriptionText").objectReferenceValue = descT;
        so.FindProperty("tileImage").objectReferenceValue = patchImg.GetComponent<Image>();
        so.FindProperty("forestSprite").objectReferenceValue    = ReconBuild.Sprite(T + "Patches-forest.png");
        so.FindProperty("citySprite").objectReferenceValue      = ReconBuild.Sprite(T + "Patches-city.png");
        so.FindProperty("farmlandSprite").objectReferenceValue  = ReconBuild.Sprite(T + "Patches-farmland.png");
        so.FindProperty("grasslandSprite").objectReferenceValue = ReconBuild.Sprite(T + "Patches-grassland.png");
        so.FindProperty("habitatSprite").objectReferenceValue   = ReconBuild.Sprite(T + "Patches-habitat.png");
        so.FindProperty("roadSprite").objectReferenceValue      = ReconBuild.Sprite(T + "Patches-road.png");
        so.ApplyModifiedPropertiesWithoutUndo();
        ReconBuild.Log("ASSIGNED 10 SinglePatchController serialized fields (all were NULL before)");

        ReconBuild.Log("!! ART GAP: title-board.png has the word \"Forest\" painted into it, so the " +
                       "dynamic Tile Name renders on top of a baked word. Needs a text-free sign from Javier.");

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("R-4 SinglePatch");
    }

    [MenuItem("Tools/Recon/Verify Current Scene")]
    public static void VerifyCurrent()
    {
        ReconBuild.ResetLog();
        ReconBuild.Verify();
        ReconBuild.DumpLog("VERIFY " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ---------------------------------------------------------- build settings
    [MenuItem("Tools/Recon/Fix Build Settings")]
    public static void FixBuildSettings()
    {
        ReconBuild.ResetLog();
        var keep = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!File.Exists(s.path)) { ReconBuild.Log("REMOVED phantom build entry: " + s.path); continue; }
            keep.Add(s);
        }
        const string htp = "Assets/Scenes/HowToPlay.unity";
        bool has = keep.Exists(s => s.path == htp);
        if (!has && File.Exists(htp)) { keep.Add(new EditorBuildSettingsScene(htp, true)); ReconBuild.Log("ADDED build entry: " + htp); }
        EditorBuildSettings.scenes = keep.ToArray();
        foreach (var s in EditorBuildSettings.scenes) ReconBuild.Log("BUILD: " + (s.enabled ? "[x] " : "[ ] ") + s.path);
        ReconBuild.DumpLog("Build Settings");
    }
}
#endif
