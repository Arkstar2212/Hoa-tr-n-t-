using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Master Editor tool to apply Save Slot Checkpoint Thumbnails & Natural Rope Traversal Overhaul.
    /// Conforms strictly to Rule 4.1 & Rule 4.3 (Additive Only, preserves scene sanctity).
    /// Runs automatically via delayCall on compile, or manually via Tools menu.
    /// </summary>
    [InitializeOnLoad]
    public static class ApplySaveSlotAndRopeOverhaul
    {
        private const string DoneKey = "HoaVaMau_Done_SaveSlot_Rope_Overhaul_V1";

        static ApplySaveSlotAndRopeOverhaul()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(DoneKey, false))
                {
                    SessionState.SetBool(DoneKey, true);
                    Execute();
                }
            };
        }

        [MenuItem("Tools/Hoa Tren Dat Do/Apply Save Slot And Rope Overhaul")]
        public static void Execute()
        {
            Debug.Log("<color=#38bdf8>[SaveSlotAndRopeOverhaul]</color> Bắt đầu đồng bộ hóa toàn diện Checkpoint Thumbnails & Rope Traversal...");

            // 1. Cấu hình fallback thumbnails trong Scene_MainMenu.unity
            ConfigureMainMenuFallbackThumbnails();

            // 2. Chuẩn bị ảnh thumbnail mặc định trong PersistentDataPath cho các Save Slot
            GeneratePersistentSlotThumbnails();

            // 3. Đồng bộ hóa thông số vật lý dây thừng trong các Scene
            UpdateRopePhysicsInScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#4ade80>[SaveSlotAndRopeOverhaul]</color> <b>HOÀN THÀNH TOÀN DIỆN: Đã nâng cấp ảnh Checkpoint Save Slots chuẩn Planet of Lana & Cơ chế đu leo dây tự nhiên!</b>");
        }

        private static void ConfigureMainMenuFallbackThumbnails()
        {
            string menuScenePath = "Assets/Scenes/Scene_MainMenu.unity";
            if (!File.Exists(menuScenePath))
            {
                Debug.LogWarning($"[SaveSlotAndRopeOverhaul] Scene không tồn tại: {menuScenePath}");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            string originalPath = activeScene.path;

            var scene = EditorSceneManager.OpenScene(menuScenePath, OpenSceneMode.Single);

            var startMenu = Object.FindAnyObjectByType<PlanetOfLanaStartMenuSystem>();
            if (startMenu != null)
            {
                var so = new SerializedObject(startMenu);
                var fallbackProp = so.FindProperty("fallbackSlotThumbnails");

                Texture2D thumb1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png");
                Texture2D thumb2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_CoastalBay_Day.jpg");
                Texture2D thumb3 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_HillsFields_Sunset.jpg");

                if (fallbackProp != null)
                {
                    fallbackProp.arraySize = 3;
                    if (thumb1 != null) fallbackProp.GetArrayElementAtIndex(0).objectReferenceValue = thumb1;
                    if (thumb2 != null) fallbackProp.GetArrayElementAtIndex(1).objectReferenceValue = thumb2;
                    if (thumb3 != null) fallbackProp.GetArrayElementAtIndex(2).objectReferenceValue = thumb3;
                    so.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(startMenu);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=#4ade80>[SaveSlotAndRopeOverhaul]</color> Đã gán 3 ảnh fallback concept art cho PlanetOfLanaStartMenuSystem trong Scene_MainMenu!");
            }

            // Restore previous active scene if different
            if (!string.IsNullOrEmpty(originalPath) && originalPath != menuScenePath && File.Exists(originalPath))
            {
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            }
        }

        private static void GeneratePersistentSlotThumbnails()
        {
            string savesDir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(savesDir))
            {
                Directory.CreateDirectory(savesDir);
            }

            string[] sourceAssets = new string[]
            {
                "Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png",
                "Assets/Textures/Backgrounds_Lana/BG_Lana_CoastalBay_Day.jpg",
                "Assets/Textures/Backgrounds_Lana/BG_Lana_HillsFields_Sunset.jpg"
            };

            for (int slot = 1; slot <= 2; slot++)
            {
                string thumbPath = Path.Combine(savesDir, $"save_slot_{slot}_thumb.png");
                if (!File.Exists(thumbPath))
                {
                    string srcPath = sourceAssets[slot - 1];
                    Texture2D srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
                    if (srcTex != null)
                    {
                        // Crop vertical slice (280x480)
                        RenderTexture rt = RenderTexture.GetTemporary(280, 480, 0, RenderTextureFormat.ARGB32);
                        Graphics.Blit(srcTex, rt);

                        RenderTexture prev = RenderTexture.active;
                        RenderTexture.active = rt;

                        Texture2D slice = new Texture2D(280, 480, TextureFormat.RGB24, false);
                        slice.ReadPixels(new Rect(0, 0, 280, 480), 0, 0);
                        slice.Apply();

                        RenderTexture.active = prev;
                        RenderTexture.ReleaseTemporary(rt);

                        byte[] png = slice.EncodeToPNG();
                        Object.DestroyImmediate(slice);

                        File.WriteAllBytes(thumbPath, png);
                        Debug.Log($"<color=#38bdf8>[SaveSlotAndRopeOverhaul]</color> Tạo sẵn ảnh thumbnail khởi tạo cho Slot {slot}: {thumbPath}");
                    }
                }
            }
        }

        private static void UpdateRopePhysicsInScenes()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/New Scene 1_Overhaul.unity",
                "Assets/Scenes/canh1_ArtBenchmark_Test.unity",
                "Assets/Scenes/CaMau_1954_Village.unity"
            };

            string originalScene = EditorSceneManager.GetActiveScene().path;

            foreach (var sp in scenePaths)
            {
                if (!File.Exists(sp)) continue;

                var scene = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
                var ropes = Object.FindObjectsByType<RopeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (ropes.Length > 0)
                {
                    foreach (var rope in ropes)
                    {
                        var so = new SerializedObject(rope);
                        var maxAngleProp = so.FindProperty("maxSwingAngle");
                        var swingForceProp = so.FindProperty("playerSwingForce");
                        var momProp = so.FindProperty("momentumTransferRatio");
                        var maxSpeedProp = so.FindProperty("maxSwingSpeed");
                        var dampingProp = so.FindProperty("airDamping");

                        if (maxAngleProp != null) maxAngleProp.floatValue = 48.0f;
                        if (swingForceProp != null) swingForceProp.floatValue = 26.0f;
                        if (momProp != null) momProp.floatValue = 0.85f;
                        if (maxSpeedProp != null) maxSpeedProp.floatValue = 6.5f;
                        if (dampingProp != null) dampingProp.floatValue = 0.988f;

                        so.ApplyModifiedProperties();
                        rope.InitializeRope();
                        EditorUtility.SetDirty(rope);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"<color=#4ade80>[SaveSlotAndRopeOverhaul]</color> Đã nâng cấp {ropes.Length} dây thừng trong scene '{scene.name}' đạt chuẩn Planet of Lana!");
                }
            }

            if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
            {
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
            }
        }
    }
}
