using UnityEngine;
using System;
using System.Collections.Generic;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class AfricanAuthenticity : MonoBehaviour
    {
        [Header("Cultural Events")]
        [SerializeField] private CulturalEvent[] culturalEvents = new CulturalEvent[]
        {
            new CulturalEvent {
                name = "Madaraka Day",
                date = new DateTime(DateTime.Now.Year, 6, 1),
                demandMultiplier = 2.0f,
                specialRoutes = new string[] { "Uhuru Gardens", "Nyayo Stadium" },
                bonusReward = 5000f
            },
            new CulturalEvent {
                name = "Mashujaa Day",
                date = new DateTime(DateTime.Now.Year, 10, 20),
                demandMultiplier = 1.8f,
                specialRoutes = new string[] { "Uhuru Park", "KICC" },
                bonusReward = 4500f
            },
            new CulturalEvent {
                name = "Jamhuri Day",
                date = new DateTime(DateTime.Now.Year, 12, 12),
                demandMultiplier = 2.0f,
                specialRoutes = new string[] { "Kasarani Stadium", "Nyayo Stadium" },
                bonusReward = 5000f
            }
        };

        [Header("Weather Effects")]
        [SerializeField] private WeatherCondition[] weatherConditions = new WeatherCondition[]
        {
            new WeatherCondition {
                type = WeatherType.LongRains,
                months = new int[] { 3, 4, 5 },
                fareMultiplier = 1.5f,
                vehicleSpeedMultiplier = 0.7f,
                maintenanceCostMultiplier = 1.3f
            },
            new WeatherCondition {
                type = WeatherType.ShortRains,
                months = new int[] { 10, 11 },
                fareMultiplier = 1.3f,
                vehicleSpeedMultiplier = 0.8f,
                maintenanceCostMultiplier = 1.2f
            },
            new WeatherCondition {
                type = WeatherType.DrySpell,
                months = new int[] { 1, 2, 7, 8, 9 },
                fareMultiplier = 1.0f,
                vehicleSpeedMultiplier = 1.0f,
                maintenanceCostMultiplier = 1.0f
            }
        };

        [Header("Route Dynamics")]
        [SerializeField] private RouteDynamic[] routeDynamics = new RouteDynamic[]
        {
            new RouteDynamic {
                name = "CBD Rush",
                peakHours = new int[] { 6, 7, 8, 17, 18, 19 },
                basePassengerCount = 30,
                peakMultiplier = 2.0f,
                competitorCount = 5,
                trafficDensity = 0.8f
            },
            new RouteDynamic {
                name = "Estate Connect",
                peakHours = new int[] { 5, 6, 7, 19, 20, 21 },
                basePassengerCount = 25,
                peakMultiplier = 1.8f,
                competitorCount = 3,
                trafficDensity = 0.6f
            },
            new RouteDynamic {
                name = "Inter-County",
                peakHours = new int[] { 8, 9, 16, 17 },
                basePassengerCount = 40,
                peakMultiplier = 1.5f,
                competitorCount = 2,
                trafficDensity = 0.4f
            }
        };

        [Header("Social Features")]
        [SerializeField] private SocialFeature[] socialFeatures = new SocialFeature[]
        {
            new SocialFeature {
                name = "SACCO Meeting",
                frequency = 7, // days
                reputationBonus = 100f,
                networkingBonus = 50f,
                unlockRequirements = new string[] { "sacco_member" }
            },
            new SocialFeature {
                name = "Community Event",
                frequency = 14,
                reputationBonus = 200f,
                networkingBonus = 100f,
                unlockRequirements = new string[] { "community_leader" }
            },
            new SocialFeature {
                name = "Driver Training",
                frequency = 30,
                reputationBonus = 300f,
                networkingBonus = 150f,
                unlockRequirements = new string[] { "trainer_certification" }
            }
        };

        private CareerProgression careerProgression;
        private PlayerWallet playerWallet;
        private WeatherType currentWeather;
        private List<string> activeEvents;
        private Dictionary<string, float> routeReputation;

        private void Awake()
        {
            careerProgression = GetComponent<CareerProgression>();
            playerWallet = GetComponent<PlayerWallet>();
            activeEvents = new List<string>();
            routeReputation = new Dictionary<string, float>();
            InitializeAuthenticity();
        }

        private void InitializeAuthenticity()
        {
            UpdateWeatherConditions();
            CheckForCulturalEvents();
            InitializeRouteReputation();
            InvokeRepeating("UpdateGameDynamics", 0f, 3600f); // Update every hour
        }

        private void UpdateGameDynamics()
        {
            UpdateWeatherConditions();
            CheckForCulturalEvents();
            UpdateRouteDynamics();
            UpdateSocialFeatures();
        }

        private void UpdateWeatherConditions()
        {
            int currentMonth = DateTime.Now.Month;
            foreach (WeatherCondition condition in weatherConditions)
            {
                if (Array.IndexOf(condition.months, currentMonth) != -1)
                {
                    currentWeather = condition.type;
                    ApplyWeatherEffects(condition);
                    break;
                }
            }
        }

        private void ApplyWeatherEffects(WeatherCondition condition)
        {
            // Apply weather effects to game mechanics
            foreach (RouteDynamic route in routeDynamics)
            {
                route.currentFareMultiplier = condition.fareMultiplier;
                route.currentSpeedMultiplier = condition.vehicleSpeedMultiplier;
                route.maintenanceCost *= condition.maintenanceCostMultiplier;
            }
        }

        private void CheckForCulturalEvents()
        {
            DateTime today = DateTime.Now;
            activeEvents.Clear();

            foreach (CulturalEvent evt in culturalEvents)
            {
                if (today.Month == evt.date.Month && today.Day == evt.date.Day)
                {
                    activeEvents.Add(evt.name);
                    ApplyCulturalEventEffects(evt);
                }
            }
        }

        private void ApplyCulturalEventEffects(CulturalEvent evt)
        {
            // Boost demand and rewards for special routes
            foreach (string route in evt.specialRoutes)
            {
                if (routeReputation.ContainsKey(route))
                {
                    routeReputation[route] *= evt.demandMultiplier;
                }
            }

            // Award bonus for participating in cultural events
            if (IsPlayerParticipating(evt))
            {
                playerWallet.AddFunds(evt.bonusReward);
                careerProgression.UpdateCareerProgress(evt.bonusReward * 0.1f, evt.bonusReward);
            }
        }

        private bool IsPlayerParticipating(CulturalEvent evt)
        {
            // Check if player is operating on event routes
            foreach (string route in evt.specialRoutes)
            {
                if (IsPlayerOnRoute(route))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPlayerOnRoute(string route)
        {
            // Implementation to check player's current route
            return routeReputation.ContainsKey(route) && routeReputation[route] > 0;
        }

        private void InitializeRouteReputation()
        {
            foreach (RouteDynamic route in routeDynamics)
            {
                routeReputation[route.name] = 100f; // Base reputation
            }
        }

        private void UpdateRouteDynamics()
        {
            DateTime now = DateTime.Now;
            int currentHour = now.Hour;

            foreach (RouteDynamic route in routeDynamics)
            {
                // Update passenger counts based on time
                if (Array.IndexOf(route.peakHours, currentHour) != -1)
                {
                    route.currentPassengerCount = Mathf.RoundToInt(route.basePassengerCount * route.peakMultiplier);
                }
                else
                {
                    route.currentPassengerCount = route.basePassengerCount;
                }

                // Adjust for competition
                route.currentPassengerCount = Mathf.RoundToInt(
                    route.currentPassengerCount * (1f - (route.competitorCount * 0.1f))
                );

                // Apply traffic effects
                route.currentTravelTime = route.baseTravelTime * (1f + route.trafficDensity);
            }
        }

        private void UpdateSocialFeatures()
        {
            foreach (SocialFeature feature in socialFeatures)
            {
                if (IsSocialFeatureAvailable(feature) && !PlayerPrefs.HasKey($"LastSocial_{feature.name}"))
                {
                    // Award social bonuses
                    careerProgression.UpdateCareerProgress(
                        feature.reputationBonus,
                        feature.networkingBonus
                    );

                    // Record participation
                    PlayerPrefs.SetString($"LastSocial_{feature.name}", DateTime.Now.ToString());
                    PlayerPrefs.Save();
                }
            }
        }

        private bool IsSocialFeatureAvailable(SocialFeature feature)
        {
            string lastParticipation = PlayerPrefs.GetString($"LastSocial_{feature.name}", "");
            
            if (string.IsNullOrEmpty(lastParticipation))
            {
                return true;
            }

            DateTime lastDate = DateTime.Parse(lastParticipation);
            TimeSpan timeSince = DateTime.Now - lastDate;
            
            return timeSince.TotalDays >= feature.frequency;
        }

        public float GetRouteMultiplier(string routeName)
        {
            float multiplier = 1.0f;

            // Apply weather effects
            foreach (WeatherCondition condition in weatherConditions)
            {
                if (condition.type == currentWeather)
                {
                    multiplier *= condition.fareMultiplier;
                    break;
                }
            }

            // Apply cultural event effects
            foreach (CulturalEvent evt in culturalEvents)
            {
                if (activeEvents.Contains(evt.name) && Array.IndexOf(evt.specialRoutes, routeName) != -1)
                {
                    multiplier *= evt.demandMultiplier;
                }
            }

            // Apply route reputation
            if (routeReputation.ContainsKey(routeName))
            {
                multiplier *= (routeReputation[routeName] / 100f);
            }

            return multiplier;
        }
    }

    [System.Serializable]
    public class CulturalEvent
    {
        public string name;
        public DateTime date;
        public float demandMultiplier;
        public string[] specialRoutes;
        public float bonusReward;
    }

    [System.Serializable]
    public class WeatherCondition
    {
        public WeatherType type;
        public int[] months;
        public float fareMultiplier;
        public float vehicleSpeedMultiplier;
        public float maintenanceCostMultiplier;
    }

    [System.Serializable]
    public class RouteDynamic
    {
        public string name;
        public int[] peakHours;
        public int basePassengerCount;
        public float peakMultiplier;
        public int competitorCount;
        public float trafficDensity;
        public float baseTravelTime;
        public float currentTravelTime;
        public int currentPassengerCount;
        public float currentFareMultiplier;
        public float currentSpeedMultiplier;
        public float maintenanceCost;
    }

    [System.Serializable]
    public class SocialFeature
    {
        public string name;
        public int frequency;
        public float reputationBonus;
        public float networkingBonus;
        public string[] unlockRequirements;
    }

    public enum WeatherType
    {
        LongRains,
        ShortRains,
        DrySpell
    }
} 