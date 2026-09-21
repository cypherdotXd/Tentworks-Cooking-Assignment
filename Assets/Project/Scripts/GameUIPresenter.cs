using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameUIPresenter : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button startButton;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text timeRemainingText;

    private void Awake()
    {
        if (!gameManager) gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
    }

    private void Start()
    {
        if (!gameManager) gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        RefreshUI();
    }

    private void OnEnable()
    {
        if (startButton) startButton.onClick.AddListener(OnStartButtonClicked);

        HookEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (startButton) startButton.onClick.RemoveListener(OnStartButtonClicked);

        UnhookEvents();
    }

    private void HookEvents()
    {
        if (!gameManager) return;

        gameManager.OnScoreChanged += UpdateScore;
        gameManager.OnHighScoreChanged += UpdateHighScore;
        gameManager.OnTimeUpdated += UpdateTimer;
        gameManager.OnGameStarted += HandleGameStarted;
    }

    private void UnhookEvents()
    {
        if (!gameManager) return;

        gameManager.OnScoreChanged -= UpdateScore;
        gameManager.OnHighScoreChanged -= UpdateHighScore;
        gameManager.OnTimeUpdated -= UpdateTimer;
        gameManager.OnGameStarted -= HandleGameStarted;
    }

    public void OnStartButtonClicked()
    {
        if (startPanel) startPanel.SetActive(false);
        if (gameManager) gameManager.StartGame();
    }

    private void HandleGameStarted()
    {
        if (startPanel) startPanel.SetActive(false);
        RefreshUI();
    }

    private void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = score.ToString();
    }

    private void UpdateHighScore(int highScore)
    {
        if (highScoreText) highScoreText.text = highScore.ToString();
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (!timeRemainingText) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timeRemainingText.text = $"{minutes:00}:{seconds:00}";
    }

    private void RefreshUI()
    {
        if (!gameManager) gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        if (gameManager)
        {
            UpdateScore(gameManager.CurrentScore);
            UpdateHighScore(gameManager.HighScore);
            UpdateTimer(gameManager.TimeRemaining);
        }
    }
}
