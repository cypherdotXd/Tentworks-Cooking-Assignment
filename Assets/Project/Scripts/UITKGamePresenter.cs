using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class UITKGamePresenter : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private UIDocument _uiDocument;

    private Label _timerLabel;
    private Label _scoreLabel;
    private Label _highScoreLabel;
    private Button _pauseButton;

    private VisualElement _startOverlay;
    private Button _startButton;

    private VisualElement _pauseOverlay;
    private Button _resumeButton;
    private Button _restartButton;
    private Button _quitButton;

    private VisualElement _roundCompleteOverlay;
    private Label _finalScoreLabel;
    private VisualElement _newHighScoreBadge;
    private Button _playAgainButton;

    void OnEnable()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        if (root == null) return;

        _timerLabel = root.Q<Label>("timerLabel");
        _scoreLabel = root.Q<Label>("scoreLabel");
        _highScoreLabel = root.Q<Label>("highScoreLabel");
        _pauseButton = root.Q<Button>("pauseButton");

        _startOverlay = root.Q<VisualElement>("startOverlay");
        _startButton = root.Q<Button>("startButton");

        _pauseOverlay = root.Q<VisualElement>("pauseOverlay");
        _resumeButton = root.Q<Button>("resumeButton");
        _restartButton = root.Q<Button>("restartButton");
        _quitButton = root.Q<Button>("quitButton");

        _roundCompleteOverlay = root.Q<VisualElement>("roundCompleteOverlay");
        _finalScoreLabel = root.Q<Label>("finalScoreLabel");
        _newHighScoreBadge = root.Q<VisualElement>("newHighScoreBadge");
        _playAgainButton = root.Q<Button>("playAgainButton");

        if (_startButton != null) _startButton.clicked += OnStartClicked;
        if (_pauseButton != null) _pauseButton.clicked += OnPauseClicked;
        if (_resumeButton != null) _resumeButton.clicked += OnResumeClicked;
        if (_restartButton != null) _restartButton.clicked += OnRestartClicked;
        if (_quitButton != null) _quitButton.clicked += OnQuitClicked;
        if (_playAgainButton != null) _playAgainButton.clicked += OnPlayAgainClicked;

        HookGameEvents();
        RefreshUI();
    }

    void OnDisable()
    {
        if (_startButton != null) _startButton.clicked -= OnStartClicked;
        if (_pauseButton != null) _pauseButton.clicked -= OnPauseClicked;
        if (_resumeButton != null) _resumeButton.clicked -= OnResumeClicked;
        if (_restartButton != null) _restartButton.clicked -= OnRestartClicked;
        if (_quitButton != null) _quitButton.clicked -= OnQuitClicked;
        if (_playAgainButton != null) _playAgainButton.clicked -= OnPlayAgainClicked;

        UnhookGameEvents();
    }

    public void RefreshUI()
    {
        if (_gameManager == null) return;
        UpdateScore(_gameManager.CurrentScore);
        UpdateHighScore(_gameManager.HighScore);
        UpdateTimer(_gameManager.TimeRemaining);
    }

    private void HookGameEvents()
    {
        if (_gameManager == null) return;
        _gameManager.OnScoreChanged += UpdateScore;
        _gameManager.OnHighScoreChanged += UpdateHighScore;
        _gameManager.OnTimeUpdated += UpdateTimer;
        _gameManager.OnGameStarted += HandleGameStarted;
        _gameManager.OnGameEnded += HandleGameEnded;
        _gameManager.OnGamePaused += HandleGamePaused;
    }

    private void UnhookGameEvents()
    {
        if (_gameManager == null) return;
        _gameManager.OnScoreChanged -= UpdateScore;
        _gameManager.OnHighScoreChanged -= UpdateHighScore;
        _gameManager.OnTimeUpdated -= UpdateTimer;
        _gameManager.OnGameStarted -= HandleGameStarted;
        _gameManager.OnGameEnded -= HandleGameEnded;
        _gameManager.OnGamePaused -= HandleGamePaused;
    }

    private void UpdateScore(int score)
    {
        if (_scoreLabel != null)
            _scoreLabel.text = score.ToString("0000");
    }

    private void UpdateHighScore(int highScore)
    {
        if (_highScoreLabel != null)
            _highScoreLabel.text = highScore.ToString("0000");
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (_timerLabel == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        _timerLabel.text = $"{minutes:00}:{seconds:00}";
    }

    private void HandleGameStarted()
    {
        SetVisible(_startOverlay, false);
        SetVisible(_pauseOverlay, false);
        SetVisible(_roundCompleteOverlay, false);
        RefreshUI();
    }

    private void HandleGameEnded()
    {
        SetVisible(_pauseOverlay, false);
        if (_finalScoreLabel != null && _gameManager != null)
            _finalScoreLabel.text = _gameManager.CurrentScore.ToString("0000");
        if (_newHighScoreBadge != null && _gameManager != null)
            SetVisible(_newHighScoreBadge, _gameManager.IsNewHighScore);
        SetVisible(_roundCompleteOverlay, true);
    }

    private void HandleGamePaused(bool isPaused)
    {
        SetVisible(_pauseOverlay, isPaused);
    }

    private void OnStartClicked()
    {
        SetVisible(_startOverlay, false);
        if (_gameManager != null)
            _gameManager.StartGame();
    }

    private void OnPauseClicked()
    {
        if (_gameManager != null)
            _gameManager.PauseGame();
    }

    private void OnResumeClicked()
    {
        if (_gameManager != null)
            _gameManager.ResumeGame();
    }

    private void OnRestartClicked()
    {
        SetVisible(_pauseOverlay, false);
        if (_gameManager != null)
            _gameManager.RestartGame();
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnPlayAgainClicked()
    {
        SetVisible(_roundCompleteOverlay, false);
        if (_gameManager != null)
            _gameManager.RestartGame();
    }

    private void SetVisible(VisualElement element, bool visible)
    {
        if (element == null) return;
        if (visible)
        {
            element.RemoveFromClassList("hidden");
            element.style.display = DisplayStyle.Flex;
        }
        else
        {
            element.AddToClassList("hidden");
            element.style.display = DisplayStyle.None;
        }
    }
}
