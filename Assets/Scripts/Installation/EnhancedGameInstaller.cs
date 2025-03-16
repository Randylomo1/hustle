using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace NairobiHustle.Installation
{
    public class EnhancedGameInstaller : MonoBehaviour
    {
        [Header("Installation UI")]
        [SerializeField] private GameObject installationPanel;
        [SerializeField] private Slider mainProgressBar;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button continueButton;

        [Header("Performance Settings")]
        [SerializeField] private bool useAsyncLoading = true;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool useGPUInstancing = true;
        [SerializeField] private bool useDynamicBatching = true;
        [SerializeField] private bool useCompression = true;

        [Header("Installation Settings")]
        [SerializeField] private string gameVersion = "1.0.0";
        [SerializeField] private string[] requiredScenes;
        [SerializeField] private float timeoutDuration = 30f;
        [SerializeField] private int maxRetryAttempts = 3;

        private InstallationState currentState;
        private int currentRetryCount;
        private bool isRecoveryMode;
        private HttpClient httpClient;
        private System.Diagnostics.Stopwatch installTimer;

        private enum InstallationState
        {
            NotStarted,
            CheckingRequirements,
            CreatingDirectories,
            InitializingServices,
            LoadingScenes,
            FinalizingSetup,
            Complete,
            Failed
        }

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Physics.autoSimulation = false;
            
            if (useGPUInstancing)
                Graphics.activeTier = GraphicsTier.Tier2;

            httpClient = new HttpClient();
            installTimer = new System.Diagnostics.Stopwatch();
            
            SetupUI();
        }

        private void SetupUI()
        {
            retryButton.onClick.AddListener(RetryInstallation);
            skipButton.onClick.AddListener(SkipCurrentStep);
            continueButton.onClick.AddListener(ContinueToGame);
            
            retryButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
            errorText.gameObject.SetActive(false);
        }

        private void Start()
        {
            StartCoroutine(InstallGameWithTimeout());
        }

        private IEnumerator InstallGameWithTimeout()
        {
            installTimer.Start();
            var installationCoroutine = StartCoroutine(InstallGame());
            
            while (!installationCoroutine.isDone)
            {
                if (installTimer.ElapsedMilliseconds > timeoutDuration * 1000)
                {
                    HandleTimeout();
                    yield break;
                }
                yield return null;
            }
        }

        private IEnumerator InstallGame()
        {
            try
            {
                // System Requirements Check
                currentState = InstallationState.CheckingRequirements;
                UpdateStatus("Checking system requirements...", 0.1f);
                
                if (!await Task.Run(() => CheckSystemRequirements()))
                {
                    throw new Exception("System requirements not met");
                }
                yield return new WaitForSeconds(0.5f);

                // Create Directories
                currentState = InstallationState.CreatingDirectories;
                UpdateStatus("Creating game directories...", 0.2f);
                
                if (!await Task.Run(() => CreateGameDirectories()))
                {
                    throw new Exception("Failed to create game directories");
                }
                yield return new WaitForSeconds(0.5f);

                // Initialize Services
                currentState = InstallationState.InitializingServices;
                UpdateStatus("Initializing game services...", 0.4f);
                yield return StartCoroutine(InitializeServicesWithRetry());

                // Load Scenes
                currentState = InstallationState.LoadingScenes;
                UpdateStatus("Loading game scenes...", 0.6f);
                yield return StartCoroutine(LoadRequiredScenesAsync());

                // Final Setup
                currentState = InstallationState.FinalizingSetup;
                UpdateStatus("Finalizing installation...", 0.8f);
                yield return StartCoroutine(FinalSetup());

                // Complete
                currentState = InstallationState.Complete;
                UpdateStatus("Installation complete!", 1.0f);
                OnInstallationComplete();
            }
            catch (Exception e)
            {
                HandleError($"Installation failed: {e.Message}");
            }
        }

        private async Task<bool> CheckSystemRequirements()
        {
            try
            {
                // CPU Check
                bool cpuCheck = SystemInfo.processorCount >= 2 &&
                              SystemInfo.processorFrequency >= 2000;

                // RAM Check
                bool ramCheck = SystemInfo.systemMemorySize >= 4096;

                // GPU Check
                bool gpuCheck = SystemInfo.graphicsMemorySize >= 1024 &&
                              SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null &&
                              SystemInfo.graphicsShaderLevel >= 35;

                // Storage Check
                DriveInfo drive = new DriveInfo(Application.dataPath);
                bool storageCheck = drive.AvailableFreeSpace > 2L * 1024L * 1024L * 1024L;

                // Network Check
                bool networkCheck = await CheckNetworkConnection();

                return cpuCheck && ramCheck && gpuCheck && storageCheck && networkCheck;
            }
            catch (Exception e)
            {
                Debug.LogError($"System requirements check failed: {e.Message}");
                return false;
            }
        }

        private async Task<bool> CheckNetworkConnection()
        {
            try
            {
                var response = await httpClient.GetAsync("https://api.safaricom.co.ke/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private bool CreateGameDirectories()
        {
            try
            {
                string baseDir = Application.persistentDataPath;
                string[] directories = new[]
                {
                    "Saves",
                    "Screenshots",
                    "Cache",
                    "Logs",
                    "Config",
                    "Assets",
                    "Temp"
                };

                foreach (string dir in directories)
                {
                    string path = Path.Combine(baseDir, dir);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create directories: {e.Message}");
                return false;
            }
        }

        private IEnumerator InitializeServicesWithRetry()
        {
            currentRetryCount = 0;
            bool servicesInitialized = false;

            while (!servicesInitialized && currentRetryCount < maxRetryAttempts)
            {
                try
                {
                    yield return StartCoroutine(InitializeServices());
                    servicesInitialized = true;
                }
                catch (Exception e)
                {
                    currentRetryCount++;
                    Debug.LogWarning($"Service initialization attempt {currentRetryCount} failed: {e.Message}");
                    
                    if (currentRetryCount >= maxRetryAttempts)
                    {
                        throw new Exception("Service initialization failed after maximum retry attempts");
                    }
                    
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        private IEnumerator InitializeServices()
        {
            // Initialize core services
            yield return StartCoroutine(InitializeNetworkServices());
            yield return StartCoroutine(InitializeStorageServices());
            yield return StartCoroutine(InitializeGameplayServices());
            yield return StartCoroutine(InitializeAudioServices());
        }

        private IEnumerator LoadRequiredScenesAsync()
        {
            int totalScenes = requiredScenes.Length;
            int loadedScenes = 0;

            foreach (string sceneName in requiredScenes)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                asyncLoad.allowSceneActivation = false;

                while (asyncLoad.progress < 0.9f)
                {
                    float progress = (loadedScenes + asyncLoad.progress) / totalScenes;
                    UpdateStatus($"Loading {sceneName}...", 0.6f + (0.2f * progress));
                    yield return null;
                }

                asyncLoad.allowSceneActivation = true;
                loadedScenes++;
            }
        }

        private IEnumerator FinalSetup()
        {
            try
            {
                // Save installation info
                PlayerPrefs.SetString("GameVersion", gameVersion);
                PlayerPrefs.SetString("InstallDate", DateTime.Now.ToString("o"));
                PlayerPrefs.SetInt("FirstTimeSetup", 1);
                PlayerPrefs.Save();

                // Create default configuration
                CreateDefaultConfig();

                // Initialize player profile
                yield return StartCoroutine(InitializePlayerProfile());

                // Optimize performance settings
                OptimizePerformanceSettings();
            }
            catch (Exception e)
            {
                throw new Exception($"Final setup failed: {e.Message}");
            }
        }

        private void CreateDefaultConfig()
        {
            var config = new
            {
                graphics = new
                {
                    quality = QualitySettings.GetQualityLevel(),
                    vsync = QualitySettings.vSyncCount,
                    antiAliasing = QualitySettings.antiAliasing,
                    shadows = QualitySettings.shadows
                },
                audio = new
                {
                    masterVolume = AudioListener.volume,
                    musicVolume = 0.8f,
                    sfxVolume = 1.0f,
                    voiceChatVolume = 0.7f
                },
                gameplay = new
                {
                    difficulty = "Normal",
                    tutorialEnabled = true,
                    autoSave = true,
                    language = "en"
                }
            };

            string configPath = Path.Combine(Application.persistentDataPath, "Config", "settings.json");
            File.WriteAllText(configPath, JsonUtility.ToJson(config, true));
        }

        private void OptimizePerformanceSettings()
        {
            // Graphics optimization
            QualitySettings.shadowDistance = 50f;
            QualitySettings.lodBias = 1.5f;
            QualitySettings.masterTextureLimit = 1;
            QualitySettings.particleRaycastBudget = 1024;

            // Memory optimization
            Application.backgroundLoadingPriority = ThreadPriority.High;
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private void HandleError(string errorMessage)
        {
            Debug.LogError(errorMessage);
            currentState = InstallationState.Failed;
            
            errorText.text = errorMessage;
            errorText.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(true);
            
            if (IsSkippableState(currentState))
            {
                skipButton.gameObject.SetActive(true);
            }
        }

        private void HandleTimeout()
        {
            HandleError("Installation timed out");
        }

        private bool IsSkippableState(InstallationState state)
        {
            return state == InstallationState.InitializingServices ||
                   state == InstallationState.LoadingScenes;
        }

        private void RetryInstallation()
        {
            errorText.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            
            isRecoveryMode = true;
            StartCoroutine(InstallGameWithTimeout());
        }

        private void SkipCurrentStep()
        {
            Debug.LogWarning($"Skipping installation step: {currentState}");
            errorText.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            
            // Move to next state
            currentState++;
            StartCoroutine(InstallGameWithTimeout());
        }

        private void ContinueToGame()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void OnInstallationComplete()
        {
            installTimer.Stop();
            continueButton.gameObject.SetActive(true);
            
            // Log installation metrics
            Debug.Log($"Installation completed in {installTimer.ElapsedMilliseconds / 1000f} seconds");
        }

        private void UpdateStatus(string status, float progress)
        {
            if (statusText != null)
                statusText.text = status;
            
            if (mainProgressBar != null)
                mainProgressBar.value = progress;
            
            Debug.Log($"Installation Status: {status} ({progress:P0})");
        }

        private void OnDestroy()
        {
            httpClient?.Dispose();
            installTimer?.Stop();
        }
    }
} 