using UnityEngine;

namespace HoaVaMau.Water
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    public class WaterMeshGenerator : MonoBehaviour
    {
        [Header("Grid Size")]
        public float width = 50f;
        public float length = 50f;

        [Header("Resolution (Segments)")]
        [Range(10, 150)]
        public int segmentsX = 80;
        [Range(10, 150)]
        public int segmentsZ = 80;

        private void Start()
        {
            GenerateMesh();
        }

        [ContextMenu("Regenerate Water Mesh")]
        public void GenerateMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            mesh.name = "Subdivided_Water_Grid";

            int vertCount = (segmentsX + 1) * (segmentsZ + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector4[] tangents = new Vector4[vertCount];

            float dx = width / segmentsX;
            float dz = length / segmentsZ;
            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            for (int z = 0, i = 0; z <= segmentsZ; z++)
            {
                for (int x = 0; x <= segmentsX; x++, i++)
                {
                    vertices[i] = new Vector3(x * dx - halfW, 0f, z * dz - halfL);
                    uvs[i] = new Vector2((float)x / segmentsX, (float)z / segmentsZ);
                    normals[i] = Vector3.up;
                    tangents[i] = new Vector4(1f, 0f, 0f, -1f);
                }
            }

            int[] triangles = new int[segmentsX * segmentsZ * 6];
            for (int ti = 0, vi = 0, z = 0; z < segmentsZ; z++, vi++)
            {
                for (int x = 0; x < segmentsX; x++, ti += 6, vi++)
                {
                    triangles[ti]     = vi;
                    triangles[ti + 1] = vi + segmentsX + 1;
                    triangles[ti + 2] = vi + 1;
                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + segmentsX + 1;
                    triangles[ti + 5] = vi + segmentsX + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }
    }
}
