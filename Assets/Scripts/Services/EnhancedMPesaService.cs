using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace NairobiHustle.Services
{
    public class EnhancedMPesaService : MonoBehaviour
    {
        [Header("M-Pesa API Configuration")]
        [SerializeField] private string consumerKey;
        [SerializeField] private string consumerSecret;
        [SerializeField] private string businessShortCode;
        [SerializeField] private string passKey;
        [SerializeField] private string callbackUrl;

        [Header("Transaction Settings")]
        [SerializeField] private float minimumTransaction = 10f;
        [SerializeField] private float maximumTransaction = 150000f;
        [SerializeField] private float transactionFeeRate = 0.01f;
        [SerializeField] private float businessTransactionFeeRate = 0.02f;

        [Header("Business Settings")]
        [SerializeField] private float businessDailyLimit = 300000f;
        [SerializeField] private float personalDailyLimit = 150000f;
        [SerializeField] private int maxDailyTransactions = 30;

        private string accessToken;
        private DateTime tokenExpiry;
        private Dictionary<string, PlayerTransactionState> playerTransactions;
        private Queue<PendingTransaction> transactionQueue;
        private bool isProcessingQueue;

        private void Awake()
        {
            playerTransactions = new Dictionary<string, PlayerTransactionState>();
            transactionQueue = new Queue<PendingTransaction>();
            StartCoroutine(RefreshAccessTokenRoutine());
        }

        public async void ProcessBusinessPayment(string playerId, float amount, string businessType, Action<bool> callback)
        {
            if (!ValidateBusinessTransaction(playerId, amount))
            {
                callback?.Invoke(false);
                return;
            }

            PendingTransaction transaction = new PendingTransaction
            {
                type = TransactionType.Business,
                playerId = playerId,
                amount = amount,
                description = $"Business Payment: {businessType}",
                callback = callback,
                timestamp = DateTime.Now
            };

            await EnqueueTransaction(transaction);
        }

        public async void ProcessInvestmentPayment(string playerId, float amount, string investmentType, Action<bool> callback)
        {
            if (!ValidateInvestmentTransaction(playerId, amount))
            {
                callback?.Invoke(false);
                return;
            }

            PendingTransaction transaction = new PendingTransaction
            {
                type = TransactionType.Investment,
                playerId = playerId,
                amount = amount,
                description = $"Investment: {investmentType}",
                callback = callback,
                timestamp = DateTime.Now
            };

            await EnqueueTransaction(transaction);
        }

        public async void ProcessPlayerToPlayerPayment(string senderId, string receiverId, float amount, string reason, Action<bool> callback)
        {
            if (!ValidateP2PTransaction(senderId, amount))
            {
                callback?.Invoke(false);
                return;
            }

            PendingTransaction transaction = new PendingTransaction
            {
                type = TransactionType.P2P,
                playerId = senderId,
                receiverId = receiverId,
                amount = amount,
                description = $"P2P Payment: {reason}",
                callback = callback,
                timestamp = DateTime.Now
            };

            await EnqueueTransaction(transaction);
        }

        public async void ProcessWithdrawal(string playerId, float amount, string phoneNumber, Action<bool> callback)
        {
            if (!ValidateWithdrawal(playerId, amount))
            {
                callback?.Invoke(false);
                return;
            }

            PendingTransaction transaction = new PendingTransaction
            {
                type = TransactionType.Withdrawal,
                playerId = playerId,
                amount = amount,
                phoneNumber = phoneNumber,
                description = "Withdrawal to M-Pesa",
                callback = callback,
                timestamp = DateTime.Now
            };

            await EnqueueTransaction(transaction);
        }

        private async Task EnqueueTransaction(PendingTransaction transaction)
        {
            transactionQueue.Enqueue(transaction);
            
            if (!isProcessingQueue)
            {
                isProcessingQueue = true;
                await ProcessTransactionQueue();
            }
        }

        private async Task ProcessTransactionQueue()
        {
            while (transactionQueue.Count > 0)
            {
                PendingTransaction transaction = transactionQueue.Peek();
                
                bool success = await ProcessSingleTransaction(transaction);
                
                if (success)
                {
                    transactionQueue.Dequeue();
                    UpdatePlayerTransactionState(transaction);
                    transaction.callback?.Invoke(true);
                }
                else
                {
                    // Retry logic
                    if (transaction.retryCount < 3)
                    {
                        transaction.retryCount++;
                        await Task.Delay(1000); // Wait before retry
                        continue;
                    }
                    else
                    {
                        transactionQueue.Dequeue();
                        transaction.callback?.Invoke(false);
                    }
                }
            }
            
            isProcessingQueue = false;
        }

        private async Task<bool> ProcessSingleTransaction(PendingTransaction transaction)
        {
            try
            {
                // Ensure we have a valid token
                if (DateTime.Now >= tokenExpiry)
                {
                    await RefreshAccessToken();
                }

                string endpoint = GetEndpointForTransactionType(transaction.type);
                
                // Prepare request body
                var requestBody = new
                {
                    BusinessShortCode = businessShortCode,
                    Password = GeneratePassword(),
                    Timestamp = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TransactionType = GetTransactionCommand(transaction.type),
                    Amount = transaction.amount,
                    PartyA = transaction.phoneNumber,
                    PartyB = businessShortCode,
                    PhoneNumber = transaction.phoneNumber,
                    CallBackURL = callbackUrl,
                    AccountReference = transaction.playerId,
                    TransactionDesc = transaction.description
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);

                using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + accessToken);

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<MPesaResponse>(request.downloadHandler.text);
                        return response.ResponseCode == "0";
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"M-Pesa transaction failed: {e.Message}");
            }

            return false;
        }

        private bool ValidateBusinessTransaction(string playerId, float amount)
        {
            if (!playerTransactions.ContainsKey(playerId))
            {
                playerTransactions[playerId] = new PlayerTransactionState();
            }

            PlayerTransactionState state = playerTransactions[playerId];

            return amount >= minimumTransaction &&
                   amount <= maximumTransaction &&
                   state.GetDailyTotal() + amount <= businessDailyLimit &&
                   state.GetDailyTransactionCount() < maxDailyTransactions;
        }

        private bool ValidateInvestmentTransaction(string playerId, float amount)
        {
            if (!playerTransactions.ContainsKey(playerId))
            {
                playerTransactions[playerId] = new PlayerTransactionState();
            }

            PlayerTransactionState state = playerTransactions[playerId];

            return amount >= minimumTransaction &&
                   amount <= maximumTransaction &&
                   state.GetDailyTotal() + amount <= personalDailyLimit;
        }

        private bool ValidateP2PTransaction(string playerId, float amount)
        {
            if (!playerTransactions.ContainsKey(playerId))
            {
                playerTransactions[playerId] = new PlayerTransactionState();
            }

            PlayerTransactionState state = playerTransactions[playerId];

            return amount >= minimumTransaction &&
                   amount <= maximumTransaction &&
                   state.GetDailyTotal() + amount <= personalDailyLimit &&
                   state.GetDailyTransactionCount() < maxDailyTransactions;
        }

        private bool ValidateWithdrawal(string playerId, float amount)
        {
            if (!playerTransactions.ContainsKey(playerId))
            {
                playerTransactions[playerId] = new PlayerTransactionState();
            }

            PlayerTransactionState state = playerTransactions[playerId];

            return amount >= minimumTransaction &&
                   amount <= maximumTransaction &&
                   state.GetDailyTotal() + amount <= personalDailyLimit;
        }

        private void UpdatePlayerTransactionState(PendingTransaction transaction)
        {
            if (!playerTransactions.ContainsKey(transaction.playerId))
            {
                playerTransactions[transaction.playerId] = new PlayerTransactionState();
            }

            PlayerTransactionState state = playerTransactions[transaction.playerId];
            state.AddTransaction(transaction);
        }

        private string GetEndpointForTransactionType(TransactionType type)
        {
            switch (type)
            {
                case TransactionType.Business:
                    return "https://api.safaricom.co.ke/mpesa/b2c/v1/paymentrequest";
                case TransactionType.Investment:
                    return "https://api.safaricom.co.ke/mpesa/stkpush/v1/processrequest";
                case TransactionType.P2P:
                    return "https://api.safaricom.co.ke/mpesa/b2c/v1/paymentrequest";
                case TransactionType.Withdrawal:
                    return "https://api.safaricom.co.ke/mpesa/b2c/v1/paymentrequest";
                default:
                    return "";
            }
        }

        private string GetTransactionCommand(TransactionType type)
        {
            switch (type)
            {
                case TransactionType.Business:
                    return "BusinessPayment";
                case TransactionType.Investment:
                    return "CustomerPayBillOnline";
                case TransactionType.P2P:
                    return "BusinessPayment";
                case TransactionType.Withdrawal:
                    return "BusinessPayment";
                default:
                    return "";
            }
        }

        private string GeneratePassword()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string str = businessShortCode + passKey + timestamp;
            var encoding = new System.Text.ASCIIEncoding();
            byte[] bytes = encoding.GetBytes(str);
            return Convert.ToBase64String(bytes);
        }

        private System.Collections.IEnumerator RefreshAccessTokenRoutine()
        {
            while (true)
            {
                RefreshAccessToken();
                yield return new WaitForSeconds(3300); // Refresh every 55 minutes
            }
        }

        private async Task RefreshAccessToken()
        {
            try
            {
                string auth = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(consumerKey + ":" + consumerSecret)
                );

                using (UnityWebRequest request = UnityWebRequest.Get(
                    "https://api.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials"))
                {
                    request.SetRequestHeader("Authorization", "Basic " + auth);
                    
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<TokenResponse>(request.downloadHandler.text);
                        accessToken = response.access_token;
                        tokenExpiry = DateTime.Now.AddSeconds(response.expires_in);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to refresh M-Pesa access token: {e.Message}");
            }
        }
    }

    public class PlayerTransactionState
    {
        private List<TransactionRecord> dailyTransactions = new List<TransactionRecord>();

        public void AddTransaction(PendingTransaction transaction)
        {
            CleanupOldTransactions();

            dailyTransactions.Add(new TransactionRecord
            {
                amount = transaction.amount,
                timestamp = transaction.timestamp
            });
        }

        public float GetDailyTotal()
        {
            CleanupOldTransactions();
            return dailyTransactions.Sum(t => t.amount);
        }

        public int GetDailyTransactionCount()
        {
            CleanupOldTransactions();
            return dailyTransactions.Count;
        }

        private void CleanupOldTransactions()
        {
            DateTime cutoff = DateTime.Now.Date;
            dailyTransactions.RemoveAll(t => t.timestamp.Date < cutoff);
        }
    }

    public class TransactionRecord
    {
        public float amount;
        public DateTime timestamp;
    }

    public class PendingTransaction
    {
        public TransactionType type;
        public string playerId;
        public string receiverId;
        public float amount;
        public string phoneNumber;
        public string description;
        public Action<bool> callback;
        public DateTime timestamp;
        public int retryCount;
    }

    public enum TransactionType
    {
        Business,
        Investment,
        P2P,
        Withdrawal
    }

    public class MPesaResponse
    {
        public string ResponseCode { get; set; }
        public string ResponseDescription { get; set; }
        public string MerchantRequestID { get; set; }
        public string CheckoutRequestID { get; set; }
    }

    public class TokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }
} 