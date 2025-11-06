using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Driving
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        public enum Axel
        {
            Front,
            Rear
        }

        [Serializable]
        public struct Wheel
        {
            public GameObject wheelModel;
            public WheelCollider wheelCollider;
            public Axel axel;
        }

        [Header("Movement Settings")]
        [SerializeField] private float maxAccel = 300f;
        [SerializeField] private float brakeAccel = 500f;
        [SerializeField] private float turnSensitivity = 1.0f;
        [SerializeField] private float maxSteerAngle = 30.0f;

        [Header("Physics")]
        [SerializeField] private Vector3 centerOfMass;
        [SerializeField] private List<Wheel> wheels;

        private Rigidbody _carRb;
        private float _currentThrottle;
        private float _currentSteerAngle;
        private bool _currentBrake;

        private void Start()
        {
            _carRb = GetComponent<Rigidbody>();
            _carRb.centerOfMass = centerOfMass;
        }

        private void FixedUpdate()
        {
            ApplyMotor();
            ApplySteering();
            ApplyBrake();
            AnimateWheels();
        }

        public void SetThrottle(float throttle)
        {
            _currentThrottle = throttle;
        }

        public void SetSteering(float steerInput)
        {
            _currentSteerAngle = steerInput * maxSteerAngle * turnSensitivity;
        }

        public void SetBrake(bool brake)
        {
            _currentBrake = brake;
        }

        private void ApplyMotor()
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.motorTorque = _currentThrottle * maxAccel;
            }
        }

        private void ApplySteering()
        {
            foreach (var wheel in wheels.Where(wheel => wheel.axel == Axel.Front))
            {
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _currentSteerAngle, 0.6f);
            }
        }

        private void ApplyBrake()
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.brakeTorque = _currentBrake ? brakeAccel : 0f;
            }
        }

        private void AnimateWheels()
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.GetWorldPose(out var pos, out var rot);
                wheel.wheelModel.transform.position = pos;
                wheel.wheelModel.transform.rotation = rot;
            }
        }
    }
}
