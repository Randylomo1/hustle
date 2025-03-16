using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NairobiHustle.GameSystems
{
    public class SceneManager : MonoBehaviour
    {
        public static SceneManager Instance { get; private set; }

        [Header("Scene Events")]
        public UnityEvent<string> OnSceneLoaded = new UnityEvent<string>();
        public UnityEvent<string> OnSceneUnloaded = new UnityEvent<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private System.Collections.IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            OnSceneLoaded.Invoke(sceneName);
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}