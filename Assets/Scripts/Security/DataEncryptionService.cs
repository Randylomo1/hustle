using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NairobiHustle.Security
{
    public class DataEncryptionService : MonoBehaviour
    {
        [Header("Encryption Configuration")]
        [SerializeField] private bool enableAtRest = true;
        [SerializeField] private bool enableInTransit = true;
        [SerializeField] private bool enableKeyRotation = true;
        [SerializeField] private int keyRotationDays = 30;

        private const int KEY_SIZE = 256;
        private const int BLOCK_SIZE = 128;
        private const int SALT_SIZE = 32;
        private const int ITERATIONS = 10000;

        private Dictionary<string, byte[]> encryptionKeys;
        private Dictionary<string, DateTime> keyCreationDates;
        private readonly object lockObject = new object();

        private void Awake()
        {
            InitializeEncryption();
        }

        private void InitializeEncryption()
        {
            try
            {
                encryptionKeys = new Dictionary<string, byte[]>();
                keyCreationDates = new Dictionary<string, DateTime>();

                // Generate initial keys
                GenerateNewKey("UserData");
                GenerateNewKey("PaymentData");
                GenerateNewKey("GameState");

                if (enableKeyRotation)
                {
                    StartCoroutine(KeyRotationRoutine());
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Encryption initialization failed: {e.Message}");
                throw;
            }
        }

        private void GenerateNewKey(string keyId)
        {
            lock (lockObject)
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    var key = new byte[KEY_SIZE / 8];
                    rng.GetBytes(key);
                    
                    encryptionKeys[keyId] = key;
                    keyCreationDates[keyId] = DateTime.UtcNow;
                }
            }
        }

        public async Task<byte[]> EncryptData(byte[] data, string keyId = "UserData")
        {
            try
            {
                if (!enableAtRest) return data;

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KEY_SIZE;
                    aes.BlockSize = BLOCK_SIZE;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Generate salt and IV
                    var salt = new byte[SALT_SIZE];
                    using (var rng = new RNGCryptoServiceProvider())
                    {
                        rng.GetBytes(salt);
                        rng.GetBytes(aes.IV);
                    }

                    // Derive key using PBKDF2
                    using (var deriveBytes = new Rfc2898DeriveBytes(
                        encryptionKeys[keyId],
                        salt,
                        ITERATIONS,
                        HashAlgorithmName.SHA256))
                    {
                        aes.Key = deriveBytes.GetBytes(KEY_SIZE / 8);
                    }

                    // Combine salt + IV + encrypted data
                    using (var msEncrypt = new MemoryStream())
                    {
                        // Write salt and IV
                        await msEncrypt.WriteAsync(salt, 0, salt.Length);
                        await msEncrypt.WriteAsync(aes.IV, 0, aes.IV.Length);

                        // Encrypt and write data
                        using (var encryptor = aes.CreateEncryptor())
                        using (var csEncrypt = new CryptoStream(
                            msEncrypt,
                            encryptor,
                            CryptoStreamMode.Write))
                        {
                            await csEncrypt.WriteAsync(data, 0, data.Length);
                            csEncrypt.FlushFinalBlock();
                        }

                        return msEncrypt.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Encryption failed: {e.Message}");
                throw;
            }
        }

        public async Task<byte[]> DecryptData(byte[] encryptedData, string keyId = "UserData")
        {
            try
            {
                if (!enableAtRest) return encryptedData;

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KEY_SIZE;
                    aes.BlockSize = BLOCK_SIZE;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Extract salt and IV
                    var salt = new byte[SALT_SIZE];
                    Buffer.BlockCopy(encryptedData, 0, salt, 0, SALT_SIZE);

                    aes.IV = new byte[aes.BlockSize / 8];
                    Buffer.BlockCopy(
                        encryptedData,
                        SALT_SIZE,
                        aes.IV,
                        0,
                        aes.IV.Length
                    );

                    // Derive key using PBKDF2
                    using (var deriveBytes = new Rfc2898DeriveBytes(
                        encryptionKeys[keyId],
                        salt,
                        ITERATIONS,
                        HashAlgorithmName.SHA256))
                    {
                        aes.Key = deriveBytes.GetBytes(KEY_SIZE / 8);
                    }

                    // Decrypt data
                    using (var msDecrypt = new MemoryStream())
                    {
                        using (var decryptor = aes.CreateDecryptor())
                        using (var csDecrypt = new CryptoStream(
                            msDecrypt,
                            decryptor,
                            CryptoStreamMode.Write))
                        {
                            await csDecrypt.WriteAsync(
                                encryptedData,
                                SALT_SIZE + aes.IV.Length,
                                encryptedData.Length - (SALT_SIZE + aes.IV.Length)
                            );
                            csDecrypt.FlushFinalBlock();
                        }

                        return msDecrypt.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Decryption failed: {e.Message}");
                throw;
            }
        }

        public async Task<string> EncryptString(string data, string keyId = "UserData")
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = await EncryptData(bytes, keyId);
            return Convert.ToBase64String(encrypted);
        }

        public async Task<string> DecryptString(string encryptedData, string keyId = "UserData")
        {
            var bytes = Convert.FromBase64String(encryptedData);
            var decrypted = await DecryptData(bytes, keyId);
            return Encoding.UTF8.GetString(decrypted);
        }

        public async Task<bool> EncryptFile(
            string sourceFile,
            string destinationFile,
            string keyId = "UserData")
        {
            try
            {
                if (!enableAtRest) return false;

                var fileData = await File.ReadAllBytesAsync(sourceFile);
                var encryptedData = await EncryptData(fileData, keyId);
                await File.WriteAllBytesAsync(destinationFile, encryptedData);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"File encryption failed: {e.Message}");
                return false;
            }
        }

        public async Task<bool> DecryptFile(
            string sourceFile,
            string destinationFile,
            string keyId = "UserData")
        {
            try
            {
                if (!enableAtRest) return false;

                var encryptedData = await File.ReadAllBytesAsync(sourceFile);
                var decryptedData = await DecryptData(encryptedData, keyId);
                await File.WriteAllBytesAsync(destinationFile, decryptedData);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"File decryption failed: {e.Message}");
                return false;
            }
        }

        private void CheckKeyRotation()
        {
            if (!enableKeyRotation) return;

            foreach (var keyId in keyCreationDates.Keys)
            {
                var age = (DateTime.UtcNow - keyCreationDates[keyId]).TotalDays;
                if (age >= keyRotationDays)
                {
                    RotateKey(keyId);
                }
            }
        }

        private async void RotateKey(string keyId)
        {
            try
            {
                lock (lockObject)
                {
                    // Generate new key
                    var oldKey = encryptionKeys[keyId];
                    GenerateNewKey(keyId);
                    var newKey = encryptionKeys[keyId];

                    // Re-encrypt data with new key
                    ReEncryptData(keyId, oldKey, newKey);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Key rotation failed: {e.Message}");
                throw;
            }
        }

        private async void ReEncryptData(string keyId, byte[] oldKey, byte[] newKey)
        {
            // Implementation for re-encrypting data with new key
            throw new NotImplementedException();
        }

        public class EncryptedData
        {
            public byte[] Data { get; set; }
            public string KeyId { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
} 