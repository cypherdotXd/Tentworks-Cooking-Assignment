using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<OrderWindow> orderWindows = new();
    [SerializeField] private float gameplayDuration = 180f;

    [SerializeField] private int vegetablePoints = 100;
    [SerializeField] private int cheesePoints = 150;
    [SerializeField] private int meatPoints = 200;

    private const string HighScoreKey = "HighScore";

    private int currentScore;
    private int highScore = -1;
    private float timeRemaining;
    private bool isGameActive;
    private bool isPaused;
    private bool isNewHighScore;

    public int CurrentScore => currentScore;
    public int HighScore
    {
        get
        {
            if (highScore < 0)
            {
                highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            }
            return highScore;
        }
    }
    public float TimeRemaining => timeRemaining;
    public float GameplayDuration => gameplayDuration;
    public bool IsGameActive => isGameActive;
    public bool IsPaused => isPaused;
    public bool IsNewHighScore => isNewHighScore;

    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action<bool> OnGamePaused;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighScoreChanged;
    public event Action<float> OnTimeUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (highScore < 0)
        {
            highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }
        timeRemaining = gameplayDuration;
    }

    private void Start()
    {
        OnHighScoreChanged?.Invoke(HighScore);
        OnScoreChanged?.Invoke(currentScore);
        OnTimeUpdated?.Invoke(timeRemaining);
    }

    private void OnEnable()
    {
        foreach (var window in orderWindows)
        {
            if (window) window.OnOrderCompleted += HandleOrderFulfilled;
        }
    }

    private void OnDisable()
    {
        foreach (var window in orderWindows)
        {
            if (window) window.OnOrderCompleted -= HandleOrderFulfilled;
        }
    }

    private void Update()
    {
        if (!isGameActive || isPaused) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndGame();
        }

        OnTimeUpdated?.Invoke(timeRemaining);
    }

    public void StartGame()
    {
        currentScore = 0;
        timeRemaining = gameplayDuration;
        isGameActive = true;
        isPaused = false;
        isNewHighScore = false;
        Time.timeScale = 1f;

        OnGameStarted?.Invoke();
        OnScoreChanged?.Invoke(currentScore);
        OnHighScoreChanged?.Invoke(HighScore);
        OnTimeUpdated?.Invoke(timeRemaining);
    }

    public void EndGame()
    {
        isGameActive = false;
        isPaused = false;
        Time.timeScale = 0f;

        if (currentScore > HighScore)
        {
            highScore = currentScore;
            isNewHighScore = true;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke(highScore);
        }

        OnGameEnded?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void PauseGame()
    {
        if (!isGameActive || isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        OnGamePaused?.Invoke(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        OnGamePaused?.Invoke(false);
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private void HandleOrderFulfilled(OrderWindow.Order order)
    {
        if (!isGameActive || isPaused) return;

        int earnedScore = CalculateScore(order);
        currentScore += earnedScore;

        if (currentScore > HighScore)
        {
            highScore = currentScore;
            isNewHighScore = true;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke(highScore);
        }

        OnScoreChanged?.Invoke(currentScore);
    }

    private int CalculateScore(OrderWindow.Order order)
    {
        if (order == null) return 0;

        int total = 0;
        for (int i = 0; i < order.items.Count; i++)
        {
            total += order.items[i].type switch
            {
                Ingredient.IngredientType.Vegetable => vegetablePoints,
                Ingredient.IngredientType.Cheese => cheesePoints,
                Ingredient.IngredientType.Meat => meatPoints,
                _ => 0
            };
        }
        return total;
    }
}
