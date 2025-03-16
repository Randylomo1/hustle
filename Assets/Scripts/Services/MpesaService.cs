using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace NairobiHustle.Services
{
    [Serializable]
    public class MpesaConfig
    {
        public string ConsumerKey;
        public string ConsumerSecret;
        public string PassKey;
        public string ShortCode;
        public string CallbackUrl;
        public bool UseSandbox;
    }

    public class MpesaService : MonoBehaviour
    {
        private static MpesaService instance;
        public static MpesaService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("MpesaService");
                    instance = go.AddComponent<MpesaService>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private MpesaConfig config;
        private string accessToken;
        private DateTime tokenExpiry;
        private readonly HttpClient httpClient;
        private const string SANDBOX_URL = "https://sandbox.safaricom.co.ke";
        private const string PRODUCTION_URL = "https://api.safaricom.co.ke";

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Load configuration
            TextAsset configFile = Resources.Load<TextAsset>("MpesaConfig");
            if (configFile != null)
            {
                config = JsonConvert.DeserializeObject<MpesaConfig>(configFile.text);
            }
            else
            {
                Debug.LogError("MpesaConfig.json not found in Resources folder!");
            }

            httpClient = new HttpClient();
        }

        private string BaseUrl => config.UseSandbox ? SANDBOX_URL : PRODUCTION_URL;

        private async Task<string> GetAccessToken()
        {
            if (!string.IsNullOrEmpty(accessToken) && DateTime.Now < tokenExpiry)
            {
                return accessToken;
            }

            try
            {
                string auth = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{config.ConsumerKey}:{config.ConsumerSecret}")
                );

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");

                var response = await httpClient.GetAsync(
                    $"{BaseUrl}/oauth/v1/generate?grant_type=client_credentials"
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(result);
                    accessToken = tokenResponse.access_token;
                    tokenExpiry = DateTime.Now.AddSeconds(3599);
                    return accessToken;
                }
                else
                {
                    throw new Exception($"Failed to get access token: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting access token: {ex.Message}");
                throw;
            }
        }

        public async Task<string> InitiatePayment(string phoneNumber, decimal amount, string accountReference)
        {
            try
            {
                string token = await GetAccessToken();
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string password = GeneratePassword(timestamp);

                var stkPushRequest = new
                {
                    BusinessShortCode = config.ShortCode,
                    Password = password,
                    Timestamp = timestamp,
                    TransactionType = "CustomerPayBillOnline",
                    Amount = amount,
                    PartyA = phoneNumber,
                    PartyB = config.ShortCode,
                    PhoneNumber = phoneNumber,
                    CallBackURL = config.CallbackUrl,
                    AccountReference = accountReference,
                    TransactionDesc = "Nairobi Hustle Game Payment"
                };

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var json = JsonConvert.SerializeObject(stkPushRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"{BaseUrl}/mpesa/stkpush/v1/processrequest",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var stkResponse = JsonConvert.DeserializeObject<StkPushResponse>(result);
                    return stkResponse.CheckoutRequestID;
                }
                else
                {
                    throw new Exception($"STK Push failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initiating payment: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ProcessWithdrawal(string phoneNumber, decimal amount)
        {
            try
            {
                string token = await GetAccessToken();
                // Implement B2C (Business to Customer) payment
                // This requires additional API endpoints and permissions
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing withdrawal: {ex.Message}");
                return false;
            }
        }

        private string GeneratePassword(string timestamp)
        {
            string str = $"{config.ShortCode}{config.PassKey}{timestamp}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        private class AccessTokenResponse
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
        }

        private class StkPushResponse
        {
            public string MerchantRequestID { get; set; }
            public string CheckoutRequestID { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseDescription { get; set; }
            public string CustomerMessage { get; set; }
        }

        private void OnDestroy()
        {
            httpClient?.Dispose();
        }
    }
} 