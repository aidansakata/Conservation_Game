using UnityEngine;

public class LevelSelectController : MonoBehaviour
{
    public void SelectLevel(int n)
    {
        GameState.SelectedLevel = n;
        SceneLoader.LoadGridScene();
    }

    public void BackToMenu() => SceneLoader.LoadMainMenu();
}


