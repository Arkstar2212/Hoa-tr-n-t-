using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Dialogue;
using HoaVaMau.Cutscene;

namespace HoaDo.Editor
{
    public static class SetupPierShipHornTrigger
    {
        private const string Scene3DPath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string Scene2DPath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string ShipHornClipPath = "Assets/Audio/SFX/SFX_Vintage_ShipHorn_1950s.mp3";

        [MenuItem("Tools/Hoa Do/Setup Pier Ship Horn & Cutscene Triggers")]
        public static void Execute()
        {
            Debug.Log("<color=#38bdf8>[ShipHornSetup] ĐANG THIẾT LẬP TIẾNG CÒI TÀU VÀ TRIGGER BẾN XUỒNG...</color>");

            var hornClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ShipHornClipPath);
            if (hornClip == null)
            {
                Debug.LogError($"[ShipHornSetup] Không tìm thấy file còi tàu: {ShipHornClipPath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(Scene3DPath, OpenSceneMode.Single);

            // 1. Assign shipHornClip to GenshinConversationSystem in 3D Village
            var convSys = Object.FindAnyObjectByType<GenshinConversationSystem>();
            if (convSys != null)
            {
                var so = new SerializedObject(convSys);
                var hornProp = so.FindProperty("shipHornClip");
                if (hornProp != null)
                {
                    hornProp.objectReferenceValue = hornClip;
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(convSys);
                Debug.Log("[ShipHornSetup] Đã gán shipHornClip vào GenshinConversationSystem.");
            }

            // 2. Setup Physical Trigger at Pier Tip (for walking out onto dock edge)
            string triggerName = "Trigger_PierDeparture_Tip";
            var existingTrigger = GameObject.Find(triggerName);
            if (existingTrigger != null) Object.DestroyImmediate(existingTrigger);

            var wharfParent = GameObject.Find("CaMau_1954_Village/Wharf_And_Boats") 
                           ?? GameObject.Find("Wharf_And_Boats")
                           ?? GameObject.Find("CaMau_1954_Village");

            var triggerGO = new GameObject(triggerName);
            if (wharfParent != null) triggerGO.transform.SetParent(wharfParent.transform, false);

            triggerGO.transform.position = new Vector3(56.0f, 0.89f, -43.8f);

            var col = triggerGO.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4.5f, 3.0f, 3.5f);
            col.center = new Vector3(0f, 1.0f, 0f);

            var pierTrigger = triggerGO.AddComponent<PierDepartureTrigger>();
            var pierSO = new SerializedObject(pierTrigger);
            pierSO.FindProperty("targetSceneName").stringValue = "Cutscene_EndGame";
            pierSO.FindProperty("waitBeforeFadeSeconds").floatValue = 1.0f;
            pierSO.FindProperty("fadeDurationSeconds").floatValue = 1.8f;
            pierSO.FindProperty("shipHornClip").objectReferenceValue = hornClip;
            pierSO.FindProperty("hornVolume").floatValue = 1.0f;
            pierSO.ApplyModifiedProperties();

            var audioSrc = triggerGO.GetComponent<AudioSource>();
            if (audioSrc != null)
            {
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0f;
                audioSrc.volume = 1f;
            }

            EditorUtility.SetDirty(triggerGO);
            Debug.Log($"[ShipHornSetup] Đã tạo trigger còi tàu tại mũi cầu tàu '{triggerName}' tại vị trí {triggerGO.transform.position}.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            // Return active scene to 2D Overhaul scene
            EditorSceneManager.OpenScene(Scene2DPath, OpenSceneMode.Single);

            Debug.Log("<color=#10b981>[ShipHornSetup] HOÀN TẤT THÀNH CÔNG! Tiếng còi tàu 1950s đã được cài đặt: cả khi nói chuyện với Chú Bảy lẫn khi bước ra mũi cầu tàu!</color>");
        }
    }
}
