using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class ProfitabilitySystem : MonoBehaviour
    {
        [Header("Developer Revenue Settings")]
        [SerializeField] private float transactionFeePercentage = 0.05f; // 5% fee
        [SerializeField] private float premiumFeaturesCost = 1000f; // KSH
        [SerializeField] private float monthlySubscriptionCost = 500f; // KSH
        [SerializeField] private float virtualCurrencyExchangeRate = 100f; // 100 game coins = 1 KSH

        [Header("Player Revenue Settings")]
        [SerializeField] private float dailyLoginBonus = 1000f; // Game coins
        [SerializeField] private float referralBonus = 5000f; // Game coins
        [SerializeField] private float achievementRewards = 2000f; // Game coins
        [SerializeField] private float minimumWithdrawalAmount = 1000f; // KSH
        [SerializeField] private int withdrawalCooldownHours = 24;

        [Header("8D Audio System")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float dopplerLevel = 1f;
        [SerializeField] private float spreadAngle = 180f;
        [SerializeField] private float reverbZoneMix = 1f;

        [Header("Additional Districts")]
        [SerializeField] private EnhancedDistrict[] newDistricts = new EnhancedDistrict[]
        {
            new EnhancedDistrict {
                name = "Karen",
                propertyValueMultiplier = 3.0f,
                footTraffic = 500,
                businessOpportunityRate = 0.95f,
                crimeRate = 0.1f,
                luxuryIndex = 0.9f,
                locations = new string[] { 
                    "Karen Hub", "Waterfront", "Karen Country Club",
                    "The Hub Karen", "Hemingways" 
                }
            },
            new EnhancedDistrict {
                name = "Kilimani",
                propertyValueMultiplier = 2.8f,
                footTraffic = 900,
                businessOpportunityRate = 0.85f,
                crimeRate = 0.2f,
                luxuryIndex = 0.8f,
                locations = new string[] {
                    "Yaya Centre", "Adlife Plaza", "Kilimani Square",
                    "Valley Arcade", "Rose Avenue"
                }
            },
            new EnhancedDistrict {
                name = "Upperhill",
                propertyValueMultiplier = 2.7f,
                footTraffic = 1100,
                businessOpportunityRate = 0.9f,
                crimeRate = 0.15f,
                luxuryIndex = 0.85f,
                locations = new string[] {
                    "Kenyatta Hospital", "Times Tower", "KCB Towers",
                    "UAP Tower", "Britam Tower"
                }
            }
        };

        [Header("New Business Types")]
        [SerializeField] private VirtualBusinessType[] businessTypes = new VirtualBusinessType[]
        {
            new VirtualBusinessType {
                name = "Tech Startup Hub",
                basePrice = 15000000f,
                dailyIncome = 300000f,
                maxCapacity = 50,
                upgradeMultiplier = 2.2f,
                type = PropertyType.Technology,
                requiredLicense = "Tech",
                staffCapacity = 30,
                maintenanceCost = 50000f,
                innovationBonus = 0.3f
            },
            new VirtualBusinessType {
                name = "Green Energy Plant",
                basePrice = 25000000f,
                dailyIncome = 450000f,
                maxCapacity = 40,
                upgradeMultiplier = 2.5f,
                type = PropertyType.Energy,
                requiredLicense = "Energy",
                staffCapacity = 45,
                maintenanceCost = 80000f,
                sustainabilityBonus = 0.4f
            },
            new VirtualBusinessType {
                name = "Digital Media Studio",
                basePrice = 12000000f,
                dailyIncome = 250000f,
                maxCapacity = 35,
                upgradeMultiplier = 2.0f,
                type = PropertyType.Media,
                requiredLicense = "Media",
                staffCapacity = 25,
                maintenanceCost = 40000f,
                creativityBonus = 0.25f
            }
        };

        [Header("Cultural Events")]
        [SerializeField] private EnhancedCulturalEvent[] enhancedEvents = new EnhancedCulturalEvent[]
        {
            new EnhancedCulturalEvent {
                name = "Nairobi International Trade Fair",
                duration = 604800f, // 7 days
                revenueMultiplier = 2.5f,
                participantBonus = 0.2f,
                requiredReputation = 60f,
                internationalBonus = 0.3f,
                networkingMultiplier = 1.5f
            },
            new EnhancedCulturalEvent {
                name = "East African Gaming Convention",
                duration = 259200f, // 3 days
                revenueMultiplier = 2.2f,
                participantBonus = 0.25f,
                requiredReputation = 45f,
                techBonus = 0.35f,
                youthEngagementMultiplier = 1.8f
            },
            new EnhancedCulturalEvent {
                name = "Kenyan Fashion Week",
                duration = 172800f, // 2 days
                revenueMultiplier = 2.0f,
                participantBonus = 0.3f,
                requiredReputation = 55f,
                luxuryBonus = 0.4f,
                mediaExposureMultiplier = 1.6f
            }
        };

        private MPesaService mpesaService;
        private Dictionary<string, DateTime> lastWithdrawal;
        private Dictionary<string, float> playerBalances;
        private Dictionary<string, List<VirtualAsset>> playerAssets;

        private void Awake()
        {
            mpesaService = GetComponent<MPesaService>();
            lastWithdrawal = new Dictionary<string, DateTime>();
            playerBalances = new Dictionary<string, float>();
            playerAssets = new Dictionary<string, List<VirtualAsset>>();
            InitializeAudioSystem();
        }

        private void InitializeAudioSystem()
        {
            AudioListener.spatialize = true;
            AudioListener.spatialBlend = spatialBlend;
            
            // Set up 8D audio parameters
            audioMixer.SetFloat("DopplerLevel", dopplerLevel);
            audioMixer.SetFloat("SpreadAngle", spreadAngle);
            audioMixer.SetFloat("ReverbZoneMix", reverbZoneMix);
        }

        public bool ListAssetForSale(string playerId, VirtualAsset asset, float price)
        {
            if (!playerAssets.ContainsKey(playerId) || 
                !playerAssets[playerId].Contains(asset)) return false;

            asset.listingPrice = price;
            asset.isListed = true;
            return true;
        }

        public bool PurchaseAsset(string buyerId, string sellerId, VirtualAsset asset)
        {
            if (!asset.isListed || !playerBalances.ContainsKey(buyerId)) return false;

            float totalCost = asset.listingPrice;
            float developerFee = totalCost * transactionFeePercentage;
            float sellerProfit = totalCost - developerFee;

            if (playerBalances[buyerId] < totalCost) return false;

            // Process transaction
            playerBalances[buyerId] -= totalCost;
            if (!playerBalances.ContainsKey(sellerId))
            {
                playerBalances[sellerId] = 0;
            }
            playerBalances[sellerId] += sellerProfit;

            // Transfer asset
            playerAssets[sellerId].Remove(asset);
            if (!playerAssets.ContainsKey(buyerId))
            {
                playerAssets[buyerId] = new List<VirtualAsset>();
            }
            playerAssets[buyerId].Add(asset);

            return true;
        }

        public async void WithdrawToMPesa(string playerId, string phoneNumber, float amount)
        {
            if (!CanWithdraw(playerId, amount)) return;

            try
            {
                bool success = await mpesaService.ProcessWithdrawal(phoneNumber, amount);
                if (success)
                {
                    playerBalances[playerId] -= amount;
                    lastWithdrawal[playerId] = DateTime.Now;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MPesa withdrawal failed: {e.Message}");
            }
        }

        private bool CanWithdraw(string playerId, float amount)
        {
            if (!playerBalances.ContainsKey(playerId) || 
                playerBalances[playerId] < amount ||
                amount < minimumWithdrawalAmount) return false;

            if (lastWithdrawal.ContainsKey(playerId))
            {
                TimeSpan timeSinceLastWithdrawal = DateTime.Now - lastWithdrawal[playerId];
                if (timeSinceLastWithdrawal.TotalHours < withdrawalCooldownHours)
                {
                    return false;
                }
            }

            return true;
        }

        public void AddDailyBonus(string playerId)
        {
            if (!playerBalances.ContainsKey(playerId))
            {
                playerBalances[playerId] = 0;
            }
            playerBalances[playerId] += dailyLoginBonus;
        }

        public void ProcessReferral(string referrerId, string newPlayerId)
        {
            if (!playerBalances.ContainsKey(referrerId))
            {
                playerBalances[referrerId] = 0;
            }
            playerBalances[referrerId] += referralBonus;
        }

        public float GetVirtualCurrencyBalance(string playerId)
        {
            return playerBalances.ContainsKey(playerId) ? playerBalances[playerId] : 0f;
        }

        public float ConvertToRealMoney(float virtualAmount)
        {
            return virtualAmount / virtualCurrencyExchangeRate;
        }
    }

    [System.Serializable]
    public class EnhancedDistrict : NairobiDistrict
    {
        public float luxuryIndex;
        public float[] peakHours;
        public string[] specialEvents;
        public float[] seasonalMultipliers;
    }

    [System.Serializable]
    public class VirtualBusinessType : VirtualPropertyEnhanced
    {
        public float innovationBonus;
        public float sustainabilityBonus;
        public float creativityBonus;
        public float[] seasonalDemand;
        public string[] requiredSkills;
    }

    [System.Serializable]
    public class EnhancedCulturalEvent : CulturalEvent
    {
        public float internationalBonus;
        public float techBonus;
        public float luxuryBonus;
        public float networkingMultiplier;
        public float youthEngagementMultiplier;
        public float mediaExposureMultiplier;
    }

    [System.Serializable]
    public class VirtualAsset
    {
        public string id;
        public string name;
        public string type;
        public float purchasePrice;
        public float listingPrice;
        public bool isListed;
        public DateTime purchaseDate;
        public float[] performanceHistory;
    }
} 