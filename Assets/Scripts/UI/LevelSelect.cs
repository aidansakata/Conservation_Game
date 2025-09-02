using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    // Called by each button in your main menu.
    public void SelectLevel1() => SelectLevel(1);
    public void SelectLevel2() => SelectLevel(2);
    public void SelectLevel3() => SelectLevel(3);
    public void SelectLevel4() => SelectLevel(4);
    public void SelectLevel5() => SelectLevel(5);

    private void SelectLevel(int levelNumber)
    {
        GameState.SelectedLevel = levelNumber;
        SceneManager.LoadScene("Grid_Scene");
    }
}
