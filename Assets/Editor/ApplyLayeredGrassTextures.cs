#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.EditorTools
{
    public static class ApplyLayeredGrassTextures
    {
        private const string MatDir = "Assets/Materials/Environment";

        [MenuItem("Hoa va Mau/Environment/Apply Layered Grass Textures", false, 15)]
        public static void Execute()
        {
            Debug.Log("<color=#3b82f6>[GrassLayers]</color> Bắt đầu thiết lập texture cho từng lớp cỏ đất thực tế...");

            if (!Directory.Exists(MatDir))
            {
                Directory.CreateDirectory(MatDir);
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[GrassLayers] Không tìm thấy shader 'Universal Render Pipeline/Lit'");
                return;
            }

            // 1. Textures & Normal maps
            Texture2D texVillageSoil = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_baseColor_2k.png");
            Texture2D normVillageSoil = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_normal_gl_2k.png");
            Texture2D texDikeTurf = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Ground_Tiles/T_PlaneMap_Grass_Tile.png");
            Texture2D texHillMeadow = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Grass/meadow_turf.png");

            // ---------------------------------------------------------------------------------------------------------
            // LỚP 1: Nền đất làng quê Nam Bộ (Village Soil with Grass Patches)
            // ---------------------------------------------------------------------------------------------------------
            Material matGround = GetOrCreateMaterial($"{MatDir}/M_Ground_Grass_VillageSoil.mat", urpLit);
            matGround.SetTexture("_BaseMap", texVillageSoil);
            matGround.SetTextureScale("_BaseMap", new Vector2(38f, 54f));
            matGround.SetColor("_BaseColor", new Color(0.48f, 0.72f, 0.30f, 1f));
            matGround.SetFloat("_Smoothness", 0.05f);
            matGround.SetFloat("_Metallic", 0.0f);
            if (normVillageSoil != null)
            {
                matGround.SetTexture("_BumpMap", normVillageSoil);
                matGround.SetFloat("_BumpScale", 1.0f);
                matGround.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(matGround);

            // ---------------------------------------------------------------------------------------------------------
            // LỚP 2: Bờ ruộng lúa (Moist Alluvial Mud Dikes with Fresh Turf)
            // ---------------------------------------------------------------------------------------------------------
            Material matDikeEW = GetOrCreateMaterial($"{MatDir}/M_Ground_Grass_PaddyDike_EW.mat", urpLit);
            matDikeEW.SetTexture("_BaseMap", texDikeTurf);
            matDikeEW.SetTextureScale("_BaseMap", new Vector2(10f, 1f));
            matDikeEW.SetColor("_BaseColor", new Color(0.55f, 0.78f, 0.32f, 1f));
            matDikeEW.SetFloat("_Smoothness", 0.05f);
            matDikeEW.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(matDikeEW);

            Material matDikeNS = GetOrCreateMaterial($"{MatDir}/M_Ground_Grass_PaddyDike_NS.mat", urpLit);
            matDikeNS.SetTexture("_BaseMap", texDikeTurf);
            matDikeNS.SetTextureScale("_BaseMap", new Vector2(1f, 8f));
            matDikeNS.SetColor("_BaseColor", new Color(0.55f, 0.78f, 0.32f, 1f));
            matDikeNS.SetFloat("_Smoothness", 0.05f);
            matDikeNS.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(matDikeNS);

            // ---------------------------------------------------------------------------------------------------------
            // LỚP 3: Triền đồi & Gò đất bao quanh (Rolling Foothills & Meadow Ridges)
            // ---------------------------------------------------------------------------------------------------------
            Material matHill = GetOrCreateMaterial($"{MatDir}/M_Ground_Grass_HillsideMeadow.mat", urpLit);
            matHill.SetTexture("_BaseMap", texHillMeadow);
            matHill.SetTextureScale("_BaseMap", new Vector2(4f, 4f));
            matHill.SetColor("_BaseColor", new Color(0.50f, 0.75f, 0.28f, 1f));
            matHill.SetFloat("_Smoothness", 0.05f);
            matHill.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(matHill);

            // ---------------------------------------------------------------------------------------------------------
            // GÁN VÀO SCENE OBJECTS
            // ---------------------------------------------------------------------------------------------------------
            int groundCount = 0;
            var groundObj = GameObject.Find("Terrain_Unified_Ground_Base");
            if (groundObj != null)
            {
                var mr = groundObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = matGround;
                    EditorUtility.SetDirty(mr);
                    groundCount++;
                }
            }

            int ewCount = 0;
            int nsCount = 0;
            int hillCount = 0;

            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r.name.Contains("Dike"))
                {
                    var s = r.transform.lossyScale;
                    if (s.x >= s.z)
                    {
                        r.sharedMaterial = matDikeEW;
                        ewCount++;
                    }
                    else
                    {
                        r.sharedMaterial = matDikeNS;
                        nsCount++;
                    }
                    EditorUtility.SetDirty(r);
                }
                else if (r.name.StartsWith("Hill_"))
                {
                    r.sharedMaterial = matHill;
                    EditorUtility.SetDirty(r);
                    hillCount++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"<color=#10b981>[GrassLayers]</color> HOÀN TẤT! Đã gán {groundCount} nền làng, {ewCount} bờ ruộng EW, {nsCount} bờ ruộng NS, {hillCount} triền đồi với 3 lớp texture cỏ đất riêng biệt!");
        }

        private static Material GetOrCreateMaterial(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
#endif
