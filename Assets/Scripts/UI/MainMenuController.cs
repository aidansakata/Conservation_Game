using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Called by the Play button
    public void GoToLevelSelect() => SceneLoader.LoadLevelSelect();
    public void GoToLevelTemplate() => SceneLoader.LoadLevelTemplate();

    // Placeholders for future WebGL routing; safe in Editor/Standalone.
    public void OpenHowTo()
    {
        GameState.PatchesReturnScene = SceneManager.GetActiveScene().name;
        SceneLoader.LoadPatches();
    }
    public void OpenAbout()  { SceneLoader.LoadPatches(); }
    public void OpenHallOfFame() { Debug.Log("TODO: Open /hall-of-fame (web)"); }
}


