using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace NairobiHustle.Installation
{
    public class GameInstaller : MonoBehaviour
    {
        [Header("Installation Settings")]
        [SerializeField] private string gameVersion = "1.0.0";
        [SerializeField] private bool enableAutoUpdates = true;
        [SerializeField] private bool enableCloudSave = true;
        [SerializeField] private string[] requiredPermissions;

        [Header("Hardware Requirements")]
        [SerializeField] private int minRAMGB = 4;
        [SerializeField] private int minStorageGB = 10;
        [SerializeField] private string minGPU = "DirectX 11";
        [SerializeField] private string minCPU = "Intel Core i3 or equivalent";

        [Header("Licensing")]
        [SerializeField] private bool requireActivation = true;
        [SerializeField] private int maxInstallations = 2;
        [SerializeField] private float trialPeriodDays = 7;

        [Header("Service Initialization")]
        [SerializeField] private EnhancedMPesaService mpesaService;
        [SerializeField] private AdvancedBusinessSystem businessSystem;
        [SerializeField] private LiveCommunicationSystem communicationSystem;

        private InstallationManager installManager;
        private LicenseManager licenseManager;
        private UpdateManager updateManager;
        private HardwareValidator hardwareValidator;
        private SecurityManager securityManager;

        private void Awake()
        {
            InitializeInstaller();
        }

        private void InitializeInstaller()
        {
            try
            {
                installManager = new InstallationManager();
                licenseManager = new LicenseManager(maxInstallations, trialPeriodDays);
                updateManager = new UpdateManager(enableAutoUpdates);
                hardwareValidator = new HardwareValidator(minRAMGB, minStorageGB, minGPU, minCPU);
                securityManager = GetComponent<SecurityManager>();

                StartCoroutine(InstallationCheckRoutine());
            }
            catch (Exception e)
            {
                Debug.LogError($"Installer initialization failed: {e.Message}");
                throw;
            }
        }

        private IEnumerator InstallationCheckRoutine()
        {
            // Show loading screen
            loadingScreen.SetActive(true);
            UpdateStatus("Starting installation...", 0.1f);
            yield return new WaitForSeconds(1f);

            // Check system requirements
            if (!CheckSystemRequirements())
            {
                Debug.LogError("System requirements not met!");
                UpdateStatus("System requirements not met!", 0f);
                yield break;
            }
            UpdateStatus("System requirements verified", 0.2f);
            yield return new WaitForSeconds(0.5f);

            // Create necessary directories
            CreateGameDirectories();
            UpdateStatus("Game directories created", 0.3f);
            yield return new WaitForSeconds(0.5f);

            // Initialize services
            yield return StartCoroutine(InitializeServices());
            UpdateStatus("Game services initialized", 0.6f);
            yield return new WaitForSeconds(0.5f);

            // Load required scenes
            yield return StartCoroutine(LoadRequiredScenes());
            UpdateStatus("Game scenes loaded", 0.8f);
            yield return new WaitForSeconds(0.5f);

            // Final setup
            yield return StartCoroutine(FinalSetup());
            UpdateStatus("Installation complete!", 1f);
            yield return new WaitForSeconds(1f);

            // Hide loading screen and start game
            loadingScreen.SetActive(false);
            SceneManager.LoadScene("MainMenu");
        }

        private bool CheckSystemRequirements()
        {
            // Check minimum specs
            bool meetsRequirements = true;

            // Check CPU
            meetsRequirements &= SystemInfo.processorCount >= 2;
            
            // Check RAM
            meetsRequirements &= SystemInfo.systemMemorySize >= 4096; // 4GB minimum

            // Check Graphics
            meetsRequirements &= SystemInfo.graphicsMemorySize >= 1024; // 1GB minimum
            meetsRequirements &= SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

            // Check storage
            DriveInfo drive = new DriveInfo(Application.dataPath);
            meetsRequirements &= drive.AvailableFreeSpace > 2L * 1024L * 1024L * 1024L; // 2GB minimum

            return meetsRequirements;
        }

        private void CreateGameDirectories()
        {
            string baseDir = Application.persistentDataPath;
            
            // Create necessary directories
            Directory.CreateDirectory(Path.Combine(baseDir, "Saves"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Screenshots"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Cache"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Logs"));
        }

        private IEnumerator InitializeServices()
        {
            // Initialize M-Pesa service
            if (mpesaService != null)
            {
                mpesaService.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }

            // Initialize business system
            if (businessSystem != null)
            {
                businessSystem.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }

            // Initialize communication system
            if (communicationSystem != null)
            {
                communicationSystem.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator LoadRequiredScenes()
        {
            foreach (string sceneName in requiredScenes)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (!asyncLoad.isDone)
                {
                    UpdateStatus($"Loading {sceneName}...", 0.8f * asyncLoad.progress);
                    yield return null;
                }
            }
        }

        private IEnumerator FinalSetup()
        {
            // Save installation info
            PlayerPrefs.SetString("GameVersion", gameVersion);
            PlayerPrefs.SetString("InstallDate", System.DateTime.Now.ToString());
            PlayerPrefs.Save();

            // Initialize player data
            if (!PlayerPrefs.HasKey("FirstTimeSetup"))
            {
                // Create new player profile
                CreateNewPlayerProfile();
                PlayerPrefs.SetInt("FirstTimeSetup", 1);
                PlayerPrefs.Save();
            }

            yield return new WaitForSeconds(0.5f);
        }

        private void CreateNewPlayerProfile()
        {
            // Create default player data
            var playerData = new
            {
                playerId = System.Guid.NewGuid().ToString(),
                creationDate = System.DateTime.Now,
                startingBalance = 10000f,
                tutorial_completed = false
            };

            // Save to file
            string json = JsonUtility.ToJson(playerData);
            File.WriteAllText(
                Path.Combine(Application.persistentDataPath, "Saves", "player_profile.json"),
                json
            );
        }

        private void UpdateStatus(string status, float progress)
        {
            if (statusText != null)
                statusText.text = status;
            
            if (progressBar != null)
                progressBar.value = progress;
            
            Debug.Log($"Installation Status: {status} ({progress:P0})");
        }
    }
} 