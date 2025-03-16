using UnityEngine;
using System.Collections.Generic;

namespace NairobiHustle.Environment
{
    [System.Serializable]
    public class LandmarkData
    {
        public string name;
        public string description;
        public GameObject model;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public Material[] materials;
        public ParticleSystem[] effects;
        public AudioClip ambientSound;
        public float detailDistance = 1000f;
        public bool isInteractive;
    }

    public class LandmarkController : MonoBehaviour
    {
        [Header("Nairobi Landmarks")]
        public LandmarkData[] nairobiLandmarks = new LandmarkData[]
        {
            new LandmarkData 
            { 
                name = "KICC",
                description = "Kenyatta International Convention Centre",
                detailDistance = 2000f,
                isInteractive = true
            },
            new LandmarkData 
            { 
                name = "Nairobi National Park",
                description = "Wildlife sanctuary with city backdrop",
                detailDistance = 3000f,
                isInteractive = true
            },
            new LandmarkData 
            { 
                name = "Two Rivers Mall",
                description = "Largest mall in East Africa",
                detailDistance = 1500f,
                isInteractive = true
            },
            new LandmarkData 
            { 
                name = "Railway Museum",
                description = "Historical railway artifacts",
                detailDistance = 800f,
                isInteractive = true
            }
        };

        [Header("Mombasa Landmarks")]
        public LandmarkData[] mombasaLandmarks = new LandmarkData[]
        {
            new LandmarkData 
            { 
                name = "Fort Jesus",
                description = "16th century Portuguese fort",
                detailDistance = 1500f,
                isInteractive = true
            },
            new LandmarkData 
            { 
                name = "Old Town",
                description = "Historical district with Arabic architecture",
                detailDistance = 1000f,
                isInteractive = true
            },
            new LandmarkData 
            { 
                name = "Mombasa Marine Park",
                description = "Protected marine sanctuary",
                detailDistance = 2000f,
                isInteractive = true
            },
            new LandmarkData 
            { 
                name = "Haller Park",
                description = "Nature park with diverse wildlife",
                detailDistance = 1200f,
                isInteractive = true
            }
        };

        [Header("Visual Settings")]
        public float lodDistance = 500f;
        public Material highlightMaterial;
        public float highlightIntensity = 1.2f;
        public ParticleSystem highlightEffect;

        private Dictionary<string, GameObject> activeLandmarks;
        private Camera mainCamera;

        private void Awake()
        {
            activeLandmarks = new Dictionary<string, GameObject>();
            mainCamera = Camera.main;
            InitializeLandmarks();
        }

        private void InitializeLandmarks()
        {
            // Initialize Nairobi landmarks
            foreach (var landmark in nairobiLandmarks)
            {
                if (landmark.model != null)
                {
                    GameObject instance = Instantiate(landmark.model, landmark.position, Quaternion.Euler(landmark.rotation));
                    instance.transform.localScale = landmark.scale;
                    SetupLandmark(instance, landmark);
                    activeLandmarks.Add(landmark.name, instance);
                }
            }

            // Initialize Mombasa landmarks
            foreach (var landmark in mombasaLandmarks)
            {
                if (landmark.model != null)
                {
                    GameObject instance = Instantiate(landmark.model, landmark.position, Quaternion.Euler(landmark.rotation));
                    instance.transform.localScale = landmark.scale;
                    SetupLandmark(instance, landmark);
                    activeLandmarks.Add(landmark.name, instance);
                }
            }
        }

        private void SetupLandmark(GameObject instance, LandmarkData data)
        {
            // Add collider for interaction
            if (data.isInteractive && instance.GetComponent<Collider>() == null)
            {
                instance.AddComponent<BoxCollider>();
            }

            // Setup LOD system
            LODGroup lodGroup = instance.AddComponent<LODGroup>();
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            
            LOD[] lods = new LOD[3];
            lods[0] = new LOD(0.6f, renderers); // High detail
            lods[1] = new LOD(0.3f, renderers); // Medium detail
            lods[2] = new LOD(0.1f, renderers); // Low detail
            lodGroup.SetLODs(lods);
            lodGroup.size = data.detailDistance;

            // Apply materials
            if (data.materials != null && data.materials.Length > 0)
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.materials = data.materials;
                }
            }

            // Setup effects
            if (data.effects != null)
            {
                foreach (ParticleSystem effect in data.effects)
                {
                    Instantiate(effect, instance.transform);
                }
            }

            // Setup audio
            if (data.ambientSound != null)
            {
                AudioSource audioSource = instance.AddComponent<AudioSource>();
                audioSource.clip = data.ambientSound;
                audioSource.loop = true;
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = data.detailDistance;
                audioSource.Play();
            }
        }

        private void Update()
        {
            UpdateLandmarkVisibility();
            HandleLandmarkInteractions();
        }

        private void UpdateLandmarkVisibility()
        {
            foreach (var landmark in activeLandmarks)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, landmark.Value.transform.position);
                bool isVisible = distance <= lodDistance;
                landmark.Value.SetActive(isVisible);
            }
        }

        private void HandleLandmarkInteractions()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    GameObject hitObject = hit.transform.gameObject;
                    foreach (var landmark in activeLandmarks)
                    {
                        if (hitObject == landmark.Value)
                        {
                            HighlightLandmark(landmark.Value);
                            ShowLandmarkInfo(landmark.Key);
                            break;
                        }
                    }
                }
            }
        }

        private void HighlightLandmark(GameObject landmark)
        {
            Renderer[] renderers = landmark.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = highlightMaterial;
                    materials[i].SetFloat("_Intensity", highlightIntensity);
                }
                renderer.materials = materials;
            }

            if (highlightEffect != null)
            {
                ParticleSystem effect = Instantiate(highlightEffect, landmark.transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }
        }

        private void ShowLandmarkInfo(string landmarkName)
        {
            LandmarkData data = System.Array.Find(nairobiLandmarks, l => l.name == landmarkName);
            if (data == null)
            {
                data = System.Array.Find(mombasaLandmarks, l => l.name == landmarkName);
            }

            if (data != null)
            {
                // Trigger UI event to show landmark information
                Debug.Log($"Landmark: {data.name}\nDescription: {data.description}");
                // In production, this would update the UI instead of using Debug.Log
            }
        }

        public void SetCityActive(CityType city)
        {
            foreach (var landmark in nairobiLandmarks)
            {
                if (activeLandmarks.ContainsKey(landmark.name))
                {
                    activeLandmarks[landmark.name].SetActive(city == CityType.Nairobi);
                }
            }

            foreach (var landmark in mombasaLandmarks)
            {
                if (activeLandmarks.ContainsKey(landmark.name))
                {
                    activeLandmarks[landmark.name].SetActive(city == CityType.Mombasa);
                }
            }
        }
    }
} 