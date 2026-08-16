using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Called by the Play button
    public void GoToLevelSelect() => SceneLoader.LoadLevelSelect();
    public void GoToLevelTemplate() => SceneLoader.LoadLevelTemplate();

    // How to Play now has its own scene; it used to land on Patches.
    // The breadcrumb write stays: Patches' back button still reads it, and this is
    // the only live writer. See GoToPatchesFromHowToPlay for the HowToPlay -> Patches leg.
    public void OpenHowTo()
    {
        GameState.PatchesReturnScene = SceneManager.GetActiveScene().name;
        SceneLoader.LoadHowToPlay();
    }

    // HowToPlay "Next" -> Patches. Sets the breadcrumb so Patches' back button returns
    // to HowToPlay instead of falling through to its "Game-Interface" default.
    public void GoToPatchesFromHowToPlay()
    {
        GameState.PatchesReturnScene = "HowToPlay";
        SceneLoader.LoadPatches();
    }

    public void OpenAbout()  { SceneLoader.LoadPatches(); }
    public void OpenHallOfFame() { Debug.Log("TODO: Open /hall-of-fame (web)"); }
}


