using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NairobiHustle.Payment
{
    public class MPesaService : MonoBehaviour
    {
        [Header("Transaction Events")]
        public UnityEvent OnTransactionStart = new UnityEvent();
        public UnityEvent<float> OnSuccess = new UnityEvent<float>();
        public UnityEvent<string> OnFailure = new UnityEvent<string>();

        [Header("M-Pesa Configuration")]
        [SerializeField] private string merchantId = "YOUR_MPESA_MERCHANT_ID_HERE";
        [SerializeField] private string apiKey = "YOUR_MPESA_API_KEY_HERE";
        [SerializeField] private string shortCode = "171717";
        // WARNING: Replace with valid credentials from Safaricom Developer Portal
        // Obtain credentials at: https://developer.safaricom.co.ke/

#if UNITY_EDITOR

[InitializeOnLoad]
public class MPesaConfigValidator
{
    static MPesaConfigValidator()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if(state == PlayModeStateChange.ExitingEditMode)
        {
            var service = GameObject.FindObjectOfType<MPesaService>();
            if(service != null && 
                (service.merchantId.Contains("YOUR") || 
                 service.apiKey.Contains("YOUR") ||
                 service.shortCode.Contains("YOUR")))
            {
                EditorApplication.isPlaying = false;
                Debug.LogError("MPesa Configuration Error: Please set valid API credentials before entering play mode!");
            }
        }
    }
}
#endif
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

        public async Task<bool> ProcessInstantPayment(float amount)
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
                    phoneNumber = ValidatePhoneNumber(PlayerPrefs.GetString("UserPhoneNumber", "")),
                    timestamp = DateTime.Now,
                    status = "PENDING"
                };

                // Process transaction faster for small amounts
                await SimulateTransaction(transaction);

                // Save transaction
                transactionHistory.Add(transaction);
                SaveTransactionHistory();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Instant payment processing failed: {e.Message}");
                return false;
            }
        }

        public async Task<bool> ProcessPayment(float amount)
        {
            OnTransactionStart.Invoke();
            
            try
            {
                // Create transaction record
                MPesaTransaction transaction = new MPesaTransaction
                {
                    transactionId = GenerateTransactionId(),
                    amount = amount,
                    phoneNumber = ValidatePhoneNumber(PlayerPrefs.GetString("UserPhoneNumber", "")),
                    timestamp = DateTime.Now,
                    status = "PENDING"
                };

                // TODO: Implement actual M-Pesa API integration here
                // For now, simulate successful transaction
                await SimulateTransaction(transaction);

                // Save transaction
                transactionHistory.Add(transaction);
                SaveTransactionHistory();

                OnSuccess.Invoke(amount);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Payment processing failed: {e.Message}");
                return false;
            }
        }

        private string ValidatePhoneNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new Exception("Phone number not registered!");
        }

        if (!number.StartsWith("254") || number.Length != 12)
        {
            throw new Exception("Invalid Kenyan phone number format! Use 2547XXXXXXXX");
        }

        return number;
    }

    private async Task SimulateTransaction(MPesaTransaction transaction)
        {
            var url = useSandbox ? 
                "https://sandbox.safaricom.co.ke/mpesa/stkpush/v1/processrequest" :
                "https://api.safaricom.co.ke/mpesa/stkpush/v1/processrequest";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                request.SetRequestHeader("Content-Type", "application/json");

                var payload = new {
                    BusinessShortCode = shortCode,
                    Password = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{shortCode}{merchantId}{DateTime.Now:yyyyMMddHHmmss}")),
                    Timestamp = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TransactionType = "CustomerPayBillOnline",
                    Amount = transaction.amount,
                    PartyA = transaction.phoneNumber,
                    PartyB = shortCode,
                    PhoneNumber = transaction.phoneNumber,
                    CallBackURL = "https://yourdomain.com/callback",
                    AccountReference = "NairobiHustle",
                    TransactionDesc = "In-game purchase"
                };

                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.downloadHandler = new DownloadHandlerBuffer();

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    transaction.status = "FAILED";
                    transaction.resultDescription = request.error;
                Debug.LogError($"[MPesa] Transaction failed: {request.error}", this);
                OnFailure.Invoke(request.error);
                    throw new Exception(request.error);
                }

                transaction.status = "COMPLETED";
                transaction.resultCode = "0";
                transaction.resultDescription = "Success";
                Debug.Log($"[MPesa] Transaction {transaction.transactionId} completed successfully!", this);
            }
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