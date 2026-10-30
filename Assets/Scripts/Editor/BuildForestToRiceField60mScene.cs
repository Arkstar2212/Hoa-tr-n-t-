#pragma warning disable 0219
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Xây dựng hoàn chỉnh Scene 60m 2.5D: "Từ Khu Rừng Rậm Chạy Ra Cánh Đồng Lúa Mênh Mông".
    /// Thiết kế chuẩn mực Planet of Lana:
    /// - 3 Tầng Chiều Sâu 2.5D thực thụ (Tiền cảnh -> Lối đi Gameplay -> Đồi Nền Gần Z=6m -> Đồi Nền Xa Z=12m -> Skybox).
    /// - Xóa sạch hiện tượng "vực lơ lửng trên mây", mọi gốc cây và xuồng ba lá đều được đặt trên đồi cỏ / mặt nước rộng.
    /// - Luật Phối Cảnh Khí Quyển (Atmospheric Depth Fog) suy giảm màu sắc và phủ sương mù theo trục Z.
    /// - Quy hoạch cây cối theo Bố Cục Cụm Tam Giác (Rule of Odd Clusters) và Nhịp điệu Gameplay:
    ///   + Rừng rậm: Đóng kín, huyền bí, vòm cây che kín rọi vệt nắng God rays.
    ///   + Vườn quê: Giàn bầu bí đứng độc tôn thoáng đãng, nổi bật từng quả bầu vàng buông rủ.
    ///   + Kênh rạch: Cầu vồng nhẹ, mặt nước rộng, xuồng ba lá nổi bập bềnh, rặng tre bên bờ đối diện.
    ///   + Cánh đồng lúa: Mở rộng 100% mênh mông, đẩy dừa ra xa tít, 1 cây đa đại ngàn ở đích cuối màn.
    /// - Đường đi (Pathway Flow) uốn lượn có nhịp điệu sinh học theo địa lý Miền Tây.
    /// </summary>
    public static class BuildForestToRiceField60mScene
    {
        public const string ScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string GenAssetsDir = "Assets/Materials/LanaOverhaul";

        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        [MenuItem("Hoa va Mau/Level Design/Build 60m Forest-to-RiceField 2.5D Complete Scene", false, 0)]
        public static void ExecuteBuild()
        {
            _spriteCache.Clear();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("<color=#38bdf8>[ForestToRiceField60m]</color> Bắt đầu dựng không gian 2.5D có chiều sâu Planet of Lana: Rừng Rậm -> Giàn Bầu Bí -> Cánh Đồng Lúa 60m...");

            // 1. Configure clean Studio Ghibli lighting, ambient trilight & volume post-processing
            ConfigureSceneLightingAndVolume();

            // 2. Repair all Prefab Sprite references (fixes fileID 21300000 mismatch in Unity 6)
            Material cleanSpriteMat = GetOrCreateCleanSpriteMaterial();
            RepairAllPackPrefabs(cleanSpriteMat);

            // 3. Purge clashing legacy objects (mooring poles, 3D mossy boulders, neon trees, misplaced vegetables)
            PurgeLegacyClashingElements();

            // 4. Rebuild Atmospheric Background Horizon (Align painted rolling plains & blue mountains with ground line)
            RebuildAtmosphericBackgroundAndHills();

            // 5. Rebuild Wide Canal Water & Far River Bank (Mặt nước rộng Z = -1.5m..6.5m và bờ kênh đối diện Z=5.5m)
            RebuildWideCanalAndRiverbanks();

            // 6. Rebuild Sculpted Ledges with realistic Pathway Flow (Cầu vồng nhẹ, bờ đê nâng cao, thềm vườn phẳng, bờ kênh đối diện)
            RebuildSculptedTerrainLedges();

            // 7. Rebuild Arched Wooden Bridge Planks & Stilts
            RebuildArchedWoodenBridge();

            var ecoRoot = GameObject.Find("05_Midground/Lana_HandDrawn_Ecosystem");
            if (ecoRoot == null)
            {
                var mg = GameObject.Find("05_Midground") ?? new GameObject("05_Midground");
                ecoRoot = new GameObject("Lana_HandDrawn_Ecosystem");
                ecoRoot.transform.SetParent(mg.transform, false);
            }

            // Clear any previous custom 2.5D master groups so rebuilds are idempotent
            DestroyChildIfExists(ecoRoot.transform, "01_DeepForest_MultiLayer_25D");
            DestroyChildIfExists(ecoRoot.transform, "02_ForestGate_And_GourdTrellis_25D");
            DestroyChildIfExists(ecoRoot.transform, "03_CanalBridge_And_Sampan_25D");
            DestroyChildIfExists(ecoRoot.transform, "04_VastRicePaddyField_MultiLayer_25D");
            DestroyChildIfExists(ecoRoot.transform, "05_NativeHD_Flora_And_Rocks_25D");

            var fgRoot = GameObject.Find("04_Foreground") ?? new GameObject("04_Foreground");
            while (fgRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(fgRoot.transform.GetChild(0).gameObject);
            }

            // 7. Build Zone 1 (-8m to 16m): Enclosed Deep Tropical Forest (Vòm lá khép kín, cụm cây tam giác, God rays)
            BuildZone1_DeepForest25D(ecoRoot.transform, fgRoot.transform, cleanSpriteMat);

            // 8. Build Zone 2 (16m to 30m): Rural Garden with Isolated Walk-Through Gourd Trellis (Ngôi sao của phân khu)
            BuildZone2_ForestGateAndGourdTrellis25D(ecoRoot.transform, fgRoot.transform, cleanSpriteMat);

            // 9. Build Zone 3 (30m to 42.5m): Arched Wooden Bridge, Wide Canal & Floating Sampan on Water
            BuildZone3_CanalAndSampan25D(ecoRoot.transform, cleanSpriteMat);

            // 10. Build Zone 4 (42.5m to 62m): Vast Continuous Rice Paddy Field (Biển lúa mênh mông, dừa mờ xa tít)
            BuildZone4_VastRicePaddyField25D(ecoRoot.transform, fgRoot.transform, cleanSpriteMat);

            // 11. Populate Native HD Grass Carpets, Wildflowers & Pebbles along Pathway
            BuildUnifiedNativeHDPathDetails(ecoRoot.transform, cleanSpriteMat);

            // 12. Setup Cloud Radiance, Volumetric God Rays & Conformed Dappled Shadows
            SetupSunbeamsAndDappledShadows(ecoRoot.transform);

            // Save Scene & Assets
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // 13. Capture 4 Showcase Screenshots with Minh Grounded in each Zone
            CaptureForestToRiceFieldScreenshots();

            Debug.Log("<color=#4ade80>[ForestToRiceField60m]</color> <b>HOÀN TẤT SCENE 60M 2.5D THEO CHUẨN MỰC PLANET OF LANA!</b>");
        }

        private static void ConfigureSceneLightingAndVolume()
        {
            RenderSettings.fog = false;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.68f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.80f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.20f, 0.25f);
            RenderSettings.ambientIntensity = 1.12f;

            var lightObj = GameObject.Find("08_Lighting/Directional Light");
            if (lightObj != null)
            {
                var l = lightObj.GetComponent<Light>();
                if (l != null)
                {
                    lightObj.transform.rotation = Quaternion.Euler(28.0f, 32.0f, 0.0f);
                    l.color = new Color(1.0f, 0.96f, 0.88f);
                    l.intensity = 1.25f;
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.42f;
                    l.shadowNormalBias = 0.35f;
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/VolumeProfile_Scene1_CaMau1954.asset");
            if (profile != null)
            {
                while (profile.components.Count > 0) profile.Remove(profile.components[0].GetType());

                var tonemapping = profile.Add<Tonemapping>(true);
                tonemapping.mode.Override(TonemappingMode.Neutral);

                var colorAdj = profile.Add<ColorAdjustments>(true);
                colorAdj.postExposure.Override(0.06f);
                colorAdj.contrast.Override(7.0f);
                colorAdj.saturation.Override(7.0f);

                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(0.95f);
                bloom.intensity.Override(0.20f);
                bloom.scatter.Override(0.65f);
            }
        }

        private static Material GetOrCreateCleanSpriteMaterial()
        {
            if (!Directory.Exists(GenAssetsDir)) Directory.CreateDirectory(GenAssetsDir);
            string matPath = $"{GenAssetsDir}/M_Lana_SpriteDefault.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (mat == null)
            {
                mat = new Material(sh) { name = "M_Lana_SpriteDefault" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (sh != null)
            {
                mat.shader = sh;
            }
            mat.SetTexture("_MainTex", null);
            mat.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(mat);

            Material builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (builtin != null)
            {
                builtin.SetTexture("_MainTex", null);
                builtin.color = Color.white;
            }
            return mat;
        }

        private static void RepairAllPackPrefabs(Material cleanSpriteMat)
        {
            string[] searchFolders = new[]
            {
                "Assets/Prefabs/Environment/Lana_VN_Pack",
                "Assets/Prefabs/Environment/Lana_Concept_Assets"
            };

            string[] allTexGuids = AssetDatabase.FindAssets("t:Texture2D", new[]
            {
                "Assets/Textures/Environment/Lana_VN_Pack",
                "Assets/Textures/Environment/Lana_Concept_Assets"
            });

            foreach (string tGuid in allTexGuids)
            {
                string tPath = AssetDatabase.GUIDToAssetPath(tGuid);
                if (string.IsNullOrEmpty(tPath)) continue;
                Sprite spr = LoadSpriteFromTexturePath(tPath);
                if (spr != null)
                {
                    string key = Path.GetFileNameWithoutExtension(tPath);
                    _spriteCache[key] = spr;
                }
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
            int fixedCount = 0;
            foreach (string pGuid in prefabGuids)
            {
                string pPath = AssetDatabase.GUIDToAssetPath(pGuid);
                GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                if (pf == null) continue;

                var sr = pf.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                string baseName = Path.GetFileNameWithoutExtension(pPath);
                string texKey = baseName;
                if (texKey.StartsWith("PF_T_HD_")) texKey = texKey.Substring(3);
                else if (texKey.StartsWith("PF_LanaVN_")) texKey = "T_LanaVN_" + texKey.Substring(10);

                if (_spriteCache.TryGetValue(texKey, out Sprite validSpr) && validSpr != null)
                {
                    if (sr.sprite != validSpr || sr.sharedMaterial != cleanSpriteMat)
                    {
                        sr.sprite = validSpr;
                        sr.sharedMaterial = cleanSpriteMat;
                        EditorUtility.SetDirty(pf);
                        PrefabUtility.SavePrefabAsset(pf);
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log($"<color=#4ade80>[ForestToRiceField60m]</color> Đã sửa liên kết Sprite cho {fixedCount} Prefabs Native HD.");
            }
        }

        private static Sprite LoadSpriteFromTexturePath(string texPath)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            if (s != null) return s;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(texPath))
            {
                if (obj is Sprite sp) return sp;
            }
            return null;
        }

        private static void DestroyChildIfExists(Transform parent, string name)
        {
            if (parent == null) return;
            var t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // =========================================================================================
        // STEP 1: PURGE LEGACY CLASHING ELEMENTS
        // =========================================================================================
        private static void PurgeLegacyClashingElements()
        {
            // 1. Purge pier mooring poles and rock islets
            var pierRoots = new[]
            {
                GameObject.Find("03_Terrain/Visual_Dock_Pier_ZoneD"),
                GameObject.Find("05_Midground/Segmented_Village_Pier_32_42m"),
                GameObject.Find("05_Midground")
            };
            foreach (var r in pierRoots)
            {
                if (r == null) continue;
                var oldPoles = r.transform.Find("Lana_Pier_MooringPoles");
                if (oldPoles != null) Object.DestroyImmediate(oldPoles.gameObject);
                var oldIslet = r.transform.Find("Lana_MidWater_RockIslet");
                if (oldIslet != null) Object.DestroyImmediate(oldIslet.gameObject);
            }

            // 2. Purge Sculpted_MossyBoulders and legacy trees/bushes
            var ecoRoot = GameObject.Find("05_Midground/Lana_HandDrawn_Ecosystem");
            if (ecoRoot != null)
            {
                DestroyChildIfExists(ecoRoot.transform, "Sculpted_MossyBoulders");
                DestroyChildIfExists(ecoRoot.transform, "HandDrawn_Bushes_Cay1To5");
                DestroyChildIfExists(ecoRoot.transform, "HandDrawn_MeadowGrass");

                var legacyTrees = ecoRoot.transform.Find("HandDrawn_HeroTrees");
                if (legacyTrees != null)
                {
                    while (legacyTrees.childCount > 0)
                    {
                        Object.DestroyImmediate(legacyTrees.GetChild(0).gameObject);
                    }
                }
            }

            // 3. Purge legacy clutter under 05_Midground
            var mgRoot = GameObject.Find("05_Midground");
            if (mgRoot != null)
            {
                string[] oldToRemove = new[]
                {
                    "Midground_Trees", "Midground_Vegetation", "Midground_Vines",
                    "ClimbObstacle_ArtDressing", "Settlement_Props_Life",
                    "Opening_Vista_Midground", "Canopy_Wall_System",
                    "Landmarks_Obstacles", "Riverside_Settlement"
                };
                foreach (var name in oldToRemove)
                {
                    var t = mgRoot.transform.Find(name);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
            }

            // 4. Purge legacy ground flora quads from 03_Terrain
            var terrainRoot = GameObject.Find("03_Terrain");
            if (terrainRoot != null)
            {
                DestroyChildIfExists(terrainRoot.transform, "Lana_Ground_Flora");
            }
        }

        // =========================================================================================
        // STEP 2: ATMOSPHERIC DEPTH TINT FORMULA (PLANET OF LANA AIR PERSPECTIVE)
        // =========================================================================================
        public static Color GetAtmosphericTint(float z, Color baseColor, bool isRiceField = false)
        {
            Color skyHaze = isRiceField
                ? new Color(0.92f, 0.90f, 0.80f, 1.0f) // Warm golden morning/afternoon haze
                : new Color(0.60f, 0.78f, 0.88f, 1.0f); // Soft cyan/blue Ghibli haze

            if (z < -1.6f)
            {
                // Foreground silhouette framing (Z <= -1.6m): dark, rich ambient occlusion
                float t = Mathf.InverseLerp(-1.6f, -3.0f, z);
                Color fgSilhouette = new Color(0.08f, 0.12f, 0.11f, 0.98f);
                return Color.Lerp(baseColor, fgSilhouette, t);
            }
            else if (z <= 2.2f)
            {
                // Gameplay walk lane & path verges (Z = -1.6m .. +2.2m): 100% full vibrant painterly colors
                return baseColor;
            }
            else if (z <= 5.5f)
            {
                // Midground (Z = 2.2m .. 5.5m): 30% to 50% atmospheric haze
                float t = Mathf.InverseLerp(2.2f, 5.5f, z);
                float haze = Mathf.Lerp(0.30f, 0.50f, t);
                Color c = Color.Lerp(baseColor, skyHaze, haze);
                c.a = baseColor.a;
                return c;
            }
            else
            {
                // Far background (Z > 5.5m): 60% to 80% sky blend (soft misty silhouettes)
                float t = Mathf.InverseLerp(5.5f, 13.0f, z);
                float haze = Mathf.Lerp(0.60f, 0.80f, t);
                Color c = Color.Lerp(baseColor, skyHaze, haze);
                c.a = baseColor.a;
                return c;
            }
        }

        // =========================================================================================
        // STEP 3: PATHWAY ELEVATION FUNCTION (SMOOTH, REALISTIC 60M RURAL LANDSCAPE)
        // =========================================================================================
        public static float SampleY(float x)
        {
            float baseH = -1.85f;

            // Zone 1: Dense Forest Trail (-12m to 16m)
            // Higher, slightly rugged primeval forest ground (Y = -1.15m)
            if (x < 4.5f)
            {
                baseH = -1.15f;
            }
            else if (x >= 4.5f && x < 8.5f)
            {
                // Gentle mossy knoll up to -0.95m
                float t = (x - 4.5f) / 4.0f;
                baseH = -1.15f + Mathf.Sin(t * Mathf.PI) * 0.20f;
            }
            else if (x >= 8.5f && x < 13.5f)
            {
                baseH = -1.15f;
            }
            else if (x >= 13.5f && x < 18.0f)
            {
                // Gentle, natural slope descending out of the forest to the rural garden terrace
                float t = (x - 13.5f) / 4.5f;
                baseH = Mathf.Lerp(-1.15f, -1.55f, SmoothCos(t));
            }
            // Zone 2: Rural Garden Terrace (18.0m to 30.0m)
            else if (x >= 18.0f && x < 28.5f)
            {
                baseH = -1.55f; // Flat, peaceful garden ground where Gourd Trellis stands
            }
            else if (x >= 28.5f && x < 31.5f)
            {
                // Gentle descent to the canal bridge western abutment
                float t = (x - 28.5f) / 3.0f;
                baseH = Mathf.Lerp(-1.55f, -1.82f, SmoothCos(t));
            }
            // Zone 3: Rural Canal Wooden Stilt Bridge (31.5m to 42.5m)
            else if (x >= 31.5f && x <= 42.5f)
            {
                // Authentic Miền Tây arched wooden plank bridge: slightly higher in center
                float tBridge = Mathf.Clamp01((x - 31.5f) / 11.0f);
                float bridgeArch = Mathf.Sin(tBridge * Mathf.PI) * 0.20f;
                baseH = -1.82f + bridgeArch;
            }
            // Zone 4: Elevated Earthen Dike Walkway (42.5m to 64.0m)
            else if (x > 42.5f && x < 44.8f)
            {
                // Ascent from bridge eastern abutment up to elevated earthen dike
                float t = (x - 42.5f) / 2.3f;
                baseH = Mathf.Lerp(-1.82f, -1.45f, SmoothCos(t));
            }
            else if (x >= 44.8f && x < 61.5f)
            {
                baseH = -1.45f; // Elevated earthen dike (bờ đê) overlooking sunken rice paddy!
            }
            else if (x >= 61.5f)
            {
                float t = Mathf.Clamp01((x - 61.5f) / 2.5f);
                baseH = Mathf.Lerp(-1.45f, -1.65f, SmoothCos(t));
            }

            // Subtle organic micro-undulation on solid ground
            if (x < 31.5f || x > 42.5f)
            {
                baseH += Mathf.Sin(x * 0.45f) * 0.015f + Mathf.Cos(x * 1.15f) * 0.010f;
            }
            return baseH;
        }

        // Height of the Near Midground Hills / Far Riverbank (Z = 5.5m .. 6.5m)
        public static float SampleNearHillY(float x)
        {
            if (x >= 31.0f && x <= 43.0f)
            {
                // Far Canal Bank (Bờ kênh bên kia): gentle muddy/grassy bank above water
                float t = Mathf.Clamp01((x - 31.0f) / 12.0f);
                return -1.65f + Mathf.Sin(t * Mathf.PI) * 0.12f;
            }
            else if (x < 18.0f)
            {
                // Forest background hills supporting trees at Z = 4.2m..5.5m
                return -0.55f + Mathf.Sin(x * 0.18f) * 0.25f + Mathf.Cos(x * 0.35f) * 0.15f;
            }
            else if (x >= 18.0f && x < 31.0f)
            {
                // Garden background hills
                return -0.85f + Mathf.Sin(x * 0.22f) * 0.20f;
            }
            else
            {
                // Rice field background dyke ridge
                return -0.90f + Mathf.Sin(x * 0.16f) * 0.20f;
            }
        }

        // Height of the Distant Rolling Hills (Z = 11.0m .. 13.5m)
        public static float SampleDistantHillY(float x)
        {
            return 0.85f + Mathf.Sin(x * 0.14f) * 0.65f + Mathf.Cos(x * 0.28f) * 0.45f;
        }

        private static float SmoothCos(float t)
        {
            t = Mathf.Clamp01(t);
            return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
        }

        // =========================================================================================
        // STEP 4: REBUILD ATMOSPHERIC BACKGROUND HORIZON (PLANET OF LANA PAINTERLY VISTA)
        // =========================================================================================
        private static void RebuildAtmosphericBackgroundAndHills()
        {
            var bgRoot = GameObject.Find("07_Background") ?? new GameObject("07_Background");
            DestroyChildIfExists(bgRoot.transform, "Lana_Painted_Vistas");
            DestroyChildIfExists(bgRoot.transform, "Lana_RollingHills_25D");

            var hillsRoot = new GameObject("Lana_RollingHills_25D");
            hillsRoot.transform.SetParent(bgRoot.transform, false);

            // 1. EXTENDED SEAMLESS SKYBOX (X = -20m to 75m, Z = 16.0m)
            // Aligns the painted rolling green plains, tree groves and distant misty blue mountains
            // of BG_Lana_PlainsHills_Day.png directly with our horizon line (Y = -1.2m .. +0.6m)!
            var texPlains = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png");
            if (texPlains != null)
            {
                Shader bgShader = Shader.Find("HoaVaMau/LanaBackgroundBlend") ?? Shader.Find("Universal Render Pipeline/Unlit");
                string skyMatPath = $"{GenAssetsDir}/M_Lana_Skybox_Unified_60m.mat";
                var skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
                if (skyMat == null)
                {
                    skyMat = new Material(bgShader);
                    AssetDatabase.CreateAsset(skyMat, skyMatPath);
                }
                skyMat.shader = bgShader;
                skyMat.SetTexture("_MainTex", texPlains);
                skyMat.SetColor("_Color", Color.white);
                skyMat.SetFloat("_LeftFeather", 0.0f);
                skyMat.SetFloat("_RightFeather", 0.0f);
                skyMat.SetFloat("_BottomFeather", 0.0f);
                skyMat.renderQueue = 2900;
                EditorUtility.SetDirty(skyMat);

                var skyQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                skyQuad.name = "Skybox_Unified_PlainsHills";
                skyQuad.transform.SetParent(hillsRoot.transform, false);
                skyQuad.transform.position = new Vector3(27.5f, 4.40f, 16.0f);
                skyQuad.transform.localScale = new Vector3(95.0f, 15.5f, 1.0f);

                var mr = skyQuad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = skyMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                Object.DestroyImmediate(skyQuad.GetComponent<Collider>());
            }
        }

        // =========================================================================================
        // STEP 5: REBUILD WIDE CANAL WATER SURFACE & FAR RIVER BANK (Z = -1.5m to 7.5m)
        // =========================================================================================
        private static void RebuildWideCanalAndRiverbanks()
        {
            var waterGroup = GameObject.Find("06_Water/River_Water_System") ?? GameObject.Find("06_Water");
            if (waterGroup != null)
            {
                var oldWater = waterGroup.transform.Find("River_Water_Surface");
                if (oldWater != null)
                {
                    // Expand water surface seamlessly deep in Z (Z = -1.5m to Z = 7.5m!)
                    oldWater.position = new Vector3(37.0f, -2.18f, 3.00f);
                    oldWater.localScale = new Vector3(1.35f, 1.0f, 2.85f); // Deep expanse in Z
                }

                var riverBed = GameObject.Find("03_Terrain/RiverBed_SandyBottom_ZoneD");
                if (riverBed != null)
                {
                    riverBed.transform.position = new Vector3(37.0f, -3.80f, 3.00f);
                    riverBed.transform.localScale = new Vector3(14.0f, 1.0f, 10.5f);
                }
            }
        }

        // =========================================================================================
        // STEP 6: REBUILD SCULPTED TERRAIN LEDGES (CONFORMING TO UPDATED PATHWAY FLOW)
        // =========================================================================================
        private static void RebuildSculptedTerrainLedges()
        {
            var terrainRoot = GameObject.Find("03_Terrain") ?? new GameObject("03_Terrain");
            DestroyChildIfExists(terrainRoot.transform, "Lana_Sculpted_Ledges");

            var ledgesGroup = new GameObject("Lana_Sculpted_Ledges");
            ledgesGroup.transform.SetParent(terrainRoot.transform, false);

            Material grassCapMat = AssetDatabase.LoadAssetAtPath<Material>($"{GenAssetsDir}/M_Lana_GrassCap.mat");
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>($"{GenAssetsDir}/M_Lana_RockUnderbelly.mat");

            string meshDir = $"{GenAssetsDir}/GeneratedMeshes";
            if (!Directory.Exists(meshDir)) Directory.CreateDirectory(meshDir);

            void CreateSection(string sName, float startX, float endX, int segX, float frontZ, float backZ, float bottomY, System.Func<float, float> heightFunc, bool hasGrass)
            {
                var sRoot = new GameObject(sName);
                sRoot.transform.SetParent(ledgesGroup.transform, false);

                // Rock Underbelly Mesh
                Mesh rockMesh = BuildChiseledRockMesh($"{sName}_Rock", startX, endX, segX, frontZ, backZ, bottomY, heightFunc);
                AssetDatabase.CreateAsset(rockMesh, $"{meshDir}/{sName}_Rock.asset");

                var rockGO = new GameObject($"{sName}_RockUnderbelly");
                rockGO.transform.SetParent(sRoot.transform, false);
                rockGO.AddComponent<MeshFilter>().sharedMesh = rockMesh;
                var rmr = rockGO.AddComponent<MeshRenderer>();
                if (rockMat != null) rmr.sharedMaterial = rockMat;
                rmr.shadowCastingMode = ShadowCastingMode.On;
                rmr.receiveShadows = true;

                if (hasGrass)
                {
                    Mesh grassMesh = BuildOverhangingGrassCapMesh($"{sName}_Grass", startX, endX, segX, frontZ - 0.18f, backZ + 0.10f, heightFunc);
                    AssetDatabase.CreateAsset(grassMesh, $"{meshDir}/{sName}_Grass.asset");

                    var grassGO = new GameObject($"{sName}_GrassTurfCap");
                    grassGO.transform.SetParent(sRoot.transform, false);
                    grassGO.AddComponent<MeshFilter>().sharedMesh = grassMesh;
                    var gmr = grassGO.AddComponent<MeshRenderer>();
                    if (grassCapMat != null) gmr.sharedMaterial = grassCapMat;
                    gmr.shadowCastingMode = ShadowCastingMode.On;
                    gmr.receiveShadows = true;
                }
            }

            // 1. West Ledge (-14m to 31.8m: Forest & Garden Terrace)
            CreateSection("Lana_Ledge_West_ZonesABC", -14.0f, 31.8f, 240, -1.75f, 2.15f, -9.5f, SampleY, true);

            // 2. East Ledge (42.2m to 72.0m: Elevated Rice Field Dike)
            CreateSection("Lana_Ledge_East_ZonesEF", 42.2f, 72.0f, 160, -1.75f, 2.15f, -9.5f, SampleY, true);

            // 3. Canal Riverbed (31.5m to 42.5m)
            CreateSection("Lana_Riverbed_Cutaway_ZoneD", 31.5f, 42.5f, 40, -1.75f, 2.15f, -9.5f, x => -2.18f, false);

            // 4. Far Canal Bank (20.0m to 48.0m, Z = 5.6m..7.5m, Y = -1.82m - Bờ kênh bên kia sông liền mạch, thoải nhẹ sát nước!)
            CreateSection("Lana_Canal_FarBank_ZoneD", 20.0f, 48.0f, 110, 5.6f, 7.5f, -2.40f, 
                x => -1.82f + Mathf.Sin((x - 20f) * 0.25f) * 0.06f, true);
        }

        // =========================================================================================
        // STEP 7: REBUILD ARCHED WOODEN STILT BRIDGE (CONFORMING TO PATHWAY SAMPLE-Y)
        // =========================================================================================
        private static void RebuildArchedWoodenBridge()
        {
            var pier = GameObject.Find("03_Terrain/Visual_Dock_Pier_ZoneD");
            if (pier == null) return;

            for (int i = 0; i < pier.transform.childCount; i++)
            {
                var child = pier.transform.GetChild(i);
                float x = child.position.x;
                float deckY = SampleY(x);

                if (child.name.StartsWith("DockPlank"))
                {
                    child.position = new Vector3(x, deckY - 0.045f, 0f);
                }
                else if (child.name.StartsWith("DockStilt"))
                {
                    float riverbedY = -3.20f;
                    float topY = deckY - 0.08f;
                    bool isMooringPost = (x <= 32.6f || x >= 41.4f);
                    if (isMooringPost)
                    {
                        topY = deckY + 0.40f; // Rustic mooring post at bridge ends
                    }
                    float height = topY - riverbedY;
                    float centerY = (topY + riverbedY) * 0.5f;
                    child.position = new Vector3(x, centerY, child.position.z);
                    child.localScale = new Vector3(child.localScale.x, height * 0.5f, child.localScale.z);
                }
                else if (child.name.StartsWith("PierPiling"))
                {
                    // Only keep mooring posts at bridge ends (X <= 33m or X >= 41m)
                    // Disables cluttering middle posts so Minh's silhouette is clean and iconic!
                    if (x <= 33.0f || x >= 41.0f)
                    {
                        child.gameObject.SetActive(true);
                        child.position = new Vector3(x, deckY + 0.40f, child.position.z);
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private static Mesh BuildChiseledRockMesh(string meshName, float startX, float endX, int segX, float frontZ, float backZ, float bottomY, System.Func<float, float> heightFunc)
        {
            int rowsY = 6;
            int cols = segX + 1;
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= rowsY; r++)
            {
                float v = (float)r / rowsY;
                for (int c = 0; c < cols; c++)
                {
                    float u = (float)c / segX;
                    float x = Mathf.Lerp(startX, endX, u);
                    float surfY = heightFunc(x);
                    float topY = surfY - 0.18f;
                    float y = Mathf.Lerp(topY, bottomY, v);

                    float facet = Mathf.Sin(x * 1.45f + r * 1.9f) * 0.10f + Mathf.Cos(x * 3.1f - r * 2.4f) * 0.05f;
                    float zOffset = facet * Mathf.Sin(v * Mathf.PI);
                    float z = frontZ + zOffset;
                    if (c == 0 || c == segX) z += 0.25f;

                    verts.Add(new Vector3(x, y, z));
                    float lipAO = (r == 0) ? 0.45f : ((r == 1) ? 0.85f : 1.0f);
                    float botAO = Mathf.Lerp(1.0f, 0.55f, Mathf.Pow(v, 1.5f));
                    colors.Add(new Color(0f, lipAO * botAO, 0f, 1f));
                    uvs.Add(new Vector2(u * 10f, v * 2f));
                }
            }

            for (int r = 0; r < rowsY; r++)
            {
                for (int c = 0; c < segX; c++)
                {
                    int i0 = r * cols + c;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * cols + c;
                    int i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

            // Top Cap
            int topStartIdx = verts.Count;
            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / segX;
                float x = Mathf.Lerp(startX, endX, u);
                float topY = heightFunc(x) - 0.18f;
                verts.Add(new Vector3(x, topY, frontZ));
                verts.Add(new Vector3(x, topY, backZ));
                colors.Add(new Color(0f, 0.88f, 0f, 1f));
                colors.Add(new Color(0f, 0.88f, 0f, 1f));
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
            }
            for (int c = 0; c < segX; c++)
            {
                int i0 = topStartIdx + c * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i2); tris.Add(i1); tris.Add(i3);
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildOverhangingGrassCapMesh(string meshName, float startX, float endX, int segX, float frontZ, float backZ, System.Func<float, float> heightFunc)
        {
            int profileCount = 6;
            int cols = segX + 1;
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / segX;
                float x = Mathf.Lerp(startX, endX, u);
                float y = heightFunc(x);
                float scallop = Mathf.Sin(x * 2.4f) * 0.055f + Mathf.Cos(x * 5.2f) * 0.032f;

                verts.Add(new Vector3(x, y - 0.22f, backZ));
                verts.Add(new Vector3(x, y + 0.04f, backZ));
                verts.Add(new Vector3(x, y + 0.01f, Mathf.Lerp(frontZ, backZ, 0.5f)));
                verts.Add(new Vector3(x, y + 0.03f + scallop * 0.20f, frontZ + 0.16f));
                verts.Add(new Vector3(x, y - 0.11f + scallop * 0.60f, frontZ));
                verts.Add(new Vector3(x, y - 0.27f + scallop, frontZ + 0.15f));

                colors.Add(new Color(0.65f, 1f, 0f, 1f));
                colors.Add(new Color(1.00f, 1f, 0f, 1f));
                colors.Add(new Color(0.96f, 1f, 0f, 1f));
                colors.Add(new Color(0.92f, 1f, 0f, 1f));
                colors.Add(new Color(0.62f, 1f, 0f, 1f));
                colors.Add(new Color(0.30f, 1f, 0f, 1f));

                for (int p = 0; p < profileCount; p++)
                {
                    uvs.Add(new Vector2(u * 12f, (float)p / (profileCount - 1)));
                }
            }

            for (int c = 0; c < segX; c++)
            {
                int baseC = c * profileCount;
                int nextC = (c + 1) * profileCount;
                for (int p = 0; p < profileCount - 1; p++)
                {
                    int i0 = baseC + p;
                    int i1 = baseC + p + 1;
                    int i2 = nextC + p;
                    int i3 = nextC + p + 1;
                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // =========================================================================================
        // HELPER: SPAWN PREFAB WITH GUARANTEED SPRITE BINDING, ATMOSPHERIC TINT & SWAY
        // =========================================================================================
        private static GameObject SpawnSpritePrefab(
            string prefabPath, string name, Transform parent,
            Vector3 pos, float uniformScale, bool flipX,
            Color baseColor, int sortingOrder, Material overrideMat,
            float swayAngle = -1f, float swaySpeed = 1.4f, bool isRiceField = false)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pf == null) return null;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            inst.name = name;
            inst.transform.position = pos;
            inst.transform.localScale = new Vector3(flipX ? -uniformScale : uniformScale, uniformScale, 1.0f);

            string baseName = Path.GetFileNameWithoutExtension(prefabPath);
            string texKey = baseName;
            if (texKey.StartsWith("PF_T_HD_")) texKey = texKey.Substring(3);
            else if (texKey.StartsWith("PF_LanaVN_")) texKey = "T_LanaVN_" + texKey.Substring(10);

            _spriteCache.TryGetValue(texKey, out Sprite fallbackSpr);

            Color finalColor = GetAtmosphericTint(pos.z, baseColor, isRiceField);

            foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null && fallbackSpr != null)
                {
                    sr.sprite = fallbackSpr;
                }
                if (overrideMat != null) sr.sharedMaterial = overrideMat;
                sr.color = finalColor;
                sr.sortingOrder = sortingOrder;
            }

            if (swayAngle >= 0f)
            {
                var sway = inst.GetComponent<PlanetOfLanaFoliageSway>();
                if (sway == null && swayAngle > 0.01f) sway = inst.AddComponent<PlanetOfLanaFoliageSway>();
                if (sway != null)
                {
                    if (swayAngle <= 0.01f)
                    {
                        sway.enabled = false;
                    }
                    else
                    {
                        var so = new SerializedObject(sway);
                        so.FindProperty("maxRotationAngle").floatValue = swayAngle;
                        so.FindProperty("swaySpeed").floatValue = swaySpeed;
                        so.FindProperty("useWorldPositionOffset").boolValue = true;
                        so.FindProperty("manualPhaseOffset").floatValue = pos.x * 0.37f + pos.z * 0.53f;
                        so.FindProperty("harmonicNoise").floatValue = 0.35f;
                        so.FindProperty("scaleBreathing").floatValue = 0.010f;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            return inst;
        }

        // =========================================================================================
        // ZONE 1 (-8m to 16m): TRUE ENCLOSED DEEP TROPICAL FOREST (DOMINANT BANYAN GATE & GOD RAYS)
        // =========================================================================================
        private static void BuildZone1_DeepForest25D(Transform ecoRoot, Transform fgRoot, Material spriteMat)
        {
            var forestGroup = new GameObject("01_DeepForest_MultiLayer_25D");
            forestGroup.transform.SetParent(ecoRoot, false);

            var treesGroup = ecoRoot.Find("HandDrawn_HeroTrees") ?? forestGroup.transform;

            string pTreeLarge   = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab";
            string pTreeMed     = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Medium.prefab";
            string pTreeSmall   = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_03_SmallBushTree.prefab";
            string pTreeTall    = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_04_SlenderTall.prefab";
            string pTreeBanana  = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab";
            string pTreeBamboo  = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab";

            string pRockCliff   = "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_01_LargeCliffOutcrop.prefab";
            string pRockDome    = "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_02_MediumMossyDome.prefab";
            string pRockCorner  = "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_03_MediumMossyCorner.prefab";
            string pRockSlab    = "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_04_FlatSlab.prefab";
            string pBushBroad1  = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab";
            string pBushBroad2  = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_02.prefab";
            string pBushFlower  = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_02_Flower.prefab";
            string pBushLow3    = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_03.prefab";

            Color vibrantHero   = Color.white;

            // --- 1. CLUSTER A: THE MONUMENTAL BANYAN FOREST GATE (X = -4.5m .. 2.0m) ---
            // Mother tree + child tree + arching canopy forming a dense canopy tunnel!
            SpawnSpritePrefab(pTreeLarge,  "Forest_Gate_MotherBanyan", treesGroup, new Vector3(-2.8f, SampleY(-2.8f) - 0.08f, 1.85f), 1.22f, false, vibrantHero, 10, spriteMat, 0.8f, 0.95f);
            SpawnSpritePrefab(pTreeMed,    "Forest_Gate_ChildTree",    treesGroup, new Vector3( 1.5f, SampleY( 1.5f) - 0.06f, 2.35f), 0.92f, true,  vibrantHero, 9,  spriteMat, 1.0f, 1.10f);
            SpawnSpritePrefab(pTreeBanana, "Forest_Gate_BananaUnder",  treesGroup, new Vector3(-1.2f, SampleY(-1.2f) - 0.05f, 1.65f), 0.90f, false, vibrantHero, 11, spriteMat, 1.6f, 1.35f);
            SpawnSpritePrefab(pRockCliff,  "Forest_Gate_CliffRock",    forestGroup.transform, new Vector3(-3.8f, SampleY(-3.8f) - 0.08f, 1.55f), 0.52f, false, vibrantHero, 12, spriteMat, 0f);

            // Midground trees: firmly anchored with root bases sunk below the foreground grassy ridge (Y = SampleY(x) - 0.28m)
            // Tinted with 45%-55% atmospheric haze so they recede deeply into the background!
            SpawnSpritePrefab(pTreeLarge,  "Forest_Deep_Canopy_Back1", forestGroup.transform, new Vector3(-6.5f, SampleY(-6.5f) - 0.28f, 5.5f), 1.25f, false, vibrantHero, 4, spriteMat, 0.7f, 0.85f);
            SpawnSpritePrefab(pTreeLarge,  "Forest_Deep_Canopy_Back2", forestGroup.transform, new Vector3( 0.5f, SampleY( 0.5f) - 0.28f, 5.2f), 1.20f, true,  vibrantHero, 4, spriteMat, 0.7f, 0.85f);
            SpawnSpritePrefab(pTreeTall,   "Forest_Deep_Slender_Back3",forestGroup.transform, new Vector3( 4.8f, SampleY( 4.8f) - 0.95f, 5.6f), 1.15f, false, vibrantHero, 5, spriteMat, 0.9f, 0.95f);
            SpawnSpritePrefab(pTreeLarge,  "Forest_Deep_Canopy_Back4", forestGroup.transform, new Vector3( 9.2f, SampleY( 9.2f) - 0.28f, 5.2f), 1.18f, false, vibrantHero, 4, spriteMat, 0.7f, 0.85f);

            // --- 2. CLUSTER B: MOSSY KNOLL & BAMBOO GROVE (X = 6.0m .. 11.5m) ---
            SpawnSpritePrefab(pTreeBamboo, "Forest_Knoll_BambooGrove", forestGroup.transform, new Vector3( 6.8f, SampleY( 6.8f) - 0.06f, 2.75f), 1.05f, false, vibrantHero, 8, spriteMat, 1.4f, 1.25f);
            SpawnSpritePrefab(pRockDome,   "Forest_Knoll_MossyDome",   forestGroup.transform, new Vector3( 7.6f, SampleY( 7.6f) - 0.05f, 1.35f), 0.48f, false, vibrantHero, 12, spriteMat, 0f);
            SpawnSpritePrefab(pBushBroad2, "Forest_Knoll_Broadleaf",   forestGroup.transform, new Vector3( 8.5f, SampleY( 8.5f) - 0.04f, 1.25f), 0.42f, false, vibrantHero, 13, spriteMat, 1.6f, 1.4f);

            // --- 3. CLUSTER C: FOREST EXIT & TRANSITION THICKET (X = 14.0m .. 17.5m) ---
            SpawnSpritePrefab(pTreeBanana, "Forest_Exit_BananaThicket",treesGroup, new Vector3(15.2f, SampleY(15.2f) - 0.05f, 2.10f), 0.95f, false, vibrantHero, 10, spriteMat, 1.5f, 1.3f);
            SpawnSpritePrefab(pTreeSmall,  "Forest_Exit_BushTree",     forestGroup.transform, new Vector3(16.8f, SampleY(16.8f) - 0.05f, 2.85f), 0.92f, true,  vibrantHero, 8,  spriteMat, 1.3f, 1.2f);
            SpawnSpritePrefab(pRockSlab,   "Forest_Exit_SteppingSlab", forestGroup.transform, new Vector3(14.2f, SampleY(14.2f) - 0.04f, 0.95f), 0.44f, false, vibrantHero, 14, spriteMat, 0f);

            // --- 4. FOREGROUND NATURAL VIGNETTE FRAMING ---
            var fgForest = new GameObject("FG_Forest_NaturalVignette");
            fgForest.transform.SetParent(fgRoot, false);

            // Natural arching leafy boughs dipping gently into upper corners (organic leaf tips only, zero flat cuts)
            SpawnSpritePrefab(pBushBroad1, "FG_Canopy_Arch_TopLeft",  fgForest.transform, new Vector3(-2.0f, 3.80f, -2.5f), 0.65f, false, new Color(0.08f, 0.12f, 0.11f, 0.98f), 31, spriteMat, 0.6f, 0.8f);
            SpawnSpritePrefab(pBushBroad2, "FG_Canopy_Arch_TopRight", fgForest.transform, new Vector3( 4.5f, 3.85f, -2.5f), 0.62f, true,  new Color(0.08f, 0.12f, 0.11f, 0.98f), 31, spriteMat, 0.6f, 0.8f);

            // Ground level low silhouette
            SpawnSpritePrefab(pBushLow3,   "FG_Silhouette_Bush_Bottom",fgForest.transform, new Vector3(-1.5f, -2.55f, -2.8f), 0.42f, false, new Color(0.08f, 0.12f, 0.11f, 0.98f), 30, spriteMat, 1.2f, 1.1f);
            SpawnSpritePrefab(pRockDome,   "FG_Silhouette_Rock_Bottom",fgForest.transform, new Vector3( 8.5f, -2.58f, -2.8f), 0.40f, true,  new Color(0.08f, 0.12f, 0.11f, 0.98f), 30, spriteMat, 0f);
        }

        // =========================================================================================
        // ZONE 2 (16m to 30m): RURAL GARDEN & ISOLATED WALK-THROUGH GOURD TRELLIS (BREATHING ROOM)
        // =========================================================================================
        private static void BuildZone2_ForestGateAndGourdTrellis25D(Transform ecoRoot, Transform fgRoot, Material spriteMat)
        {
            var zone2Group = new GameObject("02_ForestGate_And_GourdTrellis_25D");
            zone2Group.transform.SetParent(ecoRoot, false);

            var treesGroup = ecoRoot.Find("HandDrawn_HeroTrees") ?? zone2Group.transform;

            string pTreeBanana    = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab";
            string pTreeCoconut   = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab";
            string pTreeBamboo    = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab";

            string pTrellisMain   = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Trellises/T_HD_GourdTrellis_01_Main.prefab";
            string pGourdLong     = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_01_LongBottle.prefab";
            string pGourdPear     = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_02_PearCalabash.prefab";
            string pGourdSmall    = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_03_Small.prefab";
            string pGourdHalf     = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_04_HalfCut.prefab";
            string pGourdPumpkin  = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_05_GreenPumpkin.prefab";

            string pFlowerBloom   = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Flowers/T_HD_GourdFlower_01_Bloom.prefab";
            string pFlowerBud     = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Flowers/T_HD_GourdFlower_02_Bud.prefab";
            string pBranchCluster = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Foliage/T_HD_GourdBranch_Cluster.prefab";
            string pTendril1      = "Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/Foliage/T_HD_GourdTendril_01.prefab";

            string pRockSlab      = "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_04_FlatSlab.prefab";
            string pBushFlower    = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_02_Flower.prefab";

            Color sunlitWarm = Color.white;

            // 1. BOUNDARY CLUSTER: FOREST EXIT GATEWAY (X = 17.5m .. 19.5m)
            SpawnSpritePrefab(pTreeBamboo, "Garden_West_BambooGrove", zone2Group.transform, new Vector3(18.2f, SampleY(18.2f) - 0.06f, 3.20f), 0.98f, false, sunlitWarm, 7, spriteMat, 1.4f, 1.3f);
            SpawnSpritePrefab(pTreeBanana, "Garden_West_BananaFrond",  treesGroup, new Vector3(19.2f, SampleY(19.2f) - 0.05f, 2.10f), 0.92f, false, sunlitWarm, 10, spriteMat, 1.6f, 1.35f);

            // 2. THE HERO STAR: ISOLATED WALK-THROUGH 2.5D GOURD TRELLIS (X = 23.5m, Y = -1.55m)
            // Notice: Behind the trellis (Z = 2m..8m), there are NO tall trees! The eye has complete breathing room!
            var trellis25D = new GameObject("GourdTrellis_25D_HeroCenterpiece");
            trellis25D.transform.SetParent(zone2Group.transform, false);
            float tx = 23.5f;
            float ty = SampleY(tx); // -1.55m

            // Main Trellis Frame at Z = +0.55m (SortingOrder 14)
            SpawnSpritePrefab(pTrellisMain, "Trellis_MainFrame_Back", trellis25D.transform,
                new Vector3(tx, ty - 0.04f, 0.55f), 0.82f, false, sunlitWarm, 14, spriteMat, 0f);

            // Overhead Hanging Gourds & Yellow Blossoms (Z = +0.22m, SortingOrder 22 -> in front of player at Z = 0m, SortingOrder 15)
            // Fully vibrant sunlit colors, player walks visually under and behind the lush gourds!
            SpawnSpritePrefab(pBranchCluster, "Trellis_FG_OverheadVine_Left",  trellis25D.transform,
                new Vector3(tx - 0.95f, ty + 2.05f, 0.22f), 0.44f, false, sunlitWarm, 21, spriteMat, 2.0f, 1.5f);
            SpawnSpritePrefab(pBranchCluster, "Trellis_FG_OverheadVine_Right", trellis25D.transform,
                new Vector3(tx + 0.90f, ty + 2.08f, 0.22f), 0.44f, true,  sunlitWarm, 21, spriteMat, 2.0f, 1.5f);

            SpawnSpritePrefab(pGourdLong, "Trellis_FG_HangingGourd_Long1", trellis25D.transform,
                new Vector3(tx - 0.55f, ty + 1.75f, 0.25f), 0.25f, false, sunlitWarm, 22, spriteMat, 2.6f, 1.45f);
            SpawnSpritePrefab(pGourdPear, "Trellis_FG_HangingGourd_Pear2", trellis25D.transform,
                new Vector3(tx + 0.15f, ty + 1.85f, 0.25f), 0.28f, false, sunlitWarm, 22, spriteMat, 2.8f, 1.60f);
            SpawnSpritePrefab(pGourdSmall,"Trellis_FG_HangingGourd_Small3",trellis25D.transform,
                new Vector3(tx + 0.72f, ty + 1.82f, 0.25f), 0.26f, true,  sunlitWarm, 22, spriteMat, 3.0f, 1.75f);

            SpawnSpritePrefab(pFlowerBloom, "Trellis_FG_YellowFlower_1", trellis25D.transform,
                new Vector3(tx - 0.25f, ty + 2.10f, 0.20f), 0.26f, false, Color.white, 23, spriteMat, 2.4f, 1.8f);
            SpawnSpritePrefab(pFlowerBloom, "Trellis_FG_YellowFlower_2", trellis25D.transform,
                new Vector3(tx + 0.52f, ty + 2.14f, 0.20f), 0.25f, true,  Color.white, 23, spriteMat, 2.4f, 1.8f);
            SpawnSpritePrefab(pFlowerBud,   "Trellis_FG_YellowBud_3",    trellis25D.transform,
                new Vector3(tx + 0.95f, ty + 2.00f, 0.20f), 0.25f, false, Color.white, 23, spriteMat, 2.4f, 1.8f);
            SpawnSpritePrefab(pTendril1,    "Trellis_FG_Tendril_1",      trellis25D.transform,
                new Vector3(tx - 0.95f, ty + 1.90f, 0.22f), 0.28f, false, sunlitWarm, 22, spriteMat, 3.2f, 2.0f);

            // Ground Level: Authentic garden pumpkins, half-cut gourd & stepping stones at post base
            SpawnSpritePrefab(pGourdPumpkin,  "Trellis_GreenPumpkin_1",    trellis25D.transform,
                new Vector3(tx + 1.35f, ty - 0.02f, -1.18f), 0.36f, false, sunlitWarm, 19, spriteMat, 0f);
            SpawnSpritePrefab(pGourdHalf,     "Trellis_HalfCutGourd_2",    trellis25D.transform,
                new Vector3(tx + 1.65f, ty - 0.03f, -1.24f), 0.30f, false, sunlitWarm, 20, spriteMat, 0f);
            SpawnSpritePrefab(pRockSlab,      "Trellis_GardenSteppingSlab",trellis25D.transform,
                new Vector3(tx + 1.40f, ty - 0.05f, -1.05f), 0.38f, false, sunlitWarm, 17, spriteMat, 0f);
            SpawnSpritePrefab(pBushFlower,    "Trellis_GardenFlowerBush",  trellis25D.transform,
                new Vector3(tx - 1.85f, ty - 0.04f, 1.15f), 0.34f, false, sunlitWarm, 13, spriteMat, 1.6f, 1.5f);

            // 3. BOUNDARY CLUSTER: WEST BRIDGE APPROACH (X = 28.5m .. 30.5m)
            // One tall leaning coconut palm + child banana tree hugging the canal edge
            SpawnSpritePrefab(pTreeCoconut, "Garden_East_CoconutPalm", treesGroup, new Vector3(30.2f, SampleY(30.2f) - 0.06f, 2.35f), 1.05f, false, sunlitWarm, 9,  spriteMat, 1.4f, 1.20f);
            SpawnSpritePrefab(pTreeBanana,  "Garden_East_BananaTree",  treesGroup, new Vector3(28.6f, SampleY(28.6f) - 0.05f, 1.75f), 0.88f, false, sunlitWarm, 10, spriteMat, 1.6f, 1.35f);
        }

        // =========================================================================================
        // ZONE 3 (30m to 42.5m): ARCHED WOODEN BRIDGE, WIDE CANAL & GROUNDED FLOATING SAMPAN
        // =========================================================================================
        private static void BuildZone3_CanalAndSampan25D(Transform ecoRoot, Material spriteMat)
        {
            var zone3Group = new GameObject("03_CanalBridge_And_Sampan_25D");
            zone3Group.transform.SetParent(ecoRoot, false);

            string pReeds      = "Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_02_Reeds.prefab";
            string pSampanHat  = "Assets/Prefabs/Boats/PF_Sampan_Open_Hat_Floating.prefab";
            string pWaterLily  = "Assets/Prefabs/Boats/PF_WaterLily_Cluster.prefab";
            string pTreeCoconut= "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab";
            string pTreeBamboo = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab";

            Color dayTint = Color.white;

            // 1. Lush River Reeds & Bamboo groves hugging both Bridge Abutments (West X=30.5m & East X=43.5m)
            // Leading the player's eye naturally onto the bridge, NEVER floating in the river!
            SpawnSpritePrefab(pTreeBamboo, "Canal_WestAbutment_Bamboo", zone3Group.transform, new Vector3(30.5f, SampleY(30.5f) - 0.06f, 2.20f), 0.95f, false, dayTint, 9, spriteMat, 1.4f, 1.2f);
            SpawnSpritePrefab(pReeds,      "Canal_WestBank_Reeds_1",     zone3Group.transform, new Vector3(31.4f, -1.95f, 1.15f), 0.40f, false, dayTint, 14, spriteMat, 2.6f, 1.9f);
            SpawnSpritePrefab(pReeds,      "Canal_WestBank_Reeds_2",     zone3Group.transform, new Vector3(32.0f, -2.05f, 1.45f), 0.36f, true,  dayTint, 13, spriteMat, 2.6f, 1.9f);

            SpawnSpritePrefab(pTreeBamboo, "Canal_EastAbutment_Bamboo", zone3Group.transform, new Vector3(43.5f, SampleY(43.5f) - 0.06f, 2.20f), 0.95f, true,  dayTint, 9, spriteMat, 1.4f, 1.2f);
            SpawnSpritePrefab(pReeds,      "Canal_EastBank_Reeds_3",     zone3Group.transform, new Vector3(42.0f, -2.00f, 1.20f), 0.42f, false, dayTint, 14, spriteMat, 2.6f, 1.9f);
            SpawnSpritePrefab(pReeds,      "Canal_EastBank_Reeds_4",     zone3Group.transform, new Vector3(42.8f, -1.90f, 1.40f), 0.38f, true,  dayTint, 13, spriteMat, 2.6f, 1.9f);

            // 2. Traditional Vietnamese Sampan with Conical Hat (Floating naturally in open water, Z = 2.40m, Y = -2.10m)
            // Clearly visible in open bay between pilings, keel submerged in glistening water
            var sampanPf = AssetDatabase.LoadAssetAtPath<GameObject>(pSampanHat);
            if (sampanPf != null)
            {
                var sampan = (GameObject)PrefabUtility.InstantiatePrefab(sampanPf, zone3Group.transform);
                sampan.name = "Canal_Floating_Sampan_Main";
                sampan.transform.position = new Vector3(34.8f, -2.10f, 2.40f);
                sampan.transform.localScale = new Vector3(1.18f, 1.18f, 1.0f);
                foreach (var sr in sampan.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (spriteMat != null) sr.sharedMaterial = spriteMat;
                    sr.color = GetAtmosphericTint(2.40f, Color.white);
                    sr.sortingOrder = 13;
                }

                // Moored secondary canoe near the west bank reeds
                var sampan2 = (GameObject)PrefabUtility.InstantiatePrefab(sampanPf, zone3Group.transform);
                sampan2.name = "Canal_Floating_Sampan_WestBank";
                sampan2.transform.position = new Vector3(32.0f, -2.12f, 1.65f);
                sampan2.transform.localScale = new Vector3(-0.78f, 0.78f, 1.0f);
                foreach (var sr in sampan2.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (spriteMat != null) sr.sharedMaterial = spriteMat;
                    sr.color = GetAtmosphericTint(1.65f, Color.white);
                    sr.sortingOrder = 12;
                }
            }

            // 3. Water Lily Clusters floating across the wide canal surface
            var lilyPf = AssetDatabase.LoadAssetAtPath<GameObject>(pWaterLily);
            if (lilyPf != null)
            {
                var lily1 = (GameObject)PrefabUtility.InstantiatePrefab(lilyPf, zone3Group.transform);
                lily1.name = "Canal_WaterLilies_West";
                lily1.transform.position = new Vector3(36.2f, -2.16f, 1.85f);
                lily1.transform.localScale = Vector3.one * 0.70f;
                foreach (var sr in lily1.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (spriteMat != null) sr.sharedMaterial = spriteMat;
                    sr.color = GetAtmosphericTint(1.85f, Color.white);
                    sr.sortingOrder = 11;
                }

                var lily2 = (GameObject)PrefabUtility.InstantiatePrefab(lilyPf, zone3Group.transform);
                lily2.name = "Canal_WaterLilies_East";
                lily2.transform.position = new Vector3(39.5f, -2.16f, 2.20f);
                lily2.transform.localScale = Vector3.one * 0.65f;
                foreach (var sr in lily2.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (spriteMat != null) sr.sharedMaterial = spriteMat;
                    sr.color = GetAtmosphericTint(2.20f, Color.white);
                    sr.sortingOrder = 11;
                }
            }

            // 4. Shoreline River Reeds along Far Canal Bank (Z = 5.50m, Y = -1.82m)
            SpawnSpritePrefab(pReeds, "Canal_FarBank_Reeds_1", zone3Group.transform, new Vector3(33.5f, -1.82f, 5.50f), 0.35f, false, dayTint, 8, spriteMat, 2.0f, 1.5f);
            SpawnSpritePrefab(pReeds, "Canal_FarBank_Reeds_2", zone3Group.transform, new Vector3(37.5f, -1.82f, 5.50f), 0.38f, true,  dayTint, 8, spriteMat, 2.0f, 1.5f);
            SpawnSpritePrefab(pReeds, "Canal_FarBank_Reeds_3", zone3Group.transform, new Vector3(40.5f, -1.82f, 5.50f), 0.34f, false, dayTint, 8, spriteMat, 2.0f, 1.5f);

            // 5. Distant Bamboo Groves & Coconut Palm grounded solidly ON Far Canal Bank (Z = 5.8m..6.2m, Y = -1.82m)
            // Tinted with 50-60% atmospheric haze, creating a true sense of a wide river crossing!
            SpawnSpritePrefab(pTreeBamboo,  "Canal_FarBank_BambooGrove",  zone3Group.transform, new Vector3(36.5f, -1.82f, 6.20f), 0.95f, true,  dayTint, 6, spriteMat, 1.2f, 1.1f);
            SpawnSpritePrefab(pTreeCoconut, "Canal_FarBank_CoconutPalm",  zone3Group.transform, new Vector3(41.8f, -1.82f, 5.80f), 0.92f, false, dayTint, 7, spriteMat, 1.3f, 1.2f);
        }

        // =========================================================================================
        // ZONE 4 (42.5m to 62m): VAST CONTINUOUS 2.5D RICE PADDY FIELD SEA (FREEDOM OF SIGHT)
        // =========================================================================================
        private static void BuildZone4_VastRicePaddyField25D(Transform ecoRoot, Transform fgRoot, Material spriteMat)
        {
            var riceFieldRoot = new GameObject("04_VastRicePaddyField_MultiLayer_25D");
            riceFieldRoot.transform.SetParent(ecoRoot, false);

            var treesGroup = ecoRoot.Find("HandDrawn_HeroTrees") ?? riceFieldRoot.transform;

            GameObject pfRiceRow4m     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_RowStrip_4m.prefab");
            GameObject pfRiceCluster3  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_Cluster_3in1.prefab");
            GameObject pfRiceMature    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_RiceClump_Mature.prefab");

            string pTreeLarge     = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab";
            string pTreeCoconut   = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab";
            string pTreeBamboo    = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab";
            string pGrassWind     = "Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_03_Windblown.prefab";
            string pReeds         = "Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_02_Reeds.prefab";

            // 1. FLOODED PADDY FIELD WATER MIRROR (Sunken distinctly at Y = -2.00m, 55cm below elevated dike walkway!)
            var paddyWaterGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddyWaterGO.name = "RicePaddy_FloodedWaterMirror_3D";
            paddyWaterGO.transform.SetParent(riceFieldRoot.transform, false);
            paddyWaterGO.transform.position = new Vector3(53.0f, -2.00f, 4.20f);
            paddyWaterGO.transform.localScale = new Vector3(22.0f, 0.06f, 6.20f);
            Object.DestroyImmediate(paddyWaterGO.GetComponent<Collider>());

            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_RiverWater_Canh1.mat")
                             ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_StylizedWater_Sunset.mat");
            if (waterMat != null) paddyWaterGO.GetComponent<MeshRenderer>().sharedMaterial = waterMat;

            // 2. CONTINUOUS MULTI-LAYER 2.5D RICE PADDY SEA (5 TIGHTLY STAGGERED DEPTH TIERS)
            var riceRowsGroup = new GameObject("RicePaddy_StaggeredRows_25D");
            riceRowsGroup.transform.SetParent(riceFieldRoot.transform, false);

            float[] tierZs      = { 1.35f, 2.10f, 3.00f, 4.10f, 5.25f };
            float[] tierScales  = { 1.05f, 1.00f, 0.94f, 0.88f, 0.82f };
            float[] tierYOffset = { -0.22f, -0.14f, -0.02f, 0.10f, 0.22f };

            if (pfRiceRow4m != null)
            {
                for (int t = 0; t < tierZs.Length; t++)
                {
                    float z = tierZs[t];
                    float sc = tierScales[t];

                    for (float rx = 42.4f; rx <= 58.8f; rx += 2.05f)
                    {
                        float jitterX = Mathf.Sin(rx * 1.7f + t * 2.3f) * 0.15f;
                        float jitterZ = Mathf.Cos(rx * 2.1f + t * 1.3f) * 0.08f;
                        float worldX = rx + jitterX;
                        float baseSurfY = SampleY(worldX) + tierYOffset[t];

                        var rowInst = (GameObject)PrefabUtility.InstantiatePrefab(pfRiceRow4m, riceRowsGroup.transform);
                        rowInst.name = $"RiceRow_Tier{t + 1}_X{worldX:F0}";
                        rowInst.transform.position = new Vector3(worldX, baseSurfY, z + jitterZ);
                        rowInst.transform.localRotation = Quaternion.Euler(0f, (t % 2 == 0) ? 2.0f : -2.0f, 0f);
                        rowInst.transform.localScale = new Vector3(sc * 1.05f, sc * 1.02f, sc);

                        // Apply atmospheric haze per tier
                        foreach (var sr in rowInst.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            sr.color = GetAtmosphericTint(z, sr.color, isRiceField: true);
                        }
                    }
                }
            }

            // Lush Rice Clumps along the edge of the elevated dike walkway (Z = 1.15m)
            if (pfRiceCluster3 != null)
            {
                for (float vx = 42.8f; vx <= 58.2f; vx += 1.25f)
                {
                    float vy = SampleY(vx) - 0.05f;
                    GameObject srcPf = (pfRiceMature ?? pfRiceCluster3);
                    var cl = (GameObject)PrefabUtility.InstantiatePrefab(srcPf, riceRowsGroup.transform);
                    cl.name = $"DikeEdge_RiceCluster_X{vx:F1}";
                    cl.transform.position = new Vector3(vx, vy, 1.15f + Mathf.Sin(vx * 3f) * 0.06f);
                    cl.transform.localScale = Vector3.one * 0.90f;
                    foreach (var sr in cl.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.color = GetAtmosphericTint(1.15f, sr.color, isRiceField: true);
                    }
                }
            }

            // 3. FOREGROUND PARALLAX RICE BLADES SLIGHTLY OVERLAPPING MINH'S ANKLES (Z = -0.35m, SortingOrder 25)
            var fgRiceBlades = new GameObject("FG_Rice_AnkleBlades");
            fgRiceBlades.transform.SetParent(riceFieldRoot.transform, false);
            for (float fx = 45.0f; fx <= 56.0f; fx += 3.2f)
            {
                SpawnSpritePrefab(pGrassWind, $"FG_RiceStalk_Ankle_{fx:F0}", fgRiceBlades.transform,
                    new Vector3(fx, SampleY(fx) - 0.32f, -0.35f), 0.30f, (fx > 50f), new Color(0.92f, 0.88f, 0.70f, 0.95f), 25, spriteMat, 2.4f, 1.8f, true);
            }

            // 4. DISTANT COCONUT PALMS & BAMBOO PUSHED FAR TO THE HORIZON (Z = 9.0m..12.5m, Scale 0.48..0.58, 75% Warm Haze!)
            // Grounded securely on the horizon ridge, giving immense depth and freedom to the golden rice sea!
            var distantGroves = new GameObject("FarHorizon_RiceFieldGroves");
            distantGroves.transform.SetParent(riceFieldRoot.transform, false);

            Color farGroveBase = Color.white;
            SpawnSpritePrefab(pTreeCoconut, "RiceField_Horizon_Coconut_1", distantGroves.transform, new Vector3(45.5f, -1.22f,  9.50f), 0.58f, false, farGroveBase, 5, spriteMat, 1.1f, 1.0f, true);
            SpawnSpritePrefab(pTreeCoconut, "RiceField_Horizon_Coconut_2", distantGroves.transform, new Vector3(48.5f, -1.26f, 11.20f), 0.52f, true,  farGroveBase, 4, spriteMat, 1.1f, 1.0f, true);
            SpawnSpritePrefab(pTreeBamboo,  "RiceField_Horizon_Bamboo_3",  distantGroves.transform, new Vector3(52.5f, -1.24f, 10.50f), 0.56f, false, farGroveBase, 5, spriteMat, 1.1f, 1.0f, true);
            SpawnSpritePrefab(pTreeCoconut, "RiceField_Horizon_Coconut_4", distantGroves.transform, new Vector3(55.2f, -1.28f, 12.00f), 0.48f, false, farGroveBase, 4, spriteMat, 1.1f, 1.0f, true);

            // 5. CROWNING GOAL LANDMARK TREE (One monumental ancient banyan crowning level end at X = 58.5m!)
            SpawnSpritePrefab(pTreeLarge,   "ZoneF_GoalLandmark_AncientBanyan", treesGroup, new Vector3(58.5f, SampleY(58.5f) - 0.08f, 2.35f), 1.25f, false, new Color(1.0f, 0.96f, 0.85f, 1.0f), 10, spriteMat, 0.8f, 0.95f, true);
            SpawnSpritePrefab(pReeds,       "ZoneF_GoalLandmark_Reeds",         riceFieldRoot.transform, new Vector3(56.2f, SampleY(56.2f) - 0.04f, 1.35f), 0.40f, false, Color.white, 14, spriteMat, 2.6f, 1.8f, true);
        }

        // =========================================================================================
        // UNIFIED NATIVE HD GRASS CARPETS, WILDFLOWERS & PEBBLES ALONG PATHWAY
        // =========================================================================================
        private static void BuildUnifiedNativeHDPathDetails(Transform ecoRoot, Material spriteMat)
        {
            var detailsGroup = new GameObject("05_NativeHD_Flora_And_Rocks_25D");
            detailsGroup.transform.SetParent(ecoRoot, false);

            string[] grassCarpets =
            {
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_01_Short.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_02_TallMeadow.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_03_Windblown.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_04_Stepping.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_05_DensePatch.prefab"
            };

            string[] wildflowers =
            {
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_01_GrassClump.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_03_WhiteDaisy.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_04_YellowBloom.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_05_TallSprig.prefab"
            };

            string[] pebbles =
            {
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_05_SmallMossy.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_06_PebbleCluster.prefab",
                "Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_07_SmoothPebbleGroup.prefab"
            };

            System.Random rnd = new System.Random(2026);
            int idx = 0;

            for (float x = -5.0f; x <= 61.5f; x += 2.4f)
            {
                if (x >= 31.0f && x <= 42.8f) continue; // Skip canal bridge

                bool isRice = (x >= 42.5f);
                float goldT = Mathf.InverseLerp(38.0f, 48.0f, x);
                Color carpetTint = Color.Lerp(new Color(0.88f, 0.98f, 0.82f, 1f), new Color(0.98f, 0.92f, 0.72f, 1f), goldT);

                // 1. Native HD Grass Carpet along back meadow verge (Z = 1.08m..1.25m)
                string gPath = grassCarpets[idx % grassCarpets.Length];
                float gx = x + ((float)rnd.NextDouble() - 0.5f) * 0.45f;
                float gy = SampleY(gx) - 0.04f;
                SpawnSpritePrefab(gPath, $"HD_GrassCarpet_Back_{idx:D2}", detailsGroup.transform,
                    new Vector3(gx, gy, 1.12f), 0.34f, (idx % 2 == 1), carpetTint, 13, spriteMat, 2.2f, 1.8f, isRice);

                // 2. Native HD Wildflower sprig every 2nd step (in forest & garden)
                if (idx % 2 == 0 && !isRice)
                {
                    string wPath = wildflowers[(idx / 2) % wildflowers.Length];
                    float wx = x + 0.65f;
                    float wy = SampleY(wx) - 0.03f;
                    SpawnSpritePrefab(wPath, $"HD_Wildflower_{idx:D2}", detailsGroup.transform,
                        new Vector3(wx, wy, -1.32f), 0.25f, (idx % 4 == 0), Color.white, 19, spriteMat, 2.6f, 2.0f);
                }

                // 3. Native HD Mossy Pebbles every 3rd step (in forest only)
                if (idx % 3 == 0 && x < 18.0f)
                {
                    string rPath = pebbles[(idx / 3) % pebbles.Length];
                    float rx = x - 0.55f;
                    float ry = SampleY(rx) - 0.04f;
                    SpawnSpritePrefab(rPath, $"HD_Pebble_{idx:D2}", detailsGroup.transform,
                        new Vector3(rx, ry, -1.25f), 0.30f, (idx % 2 == 0), carpetTint, 18, spriteMat, 0f);
                }

                idx++;
            }
        }

        // =========================================================================================
        // VOLUMETRIC SUNBEAMS & DAPPLED SHADOWS
        // =========================================================================================
        private static void SetupSunbeamsAndDappledShadows(Transform ecoRoot)
        {
            var vfxRoot = GameObject.Find("09_VFX") ?? new GameObject("09_VFX");
            DestroyChildIfExists(vfxRoot.transform, "Lana_CloudRadiance_And_Sunbeams");

            var radianceGroup = new GameObject("Lana_CloudRadiance_And_Sunbeams");
            radianceGroup.transform.SetParent(vfxRoot.transform, false);

            Shader godRaysShader = Shader.Find("HoaVaMau/StylizedGodRays");
            if (godRaysShader != null)
            {
                string rayMatPath = $"{GenAssetsDir}/M_Lana_CloudSunbeams.mat";
                var rayMat = AssetDatabase.LoadAssetAtPath<Material>(rayMatPath);
                if (rayMat == null)
                {
                    rayMat = new Material(godRaysShader);
                    AssetDatabase.CreateAsset(rayMat, rayMatPath);
                }
                rayMat.shader = godRaysShader;
                rayMat.SetColor("_RayColor", new Color(1.0f, 0.98f, 0.86f, 0.28f));
                rayMat.SetFloat("_RayIntensity", 1.35f);
                rayMat.SetFloat("_ShimmerSpeed", 0.55f);
                rayMat.SetFloat("_FalloffPower", 1.75f);
                rayMat.SetFloat("_BeamCount", 5.0f);
                EditorUtility.SetDirty(rayMat);

                void SpawnBeam(string bName, Vector3 pos, float rotZ, Vector2 scale)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    q.name = bName;
                    q.transform.SetParent(radianceGroup.transform, false);
                    q.transform.position = pos;
                    q.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
                    q.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                    var mr = q.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = rayMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    Object.DestroyImmediate(q.GetComponent<Collider>());
                }

                // Dramatic angled god rays through the forest canopy (well centered, zero edge clipping)
                SpawnBeam("Sunbeams_ForestCanopy_ZoneA", new Vector3(1.2f, 2.8f, 3.8f), -20f, new Vector2(12.0f, 8.0f));
                SpawnBeam("Sunbeams_RicePaddy_ZoneEF",   new Vector3(50.5f, 3.5f, 6.5f), -16f, new Vector2(16.0f, 9.5f));
            }

            var dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/PF_Atmospheric_GoldenDust.prefab");
            if (dustPrefab != null)
            {
                float[] dustXs = { 0.5f, 23.5f, 48.0f };
                for (int i = 0; i < dustXs.Length; i++)
                {
                    var dust = (GameObject)PrefabUtility.InstantiatePrefab(dustPrefab, radianceGroup.transform);
                    dust.name = $"GoldenSunMotes_Zone_{i + 1}";
                    dust.transform.position = new Vector3(dustXs[i], 0.8f, -1.5f);
                }
            }
        }

        // =========================================================================================
        // CAPTURE 4 HIGH-RES SHOWCASE SCREENSHOTS ACROSS THE 60M JOURNEY
        // =========================================================================================
        public static void CaptureForestToRiceFieldScreenshots()
        {
            var cam = Camera.main;
            if (cam == null) return;

            string outDir = Path.Combine(Application.dataPath, "../Screenshots/Showcase_Presentation");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;
            float origFov = cam.fieldOfView;

            var player = GameObject.Find("PF_Player_Minh");
            Vector3 origPlayerPos = player != null ? player.transform.position : Vector3.zero;

            void SetPlayer(float x, float y)
            {
                if (player == null) return;
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = new Vector3(x, y, 0f);
                if (cc != null) cc.enabled = true;
            }

            void CaptureView(string fileName, Vector3 pos, float fov)
            {
                cam.transform.position = pos;
                cam.transform.rotation = Quaternion.identity;
                cam.fieldOfView = fov;

                int width = 1280;
                int height = 720;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = prevRt;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                byte[] bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                string fullPath = Path.Combine(outDir, fileName);
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log($"<color=#38bdf8>[ForestToRiceField60m]</color> Saved screenshot: {fileName}");
            }

            // 1. Zone 1 (X = 0.5m): Enclosed Deep Forest Entrance with Arching Canopy & God Rays
            // Camera Y = 0.72m lowers bottom underbelly to < 26% of screen while revealing full lush canopy
            SetPlayer(0.5f, SampleY(0.5f));
            CaptureView("Scene60m_01_DeepForest_Entrance.png", new Vector3(0.5f, 0.72f, -13.5f), 38f);

            // 2. Zone 2 (X = 23.5m): Isolated Walk-Through Gourd Trellis on Peaceful Garden Ground
            SetPlayer(23.5f, SampleY(23.5f));
            CaptureView("Scene60m_02_ForestGate_GourdTrellis25D.png", new Vector3(23.5f, 0.70f, -13.5f), 38f);

            // 3. Zone 3 (X = 36.8m): Arched Wooden Bridge, Wide Canal & Floating Sampan in Water
            SetPlayer(36.8f, SampleY(36.8f));
            CaptureView("Scene60m_03_CanalBridge_Sampan.png", new Vector3(36.8f, 0.58f, -13.5f), 38f);

            // 4. Zone 4 (X = 50.0m): Vast Continuous Golden Rice Sea with Distant Small Palms
            SetPlayer(50.0f, SampleY(50.0f));
            CaptureView("Scene60m_04_VastRicePaddyField_25D.png", new Vector3(50.0f, 0.70f, -13.5f), 38f);

            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = origPlayerPos;
                if (cc != null) cc.enabled = true;
            }

            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.fieldOfView = origFov;
        }
    }
}
#endif
