using UnityEngine;
using System;

namespace Driving
{
    /// <summary>
    /// Tracks vehicle damage and modifies handling physics based on damage level
    /// Inspired by Half Sword's physics-consequence gameplay
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public class VehicleHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;

        [Header("Damage Impact Settings")]
        [SerializeField] private float minImpactForDamage = 3f; // Minimum collision force to cause damage
        [SerializeField] private float damageMultiplier = 5f; // How much damage per impact force

        [Header("Physics Degradation")]
        [SerializeField] private float minAccelerationPercent = 0.6f; // 60% at 100% damage
        [SerializeField] private float minSteeringPercent = 0.7f; // 70% at 100% damage
        [SerializeField] private float maxOversteerMultiplier = 1.5f; // 150% instability at 100% damage

        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem smokeEffect;
        [SerializeField] private ParticleSystem sparksEffect;

        // Events
        public event Action<float> OnDamageReceived; // Passes damage amount
        public event Action<float> OnHealthChanged; // Passes current health percentage
        public event Action OnVehicleDestroyed;

        private VehicleController vehicleController;
        private float originalMaxAccel;
        private float originalTurnSensitivity;
        private bool isDestroyed = false;

        void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            currentHealth = maxHealth;
        }

        void Start()
        {
            // Store original physics values
            StoreOriginalPhysicsValues();
        }

        private void StoreOriginalPhysicsValues()
        {
            // Use reflection to get private fields from VehicleController
            var maxAccelField = typeof(VehicleController).GetField("maxAccel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var turnSensitivityField = typeof(VehicleController).GetField("turnSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (maxAccelField != null)
                originalMaxAccel = (float)maxAccelField.GetValue(vehicleController);
            else
                originalMaxAccel = 300f; // Default fallback

            if (turnSensitivityField != null)
                originalTurnSensitivity = (float)turnSensitivityField.GetValue(vehicleController);
            else
                originalTurnSensitivity = 1.0f; // Default fallback
        }

        /// <summary>
        /// Apply damage to the vehicle from a collision
        /// </summary>
        public void ApplyCollisionDamage(Collision collision)
        {
            if (isDestroyed) return;

            float impactMagnitude = collision.impulse.magnitude;

            // Only apply damage if impact is significant
            if (impactMagnitude >= minImpactForDamage)
            {
                float damage = (impactMagnitude - minImpactForDamage) * damageMultiplier;
                ApplyDamage(damage);

                // Visual feedback based on impact
                if (impactMagnitude > 10f && sparksEffect != null)
                {
                    sparksEffect.Play();
                }
            }
        }

        /// <summary>
        /// Apply raw damage to the vehicle
        /// </summary>
        public void ApplyDamage(float damage)
        {
            if (isDestroyed) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);

            // Fire events
            OnDamageReceived?.Invoke(damage);
            OnHealthChanged?.Invoke(GetHealthPercent());

            // Update physics based on new damage level
            UpdatePhysicsFromDamage();

            // Update visual feedback
            UpdateVisualFeedback();

            // Check for destruction
            if (currentHealth <= 0f)
            {
                DestroyVehicle();
            }

            Debug.Log($"Vehicle took {damage:F1} damage. Health: {currentHealth:F1}/{maxHealth} ({GetHealthPercent():P0})");
        }

        /// <summary>
        /// Modify vehicle physics based on damage level
        /// More damage = worse handling (Half Sword style consequences)
        /// </summary>
        private void UpdatePhysicsFromDamage()
        {
            float damagePercent = 1f - GetHealthPercent(); // 0 = no damage, 1 = destroyed

            // Use reflection to modify VehicleController private fields
            var maxAccelField = typeof(VehicleController).GetField("maxAccel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var turnSensitivityField = typeof(VehicleController).GetField("turnSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Reduce acceleration as damage increases
            float accelMultiplier = Mathf.Lerp(1f, minAccelerationPercent, damagePercent);
            if (maxAccelField != null)
            {
                maxAccelField.SetValue(vehicleController, originalMaxAccel * accelMultiplier);
            }

            // Reduce steering sensitivity as damage increases
            float steeringMultiplier = Mathf.Lerp(1f, minSteeringPercent, damagePercent);
            if (turnSensitivityField != null)
            {
                turnSensitivityField.SetValue(vehicleController, originalTurnSensitivity * steeringMultiplier);
            }

            // Optional: Add instability/oversteer by modifying center of mass
            // Higher center of mass = more likely to flip/slide
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 com = rb.centerOfMass;
                com.y = Mathf.Lerp(0f, 0.3f, damagePercent); // Raise center of mass slightly
                rb.centerOfMass = com;
            }
        }

        /// <summary>
        /// Update visual feedback based on damage level
        /// </summary>
        private void UpdateVisualFeedback()
        {
            float damagePercent = 1f - GetHealthPercent();

            // Show smoke when damaged
            if (smokeEffect != null)
            {
                if (damagePercent > 0.3f && !smokeEffect.isPlaying)
                {
                    smokeEffect.Play();
                }
                else if (damagePercent <= 0.3f && smokeEffect.isPlaying)
                {
                    smokeEffect.Stop();
                }

                // Increase smoke emission rate with damage
                var emission = smokeEffect.emission;
                emission.rateOverTime = Mathf.Lerp(0f, 50f, damagePercent);
            }
        }

        /// <summary>
        /// Vehicle is completely destroyed (permadeath)
        /// </summary>
        private void DestroyVehicle()
        {
            if (isDestroyed) return;

            isDestroyed = true;
            OnVehicleDestroyed?.Invoke();

            Debug.Log("VEHICLE DESTROYED - PERMADEATH!");

            // Stop all visual effects
            if (smokeEffect != null) smokeEffect.Stop();
            if (sparksEffect != null) sparksEffect.Stop();

            // Disable vehicle control
            if (vehicleController != null)
            {
                vehicleController.enabled = false;
            }

            // Optionally disable player input
            var playerInput = GetComponent<Player.PlayerDriverInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }

        /// <summary>
        /// Restore vehicle to full health (for testing or campaign restart)
        /// </summary>
        public void RestoreFullHealth()
        {
            currentHealth = maxHealth;
            isDestroyed = false;

            // Restore original physics
            StoreOriginalPhysicsValues(); // Re-read originals
            UpdatePhysicsFromDamage(); // Apply (which will be 100% since no damage)

            // Enable components
            if (vehicleController != null)
            {
                vehicleController.enabled = true;
            }

            var playerInput = GetComponent<Player.PlayerDriverInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }

            OnHealthChanged?.Invoke(1f);
        }

        // Public getters
        public float GetHealthPercent() => currentHealth / maxHealth;
        public float GetCurrentHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;
        public bool IsDestroyed() => isDestroyed;
    }
}
