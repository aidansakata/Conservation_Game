using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void LoadMainMenu()    => SceneManager.LoadScene("MainMenu");
    public static void LoadLevelSelect() => SceneManager.LoadScene("LevelSelect");
    public static void LoadGridScene()   => SceneManager.LoadScene("Grid_Scene");
}


