using UnityEngine;
using Photon.Pun;
using UnityEngine.XR;
using System.Collections.Generic;

namespace NairobiHustle.GameSystems
{
    public class MetaversePlayer : MonoBehaviourPunCallbacks
    {
        [Header("Avatar Settings")]
        [SerializeField] private GameObject vrAvatar;
        [SerializeField] private GameObject nonVrAvatar;
        [SerializeField] private Animator avatarAnimator;
        
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 3f;
        [SerializeField] private LayerMask interactableLayers;
        [SerializeField] private float emoteWheelRadius = 100f;

        [Header("Business Settings")]
        [SerializeField] private float businessReputationMultiplier = 1f;
        [SerializeField] private float networkingBonus = 0.1f;

        public PhotonView PhotonView { get; private set; }
        
        private RealLifeDynamics realLifeDynamics;
        private Transform vrCamera;
        private bool isVRMode;
        private Dictionary<string, float> businessRelations;
        private List<string> activeEmotes;
        private Vector3 lastPosition;
        private float idleTime;

        private void Awake()
        {
            PhotonView = GetComponent<PhotonView>();
            businessRelations = new Dictionary<string, float>();
            activeEmotes = new List<string>();
            lastPosition = transform.position;
            
            // Set up VR if available
            isVRMode = XRSettings.enabled && XRSettings.isDeviceActive;
            SetupAvatar();
        }

        public void Initialize(RealLifeDynamics dynamics)
        {
            realLifeDynamics = dynamics;
            if (PhotonView.IsMine)
            {
                // Initialize player-specific settings
                SetupPlayerComponents();
            }
        }

        private void SetupAvatar()
        {
            vrAvatar.SetActive(isVRMode);
            nonVrAvatar.SetActive(!isVRMode);

            if (isVRMode)
            {
                vrCamera = Camera.main.transform;
                // Additional VR setup (controllers, tracking, etc.)
            }
        }

        private void SetupPlayerComponents()
        {
            // Initialize player-specific components
            if (isVRMode)
            {
                SetupVRControls();
            }
            else
            {
                SetupDesktopControls();
            }
        }

        private void SetupVRControls()
        {
            // Set up VR-specific input handling
            // VR controller mappings and interactions
        }

        private void SetupDesktopControls()
        {
            // Set up keyboard/mouse controls
            // Traditional input mappings
        }

        private void Update()
        {
            if (!PhotonView.IsMine) return;

            UpdateMovement();
            CheckInteractions();
            UpdateAnimations();
            SyncPlayerState();
        }

        private void UpdateMovement()
        {
            if (isVRMode)
            {
                UpdateVRMovement();
            }
            else
            {
                UpdateDesktopMovement();
            }

            // Calculate idle time
            if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
            {
                idleTime += Time.deltaTime;
            }
            else
            {
                idleTime = 0f;
                lastPosition = transform.position;
            }
        }

        private void UpdateVRMovement()
        {
            // Handle VR movement using controllers
            // Implement teleportation or smooth locomotion
        }

        private void UpdateDesktopMovement()
        {
            // Handle traditional keyboard/mouse movement
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;
            transform.Translate(movement * Time.deltaTime * 5f);
        }

        private void CheckInteractions()
        {
            Collider[] nearbyObjects = Physics.OverlapSphere(
                transform.position, 
                interactionRadius, 
                interactableLayers
            );

            foreach (Collider col in nearbyObjects)
            {
                IMetaverseInteractable interactable = col.GetComponent<IMetaverseInteractable>();
                if (interactable != null)
                {
                    HandleInteraction(interactable);
                }
            }
        }

        private void HandleInteraction(IMetaverseInteractable interactable)
        {
            if (isVRMode)
            {
                // VR-specific interaction handling
                if (Input.GetButton("VR_Interact"))
                {
                    interactable.OnVRInteract(this);
                }
            }
            else
            {
                // Desktop interaction handling
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.OnInteract(this);
                }
            }
        }

        private void UpdateAnimations()
        {
            if (avatarAnimator != null)
            {
                // Update animator parameters based on player state
                avatarAnimator.SetFloat("Speed", GetMovementSpeed());
                avatarAnimator.SetBool("IsIdle", idleTime > 5f);
                
                // Sync animations across network
                if (PhotonView.IsMine)
                {
                    PhotonView.RPC("SyncAnimationState", RpcTarget.All, 
                        GetMovementSpeed(), idleTime > 5f);
                }
            }
        }

        private float GetMovementSpeed()
        {
            return Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        }

        [PunRPC]
        private void SyncAnimationState(float speed, bool isIdle)
        {
            if (!PhotonView.IsMine && avatarAnimator != null)
            {
                avatarAnimator.SetFloat("Speed", speed);
                avatarAnimator.SetBool("IsIdle", isIdle);
            }
        }

        public void ShowEmoteWheel()
        {
            if (!PhotonView.IsMine) return;

            // Display emote wheel UI
            // Handle emote selection
        }

        public void PlayEmote(string emoteName)
        {
            if (!PhotonView.IsMine) return;

            PhotonView.RPC("SyncEmote", RpcTarget.All, emoteName);
        }

        [PunRPC]
        private void SyncEmote(string emoteName)
        {
            if (avatarAnimator != null)
            {
                avatarAnimator.SetTrigger(emoteName);
            }
        }

        public void UpdateBusinessRelation(string playerId, float relationshipChange)
        {
            if (!businessRelations.ContainsKey(playerId))
            {
                businessRelations[playerId] = 0f;
            }

            businessRelations[playerId] = Mathf.Clamp01(
                businessRelations[playerId] + relationshipChange);

            // Apply networking bonus to business reputation
            if (businessRelations[playerId] > 0.5f)
            {
                businessReputationMultiplier += networkingBonus;
            }
        }

        public float GetBusinessReputation()
        {
            return businessReputationMultiplier * 
                (1f + businessRelations.Count * networkingBonus);
        }

        private void SyncPlayerState()
        {
            if (!PhotonView.IsMine) return;

            // Sync important player state across the network
            PhotonView.RPC("UpdatePlayerState", RpcTarget.All,
                transform.position,
                transform.rotation,
                GetBusinessReputation(),
                activeEmotes.ToArray()
            );
        }

        [PunRPC]
        private void UpdatePlayerState(Vector3 position, Quaternion rotation, 
            float reputation, string[] emotes)
        {
            if (!PhotonView.IsMine)
            {
                transform.position = position;
                transform.rotation = rotation;
                businessReputationMultiplier = reputation;
                activeEmotes = new List<string>(emotes);
            }
        }

        private void OnDrawGizmos()
        {
            // Draw interaction radius in editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }

    public interface IMetaverseInteractable
    {
        void OnInteract(MetaversePlayer player);
        void OnVRInteract(MetaversePlayer player);
    }
} 