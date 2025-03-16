using UnityEngine;
using System.Collections.Generic;

namespace NairobiHustle.Vehicles
{
    [System.Serializable]
    public class MatatuCustomization
    {
        public string saccoName;
        public Sprite saccoLogo;
        public Material[] customPaintwork;
        public string[] slogans;
        public GameObject[] neonLights;
        public GameObject[] speakers;
        public AudioClip[] customHorns;
        public GameObject[] decorations;
        public AnimationClip[] doorAnimations;
    }

    [System.Serializable]
    public class MatatuRoute
    {
        public string routeNumber;
        public string startPoint;
        public string endPoint;
        public Transform[] waypoints;
        public float basePrice = 50f;
        public float peakHourMultiplier = 1.5f;
    }

    public class MatatuSystem : MonoBehaviour
    {
        [Header("Matatu Models")]
        public GameObject[] matatuPrefabs; // Different types (14-seater, 33-seater, etc.)
        
        [Header("Customization")]
        public MatatuCustomization[] saccoStyles = new MatatuCustomization[]
        {
            new MatatuCustomization 
            { 
                saccoName = "Super Metro",
                slogans = new string[] { "Speed and Comfort", "City Life" }
            },
            new MatatuCustomization 
            { 
                saccoName = "Forward Travelers",
                slogans = new string[] { "Moving Forward", "Express Service" }
            },
            new MatatuCustomization 
            { 
                saccoName = "KBS",
                slogans = new string[] { "The Original", "Heritage Service" }
            }
        };

        [Header("Routes")]
        public MatatuRoute[] nairobiRoutes = new MatatuRoute[]
        {
            new MatatuRoute 
            { 
                routeNumber = "111",
                startPoint = "CBD",
                endPoint = "Ngong Road"
            },
            new MatatuRoute 
            { 
                routeNumber = "125",
                startPoint = "CBD",
                endPoint = "Karen"
            },
            new MatatuRoute 
            { 
                routeNumber = "237",
                startPoint = "CBD",
                endPoint = "Westlands"
            }
        };

        [Header("Audio")]
        public AudioClip[] kenyanMusic;
        public AudioClip[] calloutPhrases;
        public float maxMusicVolume = 0.8f;
        public float calloutVolume = 1f;

        [Header("Effects")]
        public ParticleSystem exhaustSmoke;
        public ParticleSystem brakeDust;
        public TrailRenderer[] neonTrails;
        public Light[] spotLights;

        private List<GameObject> activeMatatus;
        private Dictionary<string, AudioSource> matatuAudio;

        private void Awake()
        {
            activeMatatus = new List<GameObject>();
            matatuAudio = new Dictionary<string, AudioSource>();
            InitializeMatatus();
        }

        private void InitializeMatatus()
        {
            foreach (var prefab in matatuPrefabs)
            {
                // Create matatu instances for different routes
                foreach (var route in nairobiRoutes)
                {
                    GameObject matatu = CreateMatatu(prefab, route);
                    activeMatatus.Add(matatu);
                }
            }
        }

        private GameObject CreateMatatu(GameObject prefab, MatatuRoute route)
        {
            GameObject matatu = Instantiate(prefab);
            
            // Apply random SACCO style
            MatatuCustomization style = saccoStyles[Random.Range(0, saccoStyles.Length)];
            ApplyCustomization(matatu, style);

            // Setup audio system
            SetupAudioSystem(matatu);

            // Setup route
            SetupRoute(matatu, route);

            return matatu;
        }

        private void ApplyCustomization(GameObject matatu, MatatuCustomization style)
        {
            // Apply SACCO branding
            foreach (Renderer renderer in matatu.GetComponentsInChildren<Renderer>())
            {
                if (style.customPaintwork != null && style.customPaintwork.Length > 0)
                {
                    renderer.materials = style.customPaintwork;
                }
            }

            // Add decorations
            if (style.decorations != null)
            {
                foreach (GameObject decoration in style.decorations)
                {
                    Instantiate(decoration, matatu.transform);
                }
            }

            // Setup lighting
            if (style.neonLights != null)
            {
                foreach (GameObject light in style.neonLights)
                {
                    Instantiate(light, matatu.transform);
                }
            }

            // Add speakers
            if (style.speakers != null)
            {
                foreach (GameObject speaker in style.speakers)
                {
                    Instantiate(speaker, matatu.transform);
                }
            }

            // Apply random slogan
            if (style.slogans != null && style.slogans.Length > 0)
            {
                string slogan = style.slogans[Random.Range(0, style.slogans.Length)];
                // Apply slogan to matatu texture/material
            }
        }

        private void SetupAudioSystem(GameObject matatu)
        {
            // Main music system
            AudioSource musicSource = matatu.AddComponent<AudioSource>();
            musicSource.spatialBlend = 1f; // Full 3D audio
            musicSource.maxDistance = 50f;
            musicSource.rolloffMode = AudioRolloffMode.Custom;
            musicSource.SetCustomCurve(
                AudioSourceCurveType.CustomRolloff,
                AnimationCurve.EaseInOut(0f, 1f, 50f, 0f)
            );

            // Callout system for conductor
            AudioSource calloutSource = matatu.AddComponent<AudioSource>();
            calloutSource.spatialBlend = 1f;
            calloutSource.maxDistance = 30f;

            // Horn system
            AudioSource hornSource = matatu.AddComponent<AudioSource>();
            hornSource.spatialBlend = 1f;
            hornSource.maxDistance = 100f;

            string matatuId = matatu.GetInstanceID().ToString();
            matatuAudio[matatuId + "_music"] = musicSource;
            matatuAudio[matatuId + "_callout"] = calloutSource;
            matatuAudio[matatuId + "_horn"] = hornSource;

            // Start playing random music
            PlayRandomMusic(musicSource);
        }

        private void PlayRandomMusic(AudioSource source)
        {
            if (kenyanMusic != null && kenyanMusic.Length > 0)
            {
                source.clip = kenyanMusic[Random.Range(0, kenyanMusic.Length)];
                source.volume = maxMusicVolume;
                source.loop = true;
                source.Play();
            }
        }

        private void SetupRoute(GameObject matatu, MatatuRoute route)
        {
            MatatuController controller = matatu.AddComponent<MatatuController>();
            controller.Initialize(route);

            // Position at route start
            if (route.waypoints != null && route.waypoints.Length > 0)
            {
                matatu.transform.position = route.waypoints[0].position;
            }
        }

        private void Update()
        {
            UpdateMatatus();
        }

        private void UpdateMatatus()
        {
            foreach (GameObject matatu in activeMatatus)
            {
                MatatuController controller = matatu.GetComponent<MatatuController>();
                if (controller != null)
                {
                    controller.UpdateBehavior();
                }
            }
        }
    }

    public class MatatuController : MonoBehaviour
    {
        private MatatuRoute route;
        private int currentWaypoint;
        private float speed = 40f;
        private bool isStopped;
        private float waitTime;

        public void Initialize(MatatuRoute assignedRoute)
        {
            route = assignedRoute;
            currentWaypoint = 0;
            waitTime = 0f;
        }

        public void UpdateBehavior()
        {
            if (route.waypoints == null || route.waypoints.Length == 0) return;

            if (isStopped)
            {
                waitTime -= Time.deltaTime;
                if (waitTime <= 0f)
                {
                    isStopped = false;
                }
                return;
            }

            // Move to next waypoint
            Transform target = route.waypoints[currentWaypoint];
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);

            // Check if waypoint reached
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < 5f)
            {
                // Stop at waypoint
                isStopped = true;
                waitTime = Random.Range(5f, 15f); // Random wait time at stops

                // Move to next waypoint
                currentWaypoint = (currentWaypoint + 1) % route.waypoints.Length;
            }
        }
    }
} 