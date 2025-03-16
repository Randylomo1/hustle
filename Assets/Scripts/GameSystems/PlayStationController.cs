using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace NairobiHustle.GameSystems
{
    public class PlayStationController : MonoBehaviour
    {
        [Header("DualSense Settings")]
        [SerializeField] private float hapticIntensity = 1f;
        [SerializeField] private float triggerResistance = 0.5f;
        [SerializeField] private Color lightBarColor = Color.blue;

        [Header("PSVR2 Settings")]
        [SerializeField] private float headsetHaptics = 0.5f;
        [SerializeField] private float eyeTrackingSensitivity = 1f;
        [SerializeField] private bool useEyeTracking = true;

        [Header("Gesture Recognition")]
        [SerializeField] private float gestureThreshold = 0.8f;
        [SerializeField] private float gestureTimeout = 1.5f;

        private PlayerInput playerInput;
        private InputActionMap dualSenseMap;
        private InputActionMap psvrMap;
        
        private Dictionary<string, Vector2> gesturePatterns;
        private List<Vector2> currentGesture;
        private float gestureTimer;

        private void Awake()
        {
            InitializeInput();
            InitializeGestures();
        }

        private void InitializeInput()
        {
            playerInput = GetComponent<PlayerInput>();
            
            // DualSense specific actions
            dualSenseMap = new InputActionMap("DualSense");
            
            // Touchpad
            var touchPosition = dualSenseMap.AddAction("touchPosition", InputType.Value, "<DualSenseGamepad>/touchpad/position");
            var touchContact = dualSenseMap.AddAction("touchContact", InputType.Button, "<DualSenseGamepad>/touchpad/touch");
            
            // Adaptive triggers
            var rightTrigger = dualSenseMap.AddAction("rightTrigger", InputType.Value, "<DualSenseGamepad>/rightTrigger");
            var leftTrigger = dualSenseMap.AddAction("leftTrigger", InputType.Value, "<DualSenseGamepad>/leftTrigger");
            
            // Create Sense actions
            var createButton = dualSenseMap.AddAction("create", InputType.Button, "<DualSenseGamepad>/create");
            
            // PSVR2 specific actions
            psvrMap = new InputActionMap("PSVR2");
            
            // Eye tracking
            var gazePosition = psvrMap.AddAction("gazePosition", InputType.Value, "<PSVR2>/gazePosition");
            var gazeTracking = psvrMap.AddAction("gazeTracking", InputType.Value, "<PSVR2>/gazeTracking");
            
            // VR controllers
            var rightHand = psvrMap.AddAction("rightHand", InputType.Value, "<PSVR2>/rightHand/position");
            var leftHand = psvrMap.AddAction("leftHand", InputType.Value, "<PSVR2>/leftHand/position");

            // Enable all actions
            dualSenseMap.Enable();
            psvrMap.Enable();

            // Subscribe to events
            touchPosition.performed += ctx => HandleTouchPosition(ctx.ReadValue<Vector2>());
            touchContact.performed += _ => HandleTouchContact();
            rightTrigger.performed += ctx => HandleRightTrigger(ctx.ReadValue<float>());
            leftTrigger.performed += ctx => HandleLeftTrigger(ctx.ReadValue<float>());
            createButton.performed += _ => HandleCreateButton();
            
            if (useEyeTracking)
            {
                gazePosition.performed += ctx => HandleGazePosition(ctx.ReadValue<Vector2>());
                gazeTracking.performed += ctx => HandleGazeTracking(ctx.ReadValue<float>());
            }
        }

        private void InitializeGestures()
        {
            gesturePatterns = new Dictionary<string, Vector2>
            {
                { "SwipeUp", Vector2.up },
                { "SwipeDown", Vector2.down },
                { "SwipeLeft", Vector2.left },
                { "SwipeRight", Vector2.right },
                { "Circle", new Vector2(1f, 1f) } // Simplified circle pattern
            };

            currentGesture = new List<Vector2>();
        }

        private void HandleTouchPosition(Vector2 position)
        {
            if (currentGesture.Count == 0)
            {
                gestureTimer = 0f;
            }

            currentGesture.Add(position);
            
            // Check for gesture completion
            if (currentGesture.Count > 2)
            {
                RecognizeGesture();
            }
        }

        private void HandleTouchContact()
        {
            // Reset gesture when touch ends
            currentGesture.Clear();
            gestureTimer = 0f;
        }

        private void RecognizeGesture()
        {
            if (currentGesture.Count < 2) return;

            Vector2 gestureDirection = currentGesture[currentGesture.Count - 1] - currentGesture[0];
            gestureDirection.Normalize();

            foreach (var pattern in gesturePatterns)
            {
                float similarity = Vector2.Dot(gestureDirection, pattern.Value);
                if (similarity > gestureThreshold)
                {
                    ExecuteGestureAction(pattern.Key);
                    break;
                }
            }
        }

        private void ExecuteGestureAction(string gestureName)
        {
            switch (gestureName)
            {
                case "SwipeUp":
                    ShowInventory();
                    break;
                case "SwipeDown":
                    ShowMap();
                    break;
                case "SwipeLeft":
                    ShowSocialMenu();
                    break;
                case "SwipeRight":
                    ShowProperties();
                    break;
                case "Circle":
                    ToggleQuickMenu();
                    break;
            }
        }

        private void HandleRightTrigger(float value)
        {
            // Adjust trigger resistance based on context
            float resistance = CalculateContextualResistance(value);
            ApplyTriggerFeedback(true, resistance);
        }

        private void HandleLeftTrigger(float value)
        {
            float resistance = CalculateContextualResistance(value);
            ApplyTriggerFeedback(false, resistance);
        }

        private float CalculateContextualResistance(float inputValue)
        {
            // Adjust resistance based on current activity
            float baseResistance = triggerResistance;
            
            // Example: Increase resistance when driving
            if (IsPlayerDriving())
            {
                baseResistance *= 1.5f;
            }
            
            // Example: Decrease resistance when tired
            if (IsPlayerTired())
            {
                baseResistance *= 0.7f;
            }

            return Mathf.Clamp(baseResistance * inputValue, 0f, 1f);
        }

        private void ApplyTriggerFeedback(bool isRightTrigger, float resistance)
        {
            // Apply adaptive trigger settings
            // This would use the actual PlayStation SDK in a real implementation
            Debug.Log($"Applied {resistance} resistance to {(isRightTrigger ? "right" : "left")} trigger");
        }

        private void HandleCreateButton()
        {
            // Handle create button press (PlayStation specific menu)
            ShowPlayStationMenu();
        }

        private void HandleGazePosition(Vector2 position)
        {
            if (!useEyeTracking) return;

            // Handle eye tracking for UI interaction
            ProcessEyeTracking(position);
        }

        private void HandleGazeTracking(float tracking)
        {
            if (!useEyeTracking) return;

            // Handle eye tracking confidence/status
            UpdateEyeTrackingStatus(tracking);
        }

        private void ProcessEyeTracking(Vector2 position)
        {
            // Convert gaze position to screen space
            Vector2 screenPosition = new Vector2(
                position.x * Screen.width,
                position.y * Screen.height
            );

            // Ray cast from gaze position
            Ray gazeRay = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(gazeRay, out hit))
            {
                HandleGazeInteraction(hit);
            }
        }

        private void HandleGazeInteraction(RaycastHit hit)
        {
            IGazeInteractable interactable = hit.collider.GetComponent<IGazeInteractable>();
            if (interactable != null)
            {
                interactable.OnGazeFocus();
            }
        }

        private void UpdateEyeTrackingStatus(float tracking)
        {
            // Update UI or system based on eye tracking confidence
            bool isTrackingReliable = tracking > 0.8f;
            
            if (!isTrackingReliable)
            {
                // Fall back to alternative input method
                UseAlternativeInput();
            }
        }

        private void UseAlternativeInput()
        {
            // Implement fallback input method
        }

        private bool IsPlayerDriving()
        {
            // Check if player is currently driving
            return false; // Implement actual check
        }

        private bool IsPlayerTired()
        {
            // Check player's fatigue level
            return false; // Implement actual check
        }

        private void ShowInventory()
        {
            // Show inventory UI with haptic feedback
            ProvideDualSenseFeedback(FeedbackType.Light);
        }

        private void ShowMap()
        {
            // Show map UI with haptic feedback
            ProvideDualSenseFeedback(FeedbackType.Medium);
        }

        private void ShowSocialMenu()
        {
            // Show social features UI with haptic feedback
            ProvideDualSenseFeedback(FeedbackType.Light);
        }

        private void ShowProperties()
        {
            // Show properties UI with haptic feedback
            ProvideDualSenseFeedback(FeedbackType.Medium);
        }

        private void ToggleQuickMenu()
        {
            // Toggle quick access menu with haptic feedback
            ProvideDualSenseFeedback(FeedbackType.Strong);
        }

        private void ShowPlayStationMenu()
        {
            // Show PlayStation specific menu
            ProvideDualSenseFeedback(FeedbackType.Light);
        }

        private void ProvideDualSenseFeedback(FeedbackType type)
        {
            float intensity = hapticIntensity;
            switch (type)
            {
                case FeedbackType.Light:
                    intensity *= 0.3f;
                    break;
                case FeedbackType.Medium:
                    intensity *= 0.6f;
                    break;
                case FeedbackType.Strong:
                    intensity *= 1.0f;
                    break;
            }

            // Apply haptic feedback
            // This would use the actual PlayStation SDK in a real implementation
            Debug.Log($"Applied {intensity} haptic feedback");
        }

        private void Update()
        {
            // Update gesture recognition
            if (currentGesture.Count > 0)
            {
                gestureTimer += Time.deltaTime;
                if (gestureTimer > gestureTimeout)
                {
                    currentGesture.Clear();
                    gestureTimer = 0f;
                }
            }
        }
    }

    public interface IGazeInteractable
    {
        void OnGazeFocus();
        void OnGazeLeave();
    }

    public enum FeedbackType
    {
        Light,
        Medium,
        Strong
    }
} 