using UnityEngine;
using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.PUN;
using System;
using System.Collections.Generic;
using UnityEngine.Audio;
using TMPro;

namespace NairobiHustle.GameSystems
{
    public class LiveCommunicationSystem : MonoBehaviourPunCallbacks
    {
        [Header("Voice Chat Settings")]
        [SerializeField] private float voiceDetectionThreshold = 0.01f;
        [SerializeField] private bool isVoiceEnabled = true;
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float maxVoiceDistance = 20f;
        [SerializeField] private AudioMixerGroup voiceMixerGroup;

        [Header("Text Chat Settings")]
        [SerializeField] private int maxMessageLength = 200;
        [SerializeField] private int messageHistoryLength = 50;
        [SerializeField] private float chatFadeTime = 5f;
        [SerializeField] private TextMeshProUGUI chatDisplay;
        [SerializeField] private TMP_InputField chatInput;

        [Header("Quick Communication")]
        [SerializeField] private QuickMessage[] quickMessages = new QuickMessage[]
        {
            new QuickMessage { 
                message = "Karibu! Want to do business?",
                category = MessageCategory.Business,
                gesture = "Wave"
            },
            new QuickMessage {
                message = "Let's form a partnership!",
                category = MessageCategory.Business,
                gesture = "Handshake"
            },
            new QuickMessage {
                message = "Check out my new property!",
                category = MessageCategory.Property,
                gesture = "Point"
            },
            new QuickMessage {
                message = "Interested in trading?",
                category = MessageCategory.Trading,
                gesture = "Nod"
            }
        };

        [Header("Business Communication")]
        [SerializeField] private BusinessProposal[] proposalTemplates = new BusinessProposal[]
        {
            new BusinessProposal {
                title = "Partnership Offer",
                type = ProposalType.Partnership,
                template = "I propose a partnership with {0}% profit sharing."
            },
            new BusinessProposal {
                title = "Investment Opportunity",
                type = ProposalType.Investment,
                template = "Invest in my business for {0}% returns in {1} days."
            },
            new BusinessProposal {
                title = "Property Deal",
                type = ProposalType.Property,
                template = "Interested in buying my property for {0} KSH?"
            }
        };

        private PhotonVoiceNetwork voiceNetwork;
        private Recorder voiceRecorder;
        private Speaker voiceSpeaker;
        private List<ChatMessage> messageHistory;
        private Dictionary<string, PlayerCommunicationState> playerStates;
        private float lastMessageTime;

        private void Awake()
        {
            InitializeVoiceChat();
            InitializeTextChat();
            messageHistory = new List<ChatMessage>();
            playerStates = new Dictionary<string, PlayerCommunicationState>();
        }

        private void InitializeVoiceChat()
        {
            voiceNetwork = PhotonVoiceNetwork.Instance;
            voiceRecorder = gameObject.AddComponent<Recorder>();
            voiceSpeaker = gameObject.AddComponent<Speaker>();

            // Configure voice settings
            voiceRecorder.VoiceDetectionThreshold = voiceDetectionThreshold;
            voiceRecorder.TransmitEnabled = isVoiceEnabled;
            voiceRecorder.InterestGroup = 0; // Default group

            // Configure spatial audio
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = spatialBlend;
            audioSource.maxDistance = maxVoiceDistance;
            audioSource.outputAudioMixerGroup = voiceMixerGroup;
        }

        private void InitializeTextChat()
        {
            chatInput.onSubmit.AddListener(SendChatMessage);
            chatDisplay.gameObject.SetActive(true);
            UpdateChatDisplay();
        }

        public void SendChatMessage(string message)
        {
            if (string.IsNullOrEmpty(message) || message.Length > maxMessageLength)
                return;

            ChatMessage newMessage = new ChatMessage
            {
                senderId = PhotonNetwork.LocalPlayer.UserId,
                senderName = PhotonNetwork.LocalPlayer.NickName,
                content = message,
                timestamp = DateTime.Now,
                category = DetermineChatCategory(message)
            };

            photonView.RPC("ReceiveChatMessage", RpcTarget.All, 
                JsonUtility.ToJson(newMessage));

            chatInput.text = "";
        }

        [PunRPC]
        private void ReceiveChatMessage(string messageJson)
        {
            ChatMessage message = JsonUtility.FromJson<ChatMessage>(messageJson);
            messageHistory.Add(message);

            if (messageHistory.Count > messageHistoryLength)
            {
                messageHistory.RemoveAt(0);
            }

            UpdateChatDisplay();
            lastMessageTime = Time.time;
        }

        private void UpdateChatDisplay()
        {
            string displayText = "";
            foreach (ChatMessage msg in messageHistory)
            {
                string timeStr = msg.timestamp.ToString("HH:mm");
                string categoryColor = GetCategoryColor(msg.category);
                displayText += $"<color={categoryColor}>[{timeStr}] {msg.senderName}: {msg.content}</color>\n";
            }
            chatDisplay.text = displayText;
        }

        public void SendQuickMessage(int messageIndex)
        {
            if (messageIndex < 0 || messageIndex >= quickMessages.Length)
                return;

            QuickMessage qm = quickMessages[messageIndex];
            SendChatMessage(qm.message);

            if (!string.IsNullOrEmpty(qm.gesture))
            {
                PlayGesture(qm.gesture);
            }
        }

        public void SendBusinessProposal(ProposalType type, params object[] parameters)
        {
            foreach (BusinessProposal proposal in proposalTemplates)
            {
                if (proposal.type == type)
                {
                    string message = string.Format(proposal.template, parameters);
                    SendChatMessage($"[Business Proposal] {proposal.title}: {message}");
                    break;
                }
            }
        }

        public void ToggleVoiceChat(bool enabled)
        {
            isVoiceEnabled = enabled;
            voiceRecorder.TransmitEnabled = enabled;
            
            photonView.RPC("UpdatePlayerVoiceState", RpcTarget.All,
                PhotonNetwork.LocalPlayer.UserId, enabled);
        }

        [PunRPC]
        private void UpdatePlayerVoiceState(string playerId, bool isVoiceEnabled)
        {
            if (!playerStates.ContainsKey(playerId))
            {
                playerStates[playerId] = new PlayerCommunicationState();
            }
            playerStates[playerId].isVoiceEnabled = isVoiceEnabled;
        }

        public void MutePlayer(string playerId, bool isMuted)
        {
            if (!playerStates.ContainsKey(playerId))
            {
                playerStates[playerId] = new PlayerCommunicationState();
            }
            playerStates[playerId].isMuted = isMuted;
        }

        private void PlayGesture(string gestureName)
        {
            // Trigger animation on player avatar
            photonView.RPC("SyncPlayerGesture", RpcTarget.All,
                PhotonNetwork.LocalPlayer.UserId, gestureName);
        }

        [PunRPC]
        private void SyncPlayerGesture(string playerId, string gestureName)
        {
            // Update player avatar animation
            Debug.Log($"Player {playerId} performed gesture: {gestureName}");
        }

        private MessageCategory DetermineChatCategory(string message)
        {
            message = message.ToLower();
            if (message.Contains("buy") || message.Contains("sell") || message.Contains("trade"))
                return MessageCategory.Trading;
            if (message.Contains("business") || message.Contains("partner"))
                return MessageCategory.Business;
            if (message.Contains("property") || message.Contains("house") || message.Contains("building"))
                return MessageCategory.Property;
            return MessageCategory.General;
        }

        private string GetCategoryColor(MessageCategory category)
        {
            switch (category)
            {
                case MessageCategory.Business:
                    return "#4CAF50"; // Green
                case MessageCategory.Trading:
                    return "#2196F3"; // Blue
                case MessageCategory.Property:
                    return "#FFC107"; // Amber
                default:
                    return "#FFFFFF"; // White
            }
        }

        private void Update()
        {
            // Fade chat display when inactive
            if (Time.time - lastMessageTime > chatFadeTime)
            {
                float alpha = Mathf.Lerp(1f, 0f, (Time.time - lastMessageTime - chatFadeTime) / 2f);
                chatDisplay.alpha = alpha;
            }
            else
            {
                chatDisplay.alpha = 1f;
            }

            // Update voice chat visualization
            if (isVoiceEnabled && voiceRecorder.IsCurrentlyTransmitting)
            {
                UpdateVoiceVisualization();
            }
        }

        private void UpdateVoiceVisualization()
        {
            // Visualize voice activity (e.g., microphone icon, voice waves)
        }

        private void OnDestroy()
        {
            if (voiceRecorder != null)
            {
                voiceRecorder.TransmitEnabled = false;
            }
        }
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string senderId;
        public string senderName;
        public string content;
        public DateTime timestamp;
        public MessageCategory category;
    }

    [System.Serializable]
    public class QuickMessage
    {
        public string message;
        public MessageCategory category;
        public string gesture;
    }

    [System.Serializable]
    public class BusinessProposal
    {
        public string title;
        public ProposalType type;
        public string template;
    }

    public class PlayerCommunicationState
    {
        public bool isVoiceEnabled;
        public bool isMuted;
        public DateTime lastMessageTime;
    }

    public enum MessageCategory
    {
        General,
        Business,
        Trading,
        Property
    }

    public enum ProposalType
    {
        Partnership,
        Investment,
        Property,
        Trading
    }
} 