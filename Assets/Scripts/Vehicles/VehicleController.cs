using UnityEngine;
using System;

namespace NairobiHustle.Vehicles
{
    public enum VehicleType
    {
        Motorcycle,
        TukTuk,
        Car,
        Van,
        Truck
    }

    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Vehicle Information")]
        public string vehicleId;
        public VehicleType type;
        public string vehicleName;
        public bool isLocked = true;

        [Header("Performance Stats")]
        public float maxSpeed = 30f;
        public float acceleration = 10f;
        public float braking = 15f;
        public float handling = 5f;
        public float capacity = 100f;
        public float fuelEfficiency = 1f;
        public float maintenanceLevel = 100f;
        public float cost;

        [Header("Physics Settings")]
        public float centerOfMassOffset = -0.5f;
        public float groundCheckDistance = 0.5f;
        public LayerMask groundLayer;
        public WheelCollider[] wheelColliders;
        public Transform[] wheelMeshes;
        public float wheelRadius = 0.4f;
        public AnimationCurve speedToSteerAngle;
        public float downforce = 100f;

        private Rigidbody rb;
        private float currentSpeed;
        private bool isGrounded;
        private Vector3 previousPosition;
        private float distanceTraveled;
        private float currentFuel;
        private const float maxFuel = 100f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = new Vector3(0, centerOfMassOffset, 0);
            currentFuel = maxFuel;
            previousPosition = transform.position;
        }

        private void FixedUpdate()
        {
            UpdateGroundCheck();
            UpdateVehicleStats();
            ApplyDownforce();
        }

        public void HandleMovement(bool isAccelerating, bool isBraking, float steeringInput)
        {
            if (!isGrounded || currentFuel <= 0) return;

            // Calculate motor torque based on input
            float motorTorque = isAccelerating ? acceleration : (isBraking ? -braking : 0);
            
            // Apply torque to wheels
            foreach (WheelCollider wheel in wheelColliders)
            {
                wheel.motorTorque = motorTorque;
            }

            // Calculate steering angle based on speed
            float speedFactor = rb.velocity.magnitude / maxSpeed;
            float steerAngle = speedToSteerAngle.Evaluate(speedFactor) * steeringInput * handling;

            // Apply steering to front wheels
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                if (i < 2) // Assuming front wheels are first two in array
                {
                    wheelColliders[i].steerAngle = steerAngle;
                }
            }

            // Update wheel visuals
            UpdateWheelMeshes();

            // Consume fuel
            ConsumeFuel();
        }

        private void UpdateWheelMeshes()
        {
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                if (wheelMeshes[i] != null && wheelColliders[i] != null)
                {
                    Vector3 position;
                    Quaternion rotation;
                    wheelColliders[i].GetWorldPose(out position, out rotation);
                    wheelMeshes[i].position = position;
                    wheelMeshes[i].rotation = rotation;
                }
            }
        }

        private void UpdateGroundCheck()
        {
            isGrounded = Physics.Raycast(transform.position, -Vector3.up, groundCheckDistance, groundLayer);
        }

        private void ApplyDownforce()
        {
            if (isGrounded)
            {
                rb.AddForce(-transform.up * downforce * rb.velocity.magnitude);
            }
        }

        private void UpdateVehicleStats()
        {
            // Update current speed
            currentSpeed = rb.velocity.magnitude;

            // Calculate distance traveled
            float deltaDistance = Vector3.Distance(transform.position, previousPosition);
            distanceTraveled += deltaDistance;
            previousPosition = transform.position;

            // Update maintenance based on usage
            if (deltaDistance > 0)
            {
                maintenanceLevel -= deltaDistance * 0.001f;
                maintenanceLevel = Mathf.Clamp(maintenanceLevel, 0f, 100f);
            }
        }

        private void ConsumeFuel()
        {
            if (currentSpeed > 0)
            {
                float fuelConsumption = (currentSpeed / maxSpeed) * (1f / fuelEfficiency) * Time.fixedDeltaTime;
                currentFuel -= fuelConsumption;
                currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
            }
        }

        public void RefuelVehicle(float amount)
        {
            currentFuel += amount;
            currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
        }

        public void RepairVehicle(float amount)
        {
            maintenanceLevel += amount;
            maintenanceLevel = Mathf.Clamp(maintenanceLevel, 0f, 100f);
        }

        public float GetFuelPercentage()
        {
            return (currentFuel / maxFuel) * 100f;
        }

        public float GetMaintenancePercentage()
        {
            return maintenanceLevel;
        }

        public float GetDistanceTraveled()
        {
            return distanceTraveled;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Handle vehicle collision damage
            if (collision.relativeVelocity.magnitude > 5f)
            {
                float damage = collision.relativeVelocity.magnitude * 0.5f;
                maintenanceLevel -= damage;
                maintenanceLevel = Mathf.Clamp(maintenanceLevel, 0f, 100f);
            }
        }
    }
} 