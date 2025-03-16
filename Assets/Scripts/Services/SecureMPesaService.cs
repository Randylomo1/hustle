using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace NairobiHustle.Services
{
    public class SecureMPesaService : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string consumerKey;
        [SerializeField] private string consumerSecret;
        [SerializeField] private string businessShortCode;
        [SerializeField] private string passKey;
        [SerializeField] private string callbackUrl;

        [Header("Security Settings")]
        [SerializeField] private bool useAdvancedEncryption = true;
        [SerializeField] private bool useTransactionVerification = true;
        [SerializeField] private bool useBiometricAuth = true;
        [SerializeField] private float sessionTimeout = 300f; // 5 minutes

        [Header("Transaction Settings")]
        [SerializeField] private float minimumAmount = 10f;
        [SerializeField] private float maximumAmount = 150000f;
        [SerializeField] private int maxDailyTransactions = 30;
        [SerializeField] private float dailyTransactionLimit = 300000f;

        [Header("Enhanced Security Settings")]
        [SerializeField] private bool useAIFraudDetection = true;
        [SerializeField] private bool useLocationVerification = true;
        [SerializeField] private bool useDeviceFingerprinting = true;
        [SerializeField] private bool useDynamicLimits = true;
        [SerializeField] private int maxFailedAttempts = 3;
        [SerializeField] private float suspiciousActivityThreshold = 0.8f;
        
        [Header("Dynamic Transaction Limits")]
        [SerializeField] private float baseMinimumAmount = 10f;
        [SerializeField] private float baseMaximumAmount = 150000f;
        [SerializeField] private float trustScoreMultiplier = 1.5f;
        [SerializeField] private float verifiedUserMultiplier = 2.0f;
        [SerializeField] private int baseDailyTransactionLimit = 30;
        [SerializeField] private float baseDailyAmountLimit = 300000f;

        private string accessToken;
        private DateTime tokenExpiry;
        private Dictionary<string, TransactionSession> activeSessions;
        private Queue<SecureTransaction> transactionQueue;
        private SecurityProvider security;
        private TransactionMonitor monitor;
        private FraudDetectionSystem fraudDetection;
        private UserTrustSystem trustSystem;
        private DeviceVerification deviceVerification;
        private LocationValidator locationValidator;

        private void Awake()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            activeSessions = new Dictionary<string, TransactionSession>();
            transactionQueue = new Queue<SecureTransaction>();
            security = new SecurityProvider(useAdvancedEncryption);
            monitor = new TransactionMonitor(maxDailyTransactions, dailyTransactionLimit);

            fraudDetection = new FraudDetectionSystem(useAIFraudDetection, maxFailedAttempts);
            trustSystem = new UserTrustSystem(trustScoreMultiplier, verifiedUserMultiplier);
            deviceVerification = new DeviceVerification(useDeviceFingerprinting);
            locationValidator = new LocationValidator(useLocationVerification);

            StartCoroutine(RefreshTokenRoutine());
            StartCoroutine(MonitorTransactions());
            StartCoroutine(SecurityMonitoringRoutine());
        }

        public async Task<TransactionResult> ProcessSecurePayment(
            string phoneNumber, 
            float amount, 
            string description,
            BiometricData biometricData = null,
            LocationData locationData = null,
            DeviceData deviceData = null)
        {
            try
            {
                // Enhanced security checks
                if (!await PerformSecurityChecks(phoneNumber, amount, locationData, deviceData))
                {
                    return new TransactionResult 
                    { 
                        Success = false, 
                        Error = "Security validation failed" 
                    };
                }

                // Validate transaction
                if (!ValidateTransaction(phoneNumber, amount))
                {
                    return new TransactionResult 
                    { 
                        Success = false, 
                        Error = "Transaction validation failed" 
                    };
                }

                // Verify biometric data if enabled
                if (useBiometricAuth && !VerifyBiometricData(biometricData))
                {
                    return new TransactionResult 
                    { 
                        Success = false, 
                        Error = "Biometric verification failed" 
                    };
                }

                // Create secure session
                string sessionId = await CreateSecureSession(phoneNumber);
                if (string.IsNullOrEmpty(sessionId))
                {
                    return new TransactionResult 
                    { 
                        Success = false, 
                        Error = "Failed to create secure session" 
                    };
                }

                // Process payment
                var transaction = new SecureTransaction
                {
                    SessionId = sessionId,
                    PhoneNumber = phoneNumber,
                    Amount = amount,
                    Description = description,
                    Timestamp = DateTime.Now,
                    SecurityHash = GenerateSecurityHash(phoneNumber, amount)
                };

                return await ProcessTransactionSecurely(transaction);
            }
            catch (Exception e)
            {
                fraudDetection.RecordFailedAttempt(phoneNumber);
                Debug.LogError($"Payment processing failed: {e.Message}");
                return new TransactionResult 
                { 
                    Success = false, 
                    Error = "Internal processing error" 
                };
            }
        }

        private async Task<bool> PerformSecurityChecks(
            string phoneNumber, 
            float amount,
            LocationData locationData,
            DeviceData deviceData)
        {
            // Check for suspicious activity
            if (fraudDetection.IsSuspiciousActivity(phoneNumber, amount))
            {
                Debug.LogWarning($"Suspicious activity detected for {phoneNumber}");
                return false;
            }

            // Verify location if enabled
            if (useLocationVerification && !locationValidator.ValidateLocation(locationData))
            {
                Debug.LogWarning($"Location validation failed for {phoneNumber}");
                return false;
            }

            // Verify device if enabled
            if (useDeviceFingerprinting && !deviceVerification.ValidateDevice(deviceData))
            {
                Debug.LogWarning($"Device validation failed for {phoneNumber}");
                return false;
            }

            return true;
        }

        private bool ValidateTransaction(string phoneNumber, float amount)
        {
            var limits = GetDynamicTransactionLimits(phoneNumber);
            
            return amount >= limits.MinAmount &&
                   amount <= limits.MaxAmount &&
                   monitor.CanProcessTransaction(phoneNumber, amount, limits);
        }

        private TransactionLimits GetDynamicTransactionLimits(string phoneNumber)
        {
            if (!useDynamicLimits)
            {
                return new TransactionLimits
                {
                    MinAmount = baseMinimumAmount,
                    MaxAmount = baseMaximumAmount,
                    DailyLimit = baseDailyAmountLimit,
                    TransactionLimit = baseDailyTransactionLimit
                };
            }

            float trustScore = trustSystem.GetUserTrustScore(phoneNumber);
            bool isVerified = trustSystem.IsVerifiedUser(phoneNumber);

            return new TransactionLimits
            {
                MinAmount = baseMinimumAmount,
                MaxAmount = baseMaximumAmount * (isVerified ? verifiedUserMultiplier : 1) * trustScore,
                DailyLimit = baseDailyAmountLimit * (isVerified ? verifiedUserMultiplier : 1) * trustScore,
                TransactionLimit = (int)(baseDailyTransactionLimit * (isVerified ? verifiedUserMultiplier : 1))
            };
        }

        private async Task<string> CreateSecureSession(string phoneNumber)
        {
            try
            {
                string sessionId = Guid.NewGuid().ToString();
                var session = new TransactionSession
                {
                    Id = sessionId,
                    PhoneNumber = phoneNumber,
                    StartTime = DateTime.Now,
                    SecurityToken = security.GenerateSessionToken()
                };

                activeSessions[sessionId] = session;
                StartCoroutine(MonitorSession(sessionId));

                return sessionId;
            }
            catch (Exception e)
            {
                Debug.LogError($"Session creation failed: {e.Message}");
                return null;
            }
        }

        private async Task<TransactionResult> ProcessTransactionSecurely(SecureTransaction transaction)
        {
            try
            {
                // Encrypt transaction data
                var encryptedData = security.EncryptTransactionData(transaction);

                // Prepare API request
                var requestData = new
                {
                    BusinessShortCode = businessShortCode,
                    Password = GeneratePassword(),
                    Timestamp = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TransactionType = "CustomerPayBillOnline",
                    Amount = transaction.Amount,
                    PartyA = transaction.PhoneNumber,
                    PartyB = businessShortCode,
                    PhoneNumber = transaction.PhoneNumber,
                    CallBackURL = callbackUrl,
                    AccountReference = transaction.SessionId,
                    TransactionDesc = security.EncryptDescription(transaction.Description)
                };

                // Send request
                using (UnityWebRequest request = CreateSecureRequest(requestData))
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<MPesaResponse>(
                            request.downloadHandler.text);

                        // Verify response
                        if (VerifyTransactionResponse(response, transaction))
                        {
                            monitor.RecordTransaction(transaction);
                            return new TransactionResult 
                            { 
                                Success = true,
                                TransactionId = response.TransactionId,
                                SessionId = transaction.SessionId
                            };
                        }
                    }
                }

                return new TransactionResult 
                { 
                    Success = false, 
                    Error = "Transaction processing failed" 
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Secure transaction processing failed: {e.Message}");
                return new TransactionResult 
                { 
                    Success = false, 
                    Error = "Transaction processing error" 
                };
            }
        }

        private bool VerifyBiometricData(BiometricData data)
        {
            if (data == null) return false;
            // Implement biometric verification
            return true;
        }

        private string GenerateSecurityHash(string phoneNumber, float amount)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string data = $"{phoneNumber}:{amount}:{DateTime.Now.Ticks}";
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private UnityWebRequest CreateSecureRequest(object data)
        {
            string jsonData = JsonConvert.SerializeObject(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            UnityWebRequest request = new UnityWebRequest(
                "https://api.safaricom.co.ke/mpesa/stkpush/v1/processrequest",
                "POST");

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SetRequestHeader("X-Security-Hash", security.GenerateRequestHash(jsonData));

            return request;
        }

        private bool VerifyTransactionResponse(MPesaResponse response, SecureTransaction transaction)
        {
            if (response == null) return false;

            // Verify response authenticity
            string responseHash = security.GenerateResponseHash(response);
            if (responseHash != response.SecurityHash) return false;

            // Verify amount matches
            if (response.Amount != transaction.Amount) return false;

            // Verify transaction is within time limit
            TimeSpan timeDiff = DateTime.Now - transaction.Timestamp;
            if (timeDiff.TotalSeconds > 60) return false;

            return true;
        }

        private class SecurityProvider
        {
            private readonly bool useAdvancedEncryption;
            private readonly byte[] key;
            private readonly byte[] iv;

            public SecurityProvider(bool useAdvanced)
            {
                useAdvancedEncryption = useAdvanced;
                using (Aes aes = Aes.Create())
                {
                    aes.GenerateKey();
                    aes.GenerateIV();
                    key = aes.Key;
                    iv = aes.IV;
                }
            }

            public byte[] EncryptTransactionData(SecureTransaction transaction)
            {
                if (!useAdvancedEncryption) return null;

                string data = JsonConvert.SerializeObject(transaction);
                byte[] bytes = Encoding.UTF8.GetBytes(data);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                    }
                }
            }

            public string EncryptDescription(string description)
            {
                if (!useAdvancedEncryption) return description;

                byte[] bytes = Encoding.UTF8.GetBytes(description);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }

            public string GenerateSessionToken()
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] tokenData = new byte[32];
                    rng.GetBytes(tokenData);
                    return Convert.ToBase64String(tokenData);
                }
            }

            public string GenerateRequestHash(string data)
            {
                using (HMACSHA256 hmac = new HMACSHA256(key))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(data);
                    byte[] hash = hmac.ComputeHash(bytes);
                    return Convert.ToBase64String(hash);
                }
            }

            public string GenerateResponseHash(MPesaResponse response)
            {
                string data = $"{response.TransactionId}:{response.Amount}:{response.Timestamp}";
                using (HMACSHA256 hmac = new HMACSHA256(key))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(data);
                    byte[] hash = hmac.ComputeHash(bytes);
                    return Convert.ToBase64String(hash);
                }
            }
        }

        private class TransactionMonitor
        {
            private readonly int maxDailyTransactions;
            private readonly float dailyLimit;
            private Dictionary<string, List<TransactionRecord>> transactionHistory;

            public TransactionMonitor(int maxTransactions, float limit)
            {
                maxDailyTransactions = maxTransactions;
                dailyLimit = limit;
                transactionHistory = new Dictionary<string, List<TransactionRecord>>();
            }

            public bool CanProcessTransaction(string phoneNumber, float amount)
            {
                CleanupOldRecords();

                if (!transactionHistory.ContainsKey(phoneNumber))
                {
                    transactionHistory[phoneNumber] = new List<TransactionRecord>();
                    return true;
                }

                var records = transactionHistory[phoneNumber];
                float dailyTotal = records.Sum(r => r.Amount);
                
                return records.Count < maxDailyTransactions &&
                       dailyTotal + amount <= dailyLimit;
            }

            public void RecordTransaction(SecureTransaction transaction)
            {
                if (!transactionHistory.ContainsKey(transaction.PhoneNumber))
                {
                    transactionHistory[transaction.PhoneNumber] = new List<TransactionRecord>();
                }

                transactionHistory[transaction.PhoneNumber].Add(new TransactionRecord
                {
                    Amount = transaction.Amount,
                    Timestamp = transaction.Timestamp
                });
            }

            private void CleanupOldRecords()
            {
                DateTime cutoff = DateTime.Now.Date;
                foreach (var records in transactionHistory.Values)
                {
                    records.RemoveAll(r => r.Timestamp.Date < cutoff);
                }
            }
        }

        private class TransactionSession
        {
            public string Id { get; set; }
            public string PhoneNumber { get; set; }
            public DateTime StartTime { get; set; }
            public string SecurityToken { get; set; }
        }

        private class SecureTransaction
        {
            public string SessionId { get; set; }
            public string PhoneNumber { get; set; }
            public float Amount { get; set; }
            public string Description { get; set; }
            public DateTime Timestamp { get; set; }
            public string SecurityHash { get; set; }
        }

        private class TransactionRecord
        {
            public float Amount { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class TransactionResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public string TransactionId { get; set; }
            public string SessionId { get; set; }
        }

        private class BiometricData
        {
            public byte[] FingerprintData { get; set; }
            public byte[] FaceData { get; set; }
            public string DeviceId { get; set; }
        }

        private class MPesaResponse
        {
            public string TransactionId { get; set; }
            public float Amount { get; set; }
            public string Timestamp { get; set; }
            public string SecurityHash { get; set; }
        }

        private class FraudDetectionSystem
        {
            private readonly bool useAI;
            private readonly int maxAttempts;
            private Dictionary<string, List<FailedAttempt>> failedAttempts;
            private Dictionary<string, List<TransactionPattern>> transactionPatterns;

            public FraudDetectionSystem(bool useAI, int maxAttempts)
            {
                this.useAI = useAI;
                this.maxAttempts = maxAttempts;
                failedAttempts = new Dictionary<string, List<FailedAttempt>>();
                transactionPatterns = new Dictionary<string, List<TransactionPattern>>();
            }

            public bool IsSuspiciousActivity(string phoneNumber, float amount)
            {
                CleanupOldRecords();

                // Check failed attempts
                if (GetRecentFailedAttempts(phoneNumber) >= maxAttempts)
                {
                    return true;
                }

                // Check unusual patterns
                if (useAI && IsUnusualPattern(phoneNumber, amount))
                {
                    return true;
                }

                return false;
            }

            public void RecordFailedAttempt(string phoneNumber)
            {
                if (!failedAttempts.ContainsKey(phoneNumber))
                {
                    failedAttempts[phoneNumber] = new List<FailedAttempt>();
                }

                failedAttempts[phoneNumber].Add(new FailedAttempt
                {
                    Timestamp = DateTime.Now
                });
            }

            private int GetRecentFailedAttempts(string phoneNumber)
            {
                if (!failedAttempts.ContainsKey(phoneNumber)) return 0;

                DateTime cutoff = DateTime.Now.AddHours(-1);
                return failedAttempts[phoneNumber].Count(f => f.Timestamp >= cutoff);
            }

            private bool IsUnusualPattern(string phoneNumber, float amount)
            {
                if (!transactionPatterns.ContainsKey(phoneNumber)) return false;

                var patterns = transactionPatterns[phoneNumber];
                var avgAmount = patterns.Average(p => p.Amount);
                var stdDev = CalculateStandardDeviation(patterns.Select(p => p.Amount));

                return Math.Abs(amount - avgAmount) > stdDev * 3;
            }

            private void CleanupOldRecords()
            {
                DateTime cutoff = DateTime.Now.AddDays(-1);
                foreach (var attempts in failedAttempts.Values)
                {
                    attempts.RemoveAll(a => a.Timestamp < cutoff);
                }
            }

            private double CalculateStandardDeviation(IEnumerable<float> values)
            {
                var avg = values.Average();
                var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
                return Math.Sqrt(sumOfSquares / values.Count());
            }
        }

        private class UserTrustSystem
        {
            private readonly float trustMultiplier;
            private readonly float verifiedMultiplier;
            private Dictionary<string, UserTrustData> userTrustData;

            public UserTrustSystem(float trustMult, float verifiedMult)
            {
                trustMultiplier = trustMult;
                verifiedMultiplier = verifiedMult;
                userTrustData = new Dictionary<string, UserTrustData>();
            }

            public float GetUserTrustScore(string phoneNumber)
            {
                if (!userTrustData.ContainsKey(phoneNumber))
                {
                    return 1.0f;
                }

                return userTrustData[phoneNumber].TrustScore;
            }

            public bool IsVerifiedUser(string phoneNumber)
            {
                return userTrustData.ContainsKey(phoneNumber) &&
                       userTrustData[phoneNumber].IsVerified;
            }
        }

        private class DeviceVerification
        {
            private readonly bool useFingerprinting;
            private Dictionary<string, List<DeviceData>> trustedDevices;

            public DeviceVerification(bool useFingerprinting)
            {
                this.useFingerprinting = useFingerprinting;
                trustedDevices = new Dictionary<string, List<DeviceData>>();
            }

            public bool ValidateDevice(DeviceData deviceData)
            {
                if (!useFingerprinting || deviceData == null) return true;

                return IsTrustedDevice(deviceData);
            }

            private bool IsTrustedDevice(DeviceData device)
            {
                // Implementation for device trust validation
                return true;
            }
        }

        private class LocationValidator
        {
            private readonly bool useValidation;
            private Dictionary<string, List<LocationData>> userLocations;

            public LocationValidator(bool useValidation)
            {
                this.useValidation = useValidation;
                userLocations = new Dictionary<string, List<LocationData>>();
            }

            public bool ValidateLocation(LocationData location)
            {
                if (!useValidation || location == null) return true;

                return IsReasonableLocation(location);
            }

            private bool IsReasonableLocation(LocationData location)
            {
                // Implementation for location validation
                return true;
            }
        }

        private class TransactionLimits
        {
            public float MinAmount { get; set; }
            public float MaxAmount { get; set; }
            public float DailyLimit { get; set; }
            public int TransactionLimit { get; set; }
        }

        private class FailedAttempt
        {
            public DateTime Timestamp { get; set; }
        }

        private class TransactionPattern
        {
            public float Amount { get; set; }
            public DateTime Timestamp { get; set; }
            public string TransactionType { get; set; }
        }

        private class UserTrustData
        {
            public float TrustScore { get; set; }
            public bool IsVerified { get; set; }
            public DateTime LastVerification { get; set; }
            public int SuccessfulTransactions { get; set; }
        }

        private class LocationData
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public float Accuracy { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class DeviceData
        {
            public string DeviceId { get; set; }
            public string DeviceModel { get; set; }
            public string OperatingSystem { get; set; }
            public string AppVersion { get; set; }
            public Dictionary<string, string> DeviceFingerprint { get; set; }
        }
    }
} 