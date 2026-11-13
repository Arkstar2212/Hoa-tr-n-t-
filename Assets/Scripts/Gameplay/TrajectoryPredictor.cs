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

        [Header("Elastic Rubber Band (Sợi cao su nhiều đốt)")]
        [SerializeField] private int rubberSegmentCount = 12;
        [SerializeField] private Color rubberColor = new Color(0.85f, 0.42f, 0.22f, 0.95f);
        [SerializeField] private float rubberWidth = 0.042f;

        private LineRenderer lineRend;
        private LineRenderer rubberRend;
        private Transform landingMarker;
        private readonly List<Vector3> points = new List<Vector3>(40);

        private void Awake()
        {
            SetupLineRenderer();
            SetupRubberRenderer();
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

        private void SetupRubberRenderer()
        {
            Transform existing = transform.Find("Trajectory_RubberBand");
            if (existing != null) rubberRend = existing.GetComponent<LineRenderer>();
            else
            {
                GameObject rubberGO = new GameObject("Trajectory_RubberBand");
                rubberGO.transform.SetParent(transform, false);
                rubberRend = rubberGO.AddComponent<LineRenderer>();
            }

            rubberRend.useWorldSpace = true;
            rubberRend.startWidth = rubberWidth;
            rubberRend.endWidth = rubberWidth * 0.78f;
            rubberRend.numCapVertices = 4;
            rubberRend.numCornerVertices = 4;
            rubberRend.startColor = rubberColor;
            rubberRend.endColor = rubberColor;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                Material mat = new Material(unlitShader);
                mat.color = rubberColor;
                rubberRend.sharedMaterial = mat;
            }
            rubberRend.enabled = false;
        }

        /// <summary>
        /// Mô phỏng sợi dây cao su nhiều đốt đàn hồi co giãn nối giữa điểm tì tay và viên đá ném.
        /// </summary>
        public void RenderRubberBand(Vector3 anchorPos, Vector3 stonePos, float powerRatio)
        {
            if (rubberRend == null) SetupRubberRenderer();
            if (rubberRend == null) return;

            rubberRend.enabled = true;
            Vector3[] bandPoints = new Vector3[rubberSegmentCount];
            float tension = Mathf.Clamp01(powerRatio);
            float vibration = Mathf.Sin(Time.time * 26f) * (0.016f * tension);

            Vector3 dir = stonePos - anchorPos;
            Vector3 normal = Vector3.Cross(dir.normalized, Vector3.forward);

            for (int i = 0; i < rubberSegmentCount; i++)
            {
                float t = (float)i / (rubberSegmentCount - 1);
                Vector3 p = Vector3.Lerp(anchorPos, stonePos, t);
                // Catenary sag mềm mại & dao động đàn hồi tự nhiên như sợi cao su thật
                float sag = Mathf.Sin(t * Mathf.PI) * (0.038f * (1f - tension * 0.75f) + vibration);
                p += normal * sag;
                p.z = 0f;
                bandPoints[i] = p;
            }

            rubberRend.positionCount = rubberSegmentCount;
            rubberRend.SetPositions(bandPoints);
        }

        public void Hide()
        {
            if (lineRend != null)
            {
                lineRend.positionCount = 0;
                lineRend.enabled = false;
            }
            if (rubberRend != null)
            {
                rubberRend.positionCount = 0;
                rubberRend.enabled = false;
            }
            if (landingMarker != null)
            {
                landingMarker.gameObject.SetActive(false);
            }
        }
    }
}
