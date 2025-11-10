using UnityEngine;
using System;
using Driving;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Transform targetCar;

        [Header("Campaign Settings")]
        [SerializeField] private float startingMoney = 100f;
        [SerializeField] private float minBetAmount = 10f;
        [SerializeField] private float escapeTimeForWin = 60f; // Seconds to survive to win
        [SerializeField] private float basePayoutMultiplier = 1.5f;

        [Header("Cop Hit Settings")]
        [SerializeField] private int maxCopHits = 3; // 3 strikes and you're out
        [SerializeField] private float copHitCooldown = 2f; // 2 seconds between counting hits

        // Campaign state
        private float playerMoney;
        private float currentBetAmount;
        private bool campaignActive;
        private float chaseStartTime;
        private VehicleHealth playerVehicleHealth;

        // Cop hit tracking
        private int copHitsReceived = 0;
        private float lastCopHitTime = -999f;

        // Events for UI updates
        public event Action<float> OnMoneyChanged;
        public event Action<float> OnBetPlaced;
        public event Action<float, float> OnChaseWon; // (bet, winnings)
        public event Action<float> OnChaseLost; // (bet lost)
        public event Action OnPermadeath;
        public event Action<int, int> OnCopHitReceived; // (current hits, max hits)

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadCampaignProgress();
        }

        private void Update()
        {
            if (campaignActive)
            {
                CheckWinCondition();
            }
        }

        /// <summary>
        /// Start a new campaign (called from UI)
        /// </summary>
        public void StartNewCampaign()
        {
            playerMoney = startingMoney;
            SaveCampaignProgress();
            OnMoneyChanged?.Invoke(playerMoney);
            Debug.Log($"New campaign started with ${playerMoney}");
        }

        /// <summary>
        /// Place a bet before starting a chase
        /// </summary>
        public bool PlaceBet(float amount)
        {
            if (amount < minBetAmount)
            {
                Debug.LogWarning($"Bet too small! Minimum: ${minBetAmount}");
                return false;
            }

            if (amount > playerMoney)
            {
                Debug.LogWarning($"Not enough money! You have: ${playerMoney}");
                return false;
            }

            currentBetAmount = amount;
            OnBetPlaced?.Invoke(amount);
            Debug.Log($"Bet placed: ${amount}");
            return true;
        }

        /// <summary>
        /// Start the chase (called after betting and scene setup)
        /// </summary>
        public void StartChase(VehicleHealth vehicleHealth)
        {
            if (currentBetAmount <= 0)
            {
                Debug.LogError("Cannot start chase without placing a bet!");
                return;
            }

            campaignActive = true;
            chaseStartTime = Time.time;
            playerVehicleHealth = vehicleHealth;
            copHitsReceived = 0; // Reset cop hits for new chase
            lastCopHitTime = -999f;

            // Subscribe to vehicle destruction
            if (playerVehicleHealth != null)
            {
                playerVehicleHealth.OnVehicleDestroyed += HandlePermadeath;
            }

            Debug.Log($"Chase started! Bet: ${currentBetAmount}, Survive for {escapeTimeForWin}s to win! Max cop hits: {maxCopHits}");
        }

        /// <summary>
        /// Check if player has survived long enough to win
        /// </summary>
        private void CheckWinCondition()
        {
            if (!campaignActive) return;

            float elapsedTime = Time.time - chaseStartTime;
            if (elapsedTime >= escapeTimeForWin)
            {
                WinChase();
            }
        }

        /// <summary>
        /// Player successfully escaped and won the chase
        /// </summary>
        private void WinChase()
        {
            if (!campaignActive) return;

            campaignActive = false;

            // Calculate winnings
            float healthBonus = playerVehicleHealth != null ? playerVehicleHealth.GetHealthPercent() : 0.5f;
            float multiplier = basePayoutMultiplier * (1f + healthBonus * 0.5f); // Up to 2.25x if perfect health
            float winnings = currentBetAmount * multiplier;

            playerMoney += winnings;
            SaveCampaignProgress();

            OnChaseWon?.Invoke(currentBetAmount, winnings);
            OnMoneyChanged?.Invoke(playerMoney);

            Debug.Log($"CHASE WON! Bet: ${currentBetAmount}, Winnings: ${winnings:F2} ({multiplier:F2}x), Total: ${playerMoney:F2}");

            // Cleanup
            if (playerVehicleHealth != null)
            {
                playerVehicleHealth.OnVehicleDestroyed -= HandlePermadeath;
            }

            currentBetAmount = 0f;
        }

        /// <summary>
        /// Player vehicle destroyed - PERMADEATH
        /// </summary>
        private void HandlePermadeath()
        {
            if (!campaignActive) return;

            campaignActive = false;

            // Lose the bet
            float lostAmount = currentBetAmount;
            playerMoney -= currentBetAmount;
            playerMoney = Mathf.Max(0f, playerMoney);

            OnChaseLost?.Invoke(lostAmount);
            OnMoneyChanged?.Invoke(playerMoney);

            Debug.Log($"PERMADEATH! Lost bet: ${lostAmount}, Remaining: ${playerMoney:F2}");

            // If out of money, game over
            if (playerMoney < minBetAmount)
            {
                Debug.Log("CAMPAIGN OVER - Out of money!");
                OnPermadeath?.Invoke();
                ResetCampaign();
            }
            else
            {
                SaveCampaignProgress();
            }

            // Cleanup
            if (playerVehicleHealth != null)
            {
                playerVehicleHealth.OnVehicleDestroyed -= HandlePermadeath;
            }

            currentBetAmount = 0f;
        }

        /// <summary>
        /// Reset campaign progress (complete game over)
        /// </summary>
        private void ResetCampaign()
        {
            playerMoney = 0f;
            currentBetAmount = 0f;
            campaignActive = false;
            SaveCampaignProgress();
        }

        /// <summary>
        /// Save campaign progress to PlayerPrefs
        /// </summary>
        private void SaveCampaignProgress()
        {
            PlayerPrefs.SetFloat("PlayerMoney", playerMoney);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load campaign progress from PlayerPrefs
        /// </summary>
        private void LoadCampaignProgress()
        {
            if (PlayerPrefs.HasKey("PlayerMoney"))
            {
                playerMoney = PlayerPrefs.GetFloat("PlayerMoney");
            }
            else
            {
                playerMoney = startingMoney;
                SaveCampaignProgress();
            }

            OnMoneyChanged?.Invoke(playerMoney);
            Debug.Log($"Campaign loaded. Money: ${playerMoney:F2}");
        }

        public void HitByCopCar()
        {
            if (!campaignActive) return;

            // Check cooldown to prevent multiple hits from same collision
            float currentTime = Time.time;
            if (currentTime - lastCopHitTime < copHitCooldown)
            {
                return; // Still in cooldown
            }

            lastCopHitTime = currentTime;
            copHitsReceived++;

            Debug.Log($"Hit by cop car! Hits: {copHitsReceived}/{maxCopHits}");

            // Fire event for UI update
            OnCopHitReceived?.Invoke(copHitsReceived, maxCopHits);

            // Check if reached max hits (3 strikes = you're out)
            if (copHitsReceived >= maxCopHits)
            {
                Debug.Log("3 STRIKES - YOU'RE OUT!");
                LoseChaseByStrikes();
            }
        }

        /// <summary>
        /// Player lost the chase by getting hit by cops 3 times
        /// </summary>
        private void LoseChaseByStrikes()
        {
            if (!campaignActive) return;

            campaignActive = false;

            // Lose the bet
            float lostAmount = currentBetAmount;
            playerMoney -= currentBetAmount;
            playerMoney = Mathf.Max(0f, playerMoney);

            OnChaseLost?.Invoke(lostAmount);
            OnMoneyChanged?.Invoke(playerMoney);

            Debug.Log($"Lost by 3 cop hits! Lost bet: ${lostAmount}, Remaining: ${playerMoney:F2}");

            // If out of money, game over
            if (playerMoney < minBetAmount)
            {
                Debug.Log("CAMPAIGN OVER - Out of money!");
                OnPermadeath?.Invoke();
                ResetCampaign();
            }
            else
            {
                SaveCampaignProgress();
            }

            // Cleanup
            if (playerVehicleHealth != null)
            {
                playerVehicleHealth.OnVehicleDestroyed -= HandlePermadeath;
            }

            currentBetAmount = 0f;
            copHitsReceived = 0;
        }

        // Public getters
        public float GetPlayerMoney() => playerMoney;
        public float GetCurrentBet() => currentBetAmount;
        public bool IsCampaignActive() => campaignActive;
        public float GetEscapeTimeRemaining() => Mathf.Max(0f, escapeTimeForWin - (Time.time - chaseStartTime));
        public float GetMinBetAmount() => minBetAmount;
        public int GetCopHits() => copHitsReceived;
        public int GetMaxCopHits() => maxCopHits;
    }
}
