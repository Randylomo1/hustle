using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

namespace NairobiHustle.Installation
{
    public class GameLauncher : MonoBehaviour
    {
        [Header("Launch Settings")]
        [SerializeField] private GameObject splashScreen;
        [SerializeField] private float splashDuration = 3f;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string installationScene = "Installation";

        [Header("Dependencies")]
        [SerializeField] private string[] requiredFiles;
        [SerializeField] private string[] requiredDirectories;

        private void Start()
        {
            StartCoroutine(LaunchGame());
        }

        private IEnumerator LaunchGame()
        {
            // Show splash screen
            if (splashScreen != null)
            {
                splashScreen.SetActive(true);
                yield return new WaitForSeconds(splashDuration);
            }

            // Check if game is installed
            if (!IsGameInstalled())
            {
                Debug.Log("Game not installed. Starting installation...");
                SceneManager.LoadScene(installationScene);
                yield break;
            }

            // Verify game files
            if (!VerifyGameFiles())
            {
                Debug.LogError("Game files verification failed!");
                // TODO: Show error message to user
                yield break;
            }

            // Initialize services
            yield return StartCoroutine(InitializeServices());

            // Load main menu
            SceneManager.LoadScene(mainMenuScene);
        }

        private bool IsGameInstalled()
        {
            // Check if first-time setup was completed
            if (!PlayerPrefs.HasKey("FirstTimeSetup"))
                return false;

            // Check if version matches
            string installedVersion = PlayerPrefs.GetString("GameVersion", "");
            if (string.IsNullOrEmpty(installedVersion))
                return false;

            // Check if player profile exists
            string profilePath = Path.Combine(Application.persistentDataPath, "Saves", "player_profile.json");
            if (!File.Exists(profilePath))
                return false;

            return true;
        }

        private bool VerifyGameFiles()
        {
            // Check required directories
            foreach (string dir in requiredDirectories)
            {
                string fullPath = Path.Combine(Application.persistentDataPath, dir);
                if (!Directory.Exists(fullPath))
                {
                    Debug.LogError($"Required directory not found: {dir}");
                    return false;
                }
            }

            // Check required files
            foreach (string file in requiredFiles)
            {
                string fullPath = Path.Combine(Application.persistentDataPath, file);
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"Required file not found: {file}");
                    return false;
                }
            }

            return true;
        }

        private IEnumerator InitializeServices()
        {
            // Initialize network connection
            yield return StartCoroutine(InitializeNetwork());

            // Initialize M-Pesa service
            yield return StartCoroutine(InitializeMPesa());

            // Initialize other services
            yield return StartCoroutine(InitializeGameServices());
        }

        private IEnumerator InitializeNetwork()
        {
            // Check internet connection
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("No internet connection detected!");
                // TODO: Show warning to user
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator InitializeMPesa()
        {
            // Find M-Pesa service
            var mpesaService = FindObjectOfType<EnhancedMPesaService>();
            if (mpesaService != null)
            {
                mpesaService.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Debug.LogWarning("M-Pesa service not found!");
            }
        }

        private IEnumerator InitializeGameServices()
        {
            // Initialize business system
            var businessSystem = FindObjectOfType<AdvancedBusinessSystem>();
            if (businessSystem != null)
            {
                businessSystem.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }

            // Initialize communication system
            var communicationSystem = FindObjectOfType<LiveCommunicationSystem>();
            if (communicationSystem != null)
            {
                communicationSystem.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnApplicationQuit()
        {
            // Save any necessary data before quitting
            PlayerPrefs.Save();
        }
    }
} 