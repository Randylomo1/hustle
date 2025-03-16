using UnityEngine;
using System;
using System.Collections.Generic;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class PlayerProgression : MonoBehaviour
    {
        [Header("Business Progression")]
        [SerializeField] private BusinessTier[] businessTiers;
        [SerializeField] private float experienceMultiplier = 1.0f;
        [SerializeField] private float reputationMultiplier = 1.0f;

        [Header("Time-Based Bonuses")]
        [SerializeField] private float morningRushMultiplier = 1.8f; // 6AM - 9AM
        [SerializeField] private float eveningRushMultiplier = 1.6f; // 4PM - 7PM
        [SerializeField] private float weekendBonus = 1.4f;
        [SerializeField] private float holidayBonus = 2.0f;

        [Header("Achievement Rewards")]
        [SerializeField] private float routeMasteryBonus = 500f;
        [SerializeField] private float customerSatisfactionBonus = 300f;
        [SerializeField] private float businessMilestoneBonus = 1000f;
        [SerializeField] private float communityReputationBonus = 750f;

        private PlayerWallet playerWallet;
        private RevenueSystem revenueSystem;
        private float reputation;
        private float experience;
        private int currentTier;
        private Dictionary<string, float> routeExperience;
        private Dictionary<string, int> customerRatings;

        private void Awake()
        {
            playerWallet = GetComponent<PlayerWallet>();
            revenueSystem = GetComponent<RevenueSystem>();
            routeExperience = new Dictionary<string, float>();
            customerRatings = new Dictionary<string, int>();
            LoadPlayerProgress();
        }

        public float CalculateEarnings(float baseAmount, string routeId, bool isBusinessEarning)
        {
            float finalAmount = baseAmount;

            // Apply time-based multipliers
            finalAmount *= GetTimeBasedMultiplier();

            // Apply route mastery bonus
            if (routeExperience.ContainsKey(routeId))
            {
                float routeMastery = Mathf.Min(routeExperience[routeId] / 1000f, 0.5f); // Max 50% bonus
                finalAmount *= (1f + routeMastery);
            }

            // Apply business tier multiplier
            if (isBusinessEarning && currentTier < businessTiers.Length)
            {
                finalAmount *= businessTiers[currentTier].earningsMultiplier;
            }

            // Apply reputation bonus
            float reputationBonus = Mathf.Min(reputation / 1000f, 0.3f); // Max 30% bonus
            finalAmount *= (1f + reputationBonus);

            return finalAmount;
        }

        public void AddExperience(float amount, string routeId)
        {
            experience += amount * experienceMultiplier;
            
            // Add route-specific experience
            if (!routeExperience.ContainsKey(routeId))
            {
                routeExperience[routeId] = 0;
            }
            routeExperience[routeId] += amount;

            // Check for route mastery achievement
            if (routeExperience[routeId] >= 1000f && !PlayerPrefs.HasKey($"RouteMastery_{routeId}"))
            {
                AwardRouteMasteryBonus(routeId);
            }

            CheckTierProgression();
            SavePlayerProgress();
        }

        public void AddReputation(float amount, string source)
        {
            reputation += amount * reputationMultiplier;
            
            // Check for reputation milestones
            CheckReputationMilestones();
            
            SavePlayerProgress();
        }

        public void AddCustomerRating(int rating, string routeId)
        {
            if (!customerRatings.ContainsKey(routeId))
            {
                customerRatings[routeId] = 0;
            }
            customerRatings[routeId] = (customerRatings[routeId] + rating) / 2;

            // Check for customer satisfaction achievements
            CheckCustomerSatisfactionAchievements(routeId);
        }

        private float GetTimeBasedMultiplier()
        {
            DateTime now = DateTime.Now;
            float multiplier = 1.0f;

            // Morning rush hour bonus
            if (now.Hour >= 6 && now.Hour <= 9)
            {
                multiplier *= morningRushMultiplier;
            }
            // Evening rush hour bonus
            else if (now.Hour >= 16 && now.Hour <= 19)
            {
                multiplier *= eveningRushMultiplier;
            }

            // Weekend bonus
            if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
            {
                multiplier *= weekendBonus;
            }

            // Holiday bonus (example holidays)
            if (IsHoliday(now))
            {
                multiplier *= holidayBonus;
            }

            return multiplier;
        }

        private bool IsHoliday(DateTime date)
        {
            // Kenyan public holidays
            if (date.Month == 1 && date.Day == 1) return true; // New Year
            if (date.Month == 5 && date.Day == 1) return true; // Labour Day
            if (date.Month == 6 && date.Day == 1) return true; // Madaraka Day
            if (date.Month == 10 && date.Day == 20) return true; // Mashujaa Day
            if (date.Month == 12 && date.Day == 12) return true; // Jamhuri Day
            if (date.Month == 12 && date.Day == 25) return true; // Christmas
            if (date.Month == 12 && date.Day == 26) return true; // Boxing Day

            return false;
        }

        private void CheckTierProgression()
        {
            for (int i = businessTiers.Length - 1; i >= 0; i--)
            {
                if (experience >= businessTiers[i].requiredExperience && currentTier < i)
                {
                    PromoteToTier(i);
                    break;
                }
            }
        }

        private void PromoteToTier(int newTier)
        {
            currentTier = newTier;
            playerWallet.AddFunds(businessTiers[newTier].promotionBonus);
            
            // Unlock tier-specific features
            UnlockTierFeatures(newTier);
            
            // Save progress
            PlayerPrefs.SetInt("CurrentBusinessTier", currentTier);
            PlayerPrefs.Save();
        }

        private void UnlockTierFeatures(int tier)
        {
            BusinessTier currentTierData = businessTiers[tier];
            
            foreach (string feature in currentTierData.unlockedFeatures)
            {
                PlayerPrefs.SetInt($"Feature_{feature}", 1);
            }
            
            PlayerPrefs.Save();
        }

        private void AwardRouteMasteryBonus(string routeId)
        {
            playerWallet.AddFunds(routeMasteryBonus);
            PlayerPrefs.SetInt($"RouteMastery_{routeId}", 1);
            PlayerPrefs.Save();
        }

        private void CheckReputationMilestones()
        {
            int currentMilestone = Mathf.FloorToInt(reputation / 1000f);
            int savedMilestone = PlayerPrefs.GetInt("ReputationMilestone", 0);

            if (currentMilestone > savedMilestone)
            {
                playerWallet.AddFunds(communityReputationBonus);
                PlayerPrefs.SetInt("ReputationMilestone", currentMilestone);
                PlayerPrefs.Save();
            }
        }

        private void CheckCustomerSatisfactionAchievements(string routeId)
        {
            if (customerRatings[routeId] >= 4.5f && !PlayerPrefs.HasKey($"CustomerSatisfaction_{routeId}"))
            {
                playerWallet.AddFunds(customerSatisfactionBonus);
                PlayerPrefs.SetInt($"CustomerSatisfaction_{routeId}", 1);
                PlayerPrefs.Save();
            }
        }

        private void LoadPlayerProgress()
        {
            experience = PlayerPrefs.GetFloat("PlayerExperience", 0);
            reputation = PlayerPrefs.GetFloat("PlayerReputation", 0);
            currentTier = PlayerPrefs.GetInt("CurrentBusinessTier", 0);

            // Load route experience
            string routeExpJson = PlayerPrefs.GetString("RouteExperience", "{}");
            routeExperience = JsonUtility.FromJson<Dictionary<string, float>>(routeExpJson);

            // Load customer ratings
            string ratingsJson = PlayerPrefs.GetString("CustomerRatings", "{}");
            customerRatings = JsonUtility.FromJson<Dictionary<string, int>>(ratingsJson);
        }

        private void SavePlayerProgress()
        {
            PlayerPrefs.SetFloat("PlayerExperience", experience);
            PlayerPrefs.SetFloat("PlayerReputation", reputation);
            PlayerPrefs.SetInt("CurrentBusinessTier", currentTier);

            // Save route experience
            string routeExpJson = JsonUtility.ToJson(routeExperience);
            PlayerPrefs.SetString("RouteExperience", routeExpJson);

            // Save customer ratings
            string ratingsJson = JsonUtility.ToJson(customerRatings);
            PlayerPrefs.SetString("CustomerRatings", ratingsJson);

            PlayerPrefs.Save();
        }
    }

    [System.Serializable]
    public class BusinessTier
    {
        public string tierName;
        public float requiredExperience;
        public float earningsMultiplier;
        public float promotionBonus;
        public string[] unlockedFeatures;
        public string[] unlockedRoutes;
        public VehicleType[] unlockedVehicles;
    }

    public enum VehicleType
    {
        BasicMatatu,
        PremiumMatatu,
        MiniVan,
        LuxuryVan,
        Bus
    }
} 