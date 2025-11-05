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
    private bool _initialized;

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Only initialize strikes on first load, not on scene restart
        if (!_initialized)
        {
            _currentStrikes = maxStrikes;
            _initialized = true;
        }

        _isGameOver = false;
        _isRestarting = false;
        UpdateUI();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
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
        _currentStrikes = maxStrikes;
        _isGameOver = false;
        _isRestarting = false;
        _initialized = false; // Reset so strikes are set to max on next Start()
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
