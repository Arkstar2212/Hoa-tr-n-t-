using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoaTrenDatDo.Environment
{
    /// <summary>
    /// Ambient Sky Birds Spawner for "Hoa trên đất đỏ".
    /// Periodically spawns small flocks of birds (sparrows/swallows) cruising across the sky.
    /// Designed according to the "not everytime everywhere" requirement:
    /// - Sporadic, peaceful spawn intervals (20s - 50s).
    /// - Natural V-formation or staggered flock spacing.
    /// - Configurable altitude, depth, and flight corridors.
    /// </summary>
    public class AmbientSkyBirdsManager : MonoBehaviour
    {
        [Header("Bird Prefab")]
        [Tooltip("The bird entity prefab with BirdFlightEntity component.")]
        [SerializeField] private GameObject birdPrefab;

        [Header("Spawn Timing (Sporadic Sky Ambiance)")]
        [Tooltip("Minimum wait time in seconds before spawning the next flock.")]
        [SerializeField] private float minInterval = 20f;

        [Tooltip("Maximum wait time in seconds before spawning the next flock.")]
        [SerializeField] private float maxInterval = 45f;

        [Tooltip("Chance (0 to 1) that a flock actually spawns when timer fires.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float spawnChance = 0.85f;

        [Tooltip("Auto-start spawning on Start().")]
        [SerializeField] private bool autoStart = true;

        [Header("Flock Dynamics")]
        [Tooltip("Minimum number of birds in a flock.")]
        [SerializeField] private int minFlockSize = 1;

        [Tooltip("Maximum number of birds in a flock.")]
        [SerializeField] private int maxFlockSize = 3;

        [Tooltip("Average spacing between birds in a flock.")]
        [SerializeField] private Vector2 birdStaggerDistance = new Vector2(1.8f, 0.7f);

        [Header("Flight Corridor")]
        [Tooltip("Left-most boundary X where birds spawn or exit.")]
        [SerializeField] private float minBoundaryX = -30f;

        [Tooltip("Right-most boundary X where birds spawn or exit.")]
        [SerializeField] private float maxBoundaryX = 140f;

        [Tooltip("Minimum sky altitude Y.")]
        [SerializeField] private float minAltitudeY = 10f;

        [Tooltip("Maximum sky altitude Y.")]
        [SerializeField] private float maxAltitudeY = 16f;

        [Tooltip("Minimum Z depth (closer, larger scale).")]
        [SerializeField] private float minDepthZ = 16f;

        [Tooltip("Maximum Z depth (distant atmospheric haze, smaller scale).")]
        [SerializeField] private float maxDepthZ = 22f;

        [Tooltip("Flight speed range (units/sec).")]
        [SerializeField] private Vector2 speedRange = new Vector2(3.5f, 5.0f);

        [Tooltip("Scale range mapped inversely with depth.")]
        [SerializeField] private Vector2 scaleRange = new Vector2(0.5f, 0.85f);

        private Coroutine spawnLoopCoroutine;
        private Transform flockContainer;

        private void Start()
        {
            flockContainer = transform;
            if (autoStart)
            {
                spawnLoopCoroutine = StartCoroutine(FlockSpawnRoutine());
            }
        }

        private void OnDisable()
        {
            if (spawnLoopCoroutine != null)
            {
                StopCoroutine(spawnLoopCoroutine);
                spawnLoopCoroutine = null;
            }
        }

        private IEnumerator FlockSpawnRoutine()
        {
            // Initial gentle delay so scene settles first
            yield return new WaitForSeconds(Random.Range(3f, 8f));

            while (true)
            {
                if (Random.value <= spawnChance)
                {
                    SpawnFlock();
                }

                float waitTime = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(waitTime);
            }
        }

        /// <summary>
        /// Spawns a flock of birds crossing the sky. Can be triggered manually or via Inspector ContextMenu.
        /// </summary>
        [ContextMenu("Test Spawn Flock Now")]
        public void SpawnFlock()
        {
            if (birdPrefab == null)
            {
                Debug.LogWarning("[AmbientSkyBirdsManager] Bird Prefab is not assigned!");
                return;
            }

            // Decide direction: 50% left-to-right, 50% right-to-left
            bool flyRight = Random.value > 0.5f;

            float spawnX = flyRight ? minBoundaryX : maxBoundaryX;
            float exitX = flyRight ? (maxBoundaryX + 15f) : (minBoundaryX - 15f);

            // Base altitude and depth for this flock
            float baseAlt = Random.Range(minAltitudeY, maxAltitudeY);
            float depthZ = Random.Range(minDepthZ, maxDepthZ);

            // Scale inversely with depth: further away = smaller
            float depthFactor = Mathf.InverseLerp(maxDepthZ, minDepthZ, depthZ);
            float baseScale = Mathf.Lerp(scaleRange.x, scaleRange.y, depthFactor);

            // Speed: distant birds appear to move slower across sky
            float speed = Mathf.Lerp(speedRange.x, speedRange.y, depthFactor) * Random.Range(0.9f, 1.1f);

            int flockCount = Random.Range(minFlockSize, maxFlockSize + 1);

            for (int i = 0; i < flockCount; i++)
            {
                // Stagger bird positions in flock formation
                float xOffset = (flyRight ? -1f : 1f) * i * birdStaggerDistance.x + Random.Range(-0.4f, 0.4f);
                float yOffset = ((i % 2 == 0) ? 1f : -1f) * (i * 0.5f) * birdStaggerDistance.y + Random.Range(-0.3f, 0.3f);
                float zOffset = i * 0.2f;

                Vector3 birdSpawnPos = new Vector3(spawnX + xOffset, baseAlt + yOffset, depthZ + zOffset);

                GameObject birdObj = Instantiate(birdPrefab, birdSpawnPos, Quaternion.identity, flockContainer);
                birdObj.name = $"SkyBird_{i}";

                var flightEntity = birdObj.GetComponent<BirdFlightEntity>();
                if (flightEntity == null) flightEntity = birdObj.AddComponent<BirdFlightEntity>();

                float individualScale = baseScale * Random.Range(0.92f, 1.08f);
                flightEntity.Initialize(flyRight, speed, baseAlt + yOffset, exitX, depthZ + zOffset, individualScale);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw visual flight corridor in scene view
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            float width = maxBoundaryX - minBoundaryX;
            float height = maxAltitudeY - minAltitudeY;
            float depth = maxDepthZ - minDepthZ;
            Vector3 center = new Vector3((minBoundaryX + maxBoundaryX) * 0.5f, (minAltitudeY + maxAltitudeY) * 0.5f, (minDepthZ + maxDepthZ) * 0.5f);
            Vector3 size = new Vector3(width, height, depth);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
