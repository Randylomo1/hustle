using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace NairobiHustle.UI
{
    public class CommunicationUI : MonoBehaviour
    {
        [Header("Chat UI")]
        [SerializeField] private RectTransform chatPanel;
        [SerializeField] private RectTransform quickMessagePanel;
        [SerializeField] private Button toggleChatButton;
        [SerializeField] private Button toggleVoiceButton;
        [SerializeField] private Image microphoneIcon;
        [SerializeField] private Image voiceWaveform;

        [Header("Business Communication")]
        [SerializeField] private RectTransform businessPanel;
        [SerializeField] private Button toggleBusinessButton;
        [SerializeField] private TMP_Dropdown proposalTypeDropdown;
        [SerializeField] private TMP_InputField[] proposalParameters;
        [SerializeField] private Button sendProposalButton;

        [Header("Quick Access")]
        [SerializeField] private QuickAccessButton[] quickAccessButtons;
        [SerializeField] private float quickAccessRadius = 100f;
        [SerializeField] private float quickAccessShowDuration = 0.3f;

        [Header("Gesture Controls")]
        [SerializeField] private float gestureThreshold = 50f;
        [SerializeField] private float gestureHoldTime = 0.5f;

        private LiveCommunicationSystem communicationSystem;
        private bool isChatVisible = true;
        private bool isQuickAccessVisible = false;
        private Vector2 gestureStartPosition;
        private float gestureHoldTimer;

        private void Awake()
        {
            communicationSystem = FindObjectOfType<LiveCommunicationSystem>();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Chat toggle
            toggleChatButton.onClick.AddListener(ToggleChat);
            toggleVoiceButton.onClick.AddListener(ToggleVoice);

            // Business panel
            toggleBusinessButton.onClick.AddListener(ToggleBusinessPanel);
            sendProposalButton.onClick.AddListener(SendBusinessProposal);

            // Initialize proposal dropdown
            proposalTypeDropdown.ClearOptions();
            proposalTypeDropdown.AddOptions(new List<string>
            {
                "Partnership Proposal",
                "Investment Opportunity",
                "Property Deal",
                "Trading Offer"
            });

            // Quick access buttons
            InitializeQuickAccess();
        }

        private void InitializeQuickAccess()
        {
            float angleStep = 360f / quickAccessButtons.Length;
            for (int i = 0; i < quickAccessButtons.Length; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 position = new Vector2(
                    Mathf.Cos(angle) * quickAccessRadius,
                    Mathf.Sin(angle) * quickAccessRadius
                );

                QuickAccessButton button = quickAccessButtons[i];
                button.rectTransform.anchoredPosition = position;
                button.button.onClick.AddListener(() => SendQuickMessage(button.messageIndex));
            }

            SetQuickAccessVisible(false);
        }

        private void Update()
        {
            HandleGestures();
            UpdateVoiceVisualization();
        }

        private void HandleGestures()
        {
            if (Input.GetMouseButtonDown(1)) // Right mouse button
            {
                gestureStartPosition = Input.mousePosition;
                gestureHoldTimer = 0f;
                isQuickAccessVisible = true;
                SetQuickAccessVisible(true);
            }
            else if (Input.GetMouseButton(1))
            {
                gestureHoldTimer += Time.deltaTime;
                if (gestureHoldTimer >= gestureHoldTime)
                {
                    Vector2 currentPosition = Input.mousePosition;
                    Vector2 direction = currentPosition - gestureStartPosition;

                    if (direction.magnitude >= gestureThreshold)
                    {
                        int gestureIndex = GetGestureIndex(direction);
                        if (gestureIndex >= 0)
                        {
                            SendQuickMessage(gestureIndex);
                            isQuickAccessVisible = false;
                            SetQuickAccessVisible(false);
                        }
                    }
                }
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isQuickAccessVisible = false;
                SetQuickAccessVisible(false);
            }
        }

        private int GetGestureIndex(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            float angleStep = 360f / quickAccessButtons.Length;
            int index = Mathf.RoundToInt(angle / angleStep);
            return index % quickAccessButtons.Length;
        }

        private void SendQuickMessage(int index)
        {
            communicationSystem.SendQuickMessage(index);
        }

        private void ToggleChat()
        {
            isChatVisible = !isChatVisible;
            chatPanel.gameObject.SetActive(isChatVisible);
        }

        private void ToggleVoice()
        {
            bool enabled = !communicationSystem.IsVoiceEnabled;
            communicationSystem.ToggleVoiceChat(enabled);
            UpdateVoiceUI(enabled);
        }

        private void UpdateVoiceUI(bool enabled)
        {
            microphoneIcon.color = enabled ? Color.green : Color.red;
            voiceWaveform.gameObject.SetActive(enabled);
        }

        private void ToggleBusinessPanel()
        {
            businessPanel.gameObject.SetActive(!businessPanel.gameObject.activeSelf);
        }

        private void SendBusinessProposal()
        {
            ProposalType type = (ProposalType)proposalTypeDropdown.value;
            object[] parameters = new object[proposalParameters.Length];
            
            for (int i = 0; i < proposalParameters.Length; i++)
            {
                if (float.TryParse(proposalParameters[i].text, out float value))
                {
                    parameters[i] = value;
                }
                else
                {
                    parameters[i] = proposalParameters[i].text;
                }
            }

            communicationSystem.SendBusinessProposal(type, parameters);
        }

        private void SetQuickAccessVisible(bool visible)
        {
            quickMessagePanel.gameObject.SetActive(visible);
            foreach (QuickAccessButton button in quickAccessButtons)
            {
                button.SetVisible(visible, quickAccessShowDuration);
            }
        }

        private void UpdateVoiceVisualization()
        {
            if (communicationSystem.IsVoiceEnabled && communicationSystem.IsTransmitting)
            {
                // Update voice waveform visualization
                float amplitude = communicationSystem.GetVoiceAmplitude();
                voiceWaveform.transform.localScale = new Vector3(1f, amplitude, 1f);
            }
        }
    }

    [System.Serializable]
    public class QuickAccessButton
    {
        public RectTransform rectTransform;
        public Button button;
        public int messageIndex;
        public TextMeshProUGUI label;
        public Image icon;

        public void SetVisible(bool visible, float duration)
        {
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }
        }
    }
} 