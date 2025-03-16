using UnityEngine;
using System;
using System.Collections.Generic;
using NairobiHustle.Payment;

namespace NairobiHustle.GameSystems
{
    public class DailyChallenges : MonoBehaviour
    {
        [Header("Challenge Settings")]
        [SerializeField] private Challenge[] dailyChallenges;
        [SerializeField] private Challenge[] weeklyChallenges;
        [SerializeField] private Challenge[] monthlyEvents;
        
        [Header("Reward Multipliers")]
        [SerializeField] private float consecutiveDaysMultiplier = 1.1f;
        [SerializeField] private float weeklyStreakMultiplier = 1.25f;
        [SerializeField] private float monthlyEventMultiplier = 1.5f;

        private PlayerWallet playerWallet;
        private PlayerProgression playerProgression;
        private Dictionary<string, ChallengeProgress> activeChallenges;
        private int consecutiveDays;
        private int weeklyStreak;

        private void Awake()
        {
            playerWallet = GetComponent<PlayerWallet>();
            playerProgression = GetComponent<PlayerProgression>();
            activeChallenges = new Dictionary<string, ChallengeProgress>();
            LoadChallenges();
        }

        private void Start()
        {
            // Check and update challenges daily
            InvokeRepeating("CheckAndUpdateChallenges", 0f, 3600f); // Check every hour
        }

        private void CheckAndUpdateChallenges()
        {
            DateTime now = DateTime.Now;
            string lastUpdateDate = PlayerPrefs.GetString("LastChallengeUpdate", "");

            if (string.IsNullOrEmpty(lastUpdateDate) || DateTime.Parse(lastUpdateDate).Date < now.Date)
            {
                GenerateNewChallenges();
                UpdateStreaks();
                PlayerPrefs.SetString("LastChallengeUpdate", now.ToString());
                PlayerPrefs.Save();
            }
        }

        private void GenerateNewChallenges()
        {
            activeChallenges.Clear();

            // Generate daily challenges
            foreach (Challenge challenge in GetRandomChallenges(dailyChallenges, 3))
            {
                activeChallenges.Add(challenge.id, new ChallengeProgress(challenge));
            }

            // Check if we need new weekly challenges
            if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
            {
                foreach (Challenge challenge in GetRandomChallenges(weeklyChallenges, 2))
                {
                    activeChallenges.Add(challenge.id, new ChallengeProgress(challenge));
                }
            }

            // Check for monthly events
            if (DateTime.Now.Day == 1)
            {
                Challenge monthlyEvent = monthlyEvents[UnityEngine.Random.Range(0, monthlyEvents.Length)];
                activeChallenges.Add(monthlyEvent.id, new ChallengeProgress(monthlyEvent));
            }

            SaveChallenges();
        }

        private Challenge[] GetRandomChallenges(Challenge[] pool, int count)
        {
            List<Challenge> selected = new List<Challenge>();
            List<Challenge> available = new List<Challenge>(pool);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, available.Count);
                selected.Add(available[index]);
                available.RemoveAt(index);
            }

            return selected.ToArray();
        }

        public void UpdateChallengeProgress(string challengeType, float amount)
        {
            foreach (var kvp in activeChallenges)
            {
                if (kvp.Value.challenge.type == challengeType)
                {
                    kvp.Value.currentProgress += amount;
                    
                    if (kvp.Value.currentProgress >= kvp.Value.challenge.targetAmount && !kvp.Value.isCompleted)
                    {
                        CompleteChallengeAndReward(kvp.Value);
                    }
                }
            }

            SaveChallenges();
        }

        private void CompleteChallengeAndReward(ChallengeProgress progress)
        {
            progress.isCompleted = true;
            float reward = progress.challenge.reward;

            // Apply multipliers
            switch (progress.challenge.duration)
            {
                case ChallengeDuration.Daily:
                    reward *= Mathf.Pow(consecutiveDaysMultiplier, consecutiveDays);
                    break;
                case ChallengeDuration.Weekly:
                    reward *= Mathf.Pow(weeklyStreakMultiplier, weeklyStreak);
                    break;
                case ChallengeDuration.Monthly:
                    reward *= monthlyEventMultiplier;
                    break;
            }

            // Award the reward
            playerWallet.AddFunds(reward);
            
            // Add experience and reputation
            playerProgression.AddExperience(reward * 0.1f, "challenge");
            playerProgression.AddReputation(reward * 0.05f, "challenge");

            // Trigger achievement check
            CheckChallengeAchievements();
        }

        private void UpdateStreaks()
        {
            bool hasCompletedDaily = true;
            bool hasCompletedWeekly = true;

            foreach (var kvp in activeChallenges)
            {
                if (kvp.Value.challenge.duration == ChallengeDuration.Daily && !kvp.Value.isCompleted)
                {
                    hasCompletedDaily = false;
                }
                if (kvp.Value.challenge.duration == ChallengeDuration.Weekly && !kvp.Value.isCompleted)
                {
                    hasCompletedWeekly = false;
                }
            }

            if (hasCompletedDaily)
            {
                consecutiveDays++;
            }
            else
            {
                consecutiveDays = 0;
            }

            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                if (hasCompletedWeekly)
                {
                    weeklyStreak++;
                }
                else
                {
                    weeklyStreak = 0;
                }
            }

            PlayerPrefs.SetInt("ConsecutiveDays", consecutiveDays);
            PlayerPrefs.SetInt("WeeklyStreak", weeklyStreak);
            PlayerPrefs.Save();
        }

        private void CheckChallengeAchievements()
        {
            // Check for challenge completion achievements
            int totalCompleted = PlayerPrefs.GetInt("TotalChallengesCompleted", 0) + 1;
            PlayerPrefs.SetInt("TotalChallengesCompleted", totalCompleted);

            // Milestone rewards
            if (totalCompleted % 50 == 0) // Every 50 challenges
            {
                playerWallet.AddFunds(1000f); // Bonus reward
                playerProgression.AddReputation(100f, "challenge_milestone");
            }

            if (consecutiveDays >= 30) // Monthly dedication reward
            {
                playerWallet.AddFunds(5000f);
                playerProgression.AddReputation(500f, "monthly_dedication");
            }

            PlayerPrefs.Save();
        }

        private void LoadChallenges()
        {
            string challengesJson = PlayerPrefs.GetString("ActiveChallenges", "{}");
            activeChallenges = JsonUtility.FromJson<Dictionary<string, ChallengeProgress>>(challengesJson);
            consecutiveDays = PlayerPrefs.GetInt("ConsecutiveDays", 0);
            weeklyStreak = PlayerPrefs.GetInt("WeeklyStreak", 0);
        }

        private void SaveChallenges()
        {
            string challengesJson = JsonUtility.ToJson(activeChallenges);
            PlayerPrefs.SetString("ActiveChallenges", challengesJson);
            PlayerPrefs.Save();
        }
    }

    [System.Serializable]
    public class Challenge
    {
        public string id;
        public string title;
        public string description;
        public string type;
        public float targetAmount;
        public float reward;
        public ChallengeDuration duration;
        public ChallengeLocation[] validLocations;
        public VehicleType[] validVehicles;
    }

    [System.Serializable]
    public class ChallengeProgress
    {
        public Challenge challenge;
        public float currentProgress;
        public bool isCompleted;

        public ChallengeProgress(Challenge challenge)
        {
            this.challenge = challenge;
            this.currentProgress = 0f;
            this.isCompleted = false;
        }
    }

    public enum ChallengeDuration
    {
        Daily,
        Weekly,
        Monthly
    }

    public enum ChallengeLocation
    {
        Nairobi,
        Mombasa,
        Kisumu,
        Nakuru,
        Any
    }
} 