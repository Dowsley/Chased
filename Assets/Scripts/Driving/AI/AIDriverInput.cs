using Core;
using UnityEngine;

namespace Driving.AI
{
    public class AIDriverInput : BaseDriverInput
    {
        [Header("AI Settings")] [SerializeField]
        private bool disabled = false;
        [SerializeField] private float pursuitSpeed = 1.5f;
        [SerializeField] private float minSpeedMultiplier = 0.5f; // Minimum speed even when very close
        [SerializeField] private float slowDownDistance = 5f; // Only slow down when very close
        [SerializeField] private float aggressiveness = 1.2f; // Extra speed boost for ramming
        [SerializeField] private float maxSteerAngle = 30.0f;

        private float _currentThrottle;
        private float _currentSteerInput;

        protected override void FixedUpdate()
        {
            if (!GameManager.Instance.targetCar || disabled)
                return;

            CalculateAI();
            base.FixedUpdate(); // Calls UpdateVehicle()
        }

        private void CalculateAI()
        {
            Vector3 directionToTarget = GameManager.Instance.targetCar.position - transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            Vector3 localTarget = transform.InverseTransformPoint(GameManager.Instance.targetCar.position);
            float angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

            _currentSteerInput = Mathf.Clamp(angleToTarget / maxSteerAngle, -1f, 1f);

            // Aggressive throttle calculation - always accelerate toward target
            if (distanceToTarget < slowDownDistance)
            {
                // Close - but still maintain minimum speed for ramming
                float proximityFactor = distanceToTarget / slowDownDistance;
                _currentThrottle = Mathf.Lerp(minSpeedMultiplier, 1f, proximityFactor) * pursuitSpeed * aggressiveness;
            }
            else
            {
                // Far away - full aggressive pursuit
                _currentThrottle = pursuitSpeed * aggressiveness;
            }

            // Reduce speed slightly when turning sharply (but still stay aggressive)
            float turnFactor = 1f - (Mathf.Abs(_currentSteerInput) * 0.2f);
            _currentThrottle *= turnFactor;

            // Ensure we never brake - always move forward
            _currentThrottle = Mathf.Max(_currentThrottle, minSpeedMultiplier);
        }

        protected override void UpdateVehicle()
        {
            VehicleController.SetThrottle(_currentThrottle);
            VehicleController.SetSteering(_currentSteerInput);
            VehicleController.SetBrake(false); // AI never brakes
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying && GameManager.Instance.targetCar != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, GameManager.Instance.targetCar.position);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, slowDownDistance);
            }
        }
    }
}
