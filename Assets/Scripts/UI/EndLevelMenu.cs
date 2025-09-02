using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndLevelMenu : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Canvas endCanvas;
    [SerializeField] private TMP_Text endScoreText;
    [SerializeField] private Text mainScoreText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button quitButton;

    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (retryButton != null) retryButton.onClick.AddListener(RestartLevel);
        if (nextButton != null) nextButton.onClick.AddListener(NextLevel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);

        if (endCanvas != null) endCanvas.gameObject.SetActive(false);
        if (mainCanvas != null) mainCanvas.gameObject.SetActive(true);
    }

    public void ShowEndCanvas()
    {
        if (mainCanvas != null) mainCanvas.gameObject.SetActive(false);
        if (endCanvas != null) endCanvas.gameObject.SetActive(true);
        if (endScoreText != null && mainScoreText != null) endScoreText.text = mainScoreText.text;
    }

    private void NextLevel()
    {
        if (endCanvas != null) endCanvas.gameObject.SetActive(false);
        if (mainCanvas != null) mainCanvas.gameObject.SetActive(true);
        if (gridManager != null)
        {
            gridManager.NextLevel();
        }
    }

    private void RestartLevel()
    {
        if (endCanvas != null) endCanvas.gameObject.SetActive(false);
        if (mainCanvas != null) mainCanvas.gameObject.SetActive(true);
        if (gridManager != null)
        {
            gridManager.LoadLevel(GameState.SelectedLevel);
        }
    }

    private void QuitToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
