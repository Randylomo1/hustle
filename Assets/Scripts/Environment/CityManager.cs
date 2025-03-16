using UnityEngine;
using System.Collections.Generic;

namespace NairobiHustle.Environment
{
    public enum CityType
    {
        Nairobi,
        Mombasa
    }

    public class CityManager : MonoBehaviour
    {
        [Header("City Settings")]
        public CityType currentCity;
        public bool enableDynamicTimeOfDay = true;
        public bool enableWeatherSystem = true;

        [Header("Nairobi Landmarks")]
        public Transform nairobiCBD;
        public Transform kenyattaAvenue;
        public Transform uhuroPark;
        public Transform westlands;
        public Transform kiambu;
        public Transform jkia;
        public Transform upperHill;
        public Transform kilimani;

        [Header("Mombasa Landmarks")]
        public Transform oldTown;
        public Transform fortJesus;
        public Transform nyaliBridge;
        public Transform mombasaPort;
        public Transform cityMall;
        public Transform bamburi;
        public Transform shanzu;
        public Transform dianiBeach;

        [Header("Environment")]
        public Light sunLight;
        public Material skyboxDay;
        public Material skyboxNight;
        public ParticleSystem rain;
        public WindZone windZone;
        
        [Header("Traffic System")]
        public GameObject[] vehiclePrefabs;
        public GameObject[] pedestrianPrefabs;
        public float trafficDensity = 0.7f;
        public float pedestrianDensity = 0.5f;
        public int maxActiveVehicles = 100;
        public int maxActivePedestrians = 200;

        [Header("Visual Effects")]
        public Material[] buildingMaterials;
        public Material[] roadMaterials;
        public GameObject[] vegetationPrefabs;
        public ParticleSystem[] atmosphericEffects;

        private float timeOfDay = 8f; // 24-hour format
        private float weatherIntensity;
        private List<GameObject> activeTraffic;
        private List<GameObject> activePedestrians;

        private void Awake()
        {
            activeTraffic = new List<GameObject>();
            activePedestrians = new List<GameObject>();
            InitializeCity(currentCity);
        }

        private void Update()
        {
            if (enableDynamicTimeOfDay)
            {
                UpdateTimeOfDay();
            }

            if (enableWeatherSystem)
            {
                UpdateWeather();
            }

            ManageTraffic();
            ManagePedestrians();
        }

        public void InitializeCity(CityType city)
        {
            currentCity = city;
            switch (city)
            {
                case CityType.Nairobi:
                    SetupNairobiEnvironment();
                    break;
                case CityType.Mombasa:
                    SetupMombasaEnvironment();
                    break;
            }
        }

        private void SetupNairobiEnvironment()
        {
            // Configure Nairobi-specific environment
            RenderSettings.ambientIntensity = 1.2f;
            RenderSettings.fogColor = new Color(0.75f, 0.75f, 0.75f);
            RenderSettings.fogDensity = 0.01f;

            // Setup landmarks visibility
            nairobiCBD.gameObject.SetActive(true);
            kenyattaAvenue.gameObject.SetActive(true);
            uhuroPark.gameObject.SetActive(true);
            // ... activate other Nairobi landmarks

            // Deactivate Mombasa landmarks
            oldTown.gameObject.SetActive(false);
            fortJesus.gameObject.SetActive(false);
            // ... deactivate other Mombasa landmarks

            // Set appropriate traffic rules and patterns
            trafficDensity = 0.8f; // Higher traffic in Nairobi
            pedestrianDensity = 0.7f;
        }

        private void SetupMombasaEnvironment()
        {
            // Configure Mombasa-specific environment
            RenderSettings.ambientIntensity = 1.4f;
            RenderSettings.fogColor = new Color(0.8f, 0.8f, 0.9f);
            RenderSettings.fogDensity = 0.015f;

            // Setup landmarks visibility
            oldTown.gameObject.SetActive(true);
            fortJesus.gameObject.SetActive(true);
            nyaliBridge.gameObject.SetActive(true);
            // ... activate other Mombasa landmarks

            // Deactivate Nairobi landmarks
            nairobiCBD.gameObject.SetActive(false);
            kenyattaAvenue.gameObject.SetActive(false);
            // ... deactivate other Nairobi landmarks

            // Set appropriate traffic rules and patterns
            trafficDensity = 0.6f; // Moderate traffic in Mombasa
            pedestrianDensity = 0.8f;
        }

        private void UpdateTimeOfDay()
        {
            timeOfDay += Time.deltaTime * 0.01f; // Full day cycle in 100 real minutes
            if (timeOfDay >= 24f) timeOfDay = 0f;

            // Update sun position
            float sunRotation = (timeOfDay - 6) * 15f; // 15 degrees per hour, starting at 6AM
            sunLight.transform.rotation = Quaternion.Euler(sunRotation, 180f, 0f);

            // Update lighting and atmosphere
            float dayIntensity = Mathf.Clamp01(Mathf.Sin((timeOfDay - 6) / 24f * Mathf.PI));
            sunLight.intensity = dayIntensity * 1.2f;

            // Switch skybox based on time
            if (timeOfDay > 6 && timeOfDay < 18)
            {
                RenderSettings.skybox = skyboxDay;
            }
            else
            {
                RenderSettings.skybox = skyboxNight;
            }
        }

        private void UpdateWeather()
        {
            // Simple weather system - can be expanded
            weatherIntensity = Mathf.PingPong(Time.time * 0.02f, 1f);
            
            if (weatherIntensity > 0.7f)
            {
                rain.Play();
                windZone.windMain = weatherIntensity * 2f;
            }
            else
            {
                rain.Stop();
                windZone.windMain = weatherIntensity;
            }
        }

        private void ManageTraffic()
        {
            // Remove distant vehicles
            activeTraffic.RemoveAll(vehicle => 
                vehicle == null || 
                Vector3.Distance(vehicle.transform.position, Camera.main.transform.position) > 500f);

            // Spawn new vehicles if needed
            if (activeTraffic.Count < maxActiveVehicles * trafficDensity)
            {
                SpawnVehicle();
            }
        }

        private void ManagePedestrians()
        {
            // Remove distant pedestrians
            activePedestrians.RemoveAll(pedestrian => 
                pedestrian == null || 
                Vector3.Distance(pedestrian.transform.position, Camera.main.transform.position) > 200f);

            // Spawn new pedestrians if needed
            if (activePedestrians.Count < maxActivePedestrians * pedestrianDensity)
            {
                SpawnPedestrian();
            }
        }

        private void SpawnVehicle()
        {
            // Implement vehicle spawning logic
            Vector3 spawnPoint = GetRandomSpawnPoint();
            int randomIndex = Random.Range(0, vehiclePrefabs.Length);
            GameObject vehicle = Instantiate(vehiclePrefabs[randomIndex], spawnPoint, Quaternion.identity);
            activeTraffic.Add(vehicle);
        }

        private void SpawnPedestrian()
        {
            // Implement pedestrian spawning logic
            Vector3 spawnPoint = GetRandomSidewalkPoint();
            int randomIndex = Random.Range(0, pedestrianPrefabs.Length);
            GameObject pedestrian = Instantiate(pedestrianPrefabs[randomIndex], spawnPoint, Quaternion.identity);
            activePedestrians.Add(pedestrian);
        }

        private Vector3 GetRandomSpawnPoint()
        {
            // Implement logic to get valid vehicle spawn points
            return Vector3.zero; // Placeholder
        }

        private Vector3 GetRandomSidewalkPoint()
        {
            // Implement logic to get valid pedestrian spawn points
            return Vector3.zero; // Placeholder
        }
    }
} 