#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Phase 4: Lighting & Atmospheric Polish cho Cảnh 1 (Rừng Cà Mau, 1954).
    /// Thiết lập toàn diện:
    /// 1. UniversalAdditionalCameraData: Kích hoạt renderPostProcessing & khử răng cưa SMAA
    /// 2. Tạo Asset VolumeProfile_Scene1_CaMau1954 với ColorAdjustments, Bloom, Vignette, Tonemapping, WhiteBalance
    /// 3. Tạo GameObject Global_Volume_Canh1 trong 08_Lighting
    /// 4. Cân chỉnh Directional Light: Ánh nắng sớm 42 độ, bóng đổ mềm mại (soft-toon 0.65 strength)
    /// 5. Cân chỉnh Ambient Trilight (Sky/Equator/Ground) & Sương mù Linear đầm lầy Cà Mau
    /// 6. Bổ sung hệ thống đèn điểm nhẹ (Local Fill Lights) tại các vùng trọng điểm A, C, D, E
    /// </summary>
    public static class Scene1LightingAndAtmosphereBuilder
    {
        private const string ProfilePath = "Assets/Settings/VolumeProfile_Scene1_CaMau1954.asset";

        [MenuItem("Hoa va Mau/Level Design/Phase 4 - Execute Lighting & Atmospheric Polish", false, 3)]
        public static void ExecuteLightingAndAtmosphere()
        {
            Debug.Log("<color=#40b8a6>[Scene1LightingBuilder]</color> Bắt đầu thực thi Phase 4: Lighting & Atmospheric Polish...");

            GameObject lightingRoot = GameObject.Find("08_Lighting");
            if (lightingRoot == null)
            {
                lightingRoot = new GameObject("08_Lighting");
                Undo.RegisterCreatedObjectUndo(lightingRoot, "Create 08_Lighting");
            }

            // 1. Cấu hình Camera Post-Processing
            SetupCameraPostProcessing();

            // 2. Tạo và cấu hình VolumeProfile Asset
            VolumeProfile profileAsset = SetupVolumeProfileAsset();

            // 3. Tạo Global Volume trong Scene
            SetupSceneGlobalVolume(lightingRoot, profileAsset);

            // 4. Cân chỉnh Directional Light (Ánh sáng mặt trời)
            SetupDirectionalLight(lightingRoot);

            // 5. Cấu hình Ambient Lighting & Sương mù sông nước Cà Mau
            SetupAmbientAndFog();

            // 6. Bổ sung các nguồn sáng điểm cục bộ (Local Fill Lights)
            SetupLocalFillLights(lightingRoot);

            // Lưu scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("<color=#40b8a6>[Scene1LightingBuilder]</color> <b>HOÀN TẤT PHASE 4 LIGHTING & ATMOSPHERE THÀNH CÔNG RỰC RỠ!</b>");
        }

        // =========================================================================
        // 1. CẤU HÌNH CAMERA POST-PROCESSING
        // =========================================================================
        private static void SetupCameraPostProcessing()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Scene1LightingBuilder] Camera.main không tìm thấy!");
                return;
            }

            if (!cam.TryGetComponent<UniversalAdditionalCameraData>(out var camData))
            {
                camData = Undo.AddComponent<UniversalAdditionalCameraData>(cam.gameObject);
            }

            Undo.RecordObject(camData, "Configure Camera Post-Processing");
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            camData.volumeLayerMask = ~0; // Phủ tất cả các layer

            EditorUtility.SetDirty(camData);
            Debug.Log("<color=#40b8a6>[Scene1LightingBuilder]</color> Đã kích hoạt Post-Processing & SMAA High trên Camera.");
        }

        // =========================================================================
        // 2. TẠO VÀ CẤU HÌNH VOLUME PROFILE ASSET
        // =========================================================================
        private static VolumeProfile SetupVolumeProfileAsset()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "VolumeProfile_Scene1_CaMau1954";

                string dir = Path.GetDirectoryName(ProfilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(profile, ProfilePath);
                Debug.Log($"<color=#40b8a6>[Scene1LightingBuilder]</color> Tạo mới Volume Profile tại: {ProfilePath}");
            }

            Undo.RecordObject(profile, "Configure Volume Profile Overrides");

            // 1. Color Adjustments (Tươi tắn, ấm áp phong cách tranh màu nước Ghibli)
            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.active = true;
            colorAdj.postExposure.Override(0.20f);
            colorAdj.contrast.Override(12.0f);
            colorAdj.colorFilter.Override(new Color(1.0f, 0.985f, 0.95f)); // Màu giấy vẽ tranh ấm
            colorAdj.saturation.Override(16.0f); // Xanh mướt rực rỡ nhiệt đới

            // 2. Tonemapping Neutral (Giữ chuyển màu êm dịu không bị gắt đen như ACES)
            if (!profile.TryGet<Tonemapping>(out var tonemap))
            {
                tonemap = profile.Add<Tonemapping>(true);
            }
            tonemap.active = true;
            tonemap.mode.Override(TonemappingMode.Neutral);

            // 3. Bloom (Hào quang nhẹ trên mặt nước và vạt nắng)
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.threshold.Override(0.92f);
            bloom.intensity.Override(0.38f);
            bloom.scatter.Override(0.68f);
            bloom.tint.Override(new Color(1.0f, 0.95f, 0.85f));

            // 4. Vignette (Khung viền xanh rêu sẫm đậm chất điện ảnh)
            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.active = true;
            vignette.color.Override(new Color(0.04f, 0.08f, 0.06f));
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.48f);

            // 5. White Balance (Nhiệt độ nắng sớm phương Nam)
            if (!profile.TryGet<WhiteBalance>(out var wb))
            {
                wb = profile.Add<WhiteBalance>(true);
            }
            wb.active = true;
            wb.temperature.Override(5.5f);
            wb.tint.Override(-2.0f); // Nghiêng nhẹ sang sắc ngọc bích rừng đước

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // =========================================================================
        // 3. TẠO GLOBAL VOLUME TRONG SCENE
        // =========================================================================
        private static void SetupSceneGlobalVolume(GameObject lightingRoot, VolumeProfile profileAsset)
        {
            Transform existingVol = lightingRoot.transform.Find("Global_Volume_Canh1");
            GameObject volGO;
            if (existingVol != null)
            {
                volGO = existingVol.gameObject;
            }
            else
            {
                volGO = new GameObject("Global_Volume_Canh1");
                volGO.transform.SetParent(lightingRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(volGO, "Create Global_Volume_Canh1");
            }

            var vol = volGO.GetComponent<Volume>();
            if (vol == null) vol = volGO.AddComponent<Volume>();

            Undo.RecordObject(vol, "Configure Global Volume");
            vol.isGlobal = true;
            vol.weight = 1.0f;
            vol.sharedProfile = profileAsset;
            vol.priority = 10;

            EditorUtility.SetDirty(vol);
            Debug.Log("<color=#40b8a6>[Scene1LightingBuilder]</color> Đã gắn Global Volume vào scene.");
        }

        // =========================================================================
        // 4. CÂN CHỈNH DIRECTIONAL LIGHT
        // =========================================================================
        private static void SetupDirectionalLight(GameObject lightingRoot)
        {
            GameObject lightGO = GameObject.Find("Directional Light");
            if (lightGO == null)
            {
                lightGO = new GameObject("Directional Light");
                lightGO.transform.SetParent(lightingRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(lightGO, "Create Directional Light");
            }
            else if (lightGO.transform.parent != lightingRoot.transform)
            {
                Undo.SetTransformParent(lightGO.transform, lightingRoot.transform, "Move Directional Light");
            }

            var light = lightGO.GetComponent<Light>();
            if (light == null) light = lightGO.AddComponent<Light>();

            Undo.RecordObject(light, "Configure Directional Light");
            Undo.RecordObject(lightGO.transform, "Rotate Directional Light");

            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.95f, 0.86f); // Nắng vàng sớm Cà Mau
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.62f; // Bóng mờ nhẹ, không làm đen đặc mặt đất
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.35f;

            // Góc chiếu 42 độ từ trên xuống, hơi chếch sang phải (đổ bóng xuôi theo chiều tiến nhân vật)
            lightGO.transform.rotation = Quaternion.Euler(42f, -38f, 0f);

            EditorUtility.SetDirty(light);
            Debug.Log("<color=#40b8a6>[Scene1LightingBuilder]</color> Đã cấu hình Directional Light (Intensity: 1.25, Soft Shadows: 0.62).");
        }

        // =========================================================================
        // 5. CẤU HÌNH AMBIENT LIGHTING & SƯƠNG MÙ CÀ MAU
        // =========================================================================
        private static void SetupAmbientAndFog()
        {
            // Gradient Trilight Ambient
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.72f, 0.84f);   // Xanh da trời nhạt dịu
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.54f, 0.44f); // Xanh lá phản xạ từ tán rừng
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.27f, 0.22f);  // Nâu vàng ấm phản xạ từ đất

            // Sương mù đầm lầy sông nước (Linear Fog tạo chiều sâu phân lớp rõ rệt)
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 22.0f;
            RenderSettings.fogEndDistance = 72.0f;
            RenderSettings.fogColor = new Color(0.52f, 0.68f, 0.74f); // Xanh xám mờ ảo của hơi nước sông ngòi

            Debug.Log("<color=#40b8a6>[Scene1LightingBuilder]</color> Đã thiết lập Trilight Ambient & Linear Fog (22m -> 72m).");
        }

        // =========================================================================
        // 6. BỔ SUNG LOCAL FILL LIGHTS TẠI CÁC VÙNG TRỌNG ĐIỂM
        // =========================================================================
        private static void SetupLocalFillLights(GameObject lightingRoot)
        {
            Transform existingGroup = lightingRoot.transform.Find("Local_Fill_Lights");
            GameObject group;
            if (existingGroup != null)
            {
                group = existingGroup.gameObject;
                while (group.transform.childCount > 0)
                {
                    Undo.DestroyObjectImmediate(group.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                group = new GameObject("Local_Fill_Lights");
                group.transform.SetParent(lightingRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(group, "Create Local_Fill_Lights");
            }

            // 1. Ánh sáng ấm đón chào Cửa rừng Vùng A (X = 3m)
            CreatePointLight(group, "Fill_ZoneA_Entrance", 
                new Vector3(3.0f, 0.6f, 0.0f), 
                new Color(1.0f, 0.92f, 0.76f), 
                intensity: 0.35f, range: 8.5f);

            // 2. Điểm sáng nhấn gờ leo trèo Vùng C (X = 24m)
            CreatePointLight(group, "Fill_ZoneC_LedgeGlow", 
                new Vector3(24.0f, 0.8f, -0.6f), 
                new Color(1.0f, 0.94f, 0.78f), 
                intensity: 0.45f, range: 7.5f);

            // 3. Đèn lồng ấm áp làng chài Vùng D (X = 35.5m)
            CreatePointLight(group, "Fill_ZoneD_DockLantern", 
                new Vector3(35.5f, -0.9f, 1.8f), 
                new Color(1.0f, 0.78f, 0.46f), 
                intensity: 0.65f, range: 6.5f);

            // 4. Ánh sáng vàng tán xạ khoảng rừng mở Vùng E (X = 47m)
            CreatePointLight(group, "Fill_ZoneE_GladeBounce", 
                new Vector3(47.0f, -0.4f, 0.0f), 
                new Color(1.0f, 0.94f, 0.68f), 
                intensity: 0.55f, range: 10.0f);
        }

        private static GameObject CreatePointLight(GameObject parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;

            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None; // Đèn phụ không cần tính bóng đổ để tối ưu FPS

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go;
        }
    }
}
#endif
