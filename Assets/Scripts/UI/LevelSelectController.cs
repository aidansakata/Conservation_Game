using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    [Tooltip("Optional: default numeric level if string id not provided")]
    public int defaultNumericLevel = 1;

    [Tooltip("Optional: default level id if buttons don’t pass one")]
    public string defaultLevelId = "landscape_1";

    // --- USE THIS FUNCTION FOR YOUR LEVEL 1 BUTTON ---
    public void SelectLevel(int n)
    {
        // 1. Set the numeric level (Triggers the new Random Variation logic in GridManager)
        GameState.SelectedLevel = n;

        // 2. CRITICAL: Clear the String ID. 
        // This forces GridManager.Start() to skip the broken "Path A" and use "Path B".
        GameState.SelectedLevelId = null;

        // 3. Load the Game Scene
        SceneManager.LoadScene("Game-Interface");
    }

    // Keep this for legacy support (or other levels), but don't use it for Level 1
    public void OnLevelButton(string levelId)
    {
        GameState.SelectedLevelId = string.IsNullOrEmpty(levelId) ? defaultLevelId : levelId;
        SceneManager.LoadScene("Game-Interface");
    }

    public void OnLevelButtonPressed(int levelNumber)
    {
        GameState.SelectedLevel = levelNumber;
        SceneManager.LoadScene("LevelTemplate");
    }

    public void StartSelectedLevel()
    {
        GameState.SelectedLevelId = null;
        SceneManager.LoadScene("Game-Interface");
    }

    public void BackToMenu() => SceneManager.LoadScene("MainMenu");
    public void BackToLevelSelect() => SceneManager.LoadScene("LevelSelect");
}