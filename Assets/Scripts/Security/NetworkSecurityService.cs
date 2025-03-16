using UnityEngine;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Linq;

namespace NairobiHustle.Security
{
    public class NetworkSecurityService : MonoBehaviour
    {
        [Header("Network Security Configuration")]
        [SerializeField] private bool enableSSLPinning = true;
        [SerializeField] private bool enableRequestSigning = true;
        [SerializeField] private bool enablePacketEncryption = true;
        [SerializeField] private bool enableDDoSProtection = true;
        [SerializeField] private float requestTimeout = 30f;

        [Header("Rate Limiting")]
        [SerializeField] private int maxRequestsPerMinute = 60;
        [SerializeField] private int maxConcurrentConnections = 10;
        [SerializeField] private float rateLimitResetTime = 60f;

        private X509Certificate2Collection trustedCertificates;
        private Dictionary<string, List<DateTime>> requestHistory;
        private Dictionary<string, int> activeConnections;
        private Queue<PendingRequest> requestQueue;
        private readonly object lockObject = new object();

        private void Awake()
        {
            InitializeNetworkSecurity();
        }

        private void InitializeNetworkSecurity()
        {
            try
            {
                trustedCertificates = LoadTrustedCertificates();
                requestHistory = new Dictionary<string, List<DateTime>>();
                activeConnections = new Dictionary<string, int>();
                requestQueue = new Queue<PendingRequest>();

                ConfigureSecurityProtocols();
                ConfigureCertificateValidation();
                StartCoroutine(RequestProcessingRoutine());
                StartCoroutine(RateLimitCleanupRoutine());
            }
            catch (Exception e)
            {
                Debug.LogError($"Network security initialization failed: {e.Message}");
                throw;
            }
        }

        private void ConfigureSecurityProtocols()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | 
                                                 SecurityProtocolType.Tls13;
            ServicePointManager.DefaultConnectionLimit = maxConcurrentConnections;
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
        }

        private void ConfigureCertificateValidation()
        {
            if (enableSSLPinning)
            {
                ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            }
        }

        public async Task<NetworkResponse> SendSecureRequest(NetworkRequest request)
        {
            try
            {
                // Validate request
                if (!ValidateRequest(request))
                {
                    return new NetworkResponse
                    {
                        Success = false,
                        Error = "Invalid request"
                    };
                }

                // Check rate limits
                if (!CheckRateLimits(request.Endpoint))
                {
                    return new NetworkResponse
                    {
                        Success = false,
                        Error = "Rate limit exceeded"
                    };
                }

                // Sign request if enabled
                if (enableRequestSigning)
                {
                    SignRequest(request);
                }

                // Encrypt payload if enabled
                if (enablePacketEncryption)
                {
                    EncryptPayload(request);
                }

                // Queue request
                var pendingRequest = new PendingRequest
                {
                    Request = request,
                    Timestamp = DateTime.UtcNow
                };

                lock (lockObject)
                {
                    requestQueue.Enqueue(pendingRequest);
                }

                // Process request
                return await ProcessSecureRequest(request);
            }
            catch (Exception e)
            {
                Debug.LogError($"Secure request failed: {e.Message}");
                return new NetworkResponse
                {
                    Success = false,
                    Error = "Request processing error"
                };
            }
        }

        private bool ValidateRequest(NetworkRequest request)
        {
            return request != null &&
                   !string.IsNullOrEmpty(request.Endpoint) &&
                   request.Method != RequestMethod.None;
        }

        private bool CheckRateLimits(string endpoint)
        {
            lock (lockObject)
            {
                if (!requestHistory.ContainsKey(endpoint))
                {
                    requestHistory[endpoint] = new List<DateTime>();
                }

                var requests = requestHistory[endpoint];
                var now = DateTime.UtcNow;

                // Remove old requests
                requests.RemoveAll(r => (now - r).TotalSeconds > rateLimitResetTime);

                // Check rate limit
                if (requests.Count >= maxRequestsPerMinute)
                {
                    return false;
                }

                // Add new request
                requests.Add(now);
                return true;
            }
        }

        private void SignRequest(NetworkRequest request)
        {
            var dataToSign = $"{request.Endpoint}:{request.Method}:{request.Timestamp.Ticks}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("SecretKey")))
            {
                var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
                request.Signature = Convert.ToBase64String(signature);
            }
        }

        private void EncryptPayload(NetworkRequest request)
        {
            if (request.Payload == null) return;

            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(
                        msEncrypt,
                        encryptor,
                        CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(request.Payload);
                    }

                    request.EncryptedPayload = new EncryptedPayload
                    {
                        Data = msEncrypt.ToArray(),
                        Key = aes.Key,
                        IV = aes.IV
                    };
                }
            }
        }

        private bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (!enableSSLPinning) return true;

            // Check if certificate is in trusted certificates
            return trustedCertificates.Contains(new X509Certificate2(certificate));
        }

        private X509Certificate2Collection LoadTrustedCertificates()
        {
            var certificates = new X509Certificate2Collection();
            
            // Load certificates from resources or secure storage
            // Implementation depends on how certificates are stored

            return certificates;
        }

        private async Task<NetworkResponse> ProcessSecureRequest(NetworkRequest request)
        {
            try
            {
                // Create HTTP client with security settings
                using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = ValidateServerCertificate;
                    
                    using (var client = new System.Net.Http.HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(requestTimeout);
                        
                        // Add security headers
                        client.DefaultRequestHeaders.Add("X-Request-Signature", request.Signature);
                        client.DefaultRequestHeaders.Add("X-Request-Timestamp", request.Timestamp.Ticks.ToString());

                        // Send request
                        var response = await SendRequest(client, request);

                        // Validate response
                        if (!ValidateResponse(response))
                        {
                            return new NetworkResponse
                            {
                                Success = false,
                                Error = "Invalid response"
                            };
                        }

                        return response;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Request processing failed: {e.Message}");
                return new NetworkResponse
                {
                    Success = false,
                    Error = "Request processing error"
                };
            }
        }

        private async Task<NetworkResponse> SendRequest(System.Net.Http.HttpClient client, NetworkRequest request)
        {
            // Implementation for sending HTTP request
            throw new NotImplementedException();
        }

        private bool ValidateResponse(NetworkResponse response)
        {
            // Implementation for response validation
            return true;
        }

        public class NetworkRequest
        {
            public string Endpoint { get; set; }
            public RequestMethod Method { get; set; }
            public string Payload { get; set; }
            public DateTime Timestamp { get; set; }
            public string Signature { get; set; }
            public EncryptedPayload EncryptedPayload { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }

        public class NetworkResponse
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public byte[] Data { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public int StatusCode { get; set; }
        }

        public class EncryptedPayload
        {
            public byte[] Data { get; set; }
            public byte[] Key { get; set; }
            public byte[] IV { get; set; }
        }

        public class PendingRequest
        {
            public NetworkRequest Request { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public enum RequestMethod
        {
            None,
            GET,
            POST,
            PUT,
            DELETE
        }
    }
} 