using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    [Tooltip("Optional: default numeric level if string id not provided")]
    public int defaultNumericLevel = 1;

    [Tooltip("Optional: default level id if buttons don’t pass one")]
    public string defaultLevelId = "landscape_1";

    public void SelectLevel(int n)
    {
        GameState.SelectedLevel = n;
        SceneLoader.LoadGridScene();
    }

    public void OnLevelButton(string levelId)
    {
        GameState.SelectedLevelId = string.IsNullOrEmpty(levelId) ? defaultLevelId : levelId;
        SceneManager.LoadScene("Grid_Scene");
    }

    public void BackToMenu() => SceneLoader.LoadMainMenu();

    // NEW: Back button handler to go to the LevelSelect scene (not "Level_Select").
    public void BackToLevelSelect()
    {
        SceneManager.LoadScene("LevelSelect");
    }
}
