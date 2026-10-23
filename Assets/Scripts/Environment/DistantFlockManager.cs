using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Spawns and coordinates an atmospheric flock of distant seagulls flying across the sky.
    /// Supports V-formation, staggered clusters, distance atmospheric tint, and Editor preview.
    /// </summary>
    [ExecuteAlways]
    public class DistantFlockManager : MonoBehaviour
    {
        [Header("Seagull Template & Sprites")]
        [Tooltip("The 6 flight cycle frames (0: High V to 5: Recovery)")]
        [SerializeField] private Sprite[] flightFrames;
        [Tooltip("Custom material or shader for sprites (optional)")]
        [SerializeField] private Material spriteMaterial;

        [Header("Flock Configuration")]
        [Tooltip("Number of birds in the flock")]
        [Range(1, 20)]
        [SerializeField] private int birdCount = 5;
        [Tooltip("Formation style")]
        [SerializeField] private FormationType formation = FormationType.StaggeredCluster;
        [Tooltip("Spacing between birds")]
        [SerializeField] private Vector2 spacing = new Vector2(1.8f, 0.6f);

        [Header("Depth & Perspective")]
        [Tooltip("Base visual scale of the seagulls")]
        [SerializeField] private float baseScale = 0.25f;
        [Tooltip("Silhouette color / atmospheric tint (e.g. warm sunset charcoal or sky haze)")]
        [SerializeField] private Color flockTint = new Color(0.12f, 0.12f, 0.14f, 0.85f);

        [Header("Flight Dynamics")]
        [Tooltip("Overall flock flight speed")]
        [SerializeField] private float baseFlightSpeed = 1.6f;
        [Tooltip("1 = Left-to-Right, -1 = Right-to-Left")]
        [SerializeField] private float flightDirection = 1f;
        [Tooltip("Horizon boundaries")]
        [SerializeField] private float minX = -30f;
        [SerializeField] private float maxX = 30f;

        public enum FormationType
        {
            VFormation,
            StaggeredCluster,
            SingleLine
        }

        [SerializeField] [HideInInspector]
        private List<GameObject> spawnedBirds = new List<GameObject>();

        private void OnEnable()
        {
            AutoLoadFramesIfEmpty();
            if (spawnedBirds == null || spawnedBirds.Count == 0 || spawnedBirds.Exists(b => b == null))
            {
                RebuildFlock();
            }
        }

        private void AutoLoadFramesIfEmpty()
        {
#if UNITY_EDITOR
            if (flightFrames == null || flightFrames.Length == 0)
            {
                var loaded = new Sprite[6];
                bool allValid = true;
                for (int i = 0; i < 6; i++)
                {
                    loaded[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/Environment/Seagulls/Seagull_Frame_{i}.png");
                    if (loaded[i] == null) allValid = false;
                }
                if (allValid) flightFrames = loaded;
            }
#endif
        }

        [ContextMenu("Rebuild Flock")]
        public void RebuildFlock()
        {
            ClearExistingBirds();
            AutoLoadFramesIfEmpty();

            if (flightFrames == null || flightFrames.Length == 0) return;

            spawnedBirds = new List<GameObject>();

            for (int i = 0; i < birdCount; i++)
            {
                Vector3 offset = CalculateFormationOffset(i, birdCount);
                GameObject birdObj = new GameObject($"Seagull_{i}");
                birdObj.transform.SetParent(transform, false);
                birdObj.transform.localPosition = offset;

                var sr = birdObj.AddComponent<SpriteRenderer>();
                sr.sprite = flightFrames[2]; // start with glide pose
                sr.color = flockTint;
                if (spriteMaterial != null) sr.material = spriteMaterial;
                sr.sortingOrder = -5; // Behind main foreground / gameplay

                var seagull = birdObj.AddComponent<DistantSeagull>();
                seagull.SetFlightFrames(flightFrames, 2);

                // Add subtle organic variation
                float scaleVar = Random.Range(0.85f, 1.15f) * baseScale;
                float speedVar = Random.Range(0.9f, 1.1f);
                seagull.ConfigureDepth(scaleVar, flockTint, speedVar);

                spawnedBirds.Add(birdObj);
            }
        }

        private Vector3 CalculateFormationOffset(int index, int total)
        {
            switch (formation)
            {
                case FormationType.VFormation:
                    int side = (index % 2 == 1) ? 1 : -1;
                    int rank = (index + 1) / 2;
                    return new Vector3(-rank * spacing.x, -rank * spacing.y * 0.7f, 0f);

                case FormationType.SingleLine:
                    return new Vector3(-index * spacing.x, index * spacing.y * 0.2f, 0f);

                case FormationType.StaggeredCluster:
                default:
                    float ox = -index * spacing.x + Mathf.Sin(index * 1.5f) * (spacing.x * 0.4f);
                    float oy = Mathf.Cos(index * 2.2f) * spacing.y;
                    return new Vector3(ox, oy, 0f);
            }
        }

        private void ClearExistingBirds()
        {
            if (spawnedBirds != null)
            {
                foreach (var b in spawnedBirds)
                {
                    if (b != null)
                    {
                        if (Application.isPlaying) Destroy(b);
                        else DestroyImmediate(b);
                    }
                }
                spawnedBirds.Clear();
            }

            // Also cleanup any orphan children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void OnValidate()
        {
            if (gameObject.activeInHierarchy && !Application.isPlaying)
            {
                // Can quickly update tint or rebuild
            }
        }
    }
}
