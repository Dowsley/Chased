using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
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
    
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference brakeAction;
    [SerializeField] private float maxAccel = 30f;
    [SerializeField] private float brakeAccel = 50f;
    [SerializeField] private float turnSensitivity = 1.0f;
    [SerializeField] private float maxSteerAngle = 30.0f;
    [SerializeField] private Vector3 centerOfMass;
    [SerializeField] private List<Wheel> wheels;
    
    private float _moveInput;
    private float _steerInput;
    private Rigidbody _carRb;
    private bool _braking = false;

    // Collision cooldown to prevent multiple strikes from one touch
    private float _lastCollisionTime = -999f;
    private const float CollisionCooldown = 1.0f;

    private void Start()
    {
        _carRb = GetComponent<Rigidbody>();
        _carRb.centerOfMass = centerOfMass;
    }

    private void FixedUpdate()
    {
        GetInputs();
        Move();
        Steer();
        Brake();
        AnimateWheels();
    }

    private void GetInputs()
    {
        _moveInput = moveAction.action.ReadValue<Vector2>().y;
        _steerInput = moveAction.action.ReadValue<Vector2>().x;
        _braking = brakeAction.action.ReadValue<float>() > 0f;
    }

    private void Move()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = _moveInput * maxAccel;
        }
    }

    private void Steer()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                var steerAngle = _steerInput * maxSteerAngle * turnSensitivity;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, steerAngle, 0.6f);
            }
        }
    }

    private void Brake()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.brakeTorque = _braking ? brakeAccel : 0f;
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

    private void OnCollisionEnter(Collision collision)
    {
        // Check if we collided with the cop car
        if (collision.gameObject.GetComponent<CopCarAI>() != null)
        {
            // Use cooldown to prevent multiple collision events from one touch
            float currentTime = Time.time;
            if (currentTime - _lastCollisionTime < CollisionCooldown)
            {
                return; // Still in cooldown, ignore this collision
            }

            _lastCollisionTime = currentTime;
            Debug.Log("Hit by cop car!");

            // Notify the game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseStrike();
            }
        }
    }
}
