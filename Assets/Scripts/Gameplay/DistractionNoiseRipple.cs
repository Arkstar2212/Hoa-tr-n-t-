using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Visual sound ripple effect expanding on the ground surface when a distraction pebble impacts.
    /// Communicates hearing radius to the player with a soft amber/cyan glowing ring.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class DistractionNoiseRipple : MonoBehaviour
    {
        [SerializeField] private float maxRadius = 8.5f;
        [SerializeField] private float duration = 0.85f;
        [SerializeField] private Color rippleColor = new Color(1.0f, 0.85f, 0.35f, 0.85f); // Warm acoustic amber

        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private Material mat;
        private float timer = 0f;
        private const int Segments = 48;

        private void Awake()
        {
            mesh = new Mesh { name = "SoundRippleMesh" };
            GetComponent<MeshFilter>().mesh = mesh;
            meshRenderer = GetComponent<MeshRenderer>();

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") 
                        ?? Shader.Find("Sprites/Default") 
                        ?? Shader.Find("Standard");

            mat = new Material(unlit) { name = "Mat_DistractionRipple" };
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // Alpha blend
            meshRenderer.sharedMaterial = mat;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        public void Initialize(float radius, float lifeDuration, Color color)
        {
            maxRadius = radius;
            duration = lifeDuration;
            rippleColor = color;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Expanding radius with ease-out curve
            float currentRadius = Mathf.Lerp(0.3f, maxRadius, Mathf.Sin(progress * Mathf.PI * 0.5f));
            float ringThickness = Mathf.Lerp(0.45f, 0.15f, progress);
            float alpha = (1f - progress) * rippleColor.a;

            Color curColor = rippleColor;
            curColor.a = alpha;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", curColor);
                mat.color = curColor;
            }

            GenerateRingMesh(currentRadius, ringThickness);
        }

        private void GenerateRingMesh(float outerRadius, float thickness)
        {
            float innerRadius = Mathf.Max(0.01f, outerRadius - thickness);
            int vertCount = Segments * 2;
            Vector3[] vertices = new Vector3[vertCount];
            int[] triangles = new int[Segments * 6];

            float angleStep = 360f / Segments;
            for (int i = 0; i < Segments; i++)
            {
                float rad = i * angleStep * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                // Horizontal ring in X-Z plane (with slight Y lift 0.04m above ground)
                vertices[i * 2 + 0] = new Vector3(cos * innerRadius, 0.04f, sin * innerRadius);
                vertices[i * 2 + 1] = new Vector3(cos * outerRadius, 0.04f, sin * outerRadius);

                int next = (i + 1) % Segments;
                int triIndex = i * 6;

                triangles[triIndex + 0] = i * 2 + 0;
                triangles[triIndex + 1] = i * 2 + 1;
                triangles[triIndex + 2] = next * 2 + 0;

                triangles[triIndex + 3] = next * 2 + 0;
                triangles[triIndex + 4] = i * 2 + 1;
                triangles[triIndex + 5] = next * 2 + 1;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
            if (mesh != null) Destroy(mesh);
        }
    }
}
