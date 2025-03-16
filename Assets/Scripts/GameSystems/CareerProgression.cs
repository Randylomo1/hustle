using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class CareerProgression : MonoBehaviour
    {
        [Header("Career Paths")]
        [SerializeField] private CareerPath[] careerPaths = new CareerPath[]
        {
            new CareerPath
            {
                pathName = "Matatu Driver",
                stages = new CareerStage[]
                {
                    new CareerStage { 
                        title = "Rookie Driver",
                        requiredExperience = 0,
                        baseIncome = 1000,
                        unlockRequirements = new string[] { "basic_license" }
                    },
                    new CareerStage { 
                        title = "Experienced Driver",
                        requiredExperience = 5000,
                        baseIncome = 2000,
                        unlockRequirements = new string[] { "advanced_license", "safety_certification" }
                    },
                    new CareerStage { 
                        title = "Senior Driver",
                        requiredExperience = 15000,
                        baseIncome = 3500,
                        unlockRequirements = new string[] { "master_license", "customer_service_cert" }
                    },
                    new CareerStage { 
                        title = "Fleet Trainer",
                        requiredExperience = 30000,
                        baseIncome = 5000,
                        unlockRequirements = new string[] { "trainer_certification" }
                    }
                }
            },
            new CareerPath
            {
                pathName = "Business Owner",
                stages = new CareerStage[]
                {
                    new CareerStage { 
                        title = "Single Vehicle Owner",
                        requiredExperience = 10000,
                        baseIncome = 3000,
                        unlockRequirements = new string[] { "business_license", "vehicle_ownership" }
                    },
                    new CareerStage { 
                        title = "Small Fleet Owner",
                        requiredExperience = 25000,
                        baseIncome = 7000,
                        unlockRequirements = new string[] { "fleet_license", "management_cert" }
                    },
                    new CareerStage { 
                        title = "SACCO Director",
                        requiredExperience = 50000,
                        baseIncome = 15000,
                        unlockRequirements = new string[] { "sacco_license", "leadership_cert" }
                    },
                    new CareerStage { 
                        title = "Transport Mogul",
                        requiredExperience = 100000,
                        baseIncome = 30000,
                        unlockRequirements = new string[] { "mogul_certification" }
                    }
                }
            }
        }

        [Header("Investment Options")]
        [SerializeField] private InvestmentOption[] investmentOptions = new InvestmentOption[]
        {
            new InvestmentOption {
                name = "Vehicle Savings",
                minimumAmount = 1000,
                interestRate = 0.08f,
                lockPeriodDays = 30,
                earlyWithdrawalPenalty = 0.05f
            },
            new InvestmentOption {
                name = "Fleet Expansion Fund",
                minimumAmount = 5000,
                interestRate = 0.12f,
                lockPeriodDays = 90,
                earlyWithdrawalPenalty = 0.1f
            },
            new InvestmentOption {
                name = "SACCO Shares",
                minimumAmount = 10000,
                interestRate = 0.15f,
                lockPeriodDays = 180,
                earlyWithdrawalPenalty = 0.15f
            }
        };

        [Header("Real Money Conversion")]
        [SerializeField] private float minimumWithdrawalAmount = 1000f; // Minimum KES for M-Pesa withdrawal
        [SerializeField] private float conversionRate = 0.8f; // 80% of in-game currency to real money
        [SerializeField] private float withdrawalFee = 0.05f; // 5% withdrawal fee

        private PlayerWallet playerWallet;
        private MPesaService mpesaService;
        private Dictionary<string, Investment> activeInvestments;
        private CareerPath currentPath;
        private int currentStage;
        private float totalInvested;
        private float totalEarned;
        private float lifetimeEarnings;

        private void Awake()
        {
            playerWallet = GetComponent<PlayerWallet>();
            mpesaService = GetComponent<MPesaService>();
            activeInvestments = new Dictionary<string, Investment>();
            LoadCareerProgress();
        }

        public async Task<bool> InvestFunds(string optionName, float amount)
        {
            InvestmentOption option = Array.Find(investmentOptions, o => o.name == optionName);
            
            if (option == null || amount < option.minimumAmount || !playerWallet.DeductFunds(amount))
            {
                return false;
            }

            string investmentId = Guid.NewGuid().ToString();
            Investment investment = new Investment
            {
                id = investmentId,
                option = option,
                amount = amount,
                startDate = DateTime.Now,
                maturityDate = DateTime.Now.AddDays(option.lockPeriodDays)
            };
            
            // Prepare for serialization
            investment.OnBeforeSerialize();

            if (activeInvestments == null)
            {
                activeInvestments = new Dictionary<string, Investment>();
            }
            
            activeInvestments.Add(investmentId, investment);
            totalInvested += amount;
            SaveCareerProgress();

            return true;
        }

        public async Task<bool> WithdrawInvestment(string investmentId, bool toMPesa = false)
        {
            if (!activeInvestments.ContainsKey(investmentId))
            {
                return false;
            }

            Investment investment = activeInvestments[investmentId];
            float amount = CalculateInvestmentValue(investment);
            
            if (toMPesa && amount >= minimumWithdrawalAmount)
            {
                float realMoneyAmount = amount * conversionRate * (1 - withdrawalFee);
                bool success = await mpesaService.ProcessPayment(realMoneyAmount);
                
                if (success)
                {
                    activeInvestments.Remove(investmentId);
                    totalInvested -= investment.amount;
                    SaveCareerProgress();
                    return true;
                }
                return false;
            }
            
            playerWallet.AddFunds(amount);
            activeInvestments.Remove(investmentId);
            totalInvested -= investment.amount;
            SaveCareerProgress();
            return true;
        }

        public float CalculateInvestmentValue(Investment investment)
        {
            float daysInvested = (float)(DateTime.Now - investment.startDate).TotalDays;
            float maturityDays = (float)(investment.maturityDate - investment.startDate).TotalDays;
            
            if (daysInvested >= maturityDays)
            {
                // Full maturity value
                return investment.amount * (1 + investment.option.interestRate);
            }
            
            // Early withdrawal penalty
            float progress = daysInvested / maturityDays;
            float interestEarned = investment.amount * investment.option.interestRate * progress;
            float penalty = investment.amount * investment.option.earlyWithdrawalPenalty;
            
            return investment.amount + interestEarned - penalty;
        }

        public void UpdateCareerProgress(float experience, float earnings)
        {
            lifetimeEarnings += earnings;
            totalEarned += earnings;

            // Check for career stage progression
            CareerStage[] stages = currentPath.stages;
            for (int i = stages.Length - 1; i >= 0; i--)
            {
                if (experience >= stages[i].requiredExperience && currentStage < i)
                {
                    PromoteToStage(i);
                    break;
                }
            }

            SaveCareerProgress();
        }

        private void PromoteToStage(int newStage)
        {
            CareerStage stage = currentPath.stages[newStage];
            currentStage = newStage;

            // Award promotion bonus
            float promotionBonus = stage.baseIncome * 2;
            playerWallet.AddFunds(promotionBonus);

            // Unlock new features
            foreach (string requirement in stage.unlockRequirements)
            {
                PlayerPrefs.SetInt($"Career_Unlock_{requirement}", 1);
            }

            // Notify player of promotion
            Debug.Log($"Congratulations! You've been promoted to {stage.title}!");

            SaveCareerProgress();
        }

        public float GetCurrentIncome()
        {
            if (currentPath == null || currentStage >= currentPath.stages.Length)
            {
                return 0f;
            }

            CareerStage stage = currentPath.stages[currentStage];
            float baseIncome = stage.baseIncome;

            // Apply experience multiplier
            float experienceMultiplier = 1f + (lifetimeEarnings / 1000000f); // 0.1% increase per million earned
            
            // Apply investment bonus
            float investmentMultiplier = 1f + (totalInvested / 100000f); // 0.1% increase per 100k invested

            return baseIncome * experienceMultiplier * investmentMultiplier;
        }

        private void LoadCareerProgress()
        {
            string pathName = PlayerPrefs.GetString("CareerPath", "Matatu Driver");
            currentPath = Array.Find(careerPaths, p => p.pathName == pathName);
            currentStage = PlayerPrefs.GetInt("CareerStage", 0);
            totalInvested = PlayerPrefs.GetFloat("TotalInvested", 0);
            totalEarned = PlayerPrefs.GetFloat("TotalEarned", 0);
            lifetimeEarnings = PlayerPrefs.GetFloat("LifetimeEarnings", 0);

            // Initialize investments dictionary
            activeInvestments = new Dictionary<string, Investment>();
            
            // Load investments
            string investmentsJson = PlayerPrefs.GetString("ActiveInvestments", "");
            if (!string.IsNullOrEmpty(investmentsJson))
            {
                try
                {
                    InvestmentCollection investmentCollection = JsonUtility.FromJson<InvestmentCollection>(investmentsJson);
                    
                    if (investmentCollection != null && investmentCollection.investments != null)
                    {
                        foreach (Investment investment in investmentCollection.investments)
                        {
                            // Process DateTime fields after deserialization
                            investment.OnAfterDeserialize();
                            if (!string.IsNullOrEmpty(investment.id))
                            {
                                activeInvestments.Add(investment.id, investment);
                            }
                            else
                            {
                                Debug.LogWarning("Skipped investment with null or empty ID during loading");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading investments: {e.Message}");
                    activeInvestments = new Dictionary<string, Investment>();
                }
            }
        }

        private void SaveCareerProgress()
        {
            if (currentPath == null)
            {
                Debug.LogError("Cannot save career progress: currentPath is null");
                return;
            }
            
            try
            {
                PlayerPrefs.SetString("CareerPath", currentPath.pathName);
                PlayerPrefs.SetInt("CareerStage", currentStage);
                PlayerPrefs.SetFloat("TotalInvested", totalInvested);
                PlayerPrefs.SetFloat("TotalEarned", totalEarned);
                PlayerPrefs.SetFloat("LifetimeEarnings", lifetimeEarnings);

                // Prepare investments for serialization
                if (activeInvestments != null)
                {
                    foreach (Investment investment in activeInvestments.Values)
                    {
                        investment.OnBeforeSerialize();
                    }
                    
                    InvestmentCollection investmentCollection = new InvestmentCollection
                    {
                        investments = activeInvestments.Values.ToArray()
                    };

                    string investmentsJson = JsonUtility.ToJson(investmentCollection);
                    PlayerPrefs.SetString("ActiveInvestments", investmentsJson);
                }
                else
                {
                    // If no investments, save an empty collection
                    InvestmentCollection emptyCollection = new InvestmentCollection
                    {
                        investments = new Investment[0]
                    };
                    PlayerPrefs.SetString("ActiveInvestments", JsonUtility.ToJson(emptyCollection));
                }
                
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving career progress: {e.Message}");
            }
        }
    }

    [System.Serializable]
    public class CareerPath
    {
        public string pathName;
        public CareerStage[] stages;
    }

    [System.Serializable]
    public class CareerStage
    {
        public string title;
        public float requiredExperience;
        public float baseIncome;
        public string[] unlockRequirements;
    }

    [System.Serializable]
    public class InvestmentOption
    {
        public string name;
        public float minimumAmount;
        public float interestRate;
        public int lockPeriodDays;
        public float earlyWithdrawalPenalty;
    }

    [System.Serializable]
    public class Investment
    {
        public string id;
        public InvestmentOption option;
        public float amount;
        
        // DateTime serialization workaround
        [SerializeField] private string startDateString;
        [SerializeField] private string maturityDateString;
        
        [System.NonSerialized]
        public DateTime startDate;
        
        [System.NonSerialized]
        public DateTime maturityDate;
        
        // Called before serialization
        public void OnBeforeSerialize()
        {
            startDateString = startDate.ToString("o");
            maturityDateString = maturityDate.ToString("o");
        }
        
        // Called after deserialization
        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(startDateString))
            {
                startDate = DateTime.Parse(startDateString);
            }
            
            if (!string.IsNullOrEmpty(maturityDateString))
            {
                maturityDate = DateTime.Parse(maturityDateString);
            }
        }
    }
    
    [System.Serializable]
    public class InvestmentCollection
    {
        public Investment[] investments;
    }
}