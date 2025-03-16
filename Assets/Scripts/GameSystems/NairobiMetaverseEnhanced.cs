using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class NairobiMetaverseEnhanced : MonoBehaviourPunCallbacks
    {
        [Header("Platform Settings")]
        [SerializeField] private bool isPlayStation;
        [SerializeField] private GameObject psVRPrefab;
        [SerializeField] private GameObject psDualSensePrefab;

        [Header("Nairobi Districts")]
        [SerializeField] private NairobiDistrict[] districts = new NairobiDistrict[]
        {
            new NairobiDistrict {
                name = "CBD",
                propertyValueMultiplier = 2.0f,
                footTraffic = 1000,
                businessOpportunityRate = 0.8f,
                crimeRate = 0.4f,
                locations = new string[] { "Tom Mboya Street", "Moi Avenue", "Kimathi Street" }
            },
            new NairobiDistrict {
                name = "Westlands",
                propertyValueMultiplier = 2.5f,
                footTraffic = 800,
                businessOpportunityRate = 0.9f,
                crimeRate = 0.2f,
                locations = new string[] { "Sarit Centre", "The Oval", "Delta Towers" }
            },
            new NairobiDistrict {
                name = "Eastlands",
                propertyValueMultiplier = 1.2f,
                footTraffic = 1200,
                businessOpportunityRate = 0.6f,
                crimeRate = 0.5f,
                locations = new string[] { "Buruburu", "Umoja", "Kayole" }
            }
        };

        [Header("Enhanced Virtual Properties")]
        [SerializeField] private VirtualPropertyEnhanced[] availableProperties = new VirtualPropertyEnhanced[]
        {
            // Transport Properties
            new VirtualPropertyEnhanced {
                name = "Matatu SACCO Headquarters",
                basePrice = 5000000f,
                dailyIncome = 150000f,
                maxCapacity = 100,
                upgradeMultiplier = 2.0f,
                type = PropertyType.Transport,
                requiredLicense = "Transport",
                staffCapacity = 50,
                maintenanceCost = 20000f
            },
            new VirtualPropertyEnhanced {
                name = "Modern Bus Terminal",
                basePrice = 8000000f,
                dailyIncome = 200000f,
                maxCapacity = 200,
                upgradeMultiplier = 1.8f,
                type = PropertyType.Transport,
                requiredLicense = "Transport",
                staffCapacity = 80,
                maintenanceCost = 35000f
            },
            
            // Food Business Properties
            new VirtualPropertyEnhanced {
                name = "Kibanda Restaurant Chain",
                basePrice = 1000000f,
                dailyIncome = 50000f,
                maxCapacity = 30,
                upgradeMultiplier = 1.5f,
                type = PropertyType.Food,
                requiredLicense = "Food",
                staffCapacity = 15,
                maintenanceCost = 8000f
            },
            new VirtualPropertyEnhanced {
                name = "Food Court Complex",
                basePrice = 4000000f,
                dailyIncome = 120000f,
                maxCapacity = 150,
                upgradeMultiplier = 1.7f,
                type = PropertyType.Food,
                requiredLicense = "Food",
                staffCapacity = 60,
                maintenanceCost = 25000f
            },

            // Entertainment Properties
            new VirtualPropertyEnhanced {
                name = "Gaming Lounge",
                basePrice = 2000000f,
                dailyIncome = 80000f,
                maxCapacity = 50,
                upgradeMultiplier = 1.6f,
                type = PropertyType.Entertainment,
                requiredLicense = "Entertainment",
                staffCapacity = 20,
                maintenanceCost = 15000f
            },
            new VirtualPropertyEnhanced {
                name = "Club & Lounge",
                basePrice = 6000000f,
                dailyIncome = 180000f,
                maxCapacity = 300,
                upgradeMultiplier = 1.9f,
                type = PropertyType.Entertainment,
                requiredLicense = "Entertainment",
                staffCapacity = 40,
                maintenanceCost = 30000f
            },

            // Real Estate Properties
            new VirtualPropertyEnhanced {
                name = "Apartment Complex",
                basePrice = 10000000f,
                dailyIncome = 250000f,
                maxCapacity = 100,
                upgradeMultiplier = 1.5f,
                type = PropertyType.RealEstate,
                requiredLicense = "RealEstate",
                staffCapacity = 25,
                maintenanceCost = 40000f
            },
            new VirtualPropertyEnhanced {
                name = "Shopping Mall",
                basePrice = 20000000f,
                dailyIncome = 500000f,
                maxCapacity = 500,
                upgradeMultiplier = 2.0f,
                type = PropertyType.RealEstate,
                requiredLicense = "RealEstate",
                staffCapacity = 200,
                maintenanceCost = 100000f
            }
        };

        [Header("Social Features")]
        [SerializeField] private float socialInteractionRange = 5f;
        [SerializeField] private SocialActivity[] socialActivities = new SocialActivity[]
        {
            new SocialActivity {
                name = "Business Networking",
                reputationGain = 10f,
                duration = 1800f, // 30 minutes
                participantLimit = 20,
                costPerPerson = 1000f
            },
            new SocialActivity {
                name = "Community Meeting",
                reputationGain = 15f,
                duration = 3600f, // 1 hour
                participantLimit = 50,
                costPerPerson = 500f
            },
            new SocialActivity {
                name = "Trade Fair",
                reputationGain = 25f,
                duration = 7200f, // 2 hours
                participantLimit = 100,
                costPerPerson = 2000f
            }
        };

        [Header("Cultural Events")]
        [SerializeField] private CulturalEvent[] culturalEvents = new CulturalEvent[]
        {
            new CulturalEvent {
                name = "Nairobi Food Festival",
                duration = 259200f, // 3 days
                revenueMultiplier = 2.0f,
                participantBonus = 0.1f,
                requiredReputation = 50f
            },
            new CulturalEvent {
                name = "Music Festival",
                duration = 172800f, // 2 days
                revenueMultiplier = 1.8f,
                participantBonus = 0.15f,
                requiredReputation = 40f
            },
            new CulturalEvent {
                name = "Tech Innovation Expo",
                duration = 86400f, // 1 day
                revenueMultiplier = 1.5f,
                participantBonus = 0.2f,
                requiredReputation = 30f
            }
        };

        private InputActionMap playStationActions;
        private Dictionary<string, VirtualPropertyEnhanced> ownedProperties;
        private Dictionary<string, float> districtReputation;
        private List<ActiveEvent> activeEvents;
        private WeatherSystem weatherSystem;
        private TimeManager timeManager;

        private void Awake()
        {
            base.Awake();
            InitializePlayStationControls();
            InitializeDistricts();
            weatherSystem = GetComponent<WeatherSystem>();
            timeManager = GetComponent<TimeManager>();
        }

        private void InitializePlayStationControls()
        {
            if (isPlayStation)
            {
                playStationActions = new InputActionMap("PlayStation");
                
                // DualSense-specific controls
                var touchpad = playStationActions.AddAction("touchpad", InputType.Button);
                touchpad.AddBinding("<DualSenseGamepad>/touchpad");

                var adaptiveTriggers = playStationActions.AddAction("adaptiveTriggers", InputType.Button);
                adaptiveTriggers.AddBinding("<DualSenseGamepad>/rightTrigger");
                adaptiveTriggers.AddBinding("<DualSenseGamepad>/leftTrigger");

                playStationActions.Enable();
            }
        }

        private void InitializeDistricts()
        {
            districtReputation = new Dictionary<string, float>();
            foreach (NairobiDistrict district in districts)
            {
                districtReputation[district.name] = 50f; // Starting reputation
            }
        }

        public void StartCulturalEvent(string eventName)
        {
            foreach (CulturalEvent evt in culturalEvents)
            {
                if (evt.name == eventName && GetPlayerReputation() >= evt.requiredReputation)
                {
                    activeEvents.Add(new ActiveEvent
                    {
                        name = evt.name,
                        startTime = DateTime.Now,
                        duration = evt.duration,
                        multiplier = evt.revenueMultiplier
                    });

                    photonView.RPC("SyncCulturalEvent", RpcTarget.All, eventName);
                    break;
                }
            }
        }

        public void OrganizeSocialActivity(string activityName, Vector3 location)
        {
            foreach (SocialActivity activity in socialActivities)
            {
                if (activity.name == activityName)
                {
                    photonView.RPC("StartSocialActivity", RpcTarget.All,
                        activityName, location, PhotonNetwork.LocalPlayer.UserId);
                    break;
                }
            }
        }

        [PunRPC]
        private void StartSocialActivity(string activityName, Vector3 location, string organizerId)
        {
            // Create social activity instance
            // Notify nearby players
            // Set up meeting point
        }

        public void PurchaseProperty(string propertyName, string district)
        {
            foreach (VirtualPropertyEnhanced property in availableProperties)
            {
                if (property.name == propertyName)
                {
                    float finalPrice = property.basePrice * 
                        GetDistrictMultiplier(district) * 
                        GetMarketConditionMultiplier();

                    if (TryPurchaseProperty(property, finalPrice))
                    {
                        photonView.RPC("SyncPropertyPurchase", RpcTarget.All,
                            propertyName, district, PhotonNetwork.LocalPlayer.UserId);
                    }
                    break;
                }
            }
        }

        private float GetDistrictMultiplier(string district)
        {
            foreach (NairobiDistrict d in districts)
            {
                if (d.name == district)
                {
                    return d.propertyValueMultiplier;
                }
            }
            return 1f;
        }

        private float GetMarketConditionMultiplier()
        {
            // Consider time of day, weather, events, etc.
            float timeMultiplier = timeManager.IsBusinessHours() ? 1.2f : 0.8f;
            float weatherMultiplier = weatherSystem.GetCurrentWeatherMultiplier();
            float eventMultiplier = GetActiveEventMultiplier();
            
            return timeMultiplier * weatherMultiplier * eventMultiplier;
        }

        private float GetActiveEventMultiplier()
        {
            float multiplier = 1f;
            foreach (ActiveEvent evt in activeEvents)
            {
                if (IsEventActive(evt))
                {
                    multiplier *= evt.multiplier;
                }
            }
            return multiplier;
        }

        private bool IsEventActive(ActiveEvent evt)
        {
            return (DateTime.Now - evt.startTime).TotalSeconds < evt.duration;
        }

        public float GetPlayerReputation()
        {
            float totalReputation = 0f;
            foreach (float reputation in districtReputation.Values)
            {
                totalReputation += reputation;
            }
            return totalReputation / districtReputation.Count;
        }

        private void Update()
        {
            if (PhotonNetwork.IsConnected)
            {
                UpdateProperties();
                UpdateEvents();
                HandlePlayStationInput();
            }
        }

        private void HandlePlayStationInput()
        {
            if (!isPlayStation) return;

            // Handle DualSense-specific features
            if (playStationActions["touchpad"].WasPressedThisFrame())
            {
                ShowQuickMenu();
            }

            // Adaptive trigger feedback based on player actions
            UpdateAdaptiveTriggers();
        }

        private void UpdateAdaptiveTriggers()
        {
            if (!isPlayStation) return;

            // Example: Adjust trigger resistance based on vehicle speed or property management
            float triggerResistance = CalculateTriggerResistance();
            // Apply to DualSense controller
        }

        private float CalculateTriggerResistance()
        {
            // Calculate based on current activity
            return 0.5f; // Default resistance
        }

        private void ShowQuickMenu()
        {
            // Show PlayStation-specific quick menu
        }
    }

    [System.Serializable]
    public class NairobiDistrict
    {
        public string name;
        public float propertyValueMultiplier;
        public int footTraffic;
        public float businessOpportunityRate;
        public float crimeRate;
        public string[] locations;
    }

    [System.Serializable]
    public class VirtualPropertyEnhanced : VirtualProperty
    {
        public PropertyType type;
        public string requiredLicense;
        public int staffCapacity;
        public float maintenanceCost;
    }

    [System.Serializable]
    public class SocialActivity
    {
        public string name;
        public float reputationGain;
        public float duration;
        public int participantLimit;
        public float costPerPerson;
    }

    [System.Serializable]
    public class CulturalEvent
    {
        public string name;
        public float duration;
        public float revenueMultiplier;
        public float participantBonus;
        public float requiredReputation;
    }

    [System.Serializable]
    public class ActiveEvent
    {
        public string name;
        public DateTime startTime;
        public float duration;
        public float multiplier;
    }

    public enum PropertyType
    {
        Transport,
        Food,
        Entertainment,
        RealEstate
    }
} 