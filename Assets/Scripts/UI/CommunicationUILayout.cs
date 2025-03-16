using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace NairobiHustle.UI
{
    public class CommunicationUILayout : MonoBehaviour
    {
        [Header("Layout Settings")]
        [SerializeField] private RectTransform mainPanel;
        [SerializeField] private float panelWidth = 300f;
        [SerializeField] private float panelHeight = 400f;
        [SerializeField] private float minimizedHeight = 50f;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease animationEase = Ease.OutQuad;

        [Header("Chat Layout")]
        [SerializeField] private RectTransform messageContainer;
        [SerializeField] private float messageSpacing = 10f;
        [SerializeField] private float messagePadding = 15f;
        [SerializeField] private float maxMessageWidth = 250f;

        [Header("Business Panel Layout")]
        [SerializeField] private RectTransform businessContainer;
        [SerializeField] private float businessPanelWidth = 400f;
        [SerializeField] private float businessPanelHeight = 500f;

        [Header("Quick Access Layout")]
        [SerializeField] private RectTransform quickAccessContainer;
        [SerializeField] private float quickAccessButtonSize = 60f;
        [SerializeField] private float quickAccessSpacing = 10f;

        private List<RectTransform> messageItems = new List<RectTransform>();
        private Vector2 originalPosition;
        private bool isMinimized = false;
        private bool isBusinessPanelVisible = false;

        private void Awake()
        {
            originalPosition = mainPanel.anchoredPosition;
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // Set initial panel size
            mainPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

            // Initialize message container
            messageContainer.anchorMin = new Vector2(0, 0);
            messageContainer.anchorMax = new Vector2(1, 1);
            messageContainer.sizeDelta = Vector2.zero;
            messageContainer.anchoredPosition = Vector2.zero;

            // Initialize business container
            businessContainer.gameObject.SetActive(false);
            businessContainer.sizeDelta = new Vector2(businessPanelWidth, businessPanelHeight);

            // Initialize quick access container
            InitializeQuickAccessLayout();
        }

        private void InitializeQuickAccessLayout()
        {
            int buttonCount = quickAccessContainer.childCount;
            float totalAngle = 360f;
            float angleStep = totalAngle / buttonCount;
            float radius = (quickAccessButtonSize + quickAccessSpacing) * 2f;

            for (int i = 0; i < buttonCount; i++)
            {
                RectTransform button = quickAccessContainer.GetChild(i) as RectTransform;
                if (button != null)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector2 position = new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    );

                    button.sizeDelta = new Vector2(quickAccessButtonSize, quickAccessButtonSize);
                    button.anchoredPosition = position;
                    button.localScale = Vector3.zero;
                }
            }
        }

        public void AddMessage(RectTransform messageItem)
        {
            messageItems.Add(messageItem);
            messageItem.SetParent(messageContainer, false);
            
            // Set message width
            float width = Mathf.Min(maxMessageWidth, panelWidth - messagePadding * 2);
            messageItem.sizeDelta = new Vector2(width, messageItem.sizeDelta.y);

            // Position message
            float yPosition = messageItems.Count > 1 
                ? messageItems[messageItems.Count - 2].anchoredPosition.y - messageItem.sizeDelta.y - messageSpacing 
                : -messagePadding;
            
            messageItem.anchoredPosition = new Vector2(messagePadding, yPosition);

            // Animate message entry
            messageItem.localScale = Vector3.zero;
            messageItem.DOScale(Vector3.one, animationDuration).SetEase(animationEase);

            // Update content size
            float contentHeight = Mathf.Abs(yPosition) + messageItem.sizeDelta.y + messagePadding;
            messageContainer.sizeDelta = new Vector2(0, contentHeight);
        }

        public void ToggleMinimize()
        {
            isMinimized = !isMinimized;
            float targetHeight = isMinimized ? minimizedHeight : panelHeight;

            mainPanel.DOSizeDelta(new Vector2(panelWidth, targetHeight), animationDuration)
                .SetEase(animationEase);
        }

        public void ToggleBusinessPanel()
        {
            isBusinessPanelVisible = !isBusinessPanelVisible;
            businessContainer.gameObject.SetActive(isBusinessPanelVisible);

            if (isBusinessPanelVisible)
            {
                businessContainer.localScale = Vector3.zero;
                businessContainer.DOScale(Vector3.one, animationDuration)
                    .SetEase(animationEase);
            }
            else
            {
                businessContainer.DOScale(Vector3.zero, animationDuration)
                    .SetEase(animationEase)
                    .OnComplete(() => businessContainer.gameObject.SetActive(false));
            }
        }

        public void ShowQuickAccess()
        {
            quickAccessContainer.gameObject.SetActive(true);
            foreach (RectTransform button in quickAccessContainer)
            {
                button.DOScale(Vector3.one, animationDuration)
                    .SetEase(animationEase);
            }
        }

        public void HideQuickAccess()
        {
            foreach (RectTransform button in quickAccessContainer)
            {
                button.DOScale(Vector3.zero, animationDuration)
                    .SetEase(animationEase);
            }
            DOVirtual.DelayedCall(animationDuration, () => 
                quickAccessContainer.gameObject.SetActive(false));
        }

        public void ClearMessages()
        {
            foreach (RectTransform message in messageItems)
            {
                message.DOScale(Vector3.zero, animationDuration)
                    .SetEase(animationEase)
                    .OnComplete(() => Destroy(message.gameObject));
            }
            messageItems.Clear();
            messageContainer.sizeDelta = Vector2.zero;
        }

        public void SetPanelPosition(Vector2 position)
        {
            mainPanel.DOAnchorPos(position, animationDuration)
                .SetEase(animationEase);
        }

        public void ResetPanelPosition()
        {
            SetPanelPosition(originalPosition);
        }

        private void OnDestroy()
        {
            // Kill all tweens to prevent memory leaks
            DOTween.Kill(mainPanel);
            DOTween.Kill(businessContainer);
            foreach (RectTransform message in messageItems)
            {
                if (message != null)
                {
                    DOTween.Kill(message);
                }
            }
        }
    }
} 