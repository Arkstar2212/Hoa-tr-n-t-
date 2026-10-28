using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;
using HoaVaMau.Gameplay;

public static class CutsceneToggleTool
{
    private static GameObject FindGameObjectInScene(string name)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name == name) return root;
            var match = FindInChildren(root.transform, name);
            if (match != null) return match;
        }
        return null;
    }

    private static GameObject FindInChildren(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child.gameObject;
            var match = FindInChildren(child, name);
            if (match != null) return match;
        }
        return null;
    }

    [MenuItem("Tools/Hoa Tren Dat Do/Cutscene/Tắt Cutscene (Chế Độ Edit & Test Nhanh) %&c")]
    public static void DisableCutsceneForEditing()
    {
        // 1. Tắt forcePlayIntro trên IntroCutsceneController
        var ctrl = Object.FindAnyObjectByType<IntroCutsceneController>();
        if (ctrl != null)
        {
            Undo.RecordObject(ctrl, "Disable Force Play Intro");
            var so = new SerializedObject(ctrl);
            var prop = so.FindProperty("forcePlayIntro");
            if (prop != null)
            {
                prop.boolValue = false;
                so.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(ctrl);
        }

        // 2. Ẩn Canvas màn đen Cutscene
        var canvas = FindGameObjectInScene("00_UI_CutsceneCanvas");
        if (canvas != null)
        {
            Undo.RecordObject(canvas, "Disable Cutscene Canvas");
            canvas.SetActive(false);
            EditorUtility.SetDirty(canvas);
        }

        // 3. Tắt camera bầu trời VCam_IntroSky và ưu tiên camera Gameplay
        var vcamSky = FindGameObjectInScene("VCam_IntroSky");
        if (vcamSky != null)
        {
            Undo.RecordObject(vcamSky, "Disable Intro Sky Camera");
            var cm = vcamSky.GetComponent<CinemachineCamera>();
            if (cm != null) cm.Priority = 0;
            vcamSky.SetActive(false);
            EditorUtility.SetDirty(vcamSky);
        }

        var vcamGameplay = FindGameObjectInScene("CM_VirtualCamera_2.5D");
        if (vcamGameplay != null)
        {
            Undo.RecordObject(vcamGameplay, "Enable Gameplay Camera");
            vcamGameplay.SetActive(true);
            var cm = vcamGameplay.GetComponent<CinemachineCamera>();
            if (cm != null) cm.Priority = 20;
            EditorUtility.SetDirty(vcamGameplay);
        }

        // 4. Kích hoạt điều khiển nhân vật nếu đang bị khóa
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Undo.RecordObject(player, "Enable Player Controller");
            player.enabled = true;
            EditorUtility.SetDirty(player);
        }

        // 5. Cập nhật IntroCutsceneState
        IntroCutsceneState.ShouldPlayIntroCutscene = false;
        IntroCutsceneState.HasViewedInSession = true;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=#38bdf8><b>[CutsceneToggleTool] ĐÃ TẮT CUTSCENE THÀNH CÔNG!</b></color>\n" +
                  "✔ Màn đen Cutscene đã được ẩn hoàn toàn để bạn tự do Edit trong Scene.\n" +
                  "✔ Camera chuyển về góc nhìn 2.5D của nhân vật.\n" +
                  "✔ Khi nhấn Play, bạn sẽ được điều khiển nhân vật ngay lập tức mà không phải chờ Intro.\n" +
                  "<i>(Phím tắt nhanh: Ctrl + Alt + C)</i>");
    }

    [MenuItem("Tools/Hoa Tren Dat Do/Cutscene/Bật Cutscene (Chế Độ Cinematic Đầy Đủ) %#&c")]
    public static void EnableCutscene()
    {
        // 1. Bật forcePlayIntro trên IntroCutsceneController
        var ctrl = Object.FindAnyObjectByType<IntroCutsceneController>();
        if (ctrl != null)
        {
            Undo.RecordObject(ctrl, "Enable Force Play Intro");
            ctrl.enabled = true;
            var so = new SerializedObject(ctrl);
            var propEnabled = so.FindProperty("m_Enabled");
            if (propEnabled != null) propEnabled.boolValue = true;
            var prop = so.FindProperty("forcePlayIntro");
            if (prop != null)
            {
                prop.boolValue = true;
                var propDirect = so.FindProperty("startWithSkyDescent");
                if (propDirect != null) propDirect.boolValue = true;
                so.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(ctrl);
        }

        // 2. Bật lại Canvas Cutscene
        var canvas = FindGameObjectInScene("00_UI_CutsceneCanvas");
        if (canvas != null)
        {
            Undo.RecordObject(canvas, "Enable Cutscene Canvas");
            canvas.SetActive(true);
            EditorUtility.SetDirty(canvas);
        }

        // 3. Khôi phục VCam_IntroSky
        var vcamSky = FindGameObjectInScene("VCam_IntroSky");
        if (vcamSky != null)
        {
            Undo.RecordObject(vcamSky, "Enable Intro Sky Camera");
            vcamSky.SetActive(true);
            var cm = vcamSky.GetComponent<CinemachineCamera>();
            if (cm != null) cm.Priority = 30;
            EditorUtility.SetDirty(vcamSky);
        }

        // 4. Cập nhật IntroCutsceneState
        IntroCutsceneState.ShouldPlayIntroCutscene = true;
        IntroCutsceneState.HasViewedInSession = false;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=#a3e635><b>[CutsceneToggleTool] ĐÃ BẬT LẠI CUTSCENE!</b></color>\n" +
                  "✔ Intro Cinematic đầy đủ (Máy chiếu phim tài liệu + Lời tựa + Camera hạ cánh) sẽ phát khi nhấn Play.");
    }
}
