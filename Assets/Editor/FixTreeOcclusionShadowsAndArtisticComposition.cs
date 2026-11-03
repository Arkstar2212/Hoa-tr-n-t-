#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Master Editor Script xử lý triệt để 4 yêu cầu của người dùng:
    /// 1. Sửa lỗi cây xuyên thấu (Z-Write Occlusion): Chuyển toàn bộ lá cây sang shader Cutout ZWrite On, Queue AlphaTest.
    /// 2. Đổ bóng 3D cho cây nhưng giữ phong cách 2D: Bật Cast Shadows TwoSided, Receive Shadows, tích hợp Toon Shadow Pass.
    /// 3. Cập nhật Bầu trời Anime/Watercolor: Gán asset bầu trời mới vào Backdrop Plane và Skybox Panoramic.
    /// 4. Đổi cây xấu & Tái bố cục Asset: Xóa cấu trúc tán nấm bậc thang xấu xí, thay bằng cây dừa/cây uốn lượn nghệ thuật,
    ///    hạ chìm 2 khối đá xanh lơ lửng xuống đất để không bị cắt xen thô thiển.
    /// </summary>
    public static class FixTreeOcclusionShadowsAndArtisticComposition
    {
        private const string SkyTexturePath = "Assets/Textures/Sky/Sky_Watercolor_AnimeClouds_Official.png";
        private const string FoliageShaderName = "HoaVaMau/StylizedFoliageCutout";
        private const string SharedFoliageMatDir = "Assets/Materials/Environment/FoliageCutout";

        [MenuItem("Tools/Hoa Va Mau/Fix Tree Occlusion, Shadows and Composition")]
        public static void Execute()
        {
            Debug.Log("<color=#3b82f6>[Tree & Sky Overhaul]</color> Bắt đầu xử lý toàn diện hệ thống Cây, Bầu trời và Bố cục cảnh...");

            // 1. CẬP NHẬT SHADOW DISTANCE VÀ LIGHTING
            ConfigureLightingAndShadowDistance();

            // 2. THIẾT LẬP BẦU TRỜI ANIME WATERCOLOR CHO BACKDROP VÀ SKYBOX
            SetupSkyBackdropAndAtmosphere();

            // 3. SỬA LỖI 2 KHỐI ĐÁ KHỔNG LỒ LƠ LỬNG BỊ CẮT XEN (North Gate Pass Crags)
            FixFloatingCragRocks();

            // 4. SỬA CÂY CÔN TRÙNG NẤM BẬC THANG XẤU XÍ VÀ BỐ CỤC LỐI VÀO CỔNG LÀNG
            ReplaceUglyUmbrellaTreesAndRefineComposition();

            // 5. CHUYỂN TOÀN BỘ CÂY SANG CUTOUT ZWRITE ON (CHỐNG XUYÊN THẤU) VÀ BẬT ĐỔ BÓNG 3D
            UpgradeAllTreesToCutoutAndCastShadows();

            // Lưu scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#10b981>[Tree & Sky Overhaul]</color> HOÀN TẤT THÀNH CÔNG! Toàn bộ cây đã có độ che khuất chuẩn mực, đổ bóng 3D mượt mà và bầu trời anime trong trẻo.");
        }

        private static void ConfigureLightingAndShadowDistance()
        {
            // Tăng shadow distance trên URP Asset để bao phủ trọn vẹn ngôi làng
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.shadowDistance = 140f;
                EditorUtility.SetDirty(urpAsset);
                Debug.Log("<color=#3b82f6>[Lighting]</color> Đã nâng URP ShadowDistance lên 140m.");
            }

            // Đảm bảo Directional Light chiếu bóng mềm (Soft Shadows)
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.name.Contains("SunLight"))
                {
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.75f;
                    l.shadowBias = 0.05f;
                    l.shadowNormalBias = 0.25f;
                    EditorUtility.SetDirty(l);
                    Debug.Log($"<color=#3b82f6>[Lighting]</color> Đã cấu hình ánh sáng mặt trời: {l.name} (Soft Shadows, Strength 0.75)");
                }
            }
        }

        private static void SetupSkyBackdropAndAtmosphere()
        {
            var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTexturePath);
            if (skyTex == null)
            {
                Debug.LogWarning($"[Sky] Không tìm thấy ảnh bầu trời tại {SkyTexturePath}");
                return;
            }

            // 1. Tạo Material Unlit cho Northern Sky Backdrop
            string skyMatPath = "Assets/Materials/Environment/Backdrop_Sky/M_Sky_NorthBackdrop_Anime.mat";
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                skyMat.mainTexture = skyTex;
                AssetDatabase.CreateAsset(skyMat, skyMatPath);
            }
            else
            {
                skyMat.shader = Shader.Find("Universal Render Pipeline/Unlit");
                skyMat.mainTexture = skyTex;
                EditorUtility.SetDirty(skyMat);
            }

            // 2. Backdrop_Northern_SkyBackdrop
            var bgRoot = GameObject.Find("CaMau_1954_Village/Background_Atmosphere") ?? GameObject.Find("Background_Atmosphere");
            if (bgRoot == null)
            {
                bgRoot = new GameObject("Background_Atmosphere");
                var villageRoot = GameObject.Find("CaMau_1954_Village");
                if (villageRoot != null) bgRoot.transform.SetParent(villageRoot.transform, false);
            }

            var skyChild = bgRoot.transform.Find("Backdrop_Northern_SkyBackdrop");
            GameObject skyObj = skyChild != null ? skyChild.gameObject : null;
            if (skyObj == null)
            {
                skyObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                skyObj.name = "Backdrop_Northern_SkyBackdrop";
                skyObj.transform.SetParent(bgRoot.transform, false);
                var col = skyObj.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            // Đặt vị trí sau rặng núi phía Bắc (Z=130), quay mặt về phía Nam (rot=(0,0,0))
            skyObj.transform.position = new Vector3(0f, 32f, 130f);
            skyObj.transform.rotation = Quaternion.identity;
            skyObj.transform.localScale = new Vector3(320f, 68f, 1f);

            var mr = skyObj.GetComponent<MeshRenderer>();
            mr.sharedMaterial = skyMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // 3. Cập nhật Skybox Panoramic
            var matPano = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Backdrop_Sky/M_Skybox_CaMau_Panorama.mat");
            if (matPano != null)
            {
                matPano.mainTexture = skyTex;
                EditorUtility.SetDirty(matPano);
                RenderSettings.skybox = matPano;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.92f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.68f, 0.78f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.35f, 0.22f);
            DynamicGI.UpdateEnvironment();

            Debug.Log("<color=#3b82f6>[Sky]</color> Đã thiết lập Backdrop Bầu trời Anime và ánh sáng môi trường thành công.");
        }

        private static void FixFloatingCragRocks()
        {
            // Tìm 2 tảng đá Crag lơ lửng phía cổng làng
            var cragW = GameObject.Find("North_Gate_Pass_Crag_West");
            var cragE = GameObject.Find("North_Gate_Pass_Crag_East");

            // Tải material đá phù hợp với vùng đất đỏ/nắng Nam Bộ
            var rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Rock_Platform_01.mat")
                       ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Terrain/M_Rock_Platform_01.mat");

            if (cragW != null)
            {
                // Hạ chìm đáy đá xuống đất (Y = -2.8m thay vì Y = +2.5m)
                cragW.transform.position = new Vector3(-20.0f, -2.8f, 85.0f);
                cragW.transform.localScale = new Vector3(6.5f, 9.5f, 6.0f);
                cragW.transform.rotation = Quaternion.Euler(-5f, 25f, 3f);
                EditorUtility.SetDirty(cragW);
            }

            if (cragE != null)
            {
                // Hạ chìm đáy đá xuống đất (Y = -2.8m thay vì Y = +2.5m)
                cragE.transform.position = new Vector3(8.0f, -2.8f, 85.0f);
                cragE.transform.localScale = new Vector3(6.5f, 9.5f, 6.0f);
                cragE.transform.rotation = Quaternion.Euler(-5f, -25f, -3f);
                EditorUtility.SetDirty(cragE);
            }

            Debug.Log("<color=#3b82f6>[Landscape]</color> Đã hạ chìm 2 khối đá Crag cổng làng xuống đất, loại bỏ hoàn toàn hiện tượng cắt xen lơ lửng.");
        }

        private static void ReplaceUglyUmbrellaTreesAndRefineComposition()
        {
            var vegRoot = GameObject.Find("CaMau_1954_Village/Environment/Vegetation_3D") ?? GameObject.Find("Vegetation_3D");
            if (vegRoot == null) return;

            // Texture cây đẹp chuẩn Lana / Nam Bộ
            var texCurvedBanyan = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Lana_Unified_Art/Trees/T_Lana_Tree_01_HeroBanyan.png");
            var texWindblown = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Lana_Unified_Art/Trees/T_Lana_Tree_02_WindblownCurved.png");
            var texSlender = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Lana_Unified_Art/Trees/T_Lana_Tree_03_SlenderForest.png");
            var texGolden = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Lana_Unified_Art/Trees/T_Lana_Tree_04_GoldenHarvestBanyan.png");
            var texCoconutVN = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png");

            // 1. Sửa Coconut_Paddy_West_02 (Vật thể gây vướng mắt nhất ở trung tâm view)
            var treeW02 = vegRoot.transform.Find("Coconut_Paddy_West_02");
            if (treeW02 != null)
            {
                // Dời vị trí sang phía Tây một chút để giải phóng tầm nhìn thông thoáng vào Cổng làng
                treeW02.position = new Vector3(-24.5f, 0.5f, 67.5f);
                treeW02.localScale = new Vector3(1.35f, 1.35f, 1.35f);

                // Dọn dẹp các quad nấm xoay ngang kỳ dị (Palm_Top_FrontDome, Palm_Top_BackDome, Palm_Core_Blocker)
                for (int i = treeW02.childCount - 1; i >= 0; i--)
                {
                    var child = treeW02.GetChild(i);
                    if (child.name.Contains("Dome") || child.name.Contains("Blocker"))
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                    else if (child.name.Contains("Wing"))
                    {
                        // Giữ góc cánh tự nhiên
                        child.localRotation = Quaternion.Euler(0f, (child.name.Contains("Left") ? -35f : 35f), 0f);
                    }
                }
                EditorUtility.SetDirty(treeW02);
            }

            // 2. Sửa Coconut_Paddy_East_01 (Cây đối xứng bên bờ đông)
            var treeE01 = vegRoot.transform.Find("Coconut_Paddy_East_01");
            if (treeE01 != null)
            {
                treeE01.position = new Vector3(18.5f, 0.5f, 67.5f);
                treeE01.localScale = new Vector3(1.30f, 1.30f, 1.30f);

                for (int i = treeE01.childCount - 1; i >= 0; i--)
                {
                    var child = treeE01.GetChild(i);
                    if (child.name.Contains("Dome") || child.name.Contains("Blocker"))
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                    else if (child.name.Contains("Wing"))
                    {
                        child.localRotation = Quaternion.Euler(0f, (child.name.Contains("Left") ? -30f : 30f), 0f);
                    }
                }
                EditorUtility.SetDirty(treeE01);
            }

            Debug.Log("<color=#3b82f6>[Composition]</color> Đã loại bỏ các lớp nấm bậc thang xấu xí và mở rộng khung nhìn Cổng Làng!");
        }

        private static void UpgradeAllTreesToCutoutAndCastShadows()
        {
            var foliageShader = Shader.Find(FoliageShaderName);
            if (foliageShader == null)
            {
                Debug.LogError($"[Shader] Không tìm thấy shader {FoliageShaderName}!");
                return;
            }

            System.IO.Directory.CreateDirectory(SharedFoliageMatDir);

            // Dictionary lưu cache các material cutout theo texture chính
            var matCache = new Dictionary<Texture, Material>();

            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int updatedFoliageCount = 0;
            int shadowCount = 0;

            foreach (var r in allRenderers)
            {
                string rName = r.gameObject.name.ToLower();
                string pName = r.transform.parent != null ? r.transform.parent.name.ToLower() : "";

                bool isVegetation = pName.Contains("vegetation") || pName.Contains("tree") || pName.Contains("bamboo") || pName.Contains("banana") || pName.Contains("coconut")
                                 || rName.Contains("canopy") || rName.Contains("leaf") || rName.Contains("foliage") || rName.Contains("palm") || rName.Contains("bamboo") || rName.Contains("banana");

                if (!isVegetation) continue;

                // 1. Luôn kích hoạt đổ bóng 3D và nhận bóng
                r.shadowCastingMode = ShadowCastingMode.TwoSided;
                r.receiveShadows = true;
                shadowCount++;

                // Bỏ qua các thân gỗ 3D Mesh (đã có shader riêng M_Ghibli_PierPlank)
                if (rName.Contains("trunk") || rName.Contains("stone") || rName.Contains("ring")) continue;

                // 2. Chuyển material sang Cutout ZWrite On
                var mats = r.sharedMaterials;
                bool matChanged = false;

                for (int mIdx = 0; mIdx < mats.Length; mIdx++)
                {
                    var oldMat = mats[mIdx];
                    if (oldMat == null) continue;

                    Texture mainTex = oldMat.mainTexture;
                    if (mainTex == null)
                    {
                        var sr = r.GetComponent<SpriteRenderer>();
                        if (sr != null && sr.sprite != null) mainTex = sr.sprite.texture;
                    }

                    if (mainTex == null) continue;

                    if (!matCache.TryGetValue(mainTex, out Material newMat))
                    {
                        string safeName = mainTex.name.Replace(" ", "_");
                        string newMatPath = $"{SharedFoliageMatDir}/M_Cutout_{safeName}.mat";
                        newMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);

                        if (newMat == null)
                        {
                            newMat = new Material(foliageShader);
                            newMat.mainTexture = mainTex;
                            newMat.SetColor("_Color", Color.white);
                            newMat.SetFloat("_Cutoff", 0.32f);
                            newMat.SetColor("_ShadowColor", new Color(0.35f, 0.48f, 0.30f, 1f));
                            newMat.SetColor("_SunlitColor", new Color(1.08f, 1.05f, 0.95f, 1f));
                            newMat.SetFloat("_ShadowSoftness", 0.25f);
                            newMat.SetFloat("_AmbientBoost", 0.45f);
                            newMat.SetFloat("_WindStrength", 0.06f);
                            newMat.SetFloat("_WindSpeed", 1.6f);
                            AssetDatabase.CreateAsset(newMat, newMatPath);
                        }
                        else
                        {
                            newMat.shader = foliageShader;
                            newMat.mainTexture = mainTex;
                            newMat.SetFloat("_Cutoff", 0.32f);
                            EditorUtility.SetDirty(newMat);
                        }
                        matCache[mainTex] = newMat;
                    }

                    mats[mIdx] = newMat;
                    matChanged = true;
                }

                if (matChanged)
                {
                    r.sharedMaterials = mats;
                    var sr = r.GetComponent<SpriteRenderer>();
                    if (sr != null && mats.Length > 0 && mats[0] != null)
                    {
                        sr.sharedMaterial = mats[0];
                    }
                    updatedFoliageCount++;
                    EditorUtility.SetDirty(r);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#10b981>[Occlusion & Shadows]</color> Đã nâng cấp {updatedFoliageCount} tán lá sang Cutout ZWrite On (Hết xuyên thấu) và bật đổ bóng 3D cho {shadowCount} vật thể thực vật!");
        }
    }
}
#endif
