using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseUIPresenter : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (!gameManager) gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        if (!overlayPanel) overlayPanel = gameObject;
    }

    private void OnEnable()
    {
        if (pauseButton) pauseButton.onClick.AddListener(OnPauseClicked);
        if (resumeButton) resumeButton.onClick.AddListener(OnResumeClicked);
        if (restartButton) restartButton.onClick.AddListener(OnRestartClicked);
        if (quitButton) quitButton.onClick.AddListener(OnQuitClicked);

        if (gameManager)
        {
            gameManager.OnGamePaused += HandleGamePaused;
            gameManager.OnGameStarted += HandleGameStarted;
            gameManager.OnGameEnded += HandleGameEnded;
        }
    }

    private void OnDisable()
    {
        if (pauseButton) pauseButton.onClick.RemoveListener(OnPauseClicked);
        if (resumeButton) resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (restartButton) restartButton.onClick.RemoveListener(OnRestartClicked);
        if (quitButton) quitButton.onClick.RemoveListener(OnQuitClicked);

        if (gameManager)
        {
            gameManager.OnGamePaused -= HandleGamePaused;
            gameManager.OnGameStarted -= HandleGameStarted;
            gameManager.OnGameEnded -= HandleGameEnded;
        }
    }

    private void HandleGamePaused(bool isPaused)
    {
        if (overlayPanel) overlayPanel.SetActive(isPaused);
    }

    private void HandleGameStarted()
    {
        if (overlayPanel) overlayPanel.SetActive(false);
    }

    private void HandleGameEnded()
    {
        if (overlayPanel) overlayPanel.SetActive(false);
    }

    private void OnPauseClicked()
    {
        if (gameManager) gameManager.PauseGame();
    }

    private void OnResumeClicked()
    {
        if (gameManager) gameManager.ResumeGame();
    }

    private void OnRestartClicked()
    {
        if (overlayPanel) overlayPanel.SetActive(false);
        if (gameManager) gameManager.RestartGame();
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
