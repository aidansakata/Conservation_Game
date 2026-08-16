// NavWalk.cs — Play-mode walk of the navigation graph (reconstruction_4_plan.md N-9).
// Editor-only test harness. Drives each transition independently and records the scene
// actually reached, rather than sampling one case and generalising.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class NavWalk
{
    class Step
    {
        public string start;      // scene to begin in
        public string button;     // button object name under Stage (or overlay)
        public string expect;     // expected destination scene
        public string note;       // extra label
        public bool viaOverlay;   // open the hamburger panel first, then click inside it
    }

    static readonly List<Step> Steps = new List<Step>();
    static readonly List<string> Results = new List<string>();
    static int _i, _phase, _wait;
    static bool _running;

    static NavWalk() { EditorApplication.update += Tick; }

    [MenuItem("Tools/Nav4/N-9 Start Play Walk")]
    public static void Start()
    {
        if (!Application.isPlaying)
        { Debug.Log("[NavWalk] Enter Play mode first, then run this."); return; }

        Steps.Clear(); Results.Clear();
        Add("MainMenu", "Play Button", "LevelSelect", "1  MainMenu Play");
        Add("MainMenu", "How to Play Button", "HowToPlay", "2  MainMenu How to Play");
        Add("HowToPlay", "Next Button", "Patches", "3  HowToPlay Next");
        foreach (var t in new[] { "City", "Grassland", "Forest", "Road", "Farmland", "Habitats" })
            Add("Patches", t, "Single Patch", "4  Patches tile " + t);
        Add("Single Patch", "Back Button", "Patches", "5  Single Patch Back");
        Add("Patches", "Play", "LevelSelect", "6  Patches Play");
        for (int n = 1; n <= 10; n++)
            Add("LevelSelect", "Level " + n, "Game-Interface", "7  LevelSelect Level " + n);
        Add("Game-Interface", "How to Play Button", "HowToPlay", "8  Game-Interface How to Play");
        foreach (var s in new[] { "HowToPlay", "Patches", "LevelSelect", "Game-Interface" })
            Add(s, "Home", "MainMenu", "9  Hamburger Home from " + s, true);
        Add("Patches", "Back", "?", "10 Patches Back (breadcrumb)");

        _i = 0; _phase = 0; _wait = 0; _running = true;
        Debug.Log("[NavWalk] starting, " + Steps.Count + " steps");
    }

    static void Add(string start, string button, string expect, string note, bool overlay = false)
        => Steps.Add(new Step { start = start, button = button, expect = expect, note = note, viaOverlay = overlay });

    static void Tick()
    {
        if (!_running) return;
        if (!Application.isPlaying) { _running = false; Dump("aborted: left Play mode"); return; }
        if (_wait > 0) { _wait--; return; }
        if (_i >= Steps.Count) { _running = false; Dump("complete"); return; }

        var s = Steps[_i];
        switch (_phase)
        {
            case 0:                                   // get into the start scene
                if (SceneManager.GetActiveScene().name != s.start)
                { SceneManager.LoadScene(s.start); _wait = 4; return; }
                _phase = 1; _wait = 2; return;

            case 1:                                   // reset breadcrumb-sensitive state, click
                if (s.note.StartsWith("10")) { /* leave breadcrumb as the walk left it */ }
                var b = FindButton(s.button, s.viaOverlay);
                if (b == null)
                { Results.Add(string.Format("{0,-42} BUTTON NOT FOUND", s.note)); Advance(); return; }
                if (b.onClick.GetPersistentEventCount() == 0)
                { Results.Add(string.Format("{0,-42} NO PERSISTENT CALL", s.note)); Advance(); return; }
                b.onClick.Invoke();
                _phase = 2; _wait = 5; return;

            case 2:                                   // record where we ended up
                string got = SceneManager.GetActiveScene().name;
                string extra = "";
                if (s.note.StartsWith("4  ")) extra = "  tileType=" + GameState.SelectedTileType;
                if (s.note.StartsWith("7  ")) extra = "  SelectedLevel=" + GameState.SelectedLevel
                                                    + "  SelectedLevelId=" + (string.IsNullOrEmpty(GameState.SelectedLevelId) ? "(null)" : GameState.SelectedLevelId);
                if (s.note.StartsWith("3  ")) extra = "  PatchesReturnScene=" + GameState.PatchesReturnScene;
                if (s.note.StartsWith("10")) extra = "  (breadcrumb was '" + GameState.PatchesReturnScene + "')";
                bool ok = s.expect == "?" || got == s.expect;
                Results.Add(string.Format("{0,-42} -> {1,-16} {2}{3}", s.note, got,
                    s.expect == "?" ? "REPORTED" : (ok ? "PASS" : "FAIL expected " + s.expect), extra));
                Advance(); return;
        }
    }

    static void Advance() { _i++; _phase = 0; _wait = 2; }

    static Button FindButton(string name, bool viaOverlay)
    {
        if (viaOverlay)
        {
            var hmc = Object.FindObjectOfType<HamburgerMenuController>(true);
            if (hmc == null) return null;
            hmc.Open();                                // panel starts inactive by design
        }
        foreach (var b in Object.FindObjectsOfType<Button>(true))
            if (b.name == name) return b;
        return null;
    }

    static void Dump(string why)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== N-9 PLAY WALK (" + why + ") =====");
        foreach (var r in Results) sb.AppendLine(r);
        Debug.Log(sb.ToString());
    }
}
#endif
