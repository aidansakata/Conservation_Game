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
    static Color TitleBrown = new Color(70f / 255f, 31f / 255f, 0f, 1f);   // LevelSelect title
    static Color LeafGreen = new Color(169f / 255f, 217f / 255f, 42f / 255f, 1f); // pts / Patches
    static Color ResultsTan = new Color(206f / 255f, 150f / 255f, 65f / 255f, 1f);  // popup 'Results' / 'pts'
    static Color ScoreBrown = new Color(116f / 255f, 52f / 255f, 8f / 255f, 1f);   // popup score value

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

    // =====================================================================  R-5
    [MenuItem("Tools/Recon/R-5 LevelSelect")]
    public static void R5_LevelSelect()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity");

        ReconBuild.ImportFolder("LevelSelect Scene", "LevelSelect");
        const string A = ReconBuild.ReconRoot + "/LevelSelect/";

        var stage = ReconBuild.BuildSkeleton(A + "background.png", true);
        ReconBuild.PurgeCanvasChildren();

        ReconBuild.Img(stage, "Board", A + "board.png", 731, 1, 1118, 850);
        ReconBuild.Img(stage, "Pawl Wave", A + "pawl-select-your-level.png", 110, 238, 550, 714);
        ReconBuild.Img(stage, "Butterfly", A + "butterfly.png", 100, 88, 113, 107);
        ReconBuild.Img(stage, "Tortoise", A + "turtle.png", 771, 774, 207, 178);

        ReconBuild.Text(stage, "Title", "Select Your Level", 1022, 165, 580, 55, 20, 58,
            TextAlignmentOptions.Center, TitleBrown);
        ReconBuild.Text(stage, "Caption 1", "Select a level and begin your adventure with Pawl.",
            960, 600, 680, 92, 14, 38, TextAlignmentOptions.Top, BodyBrown);
        ReconBuild.Text(stage, "Caption 2", "Level difficulty increases with the level number.",
            900, 712, 800, 40, 12, 28, TextAlignmentOptions.Top, BodyBrown);

        // Ten level circles. One 123x123 sprite reused; numerals are TMP.
        float[] row1 = { 936, 1090, 1246, 1400, 1556 };
        float[] row2 = { 932, 1088, 1246, 1398, 1554 };
        var ui = ReconBuild.Find("LevelSelectUI");
        var lsc = ui.GetComponent<LevelSelectController>();

        for (int i = 0; i < 5; i++)
        {
            int n = i + 1;
            var b = ReconBuild.Btn(stage, "Level " + n, A + "Select-Your-Level-circle.png",
                n.ToString("00"), row1[i], 274, 123, 123);
            ReconBuild.WireInt(b, new UnityAction<int>(lsc.OnLevelButtonPressed), n,
                "LevelSelectController.OnLevelButtonPressed");
        }
        for (int i = 0; i < 5; i++)
        {
            int n = i + 6;
            var b = ReconBuild.Btn(stage, "Level " + n, A + "Select-Your-Level-circle.png",
                n.ToString("00"), row2[i], 437, 123, 123);
            ReconBuild.WireInt(b, new UnityAction<int>(lsc.OnLevelButtonPressed), n,
                "LevelSelectController.OnLevelButtonPressed");
        }

        var hamb = ReconBuild.Btn(stage, "Hamburger-menu", A + "hamburger-icon.png", "", 41, 979, 69, 70);
        ReconBuild.Unwired(hamb, "no MenuOverlay instance in LevelSelect and no existing binding to replicate");

        ReconBuild.Log("NOTE: the new LevelSelect art has no Home / How to Play / About buttons. " +
                       "Those three alpha-0 hitboxes (BackToMenu, OpenHowTo, OpenAbout) are retired, " +
                       "so LevelSelect currently has no route back to MainMenu. Presumed to move into " +
                       "the hamburger menu, whose four destinations are the deliberately-unresolved question.");

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("R-5 LevelSelect");
    }

    // =====================================================================  R-6
    // GAMEPLAY IS OFF LIMITS: Grid, Tilemap, GridManager and its serialized refs,
    // GridSizePreset transforms, Main Camera, EventSystem, post-processing.
    // The Canvas stays Screen Space - Camera; BuildSkeleton never changes render mode.
    [MenuItem("Tools/Recon/R-6 GameInterface")]
    public static void R6_GameInterface()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.OpenScene("Assets/Scenes/Game-Interface.unity");

        ReconBuild.ImportFolder("Game-Interface Scene", "GameInterface");
        const string A = ReconBuild.ReconRoot + "/GameInterface/";

        var canvasBefore = ReconBuild.Find("Canvas").GetComponent<Canvas>();
        var modeBefore = canvasBefore.renderMode;
        var camBefore = canvasBefore.worldCamera;

        var stage = ReconBuild.BuildSkeleton(A + "background.png", false);

        // ---- capture the live gameplay references BEFORE deleting anything ----
        var gameManager = ReconBuild.Find("GameManager");
        var tf = gameManager != null ? gameManager.GetComponent<TileFunctions>() : null;
        var tfSo = tf != null ? new SerializedObject(tf) : null;
        Font legacyFont = null;
        if (tfSo != null)
        {
            var oldScore = tfSo.FindProperty("scoreText").objectReferenceValue as Text;
            if (oldScore != null) legacyFont = oldScore.font;
            ReconBuild.Log("FOUND TileFunctions on GameManager; captured legacy font = " +
                (legacyFont != null ? legacyFont.name : "NULL"));
        }

        // Budget Warning is GridManager.budgetWarningText -- GridManager's serialized
        // refs are off limits, so rescue the object itself rather than re-assigning.
        var warn = ReconBuild.Find("Budget Warning");
        if (warn != null)
        {
            warn.transform.SetParent(stage, false);
            ReconBuild.AnchorPx(warn, 610, 470, 700, 110);
            ReconBuild.Log("RESCUED 'Budget Warning' (GridManager.budgetWarningText) -> Stage, re-anchored");
        }

        // InfoPanel: inactive, and NOTHING in Assets/Scripts references it. Classification
        // is 'orphaned legacy UI', so it is carried over untouched rather than deleted.
        var info = ReconBuild.Find("InfoPanel");
        if (info != null)
        {
            info.transform.SetParent(stage, false);
            ReconBuild.Log("PRESERVED 'InfoPanel' untouched (inactive, orphaned, absent from new art) -> reparented to Stage");
        }

        // ---- new art ----
        ReconBuild.Img(stage, "Main Board", A + "main-board.png", 400, 0, 1121, 1043);
        ReconBuild.Img(stage, "Level Score Boards", A + "level-score-boards.png", 0, 0, 401, 568);
        ReconBuild.Img(stage, "Logo", A + "logo-interface.png", 1561, 91, 336, 232);
        ReconBuild.Img(stage, "Pawl", A + "pawl-interface.png", 87, 568, 236, 383);
        ReconBuild.Img(stage, "Grass", A + "grass.png", 1574, 794, 292, 143);

        // ---- values ----
        // scoreText/budgetText must stay UnityEngine.UI.Text: TileFunctions declares
        // them as Text and this plan forbids C# changes. So they are legacy Text, not TMP.
        var scoreGo = ReconBuild.Child(stage, "Score Value");
        ReconBuild.AnchorPx(scoreGo, 139, 311, 70, 50);
        var scoreTxt = scoreGo.GetComponent<Text>() ?? scoreGo.AddComponent<Text>();
        scoreTxt.text = "60"; scoreTxt.color = Cream; scoreTxt.alignment = TextAnchor.MiddleCenter;
        scoreTxt.font = legacyFont; scoreTxt.fontSize = 40; scoreTxt.resizeTextForBestFit = true;
        scoreTxt.resizeTextMinSize = 10; scoreTxt.resizeTextMaxSize = 48; scoreTxt.raycastTarget = false;

        var budgetGo = ReconBuild.Child(stage, "Budget Value");
        ReconBuild.AnchorPx(budgetGo, 134, 436, 69, 50);
        var budgetTxt = budgetGo.GetComponent<Text>() ?? budgetGo.AddComponent<Text>();
        budgetTxt.text = "03"; budgetTxt.color = Cream; budgetTxt.alignment = TextAnchor.MiddleCenter;
        budgetTxt.font = legacyFont; budgetTxt.fontSize = 40; budgetTxt.resizeTextForBestFit = true;
        budgetTxt.resizeTextMinSize = 10; budgetTxt.resizeTextMaxSize = 48; budgetTxt.raycastTarget = false;

        ReconBuild.Text(stage, "Level Label", "Level 01", 107, 147, 199, 46, 16, 46,
            TextAlignmentOptions.Center, Cream);
        ReconBuild.Text(stage, "Pts Label", "pts", 231, 338, 48, 26, 10, 26,
            TextAlignmentOptions.Center, LeafGreen);
        ReconBuild.Text(stage, "Patches Label", "Patches", 217, 451, 90, 22, 10, 24,
            TextAlignmentOptions.Center, LeafGreen);

        // ---- buttons ----
        var howTo  = ReconBuild.Btn(stage, "How to Play Button", A + "how-to-play-btn.png", "How to Play", 1592, 392, 266, 100);
        var reset  = ReconBuild.Btn(stage, "Reset Button",       A + "reset-game-btn.png",  "Reset Game",  1592, 508, 266, 101);
        var hint   = ReconBuild.Btn(stage, "Hint Button",        A + "pawls-hint-btn.png",  "Pawl’s Hint", 1592, 624, 266, 101);
        var submit = ReconBuild.Btn(stage, "Submit Button",      A + "submit-btn.png",      "SUBMIT",      1582, 896, 286, 109);
        var hamb   = ReconBuild.Btn(stage, "Hamburger-menu",     A + "hamburger-menu.png",  "",            42, 979, 69, 70);

        var mm = ReconBuild.Find("MainMenuUI").GetComponent<MainMenuController>();
        ReconBuild.Wire(howTo, new UnityAction(mm.OpenHowTo), "MainMenuController.OpenHowTo");
        var hmc = Object.FindObjectOfType<HamburgerMenuController>(true);
        if (hmc != null) ReconBuild.Wire(hamb, new UnityAction(hmc.Toggle), "HamburgerMenuController.Toggle");
        else ReconBuild.Unwired(hamb, "no HamburgerMenuController found");

        // Reset / Hint / Submit carry no persistent onClick: TileFunctions binds them at
        // runtime through serialized fields. Restore those to the rebuilt buttons.
        ReconBuild.Unwired(reset,  "bound at runtime by TileFunctions.resetButton");
        ReconBuild.Unwired(hint,   "bound at runtime by TileFunctions.hintButton");
        ReconBuild.Unwired(submit, "bound at runtime by TileFunctions.submitButton");

        // ---- retire the old container ----
        var old = GameObject.Find("Canvas/Menu container");
        if (old != null) { Object.DestroyImmediate(old); ReconBuild.Log("DELETED 'Menu container' (old baked bg, covers, panels, hitboxes)"); }

        // ---- restore TileFunctions references to the rebuilt objects ----
        if (tfSo != null)
        {
            tfSo.Update();
            tfSo.FindProperty("scoreText").objectReferenceValue = scoreTxt;
            tfSo.FindProperty("budgetText").objectReferenceValue = budgetTxt;
            tfSo.FindProperty("resetButton").objectReferenceValue = reset.GetComponent<Button>();
            tfSo.FindProperty("submitButton").objectReferenceValue = submit.GetComponent<Button>();
            tfSo.FindProperty("hintButton").objectReferenceValue = hint.GetComponent<Button>();
            tfSo.ApplyModifiedPropertiesWithoutUndo();
            ReconBuild.Log("RESTORED TileFunctions: scoreText, budgetText, resetButton, submitButton, hintButton");
            var chk = new SerializedObject(tf);
            foreach (var f in new[] { "Tilemap", "scoreText", "budgetText", "resetButton", "submitButton", "hintButton" })
            {
                var pr = chk.FindProperty(f);
                ReconBuild.Log(string.Format("   TileFunctions.{0} = {1}", f,
                    pr != null && pr.objectReferenceValue != null ? pr.objectReferenceValue.name : "NULL <<<"));
            }
        }

        var canvasAfter = ReconBuild.Find("Canvas").GetComponent<Canvas>();
        ReconBuild.Log(string.Format("CANVAS renderMode {0} -> {1} ({2}), camera {3} -> {4}",
            modeBefore, canvasAfter.renderMode, modeBefore == canvasAfter.renderMode ? "UNCHANGED" : "CHANGED!!",
            camBefore != null ? camBefore.name : "null",
            canvasAfter.worldCamera != null ? canvasAfter.worldCamera.name : "null"));
        ReconBuild.Log("UNTOUCHED: Grid, Tilemap, GridManager (+ all serialized refs), GridSizePreset, Main Camera, EventSystem, globalvolume, MenuOverlay.");
        ReconBuild.Log("NOTE: the hex grid is world-space, framed by the orthographic Main Camera, while the board art " +
                       "letterboxes with Stage. At non-16:9 aspects the two can drift apart. Not fixable without touching " +
                       "the camera or grid, which this phase forbids.");
        ReconBuild.Log("NOTE: runtime PopupCanvas (GridManager) and MenuOverlay both claim sortingOrder 100. Left as-is.");

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("R-6 GameInterface");
    }

    // =====================================================================  R-7
    // The submit popup is built at RUNTIME in GridManager. This authors a prefab
    // matching the baked reference. GridManager is deliberately NOT modified.
    [MenuItem("Tools/Recon/R-7 PopupOverlay")]
    public static void R7_PopupOverlay()
    {
        ReconBuild.ResetLog();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // All eight Pop-Up components already live in Assets/Art/Popup/, are
        // byte-identical to the _ArtDrop copies, and are already referenced by
        // GridManager's popup*Sprite fields. Reuse instead of importing a duplicate set.
        const string P = "Assets/Art/Popup/";
        ReconBuild.Log("IMPORT: skipped all 8 Pop-Up components -- already present and wired at " + P);

        var root = new GameObject("PopupOverlay", typeof(RectTransform), typeof(Canvas),
                                  typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // matches the runtime popup it is meant to replace
        var cs = root.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;

        // An overlay must not hide the scene beneath, so the skeleton's opaque
        // Letterbox becomes a semi-transparent Scrim. Stage is unchanged.
        var scrim = ReconBuild.Child(root.transform, "Scrim");
        ReconBuild.Anchor(scrim, 0, 0, 1, 1);
        var si = scrim.AddComponent<Image>();
        si.color = new Color(0f, 0f, 0f, 0.66f);
        si.raycastTarget = true;

        var stageGo = ReconBuild.Child(root.transform, "Stage");
        var stage = ReconBuild.Anchor(stageGo, 0, 0, 1, 1);
        stage.pivot = new Vector2(0.5f, 0.5f);
        var arf = stageGo.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 1.7778f;

        ReconBuild.Img(stage, "Board", P + "Board.png", 274, 13, 1406, 936);
        ReconBuild.Img(stage, "Logo", P + "Logo.png", 753, 28, 415, 291);
        ReconBuild.Img(stage, "Pawl Running", P + "Pawl-Running.png", 995, 380, 525, 419);
        ReconBuild.Img(stage, "Butterfly", P + "Butterfly.png", 1389, 228, 81, 86);

        // Transcribed verbatim from Pop-Up_Baked.jpg; colours sampled from the art.
        ReconBuild.Text(stage, "Results Heading", "Results", 560, 445, 215, 53, 16, 52,
            TextAlignmentOptions.Center, ResultsTan);
        ReconBuild.Text(stage, "Results Caption", "Your corridor was tested by Pawl",
            487, 521, 360, 80, 14, 36, TextAlignmentOptions.Top, BodyBrown);
        ReconBuild.Text(stage, "Score Value", "930", 554, 638, 150, 79, 20, 76,
            TextAlignmentOptions.Right, ScoreBrown);
        ReconBuild.Text(stage, "Pts Label", "pts", 710, 668, 70, 46, 12, 34,
            TextAlignmentOptions.Left, ResultsTan);

        // Every Pop-Up plate already contains its own label, so these are icon buttons.
        var best  = ReconBuild.Btn(stage, "View Best Corridor", P + "view-best-corridor.png", "", 337, 865, 374, 108);
        var again = ReconBuild.Btn(stage, "Play Again",         P + "play-again.png",         "", 758, 836, 403, 156);
        var hof   = ReconBuild.Btn(stage, "Add to Hall of Fame",P + "hall-of-fame.png",       "", 1215, 865, 374, 108);
        var close = ReconBuild.Btn(stage, "Close Button",       P + "Close-Popup.png",        "", 1817, 45, 67, 63);

        ReconBuild.Unwired(best,  "GridManager builds the live popup in code; prefab is not wired to it");
        ReconBuild.Unwired(again, "as above");
        ReconBuild.Unwired(hof,   "as above");
        ReconBuild.Unwired(close, "as above");

        ReconBuild.Verify(root);

        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/UI/PopupOverlay.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        ReconBuild.Log("SAVED Assets/Prefabs/UI/PopupOverlay.prefab");

        ReconBuild.Log("TO WIRE THIS UP LATER (NOT done -- it is a C# change and out of scope):");
        ReconBuild.Log("  GridManager.cs CreateSubmitPopup(), lines 663-822, builds the popup by hand.");
        ReconBuild.Log("  Replace that body with: Instantiate the prefab under popupCanvas.transform,");
        ReconBuild.Log("  assign _submitPopup = instance, and bind _resultText / _scoreText to the");
        ReconBuild.Log("  instance's 'Results Caption' and 'Score Value' TMP components; wire the four");
        ReconBuild.Log("  buttons to OnPlayAgainClicked / OnShowBestCorridorClicked / hall-of-fame / close.");
        ReconBuild.Log("  The helper CreatePopupButton() (lines ~824-845) then becomes dead code.");
        ReconBuild.Log("  Until then the prefab does not appear in game. That is expected.");
        ReconBuild.Log("NOTE: this prefab uses sortingOrder 100, the same value as the runtime PopupCanvas");
        ReconBuild.Log("  it is meant to replace, and as MenuOverlay. Left as-is per plan.");

        ReconBuild.DumpLog("R-7 PopupOverlay");
    }

    // =====================================================================  R-8
    // Rebuilt IN PLACE via LoadPrefabContents so the root GameObject and its
    // HamburgerMenuController keep their fileIDs -- the Patches and Game-Interface
    // hamburger buttons wire to that component and would otherwise break.
    [MenuItem("Tools/Recon/R-8 MenuOverlay")]
    public static void R8_MenuOverlay()
    {
        ReconBuild.ResetLog();
        const string path = "Assets/Prefabs/MenuOverlay.prefab";

        // No images/ folder. Borrows from two other folders (R-0 finding: the logo is
        // logo-interface.png, NOT logo-welcome.png -- matched exact, MAE 0.00).
        const string LOGO = ReconBuild.ReconRoot + "/GameInterface/logo-interface.png";
        const string CLOSE = "Assets/Art/Popup/Close-Popup.png";
        ReconBuild.Log("IMPORT: none. Borrows " + LOGO + " and " + CLOSE);

        var root = PrefabUtility.LoadPrefabContents(path);

        var hmc = root.GetComponent<HamburgerMenuController>();
        ReconBuild.Log("PRESERVED HamburgerMenuController on prefab root: " + (hmc != null ? "found" : "MISSING!"));

        var canvas = root.GetComponent<Canvas>();
        ReconBuild.Log(string.Format("Canvas renderMode={0} sortingOrder={1} (unchanged)", canvas.renderMode, canvas.sortingOrder));
        var cs = root.GetComponent<CanvasScaler>();
        if (cs == null) cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;

        // Clear old contents (Panel/CloseButton) but keep the root intact.
        var doomed = new System.Collections.Generic.List<GameObject>();
        foreach (Transform t in root.transform) doomed.Add(t.gameObject);
        foreach (var d in doomed) { ReconBuild.Log("DELETED old overlay child: " + d.name); Object.DestroyImmediate(d); }

        // Panel is the toggled container -- it must be a CHILD, because disabling the
        // root would disable HamburgerMenuController and Toggle() could never re-enable it.
        var panel = ReconBuild.Child(root.transform, "Panel");
        ReconBuild.Anchor(panel, 0, 0, 1, 1);

        var scrim = ReconBuild.Child(panel.transform, "Scrim");
        ReconBuild.Anchor(scrim, 0, 0, 1, 1);
        var si = scrim.GetComponent<Image>() ?? scrim.AddComponent<Image>();
        si.color = new Color(0f, 0f, 0f, 0.66f);   // measured: RGB ~(83,82,78) over the game art
        si.raycastTarget = true;

        var stageGo = ReconBuild.Child(panel.transform, "Stage");
        var stage = ReconBuild.Anchor(stageGo, 0, 0, 1, 1);
        stage.pivot = new Vector2(0.5f, 0.5f);
        var arf = stageGo.GetComponent<AspectRatioFitter>() ?? stageGo.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 1.7778f;

        ReconBuild.Img(stage, "Logo", LOGO, 55, 62, 336, 232);

        // Four left-aligned text buttons. Boxes are measured white-pixel bounds
        // (35px tall, 90px pitch, all starting at x=64). Wired to nothing by instruction.
        float[] ys = { 596, 686, 776, 866 };
        float[] ws = { 156, 168, 166, 164 };
        for (int i = 0; i < 4; i++)
        {
            var b = ReconBuild.Btn(stage, "Menu 0" + (i + 1), null, "Menu 0" + (i + 1), 64, ys[i], ws[i], 35);
            var lab = b.GetComponentInChildren<TextMeshProUGUI>(true);
            lab.alignment = TextAlignmentOptions.Left;
            lab.color = Cream;
            lab.margin = Vector4.zero;
            ReconBuild.Unwired(b, "the four hamburger destinations are a deliberately unresolved decision");
        }

        var close = ReconBuild.Btn(stage, "CloseButton", CLOSE, "", 43, 984, 67, 63);
        if (hmc != null) ReconBuild.Wire(close, new UnityAction(hmc.Close), "HamburgerMenuController.Close");

        // Restore the controller's panel reference to the new container.
        if (hmc != null)
        {
            var so = new SerializedObject(hmc);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();
            var chk = new SerializedObject(hmc).FindProperty("panel").objectReferenceValue;
            ReconBuild.Log("RESTORED HamburgerMenuController.panel -> " + (chk != null ? chk.name : "NULL <<<"));
        }

        ReconBuild.Verify(root);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();
        ReconBuild.Log("SAVED " + path);
        ReconBuild.DumpLog("R-8 MenuOverlay");
    }

    /// Confirms the two scenes that instance MenuOverlay still resolve their
    /// hamburger onClick after the prefab was rebuilt.
    [MenuItem("Tools/Recon/R-8 Verify Overlay Instances")]
    public static void R8_VerifyInstances()
    {
        ReconBuild.ResetLog();
        foreach (var sc in new[] { "Assets/Scenes/Patches.unity", "Assets/Scenes/Game-Interface.unity" })
        {
            EditorSceneManager.OpenScene(sc);
            var hmc = Object.FindObjectOfType<HamburgerMenuController>(true);
            var panel = hmc != null ? new SerializedObject(hmc).FindProperty("panel").objectReferenceValue : null;
            ReconBuild.Log(sc);
            ReconBuild.Log("   HamburgerMenuController: " + (hmc != null ? "present" : "MISSING") +
                           "   panel -> " + (panel != null ? panel.name : "NULL <<<"));
            // Scope to the scene Canvas: MenuOverlay now has a "Stage" of its own.
            var stage = ReconBuild.FindDeep(ReconBuild.Find("Canvas").transform, "Stage");
            foreach (var b in stage.GetComponentsInChildren<Button>(true))
            {
                if (!b.name.ToLower().Contains("hamburger")) continue;
                int n = b.onClick.GetPersistentEventCount();
                for (int i = 0; i < n; i++)
                {
                    var o = b.onClick.GetPersistentTarget(i);
                    ReconBuild.Log("   " + b.name + " onClick -> " +
                        (o != null ? o.GetType().Name : "NULL TARGET <<<") + "." + b.onClick.GetPersistentMethodName(i) + "()");
                }
                if (n == 0) ReconBuild.Log("   " + b.name + " onClick -> NONE <<<");
            }
            ReconBuild.Verify();   // re-run the full checklist post-prefab-rebuild
        }
        ReconBuild.DumpLog("R-8 Overlay Instance Check");
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
