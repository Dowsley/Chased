using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;
using Driving;

namespace UI
{
    /// <summary>
    /// Manages all game UI including HUD, betting screen, and game-over screens
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("Canvas References")]
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private Canvas bettingCanvas;
        [SerializeField] private Canvas gameOverCanvas;
        [SerializeField] private Canvas winCanvas;

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI copHitsText;

        [Header("Betting Screen")]
        [SerializeField] private TextMeshProUGUI availableMoneyText;
        [SerializeField] private Slider betSlider;
        [SerializeField] private TextMeshProUGUI betAmountText;
        [SerializeField] private TextMeshProUGUI potentialWinText;
        [SerializeField] private Button startChaseButton;
        [SerializeField] private Button newCampaignButton;

        [Header("Win Screen")]
        [SerializeField] private TextMeshProUGUI winBetText;
        [SerializeField] private TextMeshProUGUI winningsText;
        [SerializeField] private TextMeshProUGUI totalMoneyText;
        [SerializeField] private Button nextChaseButton;

        [Header("Game Over Screen")]
        [SerializeField] private TextMeshProUGUI gameOverMessageText;
        [SerializeField] private Button restartCampaignButton;

        [Header("Settings")]
        [SerializeField] private float basePayoutMultiplier = 1.5f;

        private VehicleHealth playerVehicleHealth;

        void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
                GameManager.Instance.OnBetPlaced += UpdateBetDisplay;
                GameManager.Instance.OnChaseWon += ShowWinScreen;
                GameManager.Instance.OnChaseLost += ShowLossMessage;
                GameManager.Instance.OnPermadeath += ShowGameOverScreen;
                GameManager.Instance.OnCopHitReceived += UpdateCopHitsDisplay;
            }

            // Setup button listeners
            if (startChaseButton != null)
                startChaseButton.onClick.AddListener(OnStartChaseClicked);
            if (newCampaignButton != null)
                newCampaignButton.onClick.AddListener(OnNewCampaignClicked);
            if (nextChaseButton != null)
                nextChaseButton.onClick.AddListener(OnNextChaseClicked);
            if (restartCampaignButton != null)
                restartCampaignButton.onClick.AddListener(OnRestartCampaignClicked);
            if (betSlider != null)
                betSlider.onValueChanged.AddListener(OnBetSliderChanged);

            // Start with betting screen
            ShowBettingScreen();
        }

        void Update()
        {
            // Update HUD if chase is active
            if (GameManager.Instance != null && GameManager.Instance.IsCampaignActive())
            {
                UpdateHUD();
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
                GameManager.Instance.OnBetPlaced -= UpdateBetDisplay;
                GameManager.Instance.OnChaseWon -= ShowWinScreen;
                GameManager.Instance.OnChaseLost -= ShowLossMessage;
                GameManager.Instance.OnPermadeath -= ShowGameOverScreen;
                GameManager.Instance.OnCopHitReceived -= UpdateCopHitsDisplay;
            }
        }

        /// <summary>
        /// Update HUD elements during active chase
        /// </summary>
        private void UpdateHUD()
        {
            // Update timer
            if (timerText != null && GameManager.Instance != null)
            {
                float timeRemaining = GameManager.Instance.GetEscapeTimeRemaining();
                timerText.text = $"Escape in: {timeRemaining:F1}s";
            }

            // Update health bar
            if (playerVehicleHealth != null)
            {
                float healthPercent = playerVehicleHealth.GetHealthPercent();
                if (healthBar != null)
                {
                    healthBar.value = healthPercent;
                }
                if (healthText != null)
                {
                    healthText.text = $"{(healthPercent * 100f):F0}%";
                }
            }
        }

        /// <summary>
        /// Update money display
        /// </summary>
        private void UpdateMoneyDisplay(float amount)
        {
            if (moneyText != null)
            {
                moneyText.text = $"${amount:F0}";
            }
            if (availableMoneyText != null)
            {
                availableMoneyText.text = $"Available: ${amount:F0}";
            }

            // Update bet slider max
            if (betSlider != null && GameManager.Instance != null)
            {
                betSlider.maxValue = amount;
                betSlider.minValue = GameManager.Instance.GetMinBetAmount();
                betSlider.value = Mathf.Clamp(betSlider.value, betSlider.minValue, betSlider.maxValue);
            }
        }

        /// <summary>
        /// Update bet display
        /// </summary>
        private void UpdateBetDisplay(float amount)
        {
            if (betText != null)
            {
                betText.text = $"Bet: ${amount:F0}";
            }
        }

        /// <summary>
        /// Update cop hits counter display
        /// </summary>
        private void UpdateCopHitsDisplay(int currentHits, int maxHits)
        {
            if (copHitsText != null)
            {
                copHitsText.text = $"Cop Hits: {currentHits}/{maxHits}";

                // Change color based on hits
                if (currentHits == 0)
                {
                    copHitsText.color = Color.white;
                }
                else if (currentHits == 1)
                {
                    copHitsText.color = Color.yellow;
                }
                else if (currentHits == 2)
                {
                    copHitsText.color = new Color(1f, 0.5f, 0f); // Orange
                }
                else if (currentHits >= 3)
                {
                    copHitsText.color = Color.red;
                }
            }
        }

        /// <summary>
        /// Show betting screen
        /// </summary>
        public void ShowBettingScreen()
        {
            SetCanvasActive(bettingCanvas, true);
            SetCanvasActive(hudCanvas, false);
            SetCanvasActive(gameOverCanvas, false);
            SetCanvasActive(winCanvas, false);

            // Initialize betting slider
            if (GameManager.Instance != null)
            {
                float playerMoney = GameManager.Instance.GetPlayerMoney();
                UpdateMoneyDisplay(playerMoney);

                // Check if player has enough money to bet
                if (playerMoney >= GameManager.Instance.GetMinBetAmount())
                {
                    if (betSlider != null)
                    {
                        betSlider.value = Mathf.Min(playerMoney * 0.2f, playerMoney); // Default 20% bet
                    }
                    if (startChaseButton != null)
                    {
                        startChaseButton.interactable = true;
                    }
                    if (newCampaignButton != null)
                    {
                        newCampaignButton.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // Not enough money - show new campaign button
                    if (startChaseButton != null)
                    {
                        startChaseButton.interactable = false;
                    }
                    if (newCampaignButton != null)
                    {
                        newCampaignButton.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Show HUD during chase
        /// </summary>
        public void ShowHUD(VehicleHealth vehicleHealth)
        {
            playerVehicleHealth = vehicleHealth;

            SetCanvasActive(hudCanvas, true);
            SetCanvasActive(bettingCanvas, false);
            SetCanvasActive(gameOverCanvas, false);
            SetCanvasActive(winCanvas, false);

            // Initialize health bar
            if (healthBar != null)
            {
                healthBar.value = 1f;
            }

            // Initialize cop hits counter
            if (GameManager.Instance != null)
            {
                UpdateCopHitsDisplay(0, GameManager.Instance.GetMaxCopHits());
            }
        }

        /// <summary>
        /// Show win screen
        /// </summary>
        private void ShowWinScreen(float bet, float winnings)
        {
            SetCanvasActive(winCanvas, true);
            SetCanvasActive(hudCanvas, false);
            SetCanvasActive(bettingCanvas, false);
            SetCanvasActive(gameOverCanvas, false);

            if (winBetText != null)
            {
                winBetText.text = $"Bet: ${bet:F0}";
            }
            if (winningsText != null)
            {
                float multiplier = winnings / bet;
                winningsText.text = $"Won: ${winnings:F0} ({multiplier:F2}x)";
            }
            if (totalMoneyText != null && GameManager.Instance != null)
            {
                totalMoneyText.text = $"Total: ${GameManager.Instance.GetPlayerMoney():F0}";
            }

            Time.timeScale = 0f; // Pause game
        }

        /// <summary>
        /// Show loss message (still have money to continue)
        /// </summary>
        private void ShowLossMessage(float lostBet)
        {
            Debug.Log($"Lost ${lostBet} - returning to betting screen");

            // Brief delay then return to betting
            Invoke(nameof(ShowBettingScreen), 2f);
        }

        /// <summary>
        /// Show game over screen (out of money - permadeath)
        /// </summary>
        private void ShowGameOverScreen()
        {
            SetCanvasActive(gameOverCanvas, true);
            SetCanvasActive(hudCanvas, false);
            SetCanvasActive(bettingCanvas, false);
            SetCanvasActive(winCanvas, false);

            if (gameOverMessageText != null)
            {
                gameOverMessageText.text = "CAMPAIGN OVER\n\nYou're out of money!\n\nStart a new campaign?";
            }

            Time.timeScale = 0f; // Pause game
        }

        /// <summary>
        /// Bet slider changed
        /// </summary>
        private void OnBetSliderChanged(float value)
        {
            if (betAmountText != null)
            {
                betAmountText.text = $"${value:F0}";
            }

            if (potentialWinText != null)
            {
                float potentialWin = value * basePayoutMultiplier;
                potentialWinText.text = $"Potential Win: ${potentialWin:F0} - ${(value * 2.25f):F0}";
            }
        }

        /// <summary>
        /// Start chase button clicked
        /// </summary>
        private void OnStartChaseClicked()
        {
            if (GameManager.Instance != null && betSlider != null)
            {
                float betAmount = betSlider.value;
                if (GameManager.Instance.PlaceBet(betAmount))
                {
                    // Bet placed successfully - now start the chase
                    Debug.Log("Bet placed - starting chase");

                    // Find ChaseSceneSetup and start the chase
                    ChaseSceneSetup sceneSetup = FindObjectOfType<ChaseSceneSetup>();
                    if (sceneSetup != null)
                    {
                        sceneSetup.StartChase();
                    }
                    else
                    {
                        Debug.LogError("ChaseSceneSetup not found! Cannot start chase.");
                    }
                }
            }
        }

        /// <summary>
        /// New campaign button clicked
        /// </summary>
        private void OnNewCampaignClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewCampaign();
                ShowBettingScreen();
            }
        }

        /// <summary>
        /// Next chase button clicked (after win)
        /// </summary>
        private void OnNextChaseClicked()
        {
            Time.timeScale = 1f; // Resume game
            ShowBettingScreen();
        }

        /// <summary>
        /// Restart campaign button clicked (after game over)
        /// </summary>
        private void OnRestartCampaignClicked()
        {
            Time.timeScale = 1f; // Resume game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewCampaign();
            }
            ShowBettingScreen();
        }

        /// <summary>
        /// Helper to toggle canvas active state
        /// </summary>
        private void SetCanvasActive(Canvas canvas, bool active)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(active);
            }
        }
    }
}
