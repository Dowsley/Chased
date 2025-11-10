using Core;
using Driving.AI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Driving.Player
{
    public class PlayerDriverInput : BaseDriverInput
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference brakeAction;

        private float _moveInput;
        private float _steerInput;
        private bool _braking;

        private float _lastCollisionTime = -999f;
        private const float CollisionCooldown = 1.0f;

        private void GetInputs()
        {
            _moveInput = moveAction.action.ReadValue<Vector2>().y;
            _steerInput = moveAction.action.ReadValue<Vector2>().x;
            _braking = brakeAction.action.ReadValue<float>() > 0f;
        }

        protected override void FixedUpdate()
        {
            GetInputs();
            base.FixedUpdate(); // Calls UpdateVehicle()
        }

        protected override void UpdateVehicle()
        {
            VehicleController.SetThrottle(_moveInput);
            VehicleController.SetSteering(_steerInput);
            VehicleController.SetBrake(_braking);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Apply damage from any collision (building, cop car, etc.)
            VehicleHealth health = GetComponent<VehicleHealth>();
            if (health != null)
            {
                health.ApplyCollisionDamage(collision);
            }

            // Specific handling for cop car collisions
            if (collision.gameObject.GetComponent<AIDriverInput>() != null)
            {
                float currentTime = Time.time;
                if (currentTime - _lastCollisionTime < CollisionCooldown)
                {
                    return; // still in cooldown, ignore collision
                }

                _lastCollisionTime = currentTime;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.HitByCopCar();
                }
            }
        }
    }
}
