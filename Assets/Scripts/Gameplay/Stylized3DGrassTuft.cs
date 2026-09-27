using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoaVaMau.Gameplay
{
    public enum TuftMeshStyle
    {
        CrossCards,    // 2 intersecting cards (45° and -45°)
        StarCards,     // 3 intersecting cards (0°, 60°, 120°) - 360° volumetric view
        CurvedFan,     // 3 arched cards with Z-depth offset - rich parallax
        ClusteredPatch // Scattered multi-card cluster for lush meadow patches
    }

    public enum TuftTextureVariant
    {
        Upright,
        Leaning
    }

    /// <summary>
    /// Transforms 2D painterly grass artwork into volumetric 3D grass using MeshRenderer.
    /// Features:
    /// - Multi-plane cross-card & curved fan volumetric topologies
    /// - 5-segment vertical subdivision for organic wind sway curvature
    /// - Upward-biased painterly normals for soft Ghibli / Planet of Lana lighting
    /// - Integrated with URP Stylized3DGrassTuft shader & GrassManager
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Stylized3DGrassTuft : MonoBehaviour
    {
        [Header("Volumetric Mesh Topology")]
        [SerializeField] private TuftMeshStyle meshStyle = TuftMeshStyle.StarCards;
        [SerializeField] private TuftTextureVariant textureVariant = TuftTextureVariant.Upright;

        [Header("Dimensions (Meters)")]
        [Tooltip("Horizontal width of each grass card.")]
        [Range(0.2f, 2.5f)] public float cardWidth = 0.85f;

        [Tooltip("Vertical height of the grass clump.")]
        [Range(0.2f, 2.0f)] public float cardHeight = 0.60f;

        [Tooltip("Volumetric depth spread along Z axis.")]
        [Range(0.0f, 0.8f)] public float depthSpread = 0.25f;

        [Tooltip("Curvature arching of the blades along the card normal.")]
        [Range(0.0f, 0.4f)] public float bladeCurvature = 0.12f;

        [Header("Clustered Patch Settings (for ClusteredPatch style)")]
        [Range(2, 10)] public int clusterTuftCount = 4;
        [Range(0.5f, 3.0f)] public float patchRadius = 1.2f;

        [Header("Mesh Subdivision")]
        [Tooltip("Vertical segments for smooth wind sway curvature (minimum 3).")]
        [Range(3, 8)] public int heightSegments = 5;

        [Header("Material")]
        [SerializeField] private Material customMaterial = null;

        public TuftMeshStyle Style => meshStyle;

        private void Awake()
        {
            EnsureMeshAndMaterial();
        }

        private void Start()
        {
            EnsureMeshAndMaterial();
        }

        private void OnEnable()
        {
            EnsureMeshAndMaterial();
        }

        private void OnValidate()
        {
            BuildTuftMesh();
            EnsureMaterial();
        }

        public void Setup(TuftMeshStyle style, TuftTextureVariant variant, float width, float height)
        {
            meshStyle = style;
            textureVariant = variant;
            cardWidth = width;
            cardHeight = height;
            BuildTuftMesh();
            EnsureMaterial();
        }

        public void EnsureMeshAndMaterial()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null) return;

            if (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0)
            {
                BuildTuftMesh();
            }
            EnsureMaterial();
        }

        [ContextMenu("Rebuild 3D Grass Mesh")]
        public void BuildTuftMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mf == null || mr == null) return;

            Mesh mesh = mf.sharedMesh;
            if (mesh == null || mesh.hideFlags != HideFlags.DontSave)
            {
                mesh = new Mesh
                {
                    name = $"3DGrassTuft_{meshStyle}_{gameObject.name}",
                    hideFlags = HideFlags.DontSave
                };
            }
            else
            {
                mesh.Clear();
            }

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var roots = new List<Vector3>();
            var colors = new List<Color>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            int seed = (int)(transform.position.x * 6173f + transform.position.z * 98711f);
            Random.InitState(seed != 0 ? seed : 1337);

            switch (meshStyle)
            {
                case TuftMeshStyle.CrossCards:
                    // Two intersecting diagonal cards
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        Vector3.zero, 40f, cardWidth, cardHeight, bladeCurvature, heightSegments);
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        Vector3.zero, -45f, cardWidth * 0.95f, cardHeight * 0.98f, -bladeCurvature, heightSegments);
                    break;

                case TuftMeshStyle.StarCards:
                    // Three cards forming a 360-degree volumetric star
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        new Vector3(0, 0, -depthSpread * 0.3f), -55f, cardWidth * 0.92f, cardHeight * 0.94f, bladeCurvature, heightSegments);
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        new Vector3(0, 0, 0), 5f, cardWidth, cardHeight, 0f, heightSegments);
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        new Vector3(0, 0, depthSpread * 0.3f), 65f, cardWidth * 0.95f, cardHeight * 0.96f, -bladeCurvature, heightSegments);
                    break;

                case TuftMeshStyle.CurvedFan:
                    // Three arched cards layered with depth spread
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        new Vector3(-cardWidth * 0.15f, 0, depthSpread * 0.85f), 15f, cardWidth * 0.90f, cardHeight * 0.90f, bladeCurvature * 1.2f, heightSegments);
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        new Vector3(0, 0, 0), -5f, cardWidth, cardHeight, bladeCurvature, heightSegments);
                    AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                        new Vector3(cardWidth * 0.15f, 0, -depthSpread * 0.85f), 20f, cardWidth * 0.92f, cardHeight * 1.05f, bladeCurvature * 1.1f, heightSegments);
                    break;

                case TuftMeshStyle.ClusteredPatch:
                    // Multi-tuft volumetric clump
                    int count = Mathf.Max(3, clusterTuftCount);
                    for (int i = 0; i < count; i++)
                    {
                        float angleRad = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                        float r = (i == 0) ? 0f : Random.Range(patchRadius * 0.25f, patchRadius * 0.65f);
                        Vector3 offset = new Vector3(Mathf.Cos(angleRad) * r, 0f, Mathf.Sin(angleRad) * r * (depthSpread / 0.5f));

                        float rotDeg = (angleRad * Mathf.Rad2Deg) + Random.Range(-25f, 25f);
                        float scaleH = cardHeight * Random.Range(0.85f, 1.15f);
                        float scaleW = cardWidth * Random.Range(0.88f, 1.12f);
                        float curve = bladeCurvature * Random.Range(-1.2f, 1.2f);

                        AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                            offset, rotDeg, scaleW, scaleH, curve, heightSegments);

                        // Cross card for central/larger tufts
                        if (i % 2 == 0)
                        {
                            AddSubdividedCard(verts, uvs, roots, colors, normals, tris,
                                offset, rotDeg + 80f, scaleW * 0.8f, scaleH * 0.9f, -curve, heightSegments);
                        }
                    }
                    break;
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, roots); // Root anchor point for shader wind anchoring
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);

            // Bounds enlarged so wind sway never clips geometry in camera frustum
            mesh.bounds = new Bounds(Vector3.up * (cardHeight * 0.5f), new Vector3(cardWidth * 3f, cardHeight * 3f, depthSpread * 4f + 2f));
            mf.sharedMesh = mesh;
        }

        private void AddSubdividedCard(
            List<Vector3> verts, List<Vector2> uvs, List<Vector3> roots,
            List<Color> colors, List<Vector3> normals, List<int> tris,
            Vector3 centerOffset, float angleDeg, float width, float height,
            float curveAmount, int segments)
        {
            int baseVertexIndex = verts.Count;
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 right = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 forward = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            float halfW = width * 0.5f;

            // Generate vertices along vertical segments from root (h=0) to tip (h=1)
            for (int seg = 0; seg <= segments; seg++)
            {
                float t = seg / (float)segments;
                float y = t * height;

                // Subtle organic forward curvature along height
                float forwardOffset = curveAmount * (t * t);

                // Slight natural tapering towards tip
                float widthFactor = Mathf.Lerp(1.0f, 0.88f, t * t);
                float curHalfW = halfW * widthFactor;

                Vector3 segCenter = centerOffset + Vector3.up * y + forward * forwardOffset;
                Vector3 leftPos = segCenter - right * curHalfW;
                Vector3 rightPos = segCenter + right * curHalfW;

                verts.Add(leftPos);
                verts.Add(rightPos);

                uvs.Add(new Vector2(0f, t));
                uvs.Add(new Vector2(1f, t));

                roots.Add(centerOffset);
                roots.Add(centerOffset);

                // Upward-biased soft painterly normals (prevents harsh card edge shadows)
                Vector3 leftNormal = Vector3.Normalize(Vector3.up * 0.85f - right * 0.25f + forward * 0.15f);
                Vector3 rightNormal = Vector3.Normalize(Vector3.up * 0.85f + right * 0.25f + forward * 0.15f);
                normals.Add(leftNormal);
                normals.Add(rightNormal);

                // Vertex colors: subtle ambient occlusion at base, full white at tips
                Color vertCol = Color.Lerp(new Color(0.7f, 0.75f, 0.7f, 1f), Color.white, t);
                colors.Add(vertCol);
                colors.Add(vertCol);
            }

            // Triangles for both front and back faces of each quad segment
            for (int seg = 0; seg < segments; seg++)
            {
                int row0 = baseVertexIndex + seg * 2;
                int row1 = row0 + 2;

                int vL0 = row0;
                int vR0 = row0 + 1;
                int vL1 = row1;
                int vR1 = row1 + 1;

                // Front face
                tris.Add(vL0); tris.Add(vL1); tris.Add(vR0);
                tris.Add(vR0); tris.Add(vL1); tris.Add(vR1);

                // Back face
                tris.Add(vL0); tris.Add(vR0); tris.Add(vL1);
                tris.Add(vR0); tris.Add(vR1); tris.Add(vL1);
            }
        }

        private void EnsureMaterial()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr == null) return;

            if (customMaterial != null)
            {
                mr.sharedMaterial = customMaterial;
                return;
            }

            string matName = (textureVariant == TuftTextureVariant.Leaning)
                ? "Mat_PainterlyGrass_Tuft_Leaning"
                : "Mat_PainterlyGrass_Tuft_Upright";

            string matPath = $"Assets/Materials/{matName}.mat";

#if UNITY_EDITOR
            Material assetMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (assetMat != null)
            {
                mr.sharedMaterial = assetMat;
                return;
            }
#endif

            // Fallback runtime material
            Shader shader = Shader.Find("HoaVaMau/Stylized3DGrassTuft");
            if (shader == null) shader = Shader.Find("HoaVaMau/WindGrassSway");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

            if (mr.sharedMaterial == null || mr.sharedMaterial.shader != shader)
            {
                Material runtimeMat = new Material(shader)
                {
                    name = $"{matName}_Runtime",
                    hideFlags = HideFlags.DontSave
                };
                mr.sharedMaterial = runtimeMat;
            }
        }
    }
}
