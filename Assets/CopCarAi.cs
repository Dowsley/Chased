using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CopCarAI : MonoBehaviour
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
    
    [Header("Target")]
    [SerializeField] private Transform targetCar;
    
    [Header("Movement Settings")]
    [SerializeField] private float maxAccel = 30f;
    [SerializeField] private float brakeAccel = 50f;
    [SerializeField] private float maxSteerAngle = 30.0f;
    [SerializeField] private float steeringSmoothness = 0.6f;
    
    [Header("AI Settings")]
    [SerializeField] private float pursuitSpeed = 1.5f;
    [SerializeField] private float minSpeedMultiplier = 0.5f; // Minimum speed even when very close
    [SerializeField] private float slowDownDistance = 5f; // Only slow down when very close
    [SerializeField] private float aggressiveness = 1.2f; // Extra speed boost for ramming
    
    [Header("Physics")]
    [SerializeField] private Vector3 centerOfMass;
    [SerializeField] private List<Wheel> wheels;
    
    private Rigidbody _carRb;
    private float _currentThrottle;
    private float _currentSteerAngle;

    private void Start()
    {
        _carRb = GetComponent<Rigidbody>();
        _carRb.centerOfMass = centerOfMass;
        
        if (targetCar == null)
        {
            Debug.LogError("CopCarAI: No target car assigned!");
        }
    }

    private void FixedUpdate()
    {
        if (targetCar == null) return;
        
        CalculateAI();
        ApplyMotor();
        ApplySteering();
    }

    private void CalculateAI()
    {
        // Calculate direction to target
        Vector3 directionToTarget = targetCar.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        
        // Calculate the angle to target (used for steering)
        Vector3 localTarget = transform.InverseTransformPoint(targetCar.position);
        float angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        
        // Calculate steering based on angle to target
        _currentSteerAngle = Mathf.Clamp(angleToTarget / maxSteerAngle, -1f, 1f);
        
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
        float turnFactor = 1f - (Mathf.Abs(_currentSteerAngle) * 0.2f);
        _currentThrottle *= turnFactor;
        
        // Ensure we never brake - always move forward
        _currentThrottle = Mathf.Max(_currentThrottle, minSpeedMultiplier);
    }

    private void ApplyMotor()
    {
        foreach (var wheel in wheels)
        {
            // Always apply forward throttle - never brake
            wheel.wheelCollider.motorTorque = _currentThrottle * maxAccel;
            wheel.wheelCollider.brakeTorque = 0;
        }
    }

    private void ApplySteering()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                float targetSteerAngle = _currentSteerAngle * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(
                    wheel.wheelCollider.steerAngle, 
                    targetSteerAngle, 
                    steeringSmoothness
                );
            }
        }
    }

    // Optional: Visualize pursuit in editor
    private void OnDrawGizmos()
    {
        if (targetCar != null && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetCar.position);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, slowDownDistance);
        }
    }
}