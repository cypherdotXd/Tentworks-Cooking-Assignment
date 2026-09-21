using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoundCompleteUIPresenter : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private GameObject newHighScoreObject;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (!gameManager) gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        if (!overlayPanel) overlayPanel = gameObject;
    }

    private void OnEnable()
    {
        if (playAgainButton) playAgainButton.onClick.AddListener(OnRestartClicked);
        if (restartButton) restartButton.onClick.AddListener(OnRestartClicked);

        if (gameManager)
        {
            gameManager.OnGameEnded += HandleGameEnded;
            gameManager.OnGameStarted += HandleGameStarted;
        }
    }

    private void OnDisable()
    {
        if (playAgainButton) playAgainButton.onClick.RemoveListener(OnRestartClicked);
        if (restartButton) restartButton.onClick.RemoveListener(OnRestartClicked);

        if (gameManager)
        {
            gameManager.OnGameEnded -= HandleGameEnded;
            gameManager.OnGameStarted -= HandleGameStarted;
        }
    }

    private void HandleGameEnded()
    {
        if (overlayPanel) overlayPanel.SetActive(true);

        if (finalScoreText && gameManager)
        {
            finalScoreText.text = $"FINAL SCORE  {gameManager.CurrentScore:0000}";
        }

        if (newHighScoreObject && gameManager)
        {
            newHighScoreObject.SetActive(gameManager.IsNewHighScore);
        }
    }

    private void HandleGameStarted()
    {
        if (overlayPanel) overlayPanel.SetActive(false);
    }

    private void OnRestartClicked()
    {
        if (overlayPanel) overlayPanel.SetActive(false);
        if (gameManager) gameManager.RestartGame();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
