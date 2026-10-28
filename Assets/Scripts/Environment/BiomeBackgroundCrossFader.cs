using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau
{
    /// <summary>
    /// Dynamic Biome Background Cross-Fader for "Hoa trên đất đỏ".
    /// Solves the core transition problem:
    /// Seamlessly cross-fades the dense forest backdrop as the player exits the forest
    /// (X: 30m -> 44m) to reveal the infinite blue sky backdrop, and vice versa.
    /// Operates in both Edit Mode ([ExecuteAlways]) and Play Mode for real-time visual feedback.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class BiomeBackgroundCrossFader : MonoBehaviour
    {
        [Header("Tracking Target")]
        [Tooltip("Camera or Player transform to track for zone transitions. Auto-finds Camera.main if null.")]
        [SerializeField] private Transform trackTarget;
        [SerializeField] private bool autoFindCamera = true;

        [Header("Zone Bounds (X Position)")]
        [Tooltip("Start of the forest zone where backdrop is fully visible (Alpha = 1.0)")]
        [SerializeField] private float forestStartX = -18.0f;

        [Tooltip("Point where the dense forest starts fading out toward the open overlook & rice fields")]
        [SerializeField] private float forestFullEndX = 30.0f;

        [Tooltip("Point where the forest backdrop is completely transparent (Alpha = 0.0), revealing open blue sky")]
        [SerializeField] private float forestFadeEndX = 43.5f;

        [Header("Alpha Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float maxForestAlpha = 1.0f;
        [Range(0f, 1f)]
        [SerializeField] private float outdoorSkyAlpha = 0.0f;
        [SerializeField] private float fadeSmoothSpeed = 4.0f;

        [Header("Controlled Renderers")]
        [Tooltip("All SpriteRenderers representing the forest backdrop to fade")]
        [SerializeField] private List<SpriteRenderer> targetSpriteRenderers = new List<SpriteRenderer>();

        [Tooltip("Optional MeshRenderers with materials using _Color or _BaseColor")]
        [SerializeField] private List<MeshRenderer> targetMeshRenderers = new List<MeshRenderer>();

        [Header("Atmospheric Color Modulation")]
        [SerializeField] private bool modulateAtmosphericColor = true;
        [SerializeField] private Color deepForestTint = new Color(0.78f, 0.92f, 0.82f, 1.0f);
        [SerializeField] private Color openSkyTint = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        [Header("Live Debug Status")]
        [SerializeField, Range(0f, 1f)] private float currentCalculatedAlpha = 1.0f;
        [SerializeField] private float currentTargetX = 0f;

        public float CurrentAlpha => currentCalculatedAlpha;

        private void OnEnable()
        {
            ResolveReferences();
            UpdateFade(100f); // Instant update on enable
        }

        private void Start()
        {
            ResolveReferences();
            UpdateFade(100f);
        }

        private void LateUpdate()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.033f;
            UpdateFade(dt);
        }

        public void ResolveReferences()
        {
            if (autoFindCamera && trackTarget == null)
            {
                Camera cam = Camera.main;
                if (cam != null) trackTarget = cam.transform;
            }

            // Auto-collect child sprite renderers if list is empty
            if (targetSpriteRenderers == null || targetSpriteRenderers.Count == 0)
            {
                targetSpriteRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>(true));
            }

            if (targetMeshRenderers == null || targetMeshRenderers.Count == 0)
            {
                targetMeshRenderers = new List<MeshRenderer>(GetComponentsInChildren<MeshRenderer>(true));
            }
        }

        public void AddRenderer(SpriteRenderer sr)
        {
            if (sr == null) return;
            if (targetSpriteRenderers == null) targetSpriteRenderers = new List<SpriteRenderer>();
            if (!targetSpriteRenderers.Contains(sr)) targetSpriteRenderers.Add(sr);
        }

        public void AddMeshRenderer(MeshRenderer mr)
        {
            if (mr == null) return;
            if (targetMeshRenderers == null) targetMeshRenderers = new List<MeshRenderer>();
            if (!targetMeshRenderers.Contains(mr)) targetMeshRenderers.Add(mr);
        }

        private void UpdateFade(float dt)
        {
            if (trackTarget == null)
            {
                if (autoFindCamera && Camera.main != null)
                {
                    trackTarget = Camera.main.transform;
                }
                else return;
            }

            currentTargetX = trackTarget.position.x;

            // Compute target alpha based on X position
            float targetAlpha = maxForestAlpha;

            if (currentTargetX <= forestFullEndX)
            {
                // Inside forest zone
                if (currentTargetX < forestStartX)
                {
                    // Soft fade at the extreme left entry if needed
                    float leftFade = Mathf.Clamp01((currentTargetX - (forestStartX - 8.0f)) / 8.0f);
                    targetAlpha = Mathf.Lerp(0f, maxForestAlpha, leftFade);
                }
                else
                {
                    targetAlpha = maxForestAlpha;
                }
            }
            else if (currentTargetX >= forestFadeEndX)
            {
                // Fully outside in open meadow/rice field with blue sky
                targetAlpha = outdoorSkyAlpha;
            }
            else
            {
                // Smooth cosine/smoothstep blend between forest and open sky
                float blendProgress = (currentTargetX - forestFullEndX) / Mathf.Max(0.1f, forestFadeEndX - forestFullEndX);
                float smoothT = Mathf.SmoothStep(0f, 1f, blendProgress);
                targetAlpha = Mathf.Lerp(maxForestAlpha, outdoorSkyAlpha, smoothT);
            }

            // Smooth interpolation to prevent visual jitter
            float lerpSpeed = Application.isPlaying ? fadeSmoothSpeed : 25f;
            currentCalculatedAlpha = Mathf.Lerp(currentCalculatedAlpha, targetAlpha, Mathf.Clamp01(dt * lerpSpeed));

            ApplyAlphaToRenderers(currentCalculatedAlpha);
        }

        private void ApplyAlphaToRenderers(float alpha)
        {
            Color tint = modulateAtmosphericColor
                ? Color.Lerp(deepForestTint, openSkyTint, 1f - alpha)
                : Color.white;

            // 1. Update SpriteRenderers
            if (targetSpriteRenderers != null)
            {
                for (int i = targetSpriteRenderers.Count - 1; i >= 0; i--)
                {
                    var sr = targetSpriteRenderers[i];
                    if (sr == null)
                    {
                        targetSpriteRenderers.RemoveAt(i);
                        continue;
                    }

                    Color c = sr.color;
                    c.a = alpha;
                    c.r = tint.r;
                    c.g = tint.g;
                    c.b = tint.b;
                    sr.color = c;
                }
            }

            // 2. Update MeshRenderers
            if (targetMeshRenderers != null)
            {
                for (int i = targetMeshRenderers.Count - 1; i >= 0; i--)
                {
                    var mr = targetMeshRenderers[i];
                    if (mr == null)
                    {
                        targetMeshRenderers.RemoveAt(i);
                        continue;
                    }

                    var mat = Application.isPlaying ? mr.material : mr.sharedMaterial;
                    if (mat != null)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.GetColor("_Color");
                            c.a = alpha;
                            mat.SetColor("_Color", c);
                        }
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            c.a = alpha;
                            mat.SetColor("_BaseColor", c);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.35f);
            Gizmos.DrawLine(new Vector3(forestFullEndX, -10f, 0f), new Vector3(forestFullEndX, 30f, 0f));

            Gizmos.color = new Color(0.2f, 0.6f, 1.0f, 0.35f);
            Gizmos.DrawLine(new Vector3(forestFadeEndX, -10f, 0f), new Vector3(forestFadeEndX, 30f, 0f));
        }
#endif
    }
}
