#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Utility thiết lập toàn diện Texture Atlas và Materials cho Plane Map (địa hình phẳng, đường đi, cầu ván gỗ, vách đất):
    /// - Cấu hình Texture Importer chuẩn mực (Bilinear, Uncompressed / High Quality, Max 2048, Clamped/Repeat).
    /// - Tạo ra bộ 6 Materials URP chuyên biệt sử dụng trực tiếp Texture gốc với UV Tiling/Offset tương ứng:
    ///   1. M_PlaneMap_Grass (Cỏ xanh nền chính)
    ///   2. M_PlaneMap_DirtRoad (Đường mòn đất vàng Nam Bộ có viền cỏ)
    ///   3. M_PlaneMap_WoodPlanks (Mặt sàn gỗ cầu bến tàu / nhà sàn)
    ///   4. M_PlaneMap_CliffEdge (Vách bờ đất nâu cắt đứng)
    ///   5. M_PlaneMap_Wildflower (Cỏ điểm hoa cúc dại vàng)
    ///   6. M_PlaneMap_MossyRock (Khối đá xám phủ rêu)
    /// </summary>
    public static class SetupPlaneMapAssets
    {
        private const string AtlasPath = "Assets/Textures/Ground_Tiles/T_PlaneMap_TerrainAtlas.png";
        private const string MatDir = "Assets/Materials/PlaneMap";
        private const string PrefabDir = "Assets/Prefabs/Environment/PlaneMap";

        [MenuItem("Hoa va Mau/Environment/Setup Plane Map Atlas & Materials", false, 10)]
        public static void SetupAll()
        {
            Debug.Log("<color=#3b82f6>[PlaneMap]</color> Bắt đầu thiết lập Texture Atlas & Materials cho Plane Map...");

            // 1. Cấu hình Texture Importer
            ConfigureTextureImporter(AtlasPath);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (tex == null)
            {
                Debug.LogError($"[PlaneMap] Không tìm thấy Texture tại {AtlasPath}");
                return;
            }

            // 2. Tạo hoặc Cập nhật 6 Materials URP Lit
            CreateOrUpdateMaterial("M_PlaneMap_Grass", tex, new Vector2(0.390625f, 0.75f), new Vector2(0.0f, 0.25f));
            CreateOrUpdateMaterial("M_PlaneMap_DirtRoad", tex, new Vector2(0.3125f, 0.75f), new Vector2(0.390625f, 0.25f));
            CreateOrUpdateMaterial("M_PlaneMap_WoodPlanks", tex, new Vector2(0.296875f, 0.75f), new Vector2(0.703125f, 0.25f));
            CreateOrUpdateMaterial("M_PlaneMap_CliffEdge", tex, new Vector2(0.180664f, 0.25f), new Vector2(0.0f, 0.0f));
            CreateOrUpdateMaterial("M_PlaneMap_Wildflower", tex, new Vector2(0.2734375f, 0.25f), new Vector2(0.546875f, 0.0f));
            CreateOrUpdateMaterial("M_PlaneMap_MossyRock", tex, new Vector2(0.1796875f, 0.25f), new Vector2(0.8203125f, 0.0f));

            // 3. Tạo các Prefab Quads chuẩn mực cho Plane Map
            CreatePlanePrefab("PF_PlaneMap_Road_Straight_4m", "M_PlaneMap_DirtRoad", new Vector3(4.8f, 10.0f, 1f));
            CreatePlanePrefab("PF_PlaneMap_Wood_Wharf_4m", "M_PlaneMap_WoodPlanks", new Vector3(4.0f, 8.0f, 1f));
            CreatePlanePrefab("PF_PlaneMap_Grass_Patch_10m", "M_PlaneMap_Grass", new Vector3(10.0f, 10.0f, 1f));
            CreatePlanePrefab("PF_PlaneMap_Cliff_Border_5m", "M_PlaneMap_CliffEdge", new Vector3(5.0f, 2.0f, 1f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#10b981>[PlaneMap]</color> HOÀN TẤT! Đã cấu hình Atlas 1024x512, tạo 6 Materials và 4 Prefabs chuẩn mực cho Plane Map!");
        }

        private static void ConfigureTextureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
                Debug.Log($"<color=#10b981>[PlaneMap]</color> Đã thiết lập TextureImporter chất lượng cao (Uncompressed, Bilinear) cho {path}");
            }
        }

        private static void CreateOrUpdateMaterial(string matName, Texture2D atlas, Vector2 tiling, Vector2 offset)
        {
            string path = $"{MatDir}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetTexture("_BaseMap", atlas);
            mat.SetTextureScale("_BaseMap", tiling);
            mat.SetTextureOffset("_BaseMap", offset);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);

            EditorUtility.SetDirty(mat);
            Debug.Log($"<color=#10b981>[PlaneMap]</color> Đã tạo/cập nhật Material: {path} (Tiling: {tiling}, Offset: {offset})");
        }

        private static void CreatePlanePrefab(string prefabName, string matName, Vector3 initialScale)
        {
            string prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{matName}.mat");

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = prefabName;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = initialScale;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"<color=#10b981>[PlaneMap]</color> Đã tạo Prefab mặt phẳng: {prefabPath}");
        }
    }
}
#endif
