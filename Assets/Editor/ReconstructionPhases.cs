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
    static Color Brown = new Color(0.36f, 0.20f, 0.09f, 1f);
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

        // Title cap on the board + body copy, transcribed verbatim from the baked ref.
        ReconBuild.Text(stage, "Title", "How to Play", 1063, 40, 430, 100, 20, 72,
            TextAlignmentOptions.Center, Cream);

        ReconBuild.Text(stage, "Body",
            "Pawl the Panther needs to travel from one habitat to another, and he needs your help!\n\n" +
            "Choose land patches to build a safe path for Pawl. But be careful—you can only pick a limited number of patches.\n\n" +
            "Click on a patch to add it to your path and click again to remove it.\n\n" +
            "Watch your patch budget and score as you build Pawl’s path!\n\n" +
            "Each patch gives you points based on how good it is. Help Pawl reach his destination while earning the highest score possible!",
            905, 185, 800, 650, 14, 30, TextAlignmentOptions.TopLeft, Brown);

        var hamb = ReconBuild.Btn(stage, "Hamburger Button", A + "hamburger-icon.png", "", 40, 977, 69, 70);
        hamb.GetComponentInChildren<TextMeshProUGUI>(true).text = " ";

        var playBtn = ReconBuild.Btn(stage, "Play Button", A + "button.png", "PLAY", 808, 915, 284, 108);
        var nextBtn = ReconBuild.Btn(stage, "Next Button", A + "button.png", "Next", 1475, 915, 284, 108);

        ReconBuild.Unwired(playBtn, "navigation is a separate pass (plan: replicate-only, new scene has no counterpart)");
        ReconBuild.Unwired(nextBtn, "Next buttons stay unwired by instruction");
        ReconBuild.Unwired(hamb, "no MenuOverlay counterpart in this new scene");

        ReconBuild.Verify();
        ReconBuild.SaveActive();
        ReconBuild.DumpLog("R-2 HowToPlay");
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
