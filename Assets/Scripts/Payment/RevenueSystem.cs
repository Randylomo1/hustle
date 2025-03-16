using UnityEngine;
using System;
using System.Threading.Tasks;
using NairobiHustle.MPesa;

namespace NairobiHustle.Payment
{
    public class RevenueSystem : MonoBehaviour
    {
        [Header("Developer Revenue Settings")]
        [SerializeField] private float transactionFeePercentage = 0.05f; // 5% fee on all transactions
        [SerializeField] private float premiumFeaturesFeePercentage = 0.15f; // 15% on premium features
        [SerializeField] private float advertisementRevenueShare = 0.70f; // 70% of ad revenue goes to developer
        
        [Header("Player Revenue Settings")]
        [SerializeField] private float baseMatutuEarningsMultiplier = 1.2f; // Players earn 20% more than real rates
        [SerializeField] private float businessProfitMultiplier = 1.3f; // 30% more profit than real business rates
        [SerializeField] private float bonusEarningsMultiplier = 1.5f; // 50% bonus during peak hours
        
        [Header("Premium Features")]
        [SerializeField] private float premiumRouteUnlockFee = 1000f;
        [SerializeField] private float customizationPackPrice = 500f;
        [SerializeField] private float businessLicenseFee = 2000f;
        
        [Header("Instant Rewards")]
        [SerializeField] private float dailyLoginBonus = 100f;
        [SerializeField] private float referralBonus = 500f;
        [SerializeField] private float achievementBonus = 200f;

        private MPesaService mpesaService;
        private PlayerWallet playerWallet;
        private bool isPremiumUser;

        private void Awake()
        {
            mpesaService = GetComponent<MPesaService>();
            playerWallet = GetComponent<PlayerWallet>();
        }

        public async Task<bool> ProcessTransaction(float amount, string transactionType)
        {
            float developerFee = CalculateDeveloperFee(amount, transactionType);
            float playerAmount = amount - developerFee;

            // Process M-Pesa transaction
            bool success = await ProcessMPesaTransaction(amount);
            
            if (success)
            {
                // Update player wallet
                playerWallet.AddFunds(playerAmount);
                
                // Log transaction for analytics
                LogTransaction(amount, developerFee, transactionType);
                
                // Trigger instant rewards if applicable
                CheckAndTriggerRewards(amount);
                
                return true;
            }
            
            return false;
        }

        private float CalculateDeveloperFee(float amount, string transactionType)
        {
            float feePercentage = transactionType switch
            {
                "premium" => premiumFeaturesFeePercentage,
                "standard" => transactionFeePercentage,
                "advertisement" => 1 - advertisementRevenueShare,
                _ => transactionFeePercentage
            };

            return amount * feePercentage;
        }

        private async Task<bool> ProcessMPesaTransaction(float amount)
        {
            try
            {
                // Implement fast-track processing for small amounts
                if (amount < 1000)
                {
                    return await mpesaService.ProcessInstantPayment(amount);
                }
                
                // Regular processing for larger amounts
                return await mpesaService.ProcessPayment(amount);
            }
            catch (Exception e)
            {
                Debug.LogError($"M-Pesa transaction failed: {e.Message}");
                return false;
            }
        }

        public async Task<bool> UnlockPremiumFeature(string featureType)
        {
            float fee = featureType switch
            {
                "route" => premiumRouteUnlockFee,
                "customization" => customizationPackPrice,
                "business" => businessLicenseFee,
                _ => 0f
            };

            if (fee > 0)
            {
                bool success = await ProcessTransaction(fee, "premium");
                if (success)
                {
                    UnlockFeature(featureType);
                    return true;
                }
            }
            
            return false;
        }

        private void UnlockFeature(string featureType)
        {
            switch (featureType)
            {
                case "route":
                    UnlockPremiumRoute();
                    break;
                case "customization":
                    UnlockCustomizationPack();
                    break;
                case "business":
                    UnlockBusinessLicense();
                    break;
            }
        }

        private void CheckAndTriggerRewards(float transactionAmount)
        {
            // Daily login streak bonus
            if (PlayerPrefs.GetInt("DailyLoginStreak", 0) >= 7)
            {
                playerWallet.AddFunds(dailyLoginBonus);
            }

            // Transaction milestone bonus
            if (transactionAmount >= 5000)
            {
                playerWallet.AddFunds(achievementBonus);
            }

            // Referral bonus check
            if (PlayerPrefs.HasKey("ReferralPending"))
            {
                playerWallet.AddFunds(referralBonus);
                PlayerPrefs.DeleteKey("ReferralPending");
            }
        }

        private void LogTransaction(float amount, float developerFee, string type)
        {
            // Analytics logging
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "amount", amount },
                { "developer_fee", developerFee },
                { "type", type },
                { "timestamp", DateTime.Now.ToString() }
            };

            // Send to analytics service
            Analytics.LogEvent("transaction_completed", parameters);
        }

        // Premium feature unlock methods
        private void UnlockPremiumRoute()
        {
            // Unlock premium route logic
            PlayerPrefs.SetInt("PremiumRoutesUnlocked", 1);
            PlayerPrefs.Save();
        }

        private void UnlockCustomizationPack()
        {
            // Unlock customization pack logic
            PlayerPrefs.SetInt("CustomizationPackUnlocked", 1);
            PlayerPrefs.Save();
        }

        private void UnlockBusinessLicense()
        {
            // Unlock business license logic
            PlayerPrefs.SetInt("BusinessLicenseUnlocked", 1);
            PlayerPrefs.Save();
        }

        // Player earnings calculation methods
        public float CalculateMatutuEarnings(float baseAmount)
        {
            float earnings = baseAmount * baseMatutuEarningsMultiplier;
            
            if (IsPeakHour())
            {
                earnings *= bonusEarningsMultiplier;
            }
            
            return earnings;
        }

        public float CalculateBusinessEarnings(float baseAmount)
        {
            float earnings = baseAmount * businessProfitMultiplier;
            
            if (isPremiumUser)
            {
                earnings *= 1.2f; // 20% bonus for premium users
            }
            
            return earnings;
        }

        private bool IsPeakHour()
        {
            int hour = DateTime.Now.Hour;
            return (hour >= 6 && hour <= 9) || (hour >= 17 && hour <= 19);
        }
    }

    [System.Serializable]
    public class PlayerWallet
    {
        public float Balance { get; private set; }
        public event Action<float> OnBalanceChanged;

        public void AddFunds(float amount)
        {
            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool DeductFunds(float amount)
        {
            if (Balance >= amount)
            {
                Balance -= amount;
                OnBalanceChanged?.Invoke(Balance);
                return true;
            }
            return false;
        }
    }
} 