using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace NairobiHustle.MPesa
{
    public class MPesaService : MonoBehaviour
    {
        [Header("M-Pesa API Configuration")]
        [SerializeField] private string consumerKey;
        [SerializeField] private string consumerSecret;
        [SerializeField] private string businessShortCode;
        [SerializeField] private string passKey;
        [SerializeField] private string callbackUrl;
        
        private string accessToken;
        private DateTime tokenExpiry;
        private readonly HttpClient httpClient = new HttpClient();
        private const string SANDBOX_BASE_URL = "https://sandbox.safaricom.co.ke";
        private const string PRODUCTION_BASE_URL = "https://api.safaricom.co.ke";
        
        private void Awake()
        {
            InitializeService();
        }

        private async void InitializeService()
        {
            await RefreshAccessToken();
        }

        private async Task RefreshAccessToken()
        {
            try
            {
                string credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{consumerKey}:{consumerSecret}")
                );

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

                var response = await httpClient.GetAsync(
                    $"{GetBaseUrl()}/oauth/v1/generate?grant_type=client_credentials"
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(result);
                    
                    accessToken = tokenResponse.AccessToken;
                    tokenExpiry = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to refresh M-Pesa access token: {e.Message}");
            }
        }

        public async Task<bool> ProcessInstantPayment(float amount)
        {
            try
            {
                if (DateTime.Now >= tokenExpiry)
                {
                    await RefreshAccessToken();
                }

                var request = new STKPushRequest
                {
                    BusinessShortCode = businessShortCode,
                    Password = GeneratePassword(),
                    Timestamp = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TransactionType = "CustomerPayBillOnline",
                    Amount = Mathf.CeilToInt(amount).ToString(),
                    PartyA = GetUserPhoneNumber(),
                    PartyB = businessShortCode,
                    PhoneNumber = GetUserPhoneNumber(),
                    CallBackURL = callbackUrl,
                    AccountReference = GenerateTransactionReference(),
                    TransactionDesc = "Nairobi Hustle Game Payment"
                };

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"{GetBaseUrl()}/mpesa/stkpush/v1/processrequest",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var stkResponse = JsonConvert.DeserializeObject<STKPushResponse>(result);
                    
                    // Start polling for completion
                    return await PollTransactionStatus(stkResponse.CheckoutRequestID);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to process instant payment: {e.Message}");
            }

            return false;
        }

        public async Task<bool> ProcessPayment(float amount)
        {
            // For larger amounts, use standard processing
            return await ProcessInstantPayment(amount);
        }

        private async Task<bool> PollTransactionStatus(string checkoutRequestId)
        {
            int maxAttempts = 10;
            int currentAttempt = 0;
            
            while (currentAttempt < maxAttempts)
            {
                var status = await CheckTransactionStatus(checkoutRequestId);
                
                if (status == "Success")
                {
                    return true;
                }
                else if (status == "Failed" || status == "Cancelled")
                {
                    return false;
                }
                
                await Task.Delay(2000); // Wait 2 seconds before next check
                currentAttempt++;
            }
            
            return false;
        }

        private async Task<string> CheckTransactionStatus(string checkoutRequestId)
        {
            try
            {
                var request = new
                {
                    BusinessShortCode = businessShortCode,
                    Password = GeneratePassword(),
                    Timestamp = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    CheckoutRequestID = checkoutRequestId
                };

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"{GetBaseUrl()}/mpesa/stkpushquery/v1/query",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var statusResponse = JsonConvert.DeserializeObject<TransactionStatusResponse>(result);
                    return statusResponse.ResultDesc;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to check transaction status: {e.Message}");
            }

            return "Failed";
        }

        private string GeneratePassword()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string str = businessShortCode + passKey + timestamp;
            var encoding = new ASCIIEncoding();
            byte[] bytes = encoding.GetBytes(str);
            return Convert.ToBase64String(bytes);
        }

        private string GenerateTransactionReference()
        {
            return $"NH{DateTime.Now.ToString("yyyyMMddHHmmss")}";
        }

        private string GetUserPhoneNumber()
        {
            // Get the user's phone number from PlayerPrefs or game state
            return PlayerPrefs.GetString("UserPhoneNumber", "");
        }

        private string GetBaseUrl()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
                return SANDBOX_BASE_URL;
            #else
                return PRODUCTION_BASE_URL;
            #endif
        }
    }

    [System.Serializable]
    public class AccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    [System.Serializable]
    public class STKPushRequest
    {
        [JsonProperty("BusinessShortCode")]
        public string BusinessShortCode { get; set; }

        [JsonProperty("Password")]
        public string Password { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("TransactionType")]
        public string TransactionType { get; set; }

        [JsonProperty("Amount")]
        public string Amount { get; set; }

        [JsonProperty("PartyA")]
        public string PartyA { get; set; }

        [JsonProperty("PartyB")]
        public string PartyB { get; set; }

        [JsonProperty("PhoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonProperty("CallBackURL")]
        public string CallBackURL { get; set; }

        [JsonProperty("AccountReference")]
        public string AccountReference { get; set; }

        [JsonProperty("TransactionDesc")]
        public string TransactionDesc { get; set; }
    }

    [System.Serializable]
    public class STKPushResponse
    {
        [JsonProperty("CheckoutRequestID")]
        public string CheckoutRequestID { get; set; }

        [JsonProperty("ResponseCode")]
        public string ResponseCode { get; set; }

        [JsonProperty("ResponseDescription")]
        public string ResponseDescription { get; set; }

        [JsonProperty("CustomerMessage")]
        public string CustomerMessage { get; set; }
    }

    [System.Serializable]
    public class TransactionStatusResponse
    {
        [JsonProperty("ResultCode")]
        public string ResultCode { get; set; }

        [JsonProperty("ResultDesc")]
        public string ResultDesc { get; set; }
    }
} 