using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int maxStrikes = 3;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI strikesText;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Restart Settings")]
    [SerializeField] private float restartDelay = 2f;

    private int _currentStrikes;
    private bool _isGameOver;
    private bool _isRestarting;

    private void Awake()
    {
        // Simple singleton pattern (no DontDestroyOnLoad since we reload the scene)
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Initialize strikes to max on each scene load
        _currentStrikes = maxStrikes;
        _isGameOver = false;
        _isRestarting = false;

        // Hide game over panel at start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Update UI after initializing everything
        UpdateUI();
    }

    public void LoseStrike()
    {
        if (_isGameOver || _isRestarting) return;

        _currentStrikes--;
        UpdateUI();

        Debug.Log($"Strike! Remaining strikes: {_currentStrikes}");

        if (_currentStrikes <= 0)
        {
            GameOver();
        }
        // Don't restart scene - keep playing until game over
    }

    private void UpdateUI()
    {
        if (strikesText != null)
        {
            strikesText.text = $"Strikes: {_currentStrikes}/{maxStrikes}";
        }
    }

    private void RestartScene()
    {
        if (_isRestarting) return;

        _isRestarting = true;
        Debug.Log("Restarting scene...");
        Invoke(nameof(DoRestart), restartDelay);
    }

    private void DoRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GameOver()
    {
        _isGameOver = true;
        Debug.Log("Game Over!");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Show game over for a few seconds, then restart
        Invoke(nameof(RestartGame), 3f);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
