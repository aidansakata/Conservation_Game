using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToLevelSelect : MonoBehaviour
{
    public bool clearJsonSelection = true;

    public void Go()
    {
        if (clearJsonSelection) GameState.SelectedLevelId = "";   // optional: avoid stale JSON id
        SceneManager.LoadScene("Level_Select");
    }
}
