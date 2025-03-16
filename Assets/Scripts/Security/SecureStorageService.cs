using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NairobiHustle.Security
{
    public class SecureStorageService : MonoBehaviour
    {
        [Header("Storage Configuration")]
        [SerializeField] private string encryptionKey;
        [SerializeField] private bool useCompression = true;
        [SerializeField] private bool useBackup = true;
        [SerializeField] private int backupInterval = 3600; // seconds

        private const string SECURE_STORAGE_PATH = "SecureData";
        private const string BACKUP_PATH = "Backups";
        private readonly Dictionary<string, object> memoryCache;
        private readonly object lockObject = new object();
        private DataEncryption encryption;
        private DataCompression compression;
        private BackupManager backupManager;

        private void Awake()
        {
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            try
            {
                encryption = new DataEncryption(encryptionKey);
                compression = new DataCompression();
                backupManager = new BackupManager(BACKUP_PATH, backupInterval);

                CreateSecureDirectories();
                StartCoroutine(AutoBackupRoutine());
            }
            catch (Exception e)
            {
                Debug.LogError($"Secure storage initialization failed: {e.Message}");
                throw;
            }
        }

        private void CreateSecureDirectories()
        {
            string securePath = Path.Combine(Application.persistentDataPath, SECURE_STORAGE_PATH);
            string backupPath = Path.Combine(Application.persistentDataPath, BACKUP_PATH);

            if (!Directory.Exists(securePath))
            {
                Directory.CreateDirectory(securePath);
            }

            if (useBackup && !Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }
        }

        public async Task<bool> SaveSecureData<T>(string key, T data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data);
                byte[] dataBytes = Encoding.UTF8.GetBytes(json);

                // Compress if enabled
                if (useCompression)
                {
                    dataBytes = compression.CompressData(dataBytes);
                }

                // Encrypt data
                var encryptedData = encryption.EncryptData(dataBytes);

                // Save to file
                string filePath = GetSecureFilePath(key);
                await SaveEncryptedFile(filePath, encryptedData);

                // Update cache
                UpdateCache(key, data);

                // Create backup if enabled
                if (useBackup)
                {
                    await backupManager.CreateBackup(key, encryptedData);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save secure data: {e.Message}");
                return false;
            }
        }

        public async Task<T> LoadSecureData<T>(string key)
        {
            try
            {
                // Check cache first
                if (memoryCache.TryGetValue(key, out object cachedData))
                {
                    return (T)cachedData;
                }

                string filePath = GetSecureFilePath(key);
                if (!File.Exists(filePath))
                {
                    return default(T);
                }

                // Load encrypted data
                var encryptedData = await LoadEncryptedFile(filePath);

                // Decrypt data
                byte[] decryptedData = encryption.DecryptData(encryptedData);

                // Decompress if needed
                if (useCompression)
                {
                    decryptedData = compression.DecompressData(decryptedData);
                }

                // Deserialize
                string json = Encoding.UTF8.GetString(decryptedData);
                T data = JsonConvert.DeserializeObject<T>(json);

                // Update cache
                UpdateCache(key, data);

                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load secure data: {e.Message}");
                return default(T);
            }
        }

        private string GetSecureFilePath(string key)
        {
            string hashedKey = ComputeKeyHash(key);
            return Path.Combine(
                Application.persistentDataPath,
                SECURE_STORAGE_PATH,
                $"{hashedKey}.dat"
            );
        }

        private string ComputeKeyHash(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hashBytes)
                    .Replace("/", "_")
                    .Replace("+", "-")
                    .Replace("=", "");
            }
        }

        private async Task SaveEncryptedFile(string filePath, EncryptedData data)
        {
            byte[] fileData = SerializeEncryptedData(data);
            await File.WriteAllBytesAsync(filePath, fileData);
        }

        private async Task<EncryptedData> LoadEncryptedFile(string filePath)
        {
            byte[] fileData = await File.ReadAllBytesAsync(filePath);
            return DeserializeEncryptedData(fileData);
        }

        private byte[] SerializeEncryptedData(EncryptedData data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(data.IV.Length);
                writer.Write(data.IV);
                writer.Write(data.Data.Length);
                writer.Write(data.Data);
                return ms.ToArray();
            }
        }

        private EncryptedData DeserializeEncryptedData(byte[] fileData)
        {
            using (MemoryStream ms = new MemoryStream(fileData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int ivLength = reader.ReadInt32();
                byte[] iv = reader.ReadBytes(ivLength);
                int dataLength = reader.ReadInt32();
                byte[] data = reader.ReadBytes(dataLength);

                return new EncryptedData
                {
                    IV = iv,
                    Data = data
                };
            }
        }

        private void UpdateCache(string key, object data)
        {
            lock (lockObject)
            {
                if (memoryCache.ContainsKey(key))
                {
                    memoryCache[key] = data;
                }
                else
                {
                    memoryCache.Add(key, data);
                }
            }
        }

        private class DataEncryption
        {
            private readonly byte[] key;
            private readonly int keySize = 256;

            public DataEncryption(string encryptionKey)
            {
                using (var deriveBytes = new Rfc2898DeriveBytes(
                    encryptionKey,
                    Encoding.UTF8.GetBytes("NairobiHustleSalt"),
                    10000))
                {
                    key = deriveBytes.GetBytes(keySize / 8);
                }
            }

            public EncryptedData EncryptData(byte[] data)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = keySize;
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor(key, aes.IV))
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(
                            msEncrypt,
                            encryptor,
                            CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(data, 0, data.Length);
                        }

                        return new EncryptedData
                        {
                            Data = msEncrypt.ToArray(),
                            IV = aes.IV
                        };
                    }
                }
            }

            public byte[] DecryptData(EncryptedData encryptedData)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = keySize;

                    using (var decryptor = aes.CreateDecryptor(key, encryptedData.IV))
                    using (var msDecrypt = new MemoryStream(encryptedData.Data))
                    using (var csDecrypt = new CryptoStream(
                        msDecrypt,
                        decryptor,
                        CryptoStreamMode.Read))
                    using (var resultStream = new MemoryStream())
                    {
                        csDecrypt.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }

        private class DataCompression
        {
            public byte[] CompressData(byte[] data)
            {
                using (var output = new MemoryStream())
                {
                    using (var gzip = new System.IO.Compression.GZipStream(
                        output,
                        System.IO.Compression.CompressionMode.Compress))
                    {
                        gzip.Write(data, 0, data.Length);
                    }
                    return output.ToArray();
                }
            }

            public byte[] DecompressData(byte[] compressedData)
            {
                using (var input = new MemoryStream(compressedData))
                using (var gzip = new System.IO.Compression.GZipStream(
                    input,
                    System.IO.Compression.CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
        }

        private class BackupManager
        {
            private readonly string backupPath;
            private readonly int backupInterval;
            private readonly Dictionary<string, DateTime> lastBackupTimes;

            public BackupManager(string path, int interval)
            {
                backupPath = path;
                backupInterval = interval;
                lastBackupTimes = new Dictionary<string, DateTime>();
            }

            public async Task CreateBackup(string key, EncryptedData data)
            {
                if (!ShouldCreateBackup(key))
                {
                    return;
                }

                string backupFile = GetBackupFilePath(key);
                await File.WriteAllBytesAsync(backupFile, SerializeEncryptedData(data));
                UpdateLastBackupTime(key);
            }

            private bool ShouldCreateBackup(string key)
            {
                if (!lastBackupTimes.ContainsKey(key))
                {
                    return true;
                }

                var timeSinceLastBackup = DateTime.UtcNow - lastBackupTimes[key];
                return timeSinceLastBackup.TotalSeconds >= backupInterval;
            }

            private void UpdateLastBackupTime(string key)
            {
                lastBackupTimes[key] = DateTime.UtcNow;
            }

            private string GetBackupFilePath(string key)
            {
                string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                return Path.Combine(
                    Application.persistentDataPath,
                    backupPath,
                    $"{key}_{timestamp}.bak"
                );
            }

            private byte[] SerializeEncryptedData(EncryptedData data)
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(data.IV.Length);
                    writer.Write(data.IV);
                    writer.Write(data.Data.Length);
                    writer.Write(data.Data);
                    return ms.ToArray();
                }
            }
        }

        public class EncryptedData
        {
            public byte[] Data { get; set; }
            public byte[] IV { get; set; }
        }
    }
} 