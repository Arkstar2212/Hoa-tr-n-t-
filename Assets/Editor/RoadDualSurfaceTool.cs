using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class RoadDualSurfaceTool
{
    private const string DUAL_MAT_PATH = "Assets/Materials/M_Lana_Road3D_DualSurface.mat";
    private const string ORIGINAL_GRASS_MAT_PATH = "Assets/Materials/M_Lana_GrassCap.mat";
    private const string ORIGINAL_ROCK_MAT_PATH = "Assets/Materials/M_Lana_RockUnderbelly.mat";
    private const string TEXTURE_PATH = "Assets/Textures/PainterlyGround_Seamless.png";
    private const string ROCK_CLUSTER_NAME = "[Rock_VerticalCut_Cascade]";
    private const string SCREENSHOT_PATH = "Assets/Screenshots/ShaderMethod3_Texture_Preview.jpg";

    // Cấu hình tỉ lệ UV chuẩn cho phong cách tranh vẽ 2.5D
    public const float CLIFF_BOTTOM_Y = -3.85f;      // Đáy đen kết thúc vách đất (sát mép đáy camera)
    public const float HORIZONTAL_TILING_FACTOR = 0.45f; // Tỉ lệ lặp ngang (~9 mét lặp 1 chu kỳ texture 2048px)

    [MenuItem("Tools/Hoa Tren Dat Do/Apply 3D Dual Surface Shader (Cach 3)")]
    public static void ApplyDualSurfaceMaterialAndRocks()
    {
        // 1. Cấu hình Material với texture Painterly Seamless
        Material dualMat = ConfigureMaterial();
        if (dualMat == null) return;

        // 2. Chỉnh lại UV của các Mesh mặt đất (U chạy dọc chiều dài x * 0.25, V đi từ 0 đáy đen lên 1 mép cỏ)
        AdjustMeshUVs();

        // 3. Gán Material cho toàn bộ các sculpted ledges trong Scene
        GameObject parent = GameObject.Find("03_Terrain/Lana_Sculpted_Ledges");
        if (parent != null)
        {
            var renderers = parent.GetComponentsInChildren<MeshRenderer>();
            Undo.RecordObjects(renderers, "Apply Dual Surface Road Material");
            foreach (var mr in renderers)
            {
                mr.sharedMaterial = dualMat;
            }
        }

        // 4. Cụm đá rêu phong che kín vết nối thẳng đứng tại X = 5.20
        SpawnRockClusterAtVerticalCut();

        // 5. Chụp ảnh nghiệm thu 720p từ Unity Camera
        CaptureScreenshot();

        Debug.Log("<color=green>[RoadDualSurfaceTool] Đã ốp Texture PainterlyGround_Seamless.png thành công lên khối đất 3D!</color>");
    }

    [MenuItem("Tools/Hoa Tren Dat Do/Revert Ledge Materials to Original")]
    public static void RevertToOriginalMaterials()
    {
        Material grassMat = AssetDatabase.LoadAssetAtPath<Material>(ORIGINAL_GRASS_MAT_PATH);
        Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(ORIGINAL_ROCK_MAT_PATH);

        GameObject parent = GameObject.Find("03_Terrain/Lana_Sculpted_Ledges");
        if (parent != null)
        {
            var renderers = parent.GetComponentsInChildren<MeshRenderer>();
            Undo.RecordObjects(renderers, "Revert Ledge Materials");
            foreach (var mr in renderers)
            {
                if (mr.name.Contains("Grass"))
                {
                    if (grassMat != null) mr.sharedMaterial = grassMat;
                }
                else
                {
                    if (rockMat != null) mr.sharedMaterial = rockMat;
                }
            }
        }

        // Xóa cụm đá nếu có
        GameObject cluster = GameObject.Find(ROCK_CLUSTER_NAME);
        if (cluster != null)
        {
            Undo.DestroyObjectImmediate(cluster);
        }

        Debug.Log("<color=yellow>[RoadDualSurfaceTool] Đã khôi phục Material nguyên trạng.</color>");
    }

    public static Material ConfigureMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(DUAL_MAT_PATH);
        Shader s = Shader.Find("HoaVaMau/LanaRoad3DDualSurface");
        if (s == null)
        {
            Debug.LogError("[RoadDualSurfaceTool] Shader 'HoaVaMau/LanaRoad3DDualSurface' not found!");
            return null;
        }

        if (mat == null)
        {
            mat = new Material(s);
            AssetDatabase.CreateAsset(mat, DUAL_MAT_PATH);
        }
        else
        {
            mat.shader = s;
        }

        // Load texture đã sinh
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
        if (tex == null)
        {
            Debug.LogWarning("[RoadDualSurfaceTool] Texture not found at: " + TEXTURE_PATH + ". Generating now...");
            PainterlyGroundTextureGenerator.GenerateGroundTexture();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
        }

        if (tex != null)
        {
            mat.SetTexture("_MainTex", tex);
        }

        // Tiling X điều chỉnh tỉ lệ lặp ngang tự nhiên
        mat.SetVector("_SoilTexTiling", new Vector4(HORIZONTAL_TILING_FACTOR, 1.0f, 0f, 0f));
        mat.SetFloat("_UseMeshUV", 1.0f);

        // Vết mòn lối đi & Gradient dự phòng
        mat.SetColor("_WornTrailColor", new Color(0.58f, 0.44f, 0.26f, 1f));
        mat.SetFloat("_TrailBlend", 0.28f);
        mat.SetFloat("_GradientTopY", -1.95f);
        mat.SetFloat("_GradientBottomY", CLIFF_BOTTOM_Y);
        mat.SetFloat("_WaveDisplacement", 0.035f);
        mat.SetFloat("_ShadowStrength", 0.52f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    public static void AdjustMeshUVs()
    {
        GameObject parent = GameObject.Find("03_Terrain/Lana_Sculpted_Ledges");
        if (parent == null) return;

        var meshFilters = parent.GetComponentsInChildren<MeshFilter>();

        foreach (var mf in meshFilters)
        {
            UnityEngine.Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Vector3[] verts = mesh.vertices;
            if (verts == null || verts.Length == 0) continue;

            bool isGrassCap = mf.gameObject.name.Contains("Grass");
            Vector2[] newUVs = new Vector2[verts.Length];

            if (isGrassCap)
            {
                // Đối với mặt cỏ trên cùng: V = 1.0 (mép cỏ trên cùng), U chạy theo chiều dài
                for (int i = 0; i < verts.Length; i++)
                {
                    float u = verts[i].x * 0.25f;
                    newUVs[i] = new Vector2(u, 1.0f);
                }
            }
            else
            {
                // Đối với vách đất: Tìm cao độ đỉnh (top Y) theo từng cột X
                var topYByX = new Dictionary<int, float>();
                for (int i = 0; i < verts.Length; i++)
                {
                    int key = Mathf.RoundToInt(verts[i].x * 4f); // Bước chia 0.25m
                    if (!topYByX.ContainsKey(key))
                        topYByX[key] = verts[i].y;
                    else
                        topYByX[key] = Mathf.Max(topYByX[key], verts[i].y);
                }

                // Gán UV cho từng đỉnh
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 pos = verts[i];
                    int key = Mathf.RoundToInt(pos.x * 4f);
                    float topY = topYByX.ContainsKey(key) ? topYByX[key] : -1.82f;

                    // Trục U chạy dọc theo chiều dài con đường
                    float u = pos.x * 0.25f;

                    // Trục V đi từ 0 (đáy đất đen CLIFF_BOTTOM_Y) lên 1 (mép cỏ trên cùng topY)
                    float v = Mathf.Clamp01((pos.y - CLIFF_BOTTOM_Y) / Mathf.Max(0.1f, topY - CLIFF_BOTTOM_Y));

                    newUVs[i] = new Vector2(u, v);
                }
            }

            Undo.RecordObject(mesh, "Adjust Mesh UVs");
            mesh.uv = newUVs;
            EditorUtility.SetDirty(mesh);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>[RoadDualSurfaceTool] Đã cập nhật xong UV cho tất cả các Mesh mặt đất (U dọc theo chiều dài, V từ 0 đáy đen lên 1 đỉnh cỏ)!</color>");
    }

    private static void SpawnRockClusterAtVerticalCut()
    {
        // Xóa cụm cũ nếu có
        GameObject old = GameObject.Find(ROCK_CLUSTER_NAME);
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject cluster = new GameObject(ROCK_CLUSTER_NAME);
        Undo.RegisterCreatedObjectUndo(cluster, "Spawn Vertical Cut Rock Cluster");

        // Load các sprite tảng đá chính thức của dự án
        Sprite rockDome = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_02_MediumMossyDome.png");
        Sprite rockSlab = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_04_FlatSlab.png");
        Sprite rockCorner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_03_MediumMossyCorner.png");
        Sprite rockPebbles = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_06_PebbleCluster.png");

        Color mossyRockTint = new Color(0.92f, 0.95f, 0.88f, 1.0f);
        Color baseRockTint = new Color(0.82f, 0.85f, 0.82f, 1.0f);

        // Xếp 6 tảng đá che kín hoàn toàn vết cắt dọc tại X = 5.20
        if (rockDome != null)
        {
            CreateRockSprite(cluster, "Rock_1_BaseAnchor", rockDome, new Vector3(5.10f, -1.78f, -0.05f), new Vector3(0.72f, 0.65f, 1f), new Vector3(0, 0, -6f), baseRockTint, 20);
        }

        if (rockSlab != null)
        {
            CreateRockSprite(cluster, "Rock_2_MidStepSlab", rockSlab, new Vector3(5.25f, -1.45f, -0.10f), new Vector3(0.75f, 0.62f, 1f), new Vector3(0, 0, 6f), mossyRockTint, 21);
        }

        if (rockDome != null)
        {
            CreateRockSprite(cluster, "Rock_3_UpperBoulder", rockDome, new Vector3(5.50f, -1.10f, -0.08f), new Vector3(0.75f, 0.68f, 1f), new Vector3(0, 0, -8f), mossyRockTint, 20);
        }

        if (rockCorner != null)
        {
            CreateRockSprite(cluster, "Rock_4_PlateauCrown", rockCorner, new Vector3(5.55f, -0.65f, -0.12f), new Vector3(0.80f, 0.72f, 1f), new Vector3(0, 0, 5f), mossyRockTint, 22);
        }

        if (rockSlab != null)
        {
            CreateRockSprite(cluster, "Rock_5_SideSupport", rockSlab, new Vector3(4.70f, -1.70f, -0.08f), new Vector3(0.65f, 0.55f, 1f), new Vector3(0, 0, -4f), mossyRockTint, 20);
        }

        if (rockPebbles != null)
        {
            CreateRockSprite(cluster, "Rock_6_BasePebbles", rockPebbles, new Vector3(4.35f, -1.90f, -0.15f), new Vector3(0.60f, 0.50f, 1f), Vector3.zero, mossyRockTint, 23);
        }

        // 2 Khóm lá tiền cảnh tối màu đặt ở sát đáy màn hình che nhẹ góc đất
        Sprite bushFoliage = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Lana_Concept_Assets/Bushes/T_HD_Bush_Broadleaf_01.png");
        if (bushFoliage != null)
        {
            Color darkFoliageTint = new Color(0.20f, 0.28f, 0.18f, 0.95f);
            CreateRockSprite(cluster, "Foreground_Foliage_LeftBottom", bushFoliage, new Vector3(-6.20f, -3.85f, -0.25f), new Vector3(0.85f, 0.85f, 1f), new Vector3(0, 0, 12f), darkFoliageTint, 25);
            CreateRockSprite(cluster, "Foreground_Foliage_RightBottom", bushFoliage, new Vector3(2.50f, -4.10f, -0.25f), new Vector3(0.95f, 0.90f, 1f), new Vector3(0, 0, -8f), darkFoliageTint, 25);
        }
    }

    private static SpriteRenderer CreateRockSprite(GameObject parent, string name, Sprite sprite, Vector3 localPos, Vector3 localScale, Vector3 localRot, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.transform.localEulerAngles = localRot;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        sr.sortingLayerName = "Default";

        return sr;
    }

    private static void CaptureScreenshot()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 prevPos = cam.transform.position;
        cam.transform.position = new Vector3(-2.20f, 0.35f, -15.50f);

        int width = 1280;
        int height = 720;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevRt = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevRt;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToJPG(80);
        Object.DestroyImmediate(tex);

        cam.transform.position = prevPos;

        string dir = Path.GetDirectoryName(SCREENSHOT_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(SCREENSHOT_PATH, bytes);
        AssetDatabase.Refresh();
    }
}
