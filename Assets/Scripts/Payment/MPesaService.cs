using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NairobiHustle.Payment
{
    public class MPesaService : MonoBehaviour
    {
        [Header("M-Pesa Configuration")]
        [SerializeField] private string merchantId = "YOUR_MERCHANT_ID";
        [SerializeField] private string apiKey = "YOUR_API_KEY";
        [SerializeField] private string shortCode = "YOUR_SHORTCODE";
        [SerializeField] private bool useSandbox = true;

        [Header("Transaction Settings")]
        [SerializeField] private float minimumAmount = 10f;
        [SerializeField] private float maximumAmount = 150000f;
        [SerializeField] private int transactionTimeout = 60;

        private List<MPesaTransaction> transactionHistory;

        private void Awake()
        {
            transactionHistory = new List<MPesaTransaction>();
            LoadTransactionHistory();
        }

        public async Task<bool> ProcessPayment(float amount)
        {
            if (amount < minimumAmount || amount > maximumAmount)
            {
                Debug.LogError($"Invalid amount: {amount}. Must be between {minimumAmount} and {maximumAmount}");
                return false;
            }

            try
            {
                // Create transaction record
                MPesaTransaction transaction = new MPesaTransaction
                {
                    transactionId = GenerateTransactionId(),
                    amount = amount,
                    timestamp = DateTime.Now,
                    status = "PENDING"
                };

                // TODO: Implement actual M-Pesa API integration here
                // For now, simulate successful transaction
                await SimulateTransaction(transaction);

                // Save transaction
                transactionHistory.Add(transaction);
                SaveTransactionHistory();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Payment processing failed: {e.Message}");
                return false;
            }
        }

        private async Task SimulateTransaction(MPesaTransaction transaction)
        {
            await Task.Delay(2000); // Simulate network delay
            transaction.status = "COMPLETED";
            transaction.resultCode = "0";
            transaction.resultDescription = "Success";
        }

        private string GenerateTransactionId()
        {
            return $"NH{DateTime.Now:yyyyMMddHHmmss}{UnityEngine.Random.Range(1000, 9999)}";
        }

        private void SaveTransactionHistory()
        {
            string json = JsonUtility.ToJson(new { transactions = transactionHistory });
            PlayerPrefs.SetString("MPesaTransactions", json);
            PlayerPrefs.Save();
        }

        private void LoadTransactionHistory()
        {
            string json = PlayerPrefs.GetString("MPesaTransactions", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<TransactionHistoryWrapper>(json);
                    transactionHistory = data.transactions ?? new List<MPesaTransaction>();
                }
                catch
                {
                    transactionHistory = new List<MPesaTransaction>();
                }
            }
        }

        [System.Serializable]
        private class TransactionHistoryWrapper
        {
            public List<MPesaTransaction> transactions;
        }
    }
}