using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Procedurally generates a 2D/2.5D visual cone mesh that reflects line of sight and occludes against obstacles.
    /// Features smooth volumetric radial & edge fade using the custom VisionConeFade shader.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionConeMesh : MonoBehaviour
    {
        [Header("Cone Configuration")]
        public float viewDistance = 7.5f;
        public float viewAngle = 55f;
        public int rayCount = 32; // Higher ray count for smooth curved perimeter
        public LayerMask obstacleLayer = ~0;

        private Mesh mesh;
        private MeshRenderer meshRenderer;

        private Vector3[] vertices;
        private int[] triangles;
        private Vector2[] uvs;
        private Color32[] colors;

        private void Awake()
        {
            EnsureMesh();
        }

        private void EnsureMesh()
        {
            if (mesh == null)
            {
                mesh = new Mesh { name = "VisionConeMesh" };
                GetComponent<MeshFilter>().mesh = mesh;
            }
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (meshRenderer != null)
            {
                Shader fadeShader = Shader.Find("HoaVaMau/VisionConeFade");
                if (fadeShader != null)
                {
                    if (meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.shader != fadeShader)
                    {
                        Material mat = new Material(fadeShader) { name = "Mat_VisionConeFade" };
                        Color initColor = new Color(0.28f, 0.80f, 0.95f, 0.45f);
                        mat.SetColor("_BaseColor", initColor);
                        meshRenderer.sharedMaterial = mat;
                    }
                }
            }
        }

        public void SetColor(Color col)
        {
            EnsureMesh();
            if (meshRenderer != null && meshRenderer.material != null)
            {
                if (meshRenderer.material.HasProperty("_BaseColor"))
                {
                    meshRenderer.material.SetColor("_BaseColor", col);
                }
                else
                {
                    meshRenderer.material.color = col;
                }
            }
        }

        public void UpdateCone(float facingDir)
        {
            Vector3 forward = (facingDir > 0) ? Vector3.right : Vector3.left;
            UpdateConeDirection(forward, Vector3.up);
        }

        public void UpdateConeDirection(Vector3 forwardDir, Vector3 lateralAxis)
        {
            EnsureMesh();
            if (mesh == null) return;

            forwardDir = forwardDir.normalized;
            lateralAxis = lateralAxis.normalized;

            float halfAngle = viewAngle * 0.5f;
            float angleStep = viewAngle / rayCount;

            int totalVertices = rayCount + 2;
            if (vertices == null || vertices.Length != totalVertices)
            {
                vertices = new Vector3[totalVertices];
                triangles = new int[rayCount * 3];
                uvs = new Vector2[totalVertices];
                colors = new Color32[totalVertices];
            }

            // Origin at enemy eye (brightest point)
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0f); // y = 0 is origin
            colors[0] = new Color32(255, 255, 255, 255);

            int vertexIndex = 1;
            int triangleIndex = 0;

            for (int i = 0; i <= rayCount; i++)
            {
                float currentAngleDeg = -halfAngle + (i * angleStep);
                float rad = currentAngleDeg * Mathf.Deg2Rad;

                // Fan out around the forward vector using the lateral axis
                Vector3 rayDir = (forwardDir * Mathf.Cos(rad) + lateralAxis * Mathf.Sin(rad)).normalized;

                Vector3 worldOrigin = transform.position;
                Vector3 localEndpoint;
                float distFraction = 1f;

                if (Physics.Raycast(worldOrigin, rayDir, out RaycastHit hit, viewDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
                {
                    localEndpoint = transform.InverseTransformPoint(hit.point);
                    distFraction = Mathf.Clamp01(hit.distance / viewDistance);
                }
                else
                {
                    localEndpoint = transform.InverseTransformPoint(worldOrigin + rayDir * viewDistance);
                    distFraction = 1f;
                }

                vertices[vertexIndex] = localEndpoint;

                // uv.x represents lateral angle (0 to 1), uv.y represents distance fraction from eye (0 to 1)
                uvs[vertexIndex] = new Vector2((float)i / rayCount, distFraction);

                // Vertex colors also fade to 0 at perimeter for maximum compatibility
                byte alphaByte = (byte)Mathf.RoundToInt(Mathf.Clamp01(1f - distFraction) * 255f);
                colors[vertexIndex] = new Color32(255, 255, 255, alphaByte);

                if (i > 0)
                {
                    triangles[triangleIndex + 0] = 0;
                    triangles[triangleIndex + 1] = vertexIndex - 1;
                    triangles[triangleIndex + 2] = vertexIndex;
                    triangleIndex += 3;
                }

                vertexIndex++;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.RecalculateNormals();
        }
    }
}
