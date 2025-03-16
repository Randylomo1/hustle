using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace NairobiHustle.Environment
{
    public enum VehicleAIType
    {
        Car,
        Bus,
        Truck,
        Motorcycle,
        TukTuk,
        Emergency
    }

    [System.Serializable]
    public class TrafficVehicleData
    {
        public VehicleAIType type;
        public GameObject prefab;
        public float maxSpeed = 50f;
        public float acceleration = 10f;
        public float braking = 15f;
        public float turnSpeed = 100f;
        public bool canUseBusLane;
        public bool isEmergencyVehicle;
        public AudioClip engineSound;
        public AudioClip hornSound;
    }

    public class TrafficAI : MonoBehaviour
    {
        [Header("Traffic Settings")]
        public TrafficVehicleData[] vehicleTypes;
        public int maxActiveVehicles = 100;
        public float spawnRadius = 200f;
        public float despawnRadius = 300f;
        public float minSpawnDistance = 50f;
        public LayerMask roadLayer;
        public LayerMask obstacleLayer;

        [Header("Traffic Rules")]
        public float speedLimit = 50f;
        public float minFollowDistance = 10f;
        public float intersectionSlowdownDistance = 20f;
        public float intersectionStopDistance = 5f;
        public float laneWidth = 4f;
        public float sidewalkWidth = 2f;

        [Header("AI Behavior")]
        public float reactionTime = 0.5f;
        public float visionRange = 50f;
        public float hornProbability = 0.1f;
        public float laneChangeProbability = 0.2f;
        public AnimationCurve speedByTrafficDensity;

        private List<GameObject> activeVehicles;
        private NavMeshPath navMeshPath;
        private Dictionary<VehicleAIType, Queue<GameObject>> vehiclePool;
        private Transform player;
        private Camera mainCamera;

        private void Awake()
        {
            activeVehicles = new List<GameObject>();
            vehiclePool = new Dictionary<VehicleAIType, Queue<GameObject>>();
            navMeshPath = new NavMeshPath();
            player = GameObject.FindGameObjectWithTag("Player").transform;
            mainCamera = Camera.main;

            InitializeVehiclePool();
        }

        private void InitializeVehiclePool()
        {
            foreach (var vehicleType in vehicleTypes)
            {
                vehiclePool[vehicleType.type] = new Queue<GameObject>();
                // Pre-instantiate some vehicles
                for (int i = 0; i < maxActiveVehicles / vehicleTypes.Length; i++)
                {
                    GameObject vehicle = Instantiate(vehicleType.prefab);
                    SetupVehicle(vehicle, vehicleType);
                    vehicle.SetActive(false);
                    vehiclePool[vehicleType.type].Enqueue(vehicle);
                }
            }
        }

        private void SetupVehicle(GameObject vehicle, TrafficVehicleData data)
        {
            // Add necessary components
            if (!vehicle.GetComponent<NavMeshAgent>())
            {
                NavMeshAgent agent = vehicle.AddComponent<NavMeshAgent>();
                agent.speed = data.maxSpeed;
                agent.acceleration = data.acceleration;
                agent.angularSpeed = data.turnSpeed;
                agent.radius = 1f;
                agent.height = 2f;
            }

            // Add audio
            if (!vehicle.GetComponent<AudioSource>())
            {
                AudioSource audioSource = vehicle.AddComponent<AudioSource>();
                audioSource.clip = data.engineSound;
                audioSource.loop = true;
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 50f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            // Add AI controller component
            VehicleAIController aiController = vehicle.GetComponent<VehicleAIController>();
            if (!aiController)
            {
                aiController = vehicle.AddComponent<VehicleAIController>();
            }
            aiController.Initialize(data);
        }

        private void Update()
        {
            ManageTraffic();
            UpdateActiveVehicles();
        }

        private void ManageTraffic()
        {
            // Remove out-of-range vehicles
            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                GameObject vehicle = activeVehicles[i];
                if (Vector3.Distance(vehicle.transform.position, player.position) > despawnRadius)
                {
                    ReturnVehicleToPool(vehicle);
                    activeVehicles.RemoveAt(i);
                }
            }

            // Spawn new vehicles if needed
            if (activeVehicles.Count < maxActiveVehicles)
            {
                SpawnVehicle();
            }
        }

        private void SpawnVehicle()
        {
            Vector3 spawnPoint = GetValidSpawnPoint();
            if (spawnPoint != Vector3.zero)
            {
                // Choose random vehicle type
                VehicleAIType type = vehicleTypes[Random.Range(0, vehicleTypes.Length)].type;
                
                if (vehiclePool[type].Count > 0)
                {
                    GameObject vehicle = vehiclePool[type].Dequeue();
                    vehicle.transform.position = spawnPoint;
                    vehicle.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    vehicle.SetActive(true);

                    // Set destination
                    NavMeshAgent agent = vehicle.GetComponent<NavMeshAgent>();
                    Vector3 destination = GetRandomDestination();
                    agent.SetDestination(destination);

                    activeVehicles.Add(vehicle);
                }
            }
        }

        private Vector3 GetValidSpawnPoint()
        {
            for (int i = 0; i < 10; i++) // Try 10 times to find valid spawn point
            {
                float angle = Random.Range(0f, 360f);
                float distance = Random.Range(minSpawnDistance, spawnRadius);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
                Vector3 point = player.position + offset;

                // Check if point is on road
                if (Physics.Raycast(point + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, roadLayer))
                {
                    // Check if no obstacles nearby
                    if (!Physics.CheckSphere(hit.point + Vector3.up, 2f, obstacleLayer))
                    {
                        return hit.point + Vector3.up;
                    }
                }
            }
            return Vector3.zero;
        }

        private Vector3 GetRandomDestination()
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(100f, 300f);
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            return player.position + offset;
        }

        private void ReturnVehicleToPool(GameObject vehicle)
        {
            VehicleAIController controller = vehicle.GetComponent<VehicleAIController>();
            vehicle.SetActive(false);
            vehiclePool[controller.vehicleType].Enqueue(vehicle);
        }

        private void UpdateActiveVehicles()
        {
            float trafficDensity = (float)activeVehicles.Count / maxActiveVehicles;
            float currentSpeedLimit = speedLimit * speedByTrafficDensity.Evaluate(trafficDensity);

            foreach (GameObject vehicle in activeVehicles)
            {
                VehicleAIController controller = vehicle.GetComponent<VehicleAIController>();
                controller.UpdateBehavior(currentSpeedLimit, trafficDensity);
            }
        }
    }

    public class VehicleAIController : MonoBehaviour
    {
        public VehicleAIType vehicleType { get; private set; }
        private NavMeshAgent agent;
        private AudioSource audioSource;
        private TrafficVehicleData data;
        private float currentSpeed;
        private float targetSpeed;
        private bool isHonking;
        private float lastHornTime;

        public void Initialize(TrafficVehicleData vehicleData)
        {
            data = vehicleData;
            vehicleType = data.type;
            agent = GetComponent<NavMeshAgent>();
            audioSource = GetComponent<AudioSource>();
        }

        public void UpdateBehavior(float speedLimit, float trafficDensity)
        {
            // Update speed based on conditions
            targetSpeed = Mathf.Min(data.maxSpeed, speedLimit);
            
            // Check for obstacles
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 20f))
            {
                float distanceToObstacle = hit.distance;
                targetSpeed *= Mathf.Clamp01(distanceToObstacle / 20f);
            }

            // Update actual speed
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, data.acceleration * Time.deltaTime);
            agent.speed = currentSpeed;

            // Random honking in traffic
            if (!isHonking && trafficDensity > 0.7f && Time.time > lastHornTime + 10f)
            {
                if (Random.value < 0.1f)
                {
                    StartHonking();
                }
            }

            // Update engine sound
            if (audioSource != null && data.engineSound != null)
            {
                audioSource.pitch = Mathf.Lerp(0.8f, 1.2f, currentSpeed / data.maxSpeed);
                audioSource.volume = Mathf.Lerp(0.2f, 1f, currentSpeed / data.maxSpeed);
            }
        }

        private void StartHonking()
        {
            if (data.hornSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(data.hornSound);
                isHonking = true;
                lastHornTime = Time.time;
                Invoke(nameof(StopHonking), 0.5f);
            }
        }

        private void StopHonking()
        {
            isHonking = false;
        }
    }
} 