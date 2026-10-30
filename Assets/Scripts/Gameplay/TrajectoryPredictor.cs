using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Computes and renders a smooth parabolic flight arc for throwable objects.
    /// Uses LineRenderer and dynamic raycasting to show exact landing impact point.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPredictor : MonoBehaviour
    {
        [Header("Arc Configuration")]
        [SerializeField] private int maxSteps = 35;
        [SerializeField] private float stepTimeDelta = 0.045f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Styling")]
        [SerializeField] private Color arcColorStart = new Color(0.35f, 0.85f, 1.0f, 0.95f);
        [SerializeField] private Color arcColorEnd = new Color(0.15f, 0.60f, 1.0f, 0.20f);
        [SerializeField] private float lineWidthStart = 0.08f;
        [SerializeField] private float lineWidthEnd = 0.04f;

        private LineRenderer lineRend;
        private Transform landingMarker;
        private readonly List<Vector3> points = new List<Vector3>(40);

        private void Awake()
        {
            SetupLineRenderer();
            CreateLandingMarker();
            Hide();
        }

        private void SetupLineRenderer()
        {
            lineRend = GetComponent<LineRenderer>();
            lineRend.useWorldSpace = true;
            lineRend.startWidth = lineWidthStart;
            lineRend.endWidth = lineWidthEnd;

            // Simple unlit additive/alpha gradient
            lineRend.startColor = arcColorStart;
            lineRend.endColor = arcColorEnd;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                Material mat = new Material(unlitShader);
                mat.color = arcColorStart;
                lineRend.sharedMaterial = mat;
            }
            lineRend.enabled = false;
            lineRend.positionCount = 0;
        }

        private void CreateLandingMarker()
        {
            Transform existingMarker = transform.Find("Trajectory_LandingMarker");
            if (existingMarker != null)
            {
                landingMarker = existingMarker;
                landingMarker.gameObject.SetActive(false);
                return;
            }

            GameObject markerGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerGO.name = "Trajectory_LandingMarker";
            markerGO.transform.SetParent(transform);
            markerGO.transform.localScale = new Vector3(0.42f, 0.02f, 0.42f);

            var col = markerGO.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                Material mat = new Material(unlitShader);
                mat.color = new Color(0.2f, 0.8f, 1.0f, 0.8f);
                markerGO.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            landingMarker = markerGO.transform;
            landingMarker.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the simulated physics trajectory from starting position with initial velocity.
        /// </summary>
        public void RenderTrajectory(Vector3 startPos, Vector3 initialVelocity, float customGravity = -9.81f)
        {
            if (lineRend == null) return;

            lineRend.enabled = true;
            points.Clear();

            Vector3 currentPos = new Vector3(startPos.x, startPos.y, 0f);
            Vector3 currentVel = initialVelocity;
            Vector3 gravity = new Vector3(0f, customGravity, 0f);

            points.Add(currentPos);
            bool hitSurface = false;
            Vector3 impactPos = currentPos;

            for (int i = 0; i < maxSteps; i++)
            {
                Vector3 nextPos = currentPos + currentVel * stepTimeDelta + 0.5f * gravity * (stepTimeDelta * stepTimeDelta);
                currentVel += gravity * stepTimeDelta;
                nextPos.z = 0f;

                // Check collision between currentPos and nextPos
                Vector3 dir = nextPos - currentPos;
                float dist = dir.magnitude;

                if (dist > 0.001f && Physics.Raycast(currentPos, dir.normalized, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
                {
                    impactPos = hit.point;
                    impactPos.z = 0f;
                    points.Add(impactPos);
                    hitSurface = true;
                    break;
                }

                points.Add(nextPos);
                currentPos = nextPos;
            }

            lineRend.positionCount = points.Count;
            lineRend.SetPositions(points.ToArray());

            if (landingMarker != null)
            {
                landingMarker.gameObject.SetActive(hitSurface);
                if (hitSurface)
                {
                    landingMarker.position = impactPos + Vector3.up * 0.02f;
                }
            }
        }

        public void Hide()
        {
            if (lineRend != null)
            {
                lineRend.positionCount = 0;
                lineRend.enabled = false;
            }
            if (landingMarker != null)
            {
                landingMarker.gameObject.SetActive(false);
            }
        }
    }
}
