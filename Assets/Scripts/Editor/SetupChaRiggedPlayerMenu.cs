using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;

namespace HoaVaMau.Gameplay
{
    public class SetupChaRiggedPlayerMenu
    {
        [MenuItem("Hoa va Mau/Setup cha_rigged_animations as Main Player & Add Gapbung Trigger")]
        public static void SetupPlayer()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // 1. Tìm hoặc Instantiate cha_rigged_animations trong Scene
            GameObject chaObj = GameObject.Find("cha_rigged_animations");
            if (chaObj == null)
            {
                string[] guids = AssetDatabase.FindAssets("cha_rigged_animations t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        chaObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        chaObj.name = "cha_rigged_animations";
                    }
                }
            }

            if (chaObj == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy đối tượng 'cha_rigged_animations' trong Scene hoặc Project!", "OK");
                return;
            }

            // 2. Tìm nhân vật cũ Player_LanaProtagonist (nếu có)
            GameObject oldPlayer = GameObject.Find("Player_LanaProtagonist");
            Vector3 spawnPos = new Vector3(-17.5f, 0.05f, 0f);

            if (oldPlayer != null && oldPlayer != chaObj)
            {
                // Lấy vị trí hiện tại của nhân vật cũ
                spawnPos = oldPlayer.transform.position;
                // Vô hiệu hóa hoặc ẩn hoàn toàn nhân vật cũ
                oldPlayer.SetActive(false);
                Debug.Log("<color=#fb923c>[Setup Player]</color> Đã ẩn hoàn toàn nhân vật mẫu cũ Player_LanaProtagonist.");
            }

            // 3. Đặt vị trí cha_rigged_animations ngay chỗ nhân vật đang đứng trên Scene
            spawnPos.z = 0f;
            chaObj.transform.position = spawnPos;
            chaObj.transform.localScale = Vector3.one;

            // 4. Gán script GapbungPlayerSetup
            var setupScript = chaObj.GetComponent<GapbungPlayerSetup>();
            if (setupScript == null)
            {
                setupScript = chaObj.AddComponent<GapbungPlayerSetup>();
            }

            // 5. Tìm Animation Clip trong cha_rigged_animations.fbx
            AnimationClip targetClip = null;
            string fbxPath = "Assets/Models/cha_rigged_animations.fbx";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (clip.name.Contains("Gapbung"))
                    {
                        targetClip = clip;
                        break;
                    }
                    if (targetClip == null) targetClip = clip;
                }
            }

            // 6. Tạo/Cập nhật Animator Controller chuẩn với State "Gapbung_rig"
            string controllerPath = "Assets/Models/GapbungPlayerController.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            var rootStateMachine = controller.layers[0].stateMachine;
            AnimatorState gapbungState = null;

            foreach (var childState in rootStateMachine.states)
            {
                if (childState.state.name == "Gapbung_rig" || childState.state.name.Contains("Gapbung"))
                {
                    gapbungState = childState.state;
                    break;
                }
            }

            if (gapbungState == null)
            {
                gapbungState = rootStateMachine.AddState("Gapbung_rig");
            }

            if (targetClip != null)
            {
                gapbungState.motion = targetClip;
                rootStateMachine.defaultState = gapbungState;
                Debug.Log($"<color=#4ade80>[Setup Player]</color> Đã gán clip '{targetClip.name}' vào state 'Gapbung_rig'.");
            }

            // Gán Animator Controller vào Animator component của nhân vật
            Animator animator = chaObj.GetComponent<Animator>();
            if (animator == null) animator = chaObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            // 7. Cấu hình Camera chính bắt chặt vào nhân vật cha_rigged_animations
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var cam2D = mainCam.GetComponent<Cinematic2DCamera>();
                if (cam2D != null)
                {
                    cam2D.Target = chaObj.transform;
                    cam2D.SnapToTarget();
                }
            }

            // 8. Đổi FBX Import sang Generic Rig nếu cần để giữ trọn vẹn 100% animation Rigify
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.SaveAndReimport();
                Debug.Log("<color=#4ade80>[Setup Player]</color> Đã đổi FBX Rig sang Generic để giữ 100% chuyển động xương Rigify!");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#4ade80>[SUCCESS]</color> Đã di chuyển 'cha_rigged_animations' vào màn hình và sẵn sàng bấm G để Gập Bụng!");
            EditorUtility.DisplayDialog("Thành công!", "Đã đưa 'cha_rigged_animations' ra đúng vị trí màn hình Game!\nBấm Play và bấm phím G để xem nhân vật gập bụng.", "OK");
        }
    }
}
#endif
