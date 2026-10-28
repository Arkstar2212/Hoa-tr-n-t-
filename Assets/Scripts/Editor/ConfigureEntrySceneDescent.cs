#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class ConfigureEntrySceneDescent
    {
        private const string ScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";

        [MenuItem("Tools/Hoa Tren Dat Do/Cutscene/Cấu Hình Entry Scene (Bầu Trời Trống -> Mây Thưa -> Mây Dày -> Trời Xanh -> Rừng)")]
        public static void ApplyEntrySceneDescent()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (File.Exists(ScenePath))
                {
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[ConfigureEntrySceneDescent] Scene not found at {ScenePath}");
                    return;
                }
            }

            Debug.Log("<color=#38bdf8>[ConfigureEntrySceneDescent]</color> <b>Bắt đầu tái cấu trúc Entry Scene theo đúng chỉ đạo:</b>\n" +
                      "1. Trên cùng: Trời xanh đậm không mây (Y = 75.0m)\n" +
                      "2. Xuống tí: Mây thưa (Y = 57m .. 62.5m)\n" +
                      "3. Mây nhiều / Biển mây dày (Y = 38m .. 53.5m) kéo dài 2-3s\n" +
                      "4. Hết mây: Trời xanh nhạt khoáng đạt (Y = 15m .. 37.5m - Không có mây) kéo dài 3-4s\n" +
                      "5. Xuống scene cảnh rừng: Tán cây, bãi cỏ và An, bàn giao điều khiển.");

            // 1. Cấu hình Grand_Unified_PlanetOfLana_Background bao phủ toàn bộ level (X = -50m .. +170m)
            var bgRoot = GameObject.Find("07_Background") ?? new GameObject("07_Background");
            var vistaGroup = GameObject.Find("Lana_Painted_Vistas");
            if (vistaGroup == null)
            {
                vistaGroup = new GameObject("Lana_Painted_Vistas");
                vistaGroup.transform.SetParent(bgRoot.transform, false);
            }

            var quadGO = GameObject.Find("Grand_Unified_PlanetOfLana_Background");
            if (quadGO == null)
            {
                quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quadGO.name = "Grand_Unified_PlanetOfLana_Background";
                quadGO.transform.SetParent(vistaGroup.transform, false);
                Object.DestroyImmediate(quadGO.GetComponent<Collider>());
            }

            // Đặt vị trí trung tâm để bao phủ toàn bộ chiều ngang 240m và chiều cao 90m
            // Đáy quad tại Y = -3.0m, đỉnh quad tại Y = +87.0m, bao trọn toàn bộ hành trình hạ cánh
            quadGO.transform.position = new Vector3(60.0f, 42.0f, 18.0f);
            quadGO.transform.localScale = new Vector3(240.0f, 90.0f, 1.0f);

            var quadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Generated_Overhaul/M_Grand_Unified_PlanetOfLana_Background.mat");
            var texGrand = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/T_Lana_GrandUnified_Background.png");
            if (quadMat != null)
            {
                var bgShader = Shader.Find("HoaVaMau/LanaBackgroundBlend") ?? Shader.Find("Universal Render Pipeline/Unlit");
                quadMat.shader = bgShader;
                if (texGrand != null) quadMat.SetTexture("_MainTex", texGrand);
                quadMat.SetColor("_Color", Color.white);
                quadMat.SetFloat("_LeftFeather", 0f);
                quadMat.SetFloat("_RightFeather", 0f);
                quadMat.SetFloat("_BottomFeather", 0f);
                quadMat.SetFloat("_TopFeather", 0f);
                quadMat.renderQueue = 2900;
                EditorUtility.SetDirty(quadMat);

                var mrQuad = quadGO.GetComponent<MeshRenderer>();
                mrQuad.sharedMaterial = quadMat;
                mrQuad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mrQuad.receiveShadows = false;
            }

            // Đăng ký Parallax Follower
            var follower = bgRoot.GetComponent<LanaParallaxBackgroundFollower>() ?? bgRoot.AddComponent<LanaParallaxBackgroundFollower>();
            follower.RegisterLayer("Lana_Painted_Vistas", vistaGroup.transform, 0.92f, 0.02f);

            // 2. Tìm hoặc tạo IntroSky_CloudDescentGroup
            Transform existingGroupTr = bgRoot.transform.Find("IntroSky_CloudDescentGroup");
            GameObject cloudGroup;
            if (existingGroupTr != null)
            {
                cloudGroup = existingGroupTr.gameObject;
                cloudGroup.SetActive(true);

                // Xóa các đám mây cũ để dựng lại phân tầng chuẩn xác
                for (int i = cloudGroup.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(cloudGroup.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                cloudGroup = new GameObject("IntroSky_CloudDescentGroup");
                cloudGroup.transform.SetParent(bgRoot.transform, false);
            }

            cloudGroup.transform.localPosition = Vector3.zero;
            cloudGroup.transform.localRotation = Quaternion.identity;
            cloudGroup.transform.localScale = Vector3.one;

            // 3. Tải các Sprite mây chuẩn dự án
            const string CloudBaseDir = "Assets/Art/Environment/Sky/Clouds";
            const string LanaCloudDir = "Assets/Textures/Lana_Unified_Art/Clouds";

            Sprite sprHeroDescent01  = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Foreground/T_Cloud_Hero_Descent01.png");
            Sprite sprHeroDescent02  = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Foreground/T_Cloud_Hero_Descent02.png");
            Sprite sprMidCumulusBank = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Midground/T_Cloud_Mid_CumulusBank01.png");
            Sprite sprMidCluster02   = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Midground/T_Cloud_Mid_Cluster02.png");
            Sprite sprMidSoftBank03  = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Midground/T_Cloud_Mid_SoftBank03.png");
            Sprite sprDistantHorizon = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Distant/T_Cloud_Distant_Horizon03.png");
            Sprite sprDistantLayer02 = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Distant/T_Cloud_Distant_Layer02.png");
            Sprite sprDistantWisp01  = AssetDatabase.LoadAssetAtPath<Sprite>($"{CloudBaseDir}/Distant/T_Cloud_Distant_Wisp01.png");

            Sprite sprLanaHeroCumulus = AssetDatabase.LoadAssetAtPath<Sprite>($"{LanaCloudDir}/T_Lana_Cloud_01_CumulusHero.png") ?? sprHeroDescent01;
            Sprite sprLanaWideBank    = AssetDatabase.LoadAssetAtPath<Sprite>($"{LanaCloudDir}/T_Lana_Cloud_02_WideBank.png") ?? sprMidCumulusBank;
            Sprite sprLanaHighWisps   = AssetDatabase.LoadAssetAtPath<Sprite>($"{LanaCloudDir}/T_Lana_Cloud_03_HighWisps.png") ?? sprDistantWisp01;

            Material spriteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Sprite_Default.mat");
            if (spriteMat == null)
            {
                var shaderURP = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                spriteMat = new Material(shaderURP);
                AssetDatabase.CreateAsset(spriteMat, "Assets/Materials/M_Sprite_Default.mat");
            }

            void SpawnCloud(string name, Sprite spr, Vector3 pos, Vector2 scale, Color col, int sortOrder, bool flipX = false, float driftSpeed = 0.25f)
            {
                if (spr == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(cloudGroup.transform, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sharedMaterial = spriteMat;
                sr.color = col;
                sr.sortingOrder = sortOrder;
                sr.flipX = flipX;

                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                var so = new SerializedObject(sway);
                so.FindProperty("maxRotationAngle").floatValue = 0.08f;
                so.FindProperty("swaySpeed").floatValue = driftSpeed;
                so.FindProperty("useWorldPositionOffset").boolValue = true;
                so.FindProperty("manualPhaseOffset").floatValue = pos.x * 0.15f + pos.y * 0.1f;
                so.FindProperty("scaleBreathing").floatValue = 0.010f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // =========================================================================================
            // GIAI ĐOẠN 1: Ở TRÊN CÙNG LÀ TRỜI XANH ĐẬM KHÔNG MÂY (Y = 66m .. 85m)
            // Camera xuất phát tại Y = 75.0m. Khung hình nhìn lên vòm trời xanh đậm thẳm sâu,
            // hoàn toàn trống trơn không một gợn mây che khuất.
            // =========================================================================================

            // =========================================================================================
            // GIAI ĐOẠN 2: XUỐNG TÍ THÌ THƯA MÂY (Y = 57.0m .. 62.5m)
            // Sau ~1 giây hạ cánh, máy quay chạm tầng mây thưa nhẹ nhàng lướt vào.
            // Scale chuẩn: width ~10m..15m vừa vặn với tầm nhìn 16:9.
            // =========================================================================================
            SpawnCloud("Cloud_Sparse_Wisp01", sprLanaHighWisps,  new Vector3( -7.5f, 61.5f, 8.5f), new Vector2(1.8f, 1.3f), new Color(0.92f, 0.95f, 1.0f, 0.75f), 1);
            SpawnCloud("Cloud_Sparse_Wisp02", sprDistantWisp01,  new Vector3(  4.5f, 60.0f, 7.8f), new Vector2(1.9f, 1.4f), new Color(0.95f, 0.97f, 1.0f, 0.80f), 1, true);
            SpawnCloud("Cloud_Sparse_Wisp03", sprDistantLayer02, new Vector3(-13.5f, 58.5f, 9.5f), new Vector2(2.0f, 1.3f), new Color(0.88f, 0.93f, 1.0f, 0.70f), 1);
            SpawnCloud("Cloud_Sparse_Wisp04", sprLanaHighWisps,  new Vector3( 10.5f, 57.5f, 8.2f), new Vector2(1.7f, 1.2f), new Color(0.90f, 0.94f, 1.0f, 0.72f), 1, true);
            SpawnCloud("Cloud_Sparse_CenterWisp", sprDistantWisp01, new Vector3(-1.0f, 59.0f, 6.5f), new Vector2(1.5f, 1.1f), new Color(0.92f, 0.95f, 1.0f, 0.65f), 2);

            // =========================================================================================
            // GIAI ĐOẠN 3: RỒI MÂY NHIỀU / BIỂN MÂY DÀY (Y = 38.0m .. 53.5m)
            // Máy quay lao qua biển mây cuồn cuộn nhiều tầng kéo dài 2-3 giây.
            // =========================================================================================
            // Tầng trên (Upper Deck, Y = 48m .. 53.5m)
            SpawnCloud("Cloud_Dense_Upper_Hero01", sprLanaHeroCumulus, new Vector3(-2.8f, 51.5f, 2.0f), new Vector2(2.1f, 1.6f), new Color(1.00f, 1.00f, 1.00f, 0.98f), 8);
            SpawnCloud("Cloud_Dense_Upper_Bank02", sprLanaWideBank,    new Vector3(-11.0f, 52.5f, 5.5f), new Vector2(1.9f, 1.5f), new Color(0.96f, 0.98f, 1.00f, 0.96f), 4);
            SpawnCloud("Cloud_Dense_Upper_Bank03", sprHeroDescent02,   new Vector3(  6.8f, 51.0f, 4.8f), new Vector2(2.0f, 1.5f), new Color(0.98f, 0.99f, 1.00f, 0.97f), 5, true);
            SpawnCloud("Cloud_Dense_Upper_Far04",  sprMidCumulusBank,  new Vector3( 14.5f, 53.0f, 7.5f), new Vector2(1.8f, 1.4f), new Color(0.95f, 0.97f, 1.00f, 0.94f), 3);

            // Tầng trung tâm biển mây (Heart of Cloud Sea, Y = 43m .. 48m)
            SpawnCloud("Cloud_Dense_Heart_Bank01", sprLanaWideBank,    new Vector3(-5.5f, 46.5f, 3.5f), new Vector2(2.2f, 1.7f), new Color(1.00f, 1.00f, 1.00f, 1.00f), 6);
            SpawnCloud("Cloud_Dense_Heart_Bank02", sprLanaHeroCumulus, new Vector3( 4.0f, 45.0f, 4.0f), new Vector2(2.1f, 1.6f), new Color(0.99f, 1.00f, 1.00f, 0.98f), 6, true);
            SpawnCloud("Cloud_Dense_Heart_Near03", sprHeroDescent01,   new Vector3( 1.2f, 44.0f, 1.5f), new Vector2(1.9f, 1.5f), new Color(1.00f, 1.00f, 1.00f, 0.99f), 9);
            SpawnCloud("Cloud_Dense_Heart_Left04", sprMidCumulusBank,  new Vector3(-13.5f, 46.0f, 6.0f), new Vector2(1.9f, 1.5f), new Color(0.96f, 0.98f, 1.00f, 0.96f), 4);
            SpawnCloud("Cloud_Dense_Heart_Right05",sprMidCluster02,    new Vector3( 13.0f, 45.5f, 6.5f), new Vector2(1.8f, 1.4f), new Color(0.97f, 0.98f, 1.00f, 0.95f), 4, true);

            // Tầng đáy biển mây (Lower Shelf, Y = 38m .. 43m)
            SpawnCloud("Cloud_Dense_Lower_Base01", sprMidSoftBank03,   new Vector3(-4.0f, 40.5f, 3.5f), new Vector2(2.0f, 1.5f), new Color(0.98f, 0.99f, 1.00f, 0.98f), 6);
            SpawnCloud("Cloud_Dense_Lower_Base02", sprLanaWideBank,    new Vector3( 5.5f, 39.5f, 4.5f), new Vector2(2.1f, 1.6f), new Color(0.99f, 1.00f, 1.00f, 0.98f), 5, true);
            SpawnCloud("Cloud_Dense_Lower_Left03", sprMidCluster02,    new Vector3(-12.0f, 41.0f, 5.5f), new Vector2(1.8f, 1.4f), new Color(0.96f, 0.98f, 1.00f, 0.96f), 4);
            SpawnCloud("Cloud_Dense_Lower_Right04",sprMidSoftBank03,   new Vector3( 13.5f, 39.0f, 6.0f), new Vector2(1.7f, 1.3f), new Color(0.97f, 0.98f, 1.00f, 0.95f), 4);

            // =========================================================================================
            // GIAI ĐOẠN 4: HẾT MÂY & BẦU TRỜI XANH NHẠT KHOÁNG ĐẠT (Y = 15.0m .. 37.5m)
            // TUYỆT ĐỐI KHÔNG CÓ BẤT KỲ ĐÁM MÂY NÀO!
            // Thoát khỏi biển mây, người chơi được ngắm trọn vòm trời xanh nhạt rực rỡ, trong trẻo,
            // ngập tràn ánh nắng và rặng núi xa của Nam Bộ suốt 3-4 giây.
            // =========================================================================================

            // =========================================================================================
            // GIAI ĐOẠN 5: XUỐNG SCENE CẢNH RỪNG (Y <= 15.0m)
            // Camera hạ qua tán tre, dừa, chuối, tiếp đất nhẹ nhàng bên An tại Y = 0.10m.
            // =========================================================================================

            // 4. Thiết lập Camera VCam_IntroSky
            CinemachineCamera vcamSky = null;
            var allCams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);
            foreach (var c in allCams)
            {
                if (c.name.StartsWith("VCam_IntroSky"))
                {
                    if (c.gameObject.activeInHierarchy && c.transform.position.y > 60f)
                    {
                        vcamSky = c;
                    }
                    else if (!c.gameObject.activeInHierarchy && !c.name.Contains("OldBackup"))
                    {
                        c.name = $"VCam_IntroSky_OldBackup_{Mathf.RoundToInt(c.transform.position.y)}";
                    }
                }
            }
            if (vcamSky == null)
            {
                var vcamIntroGO = new GameObject("VCam_IntroSky");
                vcamSky = vcamIntroGO.AddComponent<CinemachineCamera>();
            }
            vcamSky.gameObject.SetActive(true);
            vcamSky.transform.position = new Vector3(-3.20f, 75.0f, -13.5f);
            vcamSky.transform.rotation = Quaternion.identity;
            vcamSky.Priority = 35;
            vcamSky.Lens.FieldOfView = 38f;

            // 5. Cấu hình IntroCutsceneController
            var ctrlGO = GameObject.Find("00_Cinematic_IntroCutsceneController");
            if (ctrlGO == null)
            {
                ctrlGO = new GameObject("00_Cinematic_IntroCutsceneController");
            }
            var ctrl = ctrlGO.GetComponent<IntroCutsceneController>() ?? ctrlGO.AddComponent<IntroCutsceneController>();

            ctrl.EnsureCutsceneCanvasExists();
            ctrl.ResolveSceneReferences();

            SerializedObject soCtrl = new SerializedObject(ctrl);
            soCtrl.FindProperty("vcamIntroSky").objectReferenceValue = vcamSky;
            soCtrl.FindProperty("cameraPanDuration").floatValue = 11.5f;
            soCtrl.FindProperty("forcePlayIntro").boolValue = true;
            var propDirect = soCtrl.FindProperty("startWithSkyDescent");
            if (propDirect != null) propDirect.boolValue = true;
            soCtrl.FindProperty("skyZenithPosition").vector3Value = new Vector3(-3.20f, 75.0f, -13.5f);
            soCtrl.FindProperty("forestGroundPosition").vector3Value = new Vector3(-3.20f, 0.10f, -13.5f);
            soCtrl.FindProperty("descentProfileCurve").animationCurveValue = IntroCutsceneController.CreateDefaultDescentCurve();

            var vcamGameplayGO = GameObject.Find("CM_VirtualCamera_2.5D");
            if (vcamGameplayGO != null)
            {
                var vcamGameplay = vcamGameplayGO.GetComponent<CinemachineCamera>();
                if (vcamGameplay != null)
                {
                    soCtrl.FindProperty("vcamGameplay").objectReferenceValue = vcamGameplay;
                    vcamGameplay.Priority = 10;
                }
            }

            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");
            if (playerGO != null)
            {
                var playerCtrl = playerGO.GetComponent<PlayerController>();
                if (playerCtrl != null)
                {
                    soCtrl.FindProperty("playerController").objectReferenceValue = playerCtrl;
                    soCtrl.FindProperty("playerTransform").objectReferenceValue = playerCtrl.transform;
                }
            }

            var brain = Object.FindAnyObjectByType<CinemachineBrain>();
            if (brain != null)
            {
                soCtrl.FindProperty("cinemachineBrain").objectReferenceValue = brain;
            }

            soCtrl.ApplyModifiedProperties();

            // 6. Cập nhật IntroCutsceneState
            IntroCutsceneState.ShouldPlayIntroCutscene = true;
            IntroCutsceneState.HasViewedInSession = false;

            // 7. Lưu Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#10b981><b>[ConfigureEntrySceneDescent] THÀNH CÔNG RỰC RỠ!</b></color>\n" +
                      "✔ Nền Grand_Unified đã được mở rộng 220m với shader URP UniversalForward hiển thị hoàn hảo trong Scene View & Game View.\n" +
                      "✔ Trên cùng (Y=75m): Bầu trời xanh đậm không mây.\n" +
                      "✔ Xuống tí: Mây thưa (Y=57m..62.5m).\n" +
                      "✔ Mây nhiều: Biển mây dày (Y=38m..53.5m) kéo dài 2-3s.\n" +
                      "✔ Hết mây: Bầu trời xanh nhạt (Y=15m..37.5m) kéo dài 3-4s.\n" +
                      "✔ Xuống scene cảnh rừng: Tiếp đất êm ái tại An.");
        }
    }
}
#endif