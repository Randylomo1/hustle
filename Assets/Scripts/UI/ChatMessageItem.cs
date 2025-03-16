using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

namespace NairobiHustle.UI
{
    public enum MessageType
    {
        Text,
        BusinessProposal,
        TradeOffer,
        SystemNotification
    }

    public class ChatMessageItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private RectTransform messageContainer;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Image playerIcon;
        [SerializeField] private Image statusIcon;

        [Header("Business Components")]
        [SerializeField] private GameObject businessPanel;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;
        [SerializeField] private TextMeshProUGUI proposalTitle;
        [SerializeField] private TextMeshProUGUI proposalDetails;

        [Header("Style Settings")]
        [SerializeField] private Color selfMessageColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color otherMessageColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color systemMessageColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private float cornerRadius = 15f;
        [SerializeField] private float messagePadding = 10f;

        [Header("Animation Settings")]
        [SerializeField] private float popupDuration = 0.3f;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private Ease popupEase = Ease.OutBack;

        private MessageType messageType;
        private bool isSelfMessage;
        private Action<bool> onBusinessResponse;

        public void Initialize(string message, MessageType type, bool isSelf, string timestamp = null)
        {
            messageType = type;
            isSelfMessage = isSelf;

            // Set message content
            messageText.text = message;
            timestampText.text = timestamp ?? DateTime.Now.ToString("HH:mm");

            // Apply styling based on message type
            ApplyMessageStyle();

            // Show/hide components based on type
            businessPanel.SetActive(type == MessageType.BusinessProposal);
            statusIcon.gameObject.SetActive(type == MessageType.SystemNotification);

            // Setup animations
            SetupAnimations();

            // Add button listeners
            if (type == MessageType.BusinessProposal)
            {
                SetupBusinessProposal();
            }
        }

        private void ApplyMessageStyle()
        {
            // Set background color
            Color bgColor = messageType switch
            {
                MessageType.SystemNotification => systemMessageColor,
                _ => isSelfMessage ? selfMessageColor : otherMessageColor
            };
            backgroundImage.color = bgColor;

            // Set text alignment and anchors
            if (isSelfMessage)
            {
                messageContainer.anchorMin = new Vector2(1, 0);
                messageContainer.anchorMax = new Vector2(1, 0);
                messageContainer.pivot = new Vector2(1, 0);
                messageText.alignment = TextAlignmentOptions.Right;
            }
            else
            {
                messageContainer.anchorMin = new Vector2(0, 0);
                messageContainer.anchorMax = new Vector2(0, 0);
                messageContainer.pivot = new Vector2(0, 0);
                messageText.alignment = TextAlignmentOptions.Left;
            }

            // Apply padding
            messageContainer.sizeDelta = new Vector2(
                messageText.preferredWidth + messagePadding * 2,
                messageText.preferredHeight + messagePadding * 2
            );
        }

        private void SetupAnimations()
        {
            // Initial state
            messageContainer.localScale = Vector3.zero;
            backgroundImage.color = new Color(
                backgroundImage.color.r,
                backgroundImage.color.g,
                backgroundImage.color.b,
                0
            );

            // Animate scale
            messageContainer.DOScale(Vector3.one, popupDuration)
                .SetEase(popupEase);

            // Animate fade
            backgroundImage.DOFade(1f, fadeInDuration);
            messageText.DOFade(1f, fadeInDuration);
            timestampText.DOFade(0.5f, fadeInDuration);
        }

        private void SetupBusinessProposal()
        {
            acceptButton.onClick.AddListener(() => HandleBusinessResponse(true));
            rejectButton.onClick.AddListener(() => HandleBusinessResponse(false));
        }

        public void SetBusinessProposalDetails(string title, string details, Action<bool> responseCallback)
        {
            proposalTitle.text = title;
            proposalDetails.text = details;
            onBusinessResponse = responseCallback;
        }

        private void HandleBusinessResponse(bool accepted)
        {
            // Disable buttons after response
            acceptButton.interactable = false;
            rejectButton.interactable = false;

            // Animate response
            Color responseColor = accepted ? Color.green : Color.red;
            backgroundImage.DOColor(responseColor, 0.3f);

            // Invoke callback
            onBusinessResponse?.Invoke(accepted);
        }

        public void UpdateStatus(bool success)
        {
            if (statusIcon != null)
            {
                statusIcon.color = success ? Color.green : Color.red;
                statusIcon.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
        }

        private void OnDestroy()
        {
            // Kill all tweens
            DOTween.Kill(messageContainer);
            DOTween.Kill(backgroundImage);
            DOTween.Kill(messageText);
            DOTween.Kill(timestampText);
            DOTween.Kill(statusIcon?.transform);
        }
    }
} 