using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Called by the Play button
    public void GoToLevelSelect() => SceneLoader.LoadLevelSelect();

    // Placeholders for future WebGL routing; safe in Editor/Standalone.
    public void OpenHowTo()  { Debug.Log("TODO: Open /how-to (web)"); }
    public void OpenAbout()  { Debug.Log("TODO: Open /about (web)"); }
    public void OpenHallOfFame() { Debug.Log("TODO: Open /hall-of-fame (web)"); }
}


