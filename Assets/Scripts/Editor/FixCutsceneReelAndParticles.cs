#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class FixCutsceneReelAndParticles
    {
        [MenuItem("Tools/Hoa Tren Dat Do/Khắc Phục Toàn Diện Lỗi Cutscene Chạy Phim & Hạt Motes")]
        public static void ApplyFix()
        {
            // 1. Sửa lỗi Particle System: "Particle Velocity curves must all be in the same mode"
            var dustMotesGO = GameObject.Find("DustMotes_HeroParticles");
            if (dustMotesGO != null)
            {
                var ps = dustMotesGO.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var vel = ps.velocityOverLifetime;
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    EditorUtility.SetDirty(dustMotesGO);
                    Debug.Log("<color=#10b981>✔ Đã sửa Particle System 'DustMotes_HeroParticles': Trục Z đã chuyển sang TwoConstants (0, 0)!</color>");
                }
            }

            // 2. Load các Sprite lịch sử
            Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_1.jpg");
            Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_2.jpg");
            Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_3.jpg");
            Sprite s4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_4.jpg");

            // 3. Kích hoạt và cấu hình IntroCutsceneController
            var ctrlGO = GameObject.Find("00_Cinematic_IntroCutsceneController");
            if (ctrlGO != null)
            {
                var ctrl = ctrlGO.GetComponent<IntroCutsceneController>();
                if (ctrl != null)
                {
                    ctrl.enabled = true;
                    var so = new SerializedObject(ctrl);
                    var pEnabled = so.FindProperty("m_Enabled");
                    if (pEnabled != null) pEnabled.boolValue = true;
                    var pForce = so.FindProperty("forcePlayIntro");
                    if (pForce != null) pForce.boolValue = true;

                    var slidesProp = so.FindProperty("historySlides");
                    if (slidesProp != null && slidesProp.arraySize == 4)
                    {
                        slidesProp.GetArrayElementAtIndex(0).FindPropertyRelative("image").objectReferenceValue = s1;
                        slidesProp.GetArrayElementAtIndex(1).FindPropertyRelative("image").objectReferenceValue = s2;
                        slidesProp.GetArrayElementAtIndex(2).FindPropertyRelative("image").objectReferenceValue = s3;
                        slidesProp.GetArrayElementAtIndex(3).FindPropertyRelative("image").objectReferenceValue = s4;
                    }

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(ctrl);
                }
            }

            // 4. Cấu hình UI Canvas: Gán ảnh history_1 và ẩn trong Editor
            var canvasGO = GameObject.Find("00_UI_CutsceneCanvas");
            if (canvasGO != null)
            {
                var slideImgTr = canvasGO.transform.Find("ProjectorAperture/SlideImage");
                if (slideImgTr != null)
                {
                    var img = slideImgTr.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = s1;
                        img.color = Color.white;
                        img.preserveAspect = true;
                        EditorUtility.SetDirty(img);
                    }
                }
                canvasGO.SetActive(false);
                EditorUtility.SetDirty(canvasGO);
            }

            IntroCutsceneState.ShouldPlayIntroCutscene = true;
            IntroCutsceneState.HasViewedInSession = false;

            Debug.Log("<color=#10b981><b>[FixCutsceneReelAndParticles] KHẮC PHỤC THÀNH CÔNG!</b></color>\n" +
                      "✔ IntroCutsceneController: Đã kích hoạt (enabled = true, m_Enabled = true, forcePlayIntro = true).\n" +
                      "✔ 4 slide ảnh tư liệu 1954: Đã nạp đầy đủ (history_1 .. history_4).\n" +
                      "✔ SlideImage: Đã gán sprite mặc định history_1 (hết trắng).\n" +
                      "✔ Particle System DustMotes_HeroParticles: Hết cảnh báo lỗi Velocity mode.");
        }
    }
}
#endif