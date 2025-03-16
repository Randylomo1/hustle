using UnityEngine;
using System;
using System.Collections.Generic;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class RealLifeDynamics : MonoBehaviour
    {
        [Header("Police System")]
        [SerializeField] private PoliceEvent[] policeEvents = new PoliceEvent[]
        {
            new PoliceEvent {
                type = PoliceEventType.Checkpoint,
                frequency = 0.3f, // 30% chance per route
                baseFineCost = 2000f,
                bribeCost = 1000f,
                reputationImpact = -20f,
                escapeChance = 0.4f,
                locations = new string[] { "Thika Road", "Mombasa Road", "Waiyaki Way" }
            },
            new PoliceEvent {
                type = PoliceEventType.SpeedTrap,
                frequency = 0.2f,
                baseFineCost = 5000f,
                bribeCost = 2000f,
                reputationImpact = -30f,
                escapeChance = 0.3f,
                locations = new string[] { "Uhuru Highway", "Ngong Road", "Langata Road" }
            },
            new PoliceEvent {
                type = PoliceEventType.NightPatrol,
                frequency = 0.4f,
                baseFineCost = 3000f,
                bribeCost = 1500f,
                reputationImpact = -25f,
                escapeChance = 0.2f,
                locations = new string[] { "CBD", "Industrial Area", "Westlands" }
            }
        };

        [Header("Health System")]
        [SerializeField] private HealthCondition[] healthConditions = new HealthCondition[]
        {
            new HealthCondition {
                type = HealthConditionType.Fatigue,
                onsetTime = 8f, // Hours of continuous driving
                performanceImpact = 0.7f,
                recoveryRate = 0.1f, // Per hour of rest
                medicalCost = 500f
            },
            new HealthCondition {
                type = HealthConditionType.Stress,
                onsetTime = 6f,
                performanceImpact = 0.8f,
                recoveryRate = 0.15f,
                medicalCost = 1000f
            },
            new HealthCondition {
                type = HealthConditionType.BackPain,
                onsetTime = 10f,
                performanceImpact = 0.6f,
                recoveryRate = 0.05f,
                medicalCost = 2000f
            }
        };

        [Header("Urban Events")]
        [SerializeField] private UrbanEvent[] urbanEvents = new UrbanEvent[]
        {
            new UrbanEvent {
                type = UrbanEventType.Robbery,
                probability = 0.1f,
                moneyLoss = 5000f,
                healthImpact = 30f,
                escapeChance = 0.5f,
                policeResponseTime = 300f // seconds
            },
            new UrbanEvent {
                type = UrbanEventType.CarjackAttempt,
                probability = 0.05f,
                moneyLoss = 10000f,
                healthImpact = 50f,
                escapeChance = 0.4f,
                policeResponseTime = 240f
            },
            new UrbanEvent {
                type = UrbanEventType.RouteFight,
                probability = 0.15f,
                moneyLoss = 2000f,
                healthImpact = 20f,
                escapeChance = 0.7f,
                policeResponseTime = 360f
            }
        };

        [Header("Player Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float maxStress = 100f;
        [SerializeField] private float healthRecoveryRate = 5f; // Per hour
        [SerializeField] private float staminaRecoveryRate = 10f;
        [SerializeField] private float stressIncreaseRate = 5f;

        private PlayerWallet playerWallet;
        private CareerProgression careerProgression;
        private float currentHealth;
        private float currentStamina;
        private float currentStress;
        private float drivingTime;
        private List<HealthCondition> activeConditions;
        private Dictionary<string, float> routeSafetyRatings;
        private bool isInPolicePursuit;
        private float pursuitDuration;
        private float lastEventTime;

        private void Awake()
        {
            playerWallet = GetComponent<PlayerWallet>();
            careerProgression = GetComponent<CareerProgression>();
            activeConditions = new List<HealthCondition>();
            routeSafetyRatings = new Dictionary<string, float>();
            InitializePlayerState();
        }

        private void InitializePlayerState()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentStress = 0f;
            drivingTime = 0f;
            isInPolicePursuit = false;
            lastEventTime = Time.time;

            // Initialize route safety ratings
            foreach (PoliceEvent evt in policeEvents)
            {
                foreach (string location in evt.locations)
                {
                    routeSafetyRatings[location] = 100f;
                }
            }

            InvokeRepeating("UpdatePlayerState", 0f, 60f); // Update every minute
        }

        private void UpdatePlayerState()
        {
            UpdateHealth();
            UpdateStamina();
            UpdateStress();
            CheckForEvents();
            UpdateDrivingConditions();
        }

        private void UpdateHealth()
        {
            if (currentHealth < maxHealth)
            {
                currentHealth += (healthRecoveryRate / 60f);
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            // Apply health conditions effects
            foreach (HealthCondition condition in activeConditions)
            {
                currentHealth -= (1f - condition.performanceImpact) * (healthRecoveryRate / 60f);
            }
        }

        private void UpdateStamina()
        {
            if (IsPlayerResting())
            {
                currentStamina += (staminaRecoveryRate / 60f);
            }
            else
            {
                currentStamina -= (staminaRecoveryRate / 120f); // Depletes at half the recovery rate
            }

            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }

        private void UpdateStress()
        {
            float stressChange = 0f;

            // Increase stress during work hours
            if (!IsPlayerResting())
            {
                stressChange += (stressIncreaseRate / 60f);
            }
            else
            {
                stressChange -= (stressIncreaseRate / 30f); // Recover twice as fast while resting
            }

            // Additional stress from active conditions
            foreach (HealthCondition condition in activeConditions)
            {
                stressChange += (1f - condition.performanceImpact) * (stressIncreaseRate / 60f);
            }

            currentStress = Mathf.Clamp(currentStress + stressChange, 0f, maxStress);
        }

        private void CheckForEvents()
        {
            if (Time.time - lastEventTime < 1800f) // Minimum 30 minutes between events
                return;

            // Check for police events
            foreach (PoliceEvent evt in policeEvents)
            {
                if (UnityEngine.Random.value < evt.frequency)
                {
                    TriggerPoliceEvent(evt);
                    lastEventTime = Time.time;
                    return;
                }
            }

            // Check for urban events
            foreach (UrbanEvent evt in urbanEvents)
            {
                if (UnityEngine.Random.value < evt.probability)
                {
                    TriggerUrbanEvent(evt);
                    lastEventTime = Time.time;
                    return;
                }
            }
        }

        private void TriggerPoliceEvent(PoliceEvent evt)
        {
            // Calculate escape probability based on player stats
            float escapeModifier = (currentStamina / maxStamina) * (currentHealth / maxHealth);
            float finalEscapeChance = evt.escapeChance * escapeModifier;

            if (UnityEngine.Random.value < finalEscapeChance)
            {
                // Player escapes but takes stress damage
                currentStress += 20f;
                currentStamina -= 30f;
                StartPolicePursuit();
            }
            else
            {
                // Player must pay fine or bribe
                float finalFine = evt.baseFineCost * (1f + (currentStress / maxStress));
                if (!playerWallet.DeductFunds(finalFine))
                {
                    // Failed to pay - reputation damage
                    careerProgression.UpdateCareerProgress(-evt.reputationImpact, -finalFine);
                }
            }
        }

        private void TriggerUrbanEvent(UrbanEvent evt)
        {
            float escapeModifier = (currentStamina / maxStamina) * (currentHealth / maxHealth);
            float finalEscapeChance = evt.escapeChance * escapeModifier;

            if (UnityEngine.Random.value < finalEscapeChance)
            {
                // Player escapes with minimal damage
                currentHealth -= evt.healthImpact * 0.3f;
                currentStress += 10f;
            }
            else
            {
                // Full event impact
                currentHealth -= evt.healthImpact;
                currentStress += 30f;
                playerWallet.DeductFunds(evt.moneyLoss);
                
                // Call police
                StartCoroutine(SimulatePoliceResponse(evt.policeResponseTime));
            }
        }

        private void StartPolicePursuit()
        {
            if (!isInPolicePursuit)
            {
                isInPolicePursuit = true;
                pursuitDuration = 0f;
                StartCoroutine(HandlePolicePursuit());
            }
        }

        private System.Collections.IEnumerator HandlePolicePursuit()
        {
            while (isInPolicePursuit && pursuitDuration < 300f) // Max 5 minutes pursuit
            {
                pursuitDuration += Time.deltaTime;
                currentStamina -= Time.deltaTime * 0.2f;
                currentStress += Time.deltaTime * 0.1f;

                if (currentStamina <= 0f || UnityEngine.Random.value < 0.01f) // 1% chance per second to get caught
                {
                    // Player caught
                    EndPursuit(false);
                    break;
                }

                yield return null;
            }

            if (isInPolicePursuit)
            {
                // Player escaped
                EndPursuit(true);
            }
        }

        private void EndPursuit(bool escaped)
        {
            isInPolicePursuit = false;
            if (!escaped)
            {
                // Heavy penalties for getting caught
                playerWallet.DeductFunds(10000f);
                careerProgression.UpdateCareerProgress(-100f, -10000f);
                currentHealth -= 20f;
            }
        }

        private System.Collections.IEnumerator SimulatePoliceResponse(float responseTime)
        {
            yield return new WaitForSeconds(responseTime);
            // Police arrives - can trigger additional events or recovery options
        }

        private void UpdateDrivingConditions()
        {
            if (!IsPlayerResting())
            {
                drivingTime += 1f/60f; // Add one minute

                // Check for health conditions
                foreach (HealthCondition condition in healthConditions)
                {
                    if (drivingTime >= condition.onsetTime && !activeConditions.Contains(condition))
                    {
                        activeConditions.Add(condition);
                        ApplyHealthCondition(condition);
                    }
                }
            }
            else
            {
                // Recovery during rest
                drivingTime = Mathf.Max(0f, drivingTime - (1f/30f)); // Recover twice as fast
                RecoverFromConditions();
            }
        }

        private void ApplyHealthCondition(HealthCondition condition)
        {
            currentHealth *= condition.performanceImpact;
            currentStamina *= condition.performanceImpact;
        }

        private void RecoverFromConditions()
        {
            for (int i = activeConditions.Count - 1; i >= 0; i--)
            {
                HealthCondition condition = activeConditions[i];
                if (UnityEngine.Random.value < condition.recoveryRate)
                {
                    activeConditions.RemoveAt(i);
                }
            }
        }

        private bool IsPlayerResting()
        {
            DateTime now = DateTime.Now;
            return now.Hour < 6 || now.Hour >= 22; // Rest between 10 PM and 6 AM
        }

        public float GetHealthPercentage() => currentHealth / maxHealth;
        public float GetStaminaPercentage() => currentStamina / maxStamina;
        public float GetStressPercentage() => currentStress / maxStress;
        public bool IsInPursuit() => isInPolicePursuit;
        public List<HealthCondition> GetActiveConditions() => activeConditions;
    }

    [System.Serializable]
    public class PoliceEvent
    {
        public PoliceEventType type;
        public float frequency;
        public float baseFineCost;
        public float bribeCost;
        public float reputationImpact;
        public float escapeChance;
        public string[] locations;
    }

    [System.Serializable]
    public class HealthCondition
    {
        public HealthConditionType type;
        public float onsetTime;
        public float performanceImpact;
        public float recoveryRate;
        public float medicalCost;
    }

    [System.Serializable]
    public class UrbanEvent
    {
        public UrbanEventType type;
        public float probability;
        public float moneyLoss;
        public float healthImpact;
        public float escapeChance;
        public float policeResponseTime;
    }

    public enum PoliceEventType
    {
        Checkpoint,
        SpeedTrap,
        NightPatrol,
        Pursuit,
        Investigation
    }

    public enum HealthConditionType
    {
        Fatigue,
        Stress,
        BackPain,
        Fever,
        Injury
    }

    public enum UrbanEventType
    {
        Robbery,
        CarjackAttempt,
        RouteFight,
        Protest,
        RoadBlock
    }
} 