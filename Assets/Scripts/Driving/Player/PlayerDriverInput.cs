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
    }
}
