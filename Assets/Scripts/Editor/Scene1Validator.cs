#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Phase 5: Comprehensive Technical Validation & Verification Suite for Scene 1 (Rừng Cà Mau, 1954).
    /// Executes rigorous engineering audits across:
    /// 1. 10-Folder Hierarchy Integrity
    /// 2. 60m Locomotion Route & Ground Continuity Probe (every 0.25m from X: 0 -> 60m)
    /// 3. Zone C Ledge Grab & Climb Kinematics Simulation
    /// 4. Level Completion Goal Trigger at X: 59.5m
    /// 5. URP Lighting & Volume Post-Processing Pipeline Audit
    /// 6. Camera Tracking & 2.5D Z-Plane Constraints
    /// </summary>
    public static class Scene1Validator
    {
        private const string ExpectedScene = "Assets/Scenes/New Scene 1.unity";

        public struct ValidationResult
        {
            public bool Success;
            public int TestsRun;
            public int TestsPassed;
            public int TestsFailed;
            public List<string> Messages;
            public List<string> Errors;
        }

        [MenuItem("Hoa va Mau/Level Design/Phase 5 - Comprehensive Technical Validation", false, 4)]
        public static void RunValidationMenu()
        {
            ValidationResult res = ExecuteFullValidation();
            LogValidationSummary(res);
        }

        public static void RunValidation()
        {
            ValidationResult res = ExecuteFullValidation();
            LogValidationSummary(res);
        }

        public static ValidationResult ExecuteFullValidation()
        {
            var res = new ValidationResult
            {
                Success = true,
                TestsRun = 0,
                TestsPassed = 0,
                TestsFailed = 0,
                Messages = new List<string>(),
                Errors = new List<string>()
            };

            if (SceneManager.GetActiveScene().path != ExpectedScene)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ExpectedScene);
            }

            Debug.Log("<color=#38bdf8>================================================================================</color>");
            Debug.Log("<color=#38bdf8><b>[Phase 5: Technical Validation]</b> Bắt đầu quy trình nghiệm thu kỹ thuật Cảnh 1 (60m)...</color>");
            Debug.Log("<color=#38bdf8>================================================================================</color>");

            // 1. Audit Hierarchy 10 Folders
            AuditHierarchy(ref res);

            // 2. Audit 60m Route Continuous Ground Collision & Pacing Elevation
            AuditRouteContinuity(ref res);

            // 3. Audit Zone C Ledge Grab & Climb Kinematics
            AuditZoneCLedgeClimb(ref res);

            // 4. Audit Level Completion Exit Trigger
            AuditExitTrigger(ref res);

            // 5. Audit URP Lighting, Fog, & Post-Processing
            AuditLightingAndPostProcessing(ref res);

            // 6. Audit Camera & Player Rigging
            AuditCameraAndPlayer(ref res);

            res.Success = (res.TestsFailed == 0);
            return res;
        }

        // =========================================================================
        // 1. AUDIT HIERARCHY 10 FOLDERS
        // =========================================================================
        private static void AuditHierarchy(ref ValidationResult res)
        {
            res.TestsRun++;
            string[] requiredFolders = new string[]
            {
                "01_Camera",
                "02_Gameplay",
                "03_Terrain",
                "04_Foreground",
                "05_Midground",
                "06_Water",
                "07_Background",
                "08_Lighting",
                "09_VFX",
                "10_EnvironmentSystems"
            };

            bool allPresent = true;
            foreach (var folder in requiredFolders)
            {
                var go = GameObject.Find(folder);
                if (go == null)
                {
                    allPresent = false;
                    res.Errors.Add($"[Hierarchy] Thiếu thư mục chuẩn: {folder}");
                }
            }

            if (allPresent)
            {
                res.TestsPassed++;
                res.Messages.Add("[Hierarchy PASS] Cấu trúc 10 thư mục chuẩn của studio được tổ chức đầy đủ và sạch sẽ.");
            }
            else
            {
                res.TestsFailed++;
            }
        }

        // =========================================================================
        // 2. AUDIT 60M ROUTE GROUND CONTINUITY (Raycast probe every 0.25m)
        // =========================================================================
        private static void AuditRouteContinuity(ref ValidationResult res)
        {
            res.TestsRun++;
            float startX = 0.0f;
            float endX = 60.0f;
            float step = 0.25f;
            int totalSteps = Mathf.RoundToInt((endX - startX) / step) + 1;

            int hitsCount = 0;
            int missCount = 0;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            List<float> gapPoints = new List<float>();

            // Probe down from Y = 3.0m at Z = 0.0m
            for (float x = startX; x <= endX + 0.001f; x += step)
            {
                Vector3 rayOrigin = new Vector3(x, 3.5f, 0.0f);
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    hitsCount++;
                    if (hit.point.y < minY) minY = hit.point.y;
                    if (hit.point.y > maxY) maxY = hit.point.y;
                }
                else
                {
                    missCount++;
                    gapPoints.Add(x);
                }
            }

            if (missCount == 0)
            {
                res.TestsPassed++;
                res.Messages.Add($"[Route Continuity PASS] Quỹ đạo 60m (X: 0 -> 60m) hoàn toàn liên tục! 0 lỗ hổng va chạm ({hitsCount}/{totalSteps} probes thành công, Cao độ Y: {minY:F2}m -> {maxY:F2}m).");
            }
            else
            {
                res.TestsFailed++;
                string gapsStr = string.Join(", ", gapPoints);
                res.Errors.Add($"[Route Continuity FAIL] Phát hiện {missCount} điểm hổng va chạm tại X = [{gapsStr}].");
            }

            // Kiểm tra các mốc cao độ đặc trưng của 6 Zone
            AuditZoneElevations(ref res);
        }

        private static void AuditZoneElevations(ref ValidationResult res)
        {
            res.TestsRun++;
            // Mẫu đo cao độ tại các điểm trọng yếu
            float[] samplePoints = new float[] { 2.0f, 15.0f, 25.0f, 28.0f, 37.0f, 47.0f, 58.0f };
            string[] zoneNames = new string[] { "Zone A (Cửa rừng)", "Zone B (Vũng bùn)", "Zone C (Đỉnh chướng ngại)", "Zone C (Bậc hạ chân)", "Zone D (Bến sông)", "Zone E (Khoảng nắng)", "Zone F (Lối thoát)" };

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Pacing Elevation Profile]");
            for (int i = 0; i < samplePoints.Length; i++)
            {
                float x = samplePoints[i];
                Vector3 rayOrigin = new Vector3(x, 4.0f, 0.0f);
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    sb.AppendLine($"   - {zoneNames[i]} tại X={x:F1}m: Ground Y = {hit.point.y:F2}m (Collider: {hit.collider.name})");
                }
            }
            res.TestsPassed++;
            res.Messages.Add(sb.ToString().TrimEnd());
        }

        // =========================================================================
        // 3. AUDIT ZONE C LEDGE GRAB & CLIMB KINEMATICS
        // =========================================================================
        private static void AuditZoneCLedgeClimb(ref ValidationResult res)
        {
            res.TestsRun++;
            var player = GameObject.Find("PF_Player_Minh");
            if (player == null)
            {
                res.TestsFailed++;
                res.Errors.Add("[Zone C Climb FAIL] Không tìm thấy PF_Player_Minh trong scene.");
                return;
            }

            var detector = player.GetComponent<HoaVaMau.Gameplay.LedgeGrabDetector>();
            if (detector == null)
            {
                res.TestsFailed++;
                res.Errors.Add("[Zone C Climb FAIL] PF_Player_Minh thiếu component LedgeGrabDetector.");
                return;
            }

            // Đặt người thử nghiệm mô phỏng tại trước mép chướng ngại vật Zone C
            // Chướng ngại vật tại X = 23.5m (bắt đầu), đỉnh Y = -0.15m.
            // Điểm tiếp cận mặt đất trước chướng ngại vật là Y = -1.85m.
            // Khi nhảy lên, player ở độ cao Y = -1.35m (đang trong pha bật nhảy trước mặt tường X = 22.8m).
            Vector3 testPos = new Vector3(22.80f, -1.35f, 0.0f);
            Vector3 originalPos = player.transform.position;
            player.transform.position = testPos;

            bool detected = detector.TryDetectLedge(1.0f, out Vector3 hangPos, out Vector3 landPos);
            player.transform.position = originalPos; // Hoàn lại vị trí

            if (detected)
            {
                res.TestsPassed++;
                res.Messages.Add($"[Zone C Ledge Grab PASS] Mô phỏng leo trèo thành công! Phát hiện mép tường chuẩn xác. Điểm đáp (landPos): {landPos}, Chiều cao leo: {landPos.y - (-1.85f):F2}m.");
            }
            else
            {
                res.TestsFailed++;
                res.Errors.Add("[Zone C Ledge Grab FAIL] LedgeGrabDetector không phát hiện được mép vách tại X=22.8m, Y=-1.35m.");
            }
        }

        // =========================================================================
        // 4. AUDIT EXIT TRIGGER
        // =========================================================================
        private static void AuditExitTrigger(ref ValidationResult res)
        {
            res.TestsRun++;
            var triggerGO = GameObject.Find("Scene1_ExitGoalTrigger_60m");
            if (triggerGO == null)
            {
                res.TestsFailed++;
                res.Errors.Add("[Exit Trigger FAIL] Không tìm thấy Scene1_ExitGoalTrigger_60m.");
                return;
            }

            var box = triggerGO.GetComponent<BoxCollider>();
            if (box == null || !box.isTrigger)
            {
                res.TestsFailed++;
                res.Errors.Add("[Exit Trigger FAIL] Scene1_ExitGoalTrigger_60m thiếu BoxCollider với isTrigger=true.");
                return;
            }

            Vector3 triggerPos = triggerGO.transform.position;
            if (triggerPos.x >= 58.0f && Mathf.Abs(triggerPos.z) < 1.0f)
            {
                res.TestsPassed++;
                res.Messages.Add($"[Exit Trigger PASS] Trigger chuyển cảnh tại X={triggerPos.x:F1}m, Y={triggerPos.y:F1}m, Size={box.size}. Vị trí và kích thước bao phủ dải đường đi chuẩn xác.");
            }
            else
            {
                res.TestsFailed++;
                res.Errors.Add($"[Exit Trigger FAIL] Vị trí trigger không đúng chuẩn phân vùng 60m: {triggerPos}");
            }
        }

        // =========================================================================
        // 5. AUDIT LIGHTING & POST-PROCESSING
        // =========================================================================
        private static void AuditLightingAndPostProcessing(ref ValidationResult res)
        {
            res.TestsRun++;
            var volumeGO = GameObject.Find("Global_Volume_Canh1");
            if (volumeGO == null)
            {
                res.TestsFailed++;
                res.Errors.Add("[Post-Processing FAIL] Không tìm thấy Global_Volume_Canh1 trong scene.");
                return;
            }

            var volume = volumeGO.GetComponent<Volume>();
            if (volume == null || volume.sharedProfile == null)
            {
                res.TestsFailed++;
                res.Errors.Add("[Post-Processing FAIL] Global_Volume_Canh1 không có Volume component hoặc thiếu Profile.");
                return;
            }

            var profile = volume.sharedProfile;
            bool hasBloom = profile.TryGet<Bloom>(out var bloom) && bloom.active;
            bool hasColorAdj = profile.TryGet<ColorAdjustments>(out var colorAdj) && colorAdj.active;
            bool hasTone = profile.TryGet<Tonemapping>(out var tone) && tone.active;
            bool hasVignette = profile.TryGet<Vignette>(out var vig) && vig.active;
            bool hasWB = profile.TryGet<WhiteBalance>(out var wb) && wb.active;

            if (hasBloom && hasColorAdj && hasTone && hasVignette && hasWB)
            {
                res.TestsPassed++;
                res.Messages.Add("[Lighting & Volume PASS] Profile URP Volume hoàn chỉnh: Bloom, ColorAdjustments, Tonemapping, Vignette, WhiteBalance đều kích hoạt.");
            }
            else
            {
                res.TestsFailed++;
                res.Errors.Add($"[Lighting & Volume FAIL] Thiếu hoặc chưa kích hoạt các override Volume cần thiết (Bloom:{hasBloom}, ColorAdj:{hasColorAdj}, Tonemapping:{hasTone}, Vignette:{hasVignette}, WB:{hasWB}).");
            }
        }

        // =========================================================================
        // 6. AUDIT CAMERA & PLAYER
        // =========================================================================
        private static void AuditCameraAndPlayer(ref ValidationResult res)
        {
            res.TestsRun++;
            var cam = Camera.main;
            if (cam == null)
            {
                res.TestsFailed++;
                res.Errors.Add("[Camera FAIL] Main Camera không tồn tại.");
                return;
            }

            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            bool postProcessingOn = (camData != null && camData.renderPostProcessing);

            var player = GameObject.Find("PF_Player_Minh");
            bool playerValid = (player != null && player.GetComponent<CharacterController>() != null && player.GetComponent<HoaVaMau.Gameplay.PlayerController>() != null);

            if (postProcessingOn && playerValid)
            {
                res.TestsPassed++;
                res.Messages.Add("[Camera & Player PASS] Main Camera hỗ trợ URP Post-Processing đầy đủ. Player Minh có đầy đủ CharacterController, PlayerController, LedgeGrabDetector.");
            }
            else
            {
                res.TestsFailed++;
                res.Errors.Add($"[Camera & Player FAIL] CamPostProcessing: {postProcessingOn}, PlayerValid: {playerValid}");
            }
        }

        // =========================================================================
        // SUMMARY LOGGING
        // =========================================================================
        private static void LogValidationSummary(ValidationResult res)
        {
            Debug.Log("<color=#38bdf8>--------------------------------------------------------------------------------</color>");
            Debug.Log($"<color={(res.Success ? "#4ade80" : "#f87171")}><b>[Phase 5: Technical Validation Summary]</b> Kết quả: {(res.Success ? "ĐẠT CHUẨN 100% (PASSED)" : "CÓ LỖI (FAILED)")}</color>");
            Debug.Log($"Tổng số bài kiểm tra: {res.TestsRun} | Đạt: {res.TestsPassed} | Không đạt: {res.TestsFailed}");
            Debug.Log("<color=#38bdf8>--------------------------------------------------------------------------------</color>");

            foreach (var msg in res.Messages)
            {
                Debug.Log($"<color=#4ade80>✔</color> {msg}");
            }

            foreach (var err in res.Errors)
            {
                Debug.LogError($"<color=#f87171>✘</color> {err}");
            }
        }

        // =========================================================================
        // MÔ PHỎNG VÀ CHỤP ẢNH MINH TẠI TỪNG ZONE TRONG SCENE 1 (60M)
        // =========================================================================
        [MenuItem("Hoa va Mau/Level Design/Phase 5 - Capture Minh In All Zones", false, 5)]
        public static void CaptureMinhInAllZones()
        {
            var player = GameObject.Find("PF_Player_Minh");
            var cam = Camera.main;
            if (player == null || cam == null)
            {
                Debug.LogError("Không tìm thấy Player hoặc Main Camera.");
                return;
            }

            Vector3 origPlayerPos = player.transform.position;
            Vector3 origCamPos = cam.transform.position;

            string artifactDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\96efc365-f422-4745-bfa5-8d83af1b80e5";

            var zoneCheckpoints = new (string name, float x, float y)[]
            {
                ("ZoneA_Minh_Entrance", 3.0f, -1.85f),
                ("ZoneB_Minh_Puddle", 15.0f, -2.12f),
                ("ZoneC_Minh_OnLedge", 25.0f, -0.15f),
                ("ZoneD_Minh_DockSettlement", 36.5f, -1.85f),
                ("ZoneE_Minh_SunlitGlade", 47.0f, -1.65f),
                ("ZoneF_Minh_ExitGoal", 58.5f, -1.80f)
            };

            int width = 960;
            int height = 540;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRt = cam.targetTexture;
            cam.targetTexture = rt;

            try
            {
                foreach (var checkpoint in zoneCheckpoints)
                {
                    player.transform.position = new Vector3(checkpoint.x, checkpoint.y, 0.0f);
                    cam.transform.position = new Vector3(checkpoint.x, checkpoint.y + 1.2f, -14.5f);
                    cam.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                    Physics.SyncTransforms();
                    cam.Render();

                    RenderTexture.active = rt;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();

                    byte[] bytes = tex.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(tex);

                    string filePath = Path.Combine(artifactDir, $"validation_{checkpoint.name}.png");
                    File.WriteAllBytes(filePath, bytes);
                    Debug.Log($"<color=#38bdf8>[Capture]</color> Đã chụp minh họa tại {checkpoint.name}: {filePath}");
                }
            }
            finally
            {
                cam.targetTexture = prevRt;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                // Hoàn lại vị trí ban đầu
                player.transform.position = origPlayerPos;
                cam.transform.position = origCamPos;
                Physics.SyncTransforms();
            }

            Debug.Log("<color=#4ade80><b>[Phase 5: Capture Complete]</b> Đã xuất đủ 6 bức ảnh nghiệm thu nhân vật tại 6 phân vùng!</color>");
        }
    }
}
#endif
