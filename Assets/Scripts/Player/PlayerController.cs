using UnityEngine;
using System;
using System.Collections.Generic;

namespace NairobiHustle.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Stats")]
        public string playerId;
        public string username;
        public float balance;
        public int experience;
        public int level;
        public float rating;
        public string mpesaPhone;
        public int completedDeliveries;

        [Header("Vehicle Settings")]
        public List<VehicleController> ownedVehicles;
        public VehicleController currentVehicle;

        [Header("Movement Settings")]
        public float accelerationSpeed = 10f;
        public float brakingSpeed = 15f;
        public float steeringSpeed = 100f;
        public float maxSpeed = 30f;

        private Rigidbody rb;
        private float currentSpeed;
        private bool isAccelerating;
        private bool isBraking;
        private float steeringInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody component missing from PlayerController!");
            }
        }

        private void Update()
        {
            // Handle input
            HandleInput();
            
            // Update UI elements if needed
            UpdateUI();
        }

        private void FixedUpdate()
        {
            // Handle vehicle physics
            HandleMovement();
        }

        private void HandleInput()
        {
            // Get input based on platform (mobile touch, keyboard, or gamepad)
            #if UNITY_ANDROID || UNITY_IOS
                HandleMobileInput();
            #else
                HandleDesktopInput();
            #endif
        }

        private void HandleMobileInput()
        {
            // Mobile-specific input handling
            if (Input.touchCount > 0)
            {
                // Implement touch controls
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        // Handle touch input
                    }
                }
            }
        }

        private void HandleDesktopInput()
        {
            // Keyboard/Gamepad input
            isAccelerating = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            isBraking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            steeringInput = Input.GetAxis("Horizontal");
        }

        private void HandleMovement()
        {
            if (currentVehicle != null)
            {
                // Delegate movement to current vehicle
                currentVehicle.HandleMovement(isAccelerating, isBraking, steeringInput);
            }
            else
            {
                // Direct player movement when not in vehicle
                Vector3 movement = transform.forward * (isAccelerating ? accelerationSpeed : (isBraking ? -brakingSpeed : 0));
                rb.AddForce(movement, ForceMode.Acceleration);

                // Apply steering
                transform.Rotate(Vector3.up * (steeringInput * steeringSpeed * Time.fixedDeltaTime));

                // Limit speed
                if (rb.velocity.magnitude > maxSpeed)
                {
                    rb.velocity = rb.velocity.normalized * maxSpeed;
                }
            }
        }

        private void UpdateUI()
        {
            // Update UI elements with current player stats
            // This would be implemented by your UI system
        }

        public void EnterVehicle(VehicleController vehicle)
        {
            if (vehicle != null && ownedVehicles.Contains(vehicle))
            {
                currentVehicle = vehicle;
                // Additional vehicle entry logic
            }
        }

        public void ExitVehicle()
        {
            if (currentVehicle != null)
            {
                // Store vehicle state
                currentVehicle = null;
                // Additional vehicle exit logic
            }
        }

        public void AddExperience(int amount)
        {
            experience += amount;
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            // Implement level up logic based on experience thresholds
            int newLevel = Mathf.FloorToInt(Mathf.Sqrt(experience / 100f)) + 1;
            if (newLevel > level)
            {
                level = newLevel;
                OnLevelUp();
            }
        }

        private void OnLevelUp()
        {
            // Implement level up rewards and notifications
            Debug.Log($"Level Up! New Level: {level}");
        }

        public void UpdateBalance(float amount)
        {
            balance += amount;
            // Trigger UI update and save
        }

        public void CompleteDelivery()
        {
            completedDeliveries++;
            // Update stats and trigger achievements
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Handle collisions
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                // Implement collision penalties
            }
        }
    }
} 