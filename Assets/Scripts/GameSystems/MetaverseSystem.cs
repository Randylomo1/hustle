using UnityEngine;
using UnityEngine.XR;
using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class MetaverseSystem : MonoBehaviourPunCallbacks
    {
        [Header("Metaverse Settings")]
        [SerializeField] private string metaverseRoomPrefix = "NairobiHustle_";
        [SerializeField] private int maxPlayersPerInstance = 100;
        [SerializeField] private float voiceChatRange = 20f;
        [SerializeField] private float playerInteractionRange = 5f;

        [Header("Virtual Reality")]
        [SerializeField] private GameObject vrPlayerPrefab;
        [SerializeField] private GameObject nonVrPlayerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        
        [Header("Virtual Properties")]
        [SerializeField] private VirtualProperty[] availableProperties = new VirtualProperty[]
        {
            new VirtualProperty {
                name = "Virtual Matatu Terminal",
                basePrice = 1000000f,
                dailyIncome = 50000f,
                maxCapacity = 50,
                upgradeMultiplier = 1.5f
            },
            new VirtualProperty {
                name = "Virtual Car Showroom",
                basePrice = 2000000f,
                dailyIncome = 75000f,
                maxCapacity = 30,
                upgradeMultiplier = 1.8f
            },
            new VirtualProperty {
                name = "Virtual Mechanic Shop",
                basePrice = 500000f,
                dailyIncome = 25000f,
                maxCapacity = 20,
                upgradeMultiplier = 1.3f
            }
        };

        [Header("Social Features")]
        [SerializeField] private float tradingRange = 10f;
        [SerializeField] private float businessMeetingRange = 15f;
        
        private bool isVREnabled;
        private PhotonView photonView;
        private Dictionary<string, VirtualProperty> ownedProperties;
        private List<MetaversePlayer> nearbyPlayers;
        private VoiceChatManager voiceChatManager;
        private TradeManager tradeManager;
        private RealLifeDynamics realLifeDynamics;

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            ownedProperties = new Dictionary<string, VirtualProperty>();
            nearbyPlayers = new List<MetaversePlayer>();
            voiceChatManager = GetComponent<VoiceChatManager>();
            tradeManager = GetComponent<TradeManager>();
            realLifeDynamics = GetComponent<RealLifeDynamics>();

            // Check if VR is available and enabled
            isVREnabled = XRSettings.enabled && XRSettings.isDeviceActive;
            
            // Initialize Photon networking
            PhotonNetwork.AutomaticallySyncScene = true;
            ConnectToMetaverse();
        }

        private void ConnectToMetaverse()
        {
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = Application.version;
            }
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Metaverse Master Server");
            JoinMetaverseRoom();
        }

        private void JoinMetaverseRoom()
        {
            // Join or create a room based on player location
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerInstance,
                PublishUserId = true,
                IsVisible = true
            };

            string roomName = metaverseRoomPrefix + GetPlayerLocation();
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
        }

        private string GetPlayerLocation()
        {
            // Get the player's current in-game location/district
            return "Nairobi_CBD"; // This should be dynamic based on player position
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"Joined Metaverse Room: {PhotonNetwork.CurrentRoom.Name}");
            SpawnMetaversePlayer();
        }

        private void SpawnMetaversePlayer()
        {
            Transform spawnPoint = GetRandomSpawnPoint();
            GameObject playerPrefab = isVREnabled ? vrPlayerPrefab : nonVrPlayerPrefab;
            
            GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, 
                spawnPoint.position, spawnPoint.rotation);
                
            // Initialize player components
            MetaversePlayer metaversePlayer = player.GetComponent<MetaversePlayer>();
            metaversePlayer.Initialize(realLifeDynamics);
        }

        private Transform GetRandomSpawnPoint()
        {
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        }

        public void PurchaseVirtualProperty(string propertyName)
        {
            foreach (VirtualProperty property in availableProperties)
            {
                if (property.name == propertyName && !ownedProperties.ContainsKey(propertyName))
                {
                    if (TryPurchaseProperty(property))
                    {
                        ownedProperties.Add(propertyName, property);
                        photonView.RPC("SyncPropertyOwnership", RpcTarget.All, 
                            PhotonNetwork.LocalPlayer.UserId, propertyName);
                    }
                    break;
                }
            }
        }

        private bool TryPurchaseProperty(VirtualProperty property)
        {
            PlayerWallet wallet = GetComponent<PlayerWallet>();
            if (wallet.GetBalance() >= property.basePrice)
            {
                wallet.DeductFunds(property.basePrice);
                return true;
            }
            return false;
        }

        [PunRPC]
        private void SyncPropertyOwnership(string playerId, string propertyName)
        {
            // Update property ownership across the network
            Debug.Log($"Player {playerId} purchased {propertyName}");
        }

        public void InitiatePlayerTrade(MetaversePlayer targetPlayer)
        {
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= tradingRange)
            {
                tradeManager.StartTrade(targetPlayer);
            }
        }

        public void StartBusinessMeeting(List<MetaversePlayer> participants)
        {
            foreach (MetaversePlayer participant in participants)
            {
                if (Vector3.Distance(transform.position, participant.transform.position) > businessMeetingRange)
                {
                    return;
                }
            }
            
            // Create virtual meeting room
            photonView.RPC("JoinBusinessMeeting", RpcTarget.All, 
                participants.ConvertAll(p => p.PhotonView.ViewID).ToArray());
        }

        [PunRPC]
        private void JoinBusinessMeeting(int[] participantIds)
        {
            // Initialize virtual meeting room
            Debug.Log("Joining business meeting");
            // Additional meeting room logic here
        }

        private void Update()
        {
            if (PhotonNetwork.IsConnected)
            {
                UpdateNearbyPlayers();
                UpdateVoiceChat();
                SyncMetaverseState();
            }
        }

        private void UpdateNearbyPlayers()
        {
            nearbyPlayers.Clear();
            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (player != PhotonNetwork.LocalPlayer)
                {
                    MetaversePlayer metaversePlayer = GetMetaversePlayer(player);
                    if (IsPlayerInRange(metaversePlayer, playerInteractionRange))
                    {
                        nearbyPlayers.Add(metaversePlayer);
                    }
                }
            }
        }

        private void UpdateVoiceChat()
        {
            foreach (MetaversePlayer player in nearbyPlayers)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= voiceChatRange)
                {
                    voiceChatManager.EnableVoiceChatWith(player, 1f - (distance / voiceChatRange));
                }
                else
                {
                    voiceChatManager.DisableVoiceChatWith(player);
                }
            }
        }

        private void SyncMetaverseState()
        {
            if (photonView.IsMine)
            {
                // Sync important game state across the network
                photonView.RPC("UpdateMetaverseState", RpcTarget.All,
                    JsonUtility.ToJson(GetMetaverseState()));
            }
        }

        private MetaversePlayer GetMetaversePlayer(Player photonPlayer)
        {
            // Find and return the MetaversePlayer component for the given Photon player
            return null; // Implement actual player finding logic
        }

        private bool IsPlayerInRange(MetaversePlayer player, float range)
        {
            if (player == null) return false;
            return Vector3.Distance(transform.position, player.transform.position) <= range;
        }

        private MetaverseState GetMetaverseState()
        {
            return new MetaverseState
            {
                timestamp = DateTime.Now.Ticks,
                playerCount = PhotonNetwork.CurrentRoom.PlayerCount,
                activeEvents = GetActiveEvents()
            };
        }

        private string[] GetActiveEvents()
        {
            // Get current active events in the metaverse
            return new string[0]; // Implement actual event tracking
        }
    }

    [System.Serializable]
    public class VirtualProperty
    {
        public string name;
        public float basePrice;
        public float dailyIncome;
        public int maxCapacity;
        public float upgradeMultiplier;
    }

    [System.Serializable]
    public class MetaverseState
    {
        public long timestamp;
        public int playerCount;
        public string[] activeEvents;
    }
} 