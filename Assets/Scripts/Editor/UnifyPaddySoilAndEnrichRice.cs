using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HoaVaMau.EditorScripts
{
    public static class UnifyPaddySoilAndEnrichRice
    {
        [MenuItem("Tools/CaMau/Unify Paddy Soil And Enrich Rice")]
        public static void Execute()
        {
            Debug.Log("<color=#40b8a6>[UnifyPaddySoilAndEnrichRice]</color> Bắt đầu đồng bộ màu đất và tăng mật độ lúa...");

            var activeScene = SceneManager.GetActiveScene();

            // =========================================================================
            // 1. ĐỒNG BỘ MÀU ĐẤT TRONG RUỘNG (UNIFY SOIL MATERIAL ACROSS ALL 22 PLOTS)
            // =========================================================================
            var villageSoilMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/M_Ground_Grass_VillageSoil.mat");
            if (villageSoilMat == null)
            {
                var guids = AssetDatabase.FindAssets("M_Ground_Grass_VillageSoil");
                if (guids.Length > 0)
                {
                    villageSoilMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            // Đồng bộ luôn 2 file material M_RiceCrop_GoldenRipe và M_RiceCrop_LushGreen để tuyệt đối không còn bị lệch màu
            var matGolden = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_RiceCrop_GoldenRipe.mat");
            var matLush = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_RiceCrop_LushGreen.mat");

            Color unifiedSoilColor = villageSoilMat != null ? villageSoilMat.GetColor("_BaseColor") : new Color(0.48f, 0.72f, 0.30f, 1f);
            Texture2D unifiedSoilTex = villageSoilMat != null ? (Texture2D)villageSoilMat.mainTexture : null;

            if (matGolden != null)
            {
                Undo.RecordObject(matGolden, "Unify Golden Rice Plot Mat");
                matGolden.SetColor("_BaseColor", unifiedSoilColor);
                if (unifiedSoilTex != null) matGolden.mainTexture = unifiedSoilTex;
                EditorUtility.SetDirty(matGolden);
            }
            if (matLush != null)
            {
                Undo.RecordObject(matLush, "Unify Lush Rice Plot Mat");
                matLush.SetColor("_BaseColor", unifiedSoilColor);
                if (unifiedSoilTex != null) matLush.mainTexture = unifiedSoilTex;
                EditorUtility.SetDirty(matLush);
            }

            // Gán trực tiếp material đất chuẩn cho cả 22 thửa ruộng
            var allRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int plotsUnified = 0;
            var plotDict = new Dictionary<int, GameObject>();

            foreach (var mr in allRenderers)
            {
                if (mr == null || mr.gameObject == null) continue;
                string n = mr.gameObject.name;

                // Thửa ruộng chính có tên dạng "Paddy_Plot_XX" (không chứa _Dike hay _R0)
                if (n.StartsWith("Paddy_Plot_") && !n.Contains("_Dike") && !n.Contains("_R0") && !n.Contains("Filler"))
                {
                    // Lấy số thứ tự thửa ruộng
                    string[] parts = n.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int plotIdx))
                    {
                        plotDict[plotIdx] = mr.gameObject;
                    }

                    if (villageSoilMat != null)
                    {
                        Undo.RecordObject(mr, "Set Unified Paddy Soil Material");
                        mr.sharedMaterial = villageSoilMat;
                        EditorUtility.SetDirty(mr);
                        plotsUnified++;
                    }
                }
            }
            Debug.Log($"<color=#40b8a6>[UnifyPaddySoilAndEnrichRice]</color> Đã đồng bộ màu nền đất cho {plotsUnified} thửa ruộng!");

            // =========================================================================
            // 2. TĂNG KÍCH THƯỚC VÀ ĐỘ DÀY CHO TẤT CẢ CÁC KHÓM LÚA HIỆN CÓ
            // =========================================================================
            // Lúa hiện tại scale 0.90 khá nhỏ, khoảng cách giữa các hàng 1.35m để lộ nhiều khoảng đất trống.
            // Nâng scale lên 1.28 - 1.35m để các khóm lúa nở to, trĩu hạt, cành lá chạm nhau tạo thành thảm lúa vàng óng ả!
            var riceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RicePlants/Mat_Rice_Meshy.mat");
            if (riceMat == null)
            {
                var guids = AssetDatabase.FindAssets("Mat_Rice_Meshy");
                if (guids.Length > 0) riceMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            int tuftsScaled = 0;
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                if (t.name.StartsWith("Tuft_Row_"))
                {
                    // Random scale nhẹ để tạo cảm giác ruộng lúa tự nhiên không bị khuôn mẫu
                    float baseScale = UnityEngine.Random.Range(1.24f, 1.34f);
                    float heightScale = baseScale * UnityEngine.Random.Range(1.02f, 1.14f);
                    t.localScale = new Vector3(baseScale, heightScale, baseScale);

                    // Xoay góc nhẹ ngẫu nhiên để các bông lúa hướng đa chiều
                    t.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                    // Đảm bảo dùng vật liệu chuẩn
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr != null && riceMat != null)
                    {
                        mr.sharedMaterial = riceMat;
                    }

                    tuftsScaled++;
                }
            }
            Debug.Log($"<color=#40b8a6>[UnifyPaddySoilAndEnrichRice]</color> Đã nâng kích thước và độ xum xuê cho {tuftsScaled} khóm lúa hiện có.");

            // =========================================================================
            // 3. BỔ SUNG CÁC HÀNG LÚA MỚI VÀO CÁC VÙNG TRỐNG TRONG TỪNG THỬA RUỘNG
            // =========================================================================
            var prefabStrip = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_RowStrip_4m.prefab");
            var riceMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_0.fbx");
            if (riceMesh == null)
            {
                var sampleTuft = GameObject.Find("Tuft_Row_0");
                if (sampleTuft != null) riceMesh = sampleTuft.GetComponent<MeshFilter>()?.sharedMesh;
            }

            int fillerRowsCreated = 0;
            int fillerTuftsCreated = 0;

            // Xóa các nhóm filler cũ nếu đã có
            var existingFillers = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingFillers)
            {
                if (go != null && go.name.Contains("_DenseFiller_"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            foreach (var kvp in plotDict)
            {
                int plotIdx = kvp.Key;
                GameObject plotGO = kvp.Value;
                var mr = plotGO.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                Bounds b = mr.bounds;
                float minX = b.min.x + 1.2f;
                float maxX = b.max.x - 1.2f;
                float minZ = b.min.z + 0.8f;
                float maxZ = b.max.z - 0.8f;
                float groundY = b.center.y + 0.01f;

                // Thu thập toạ độ Z của các hàng hiện có trong thửa này
                var existingZs = new List<float>();
                var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                string prefix = $"Paddy_Plot_{plotIdx:D2}_";
                foreach (var go in allGOs)
                {
                    if (go.name.StartsWith(prefix) && go.name.Contains("_R0"))
                    {
                        existingZs.Add(go.transform.position.z);
                    }
                }

                existingZs.Sort();

                // Tạo nhóm chứa filler cho thửa này
                var fillerGroup = new GameObject($"{plotGO.name}_DenseFiller_Root");
                fillerGroup.transform.SetParent(plotGO.transform.parent, true);
                fillerGroup.transform.position = Vector3.zero;

                // 3A. Bổ sung hàng lúa ở mép Bắc (maxZ) và mép Nam (minZ) nếu chưa có hàng sát mép
                bool needsNorthRow = existingZs.Count == 0 || (maxZ - existingZs[existingZs.Count - 1] > 1.2f);
                bool needsSouthRow = existingZs.Count == 0 || (existingZs[0] - minZ > 1.2f);

                var targetZsToFill = new List<float>();
                if (needsNorthRow) targetZsToFill.Add(maxZ);
                if (needsSouthRow) targetZsToFill.Add(minZ);

                // 3B. Bổ sung các hàng xen kẽ vào giữa các khoảng cách rộng (> 1.25m)
                for (int i = 0; i < existingZs.Count - 1; i++)
                {
                    float gap = existingZs[i + 1] - existingZs[i];
                    if (gap > 1.25f)
                    {
                        targetZsToFill.Add((existingZs[i] + existingZs[i + 1]) * 0.5f);
                    }
                }

                // Đặt hàng lúa mới tại các toạ độ Z cần bổ sung
                foreach (float z in targetZsToFill)
                {
                    // Trải các khóm lúa liên tục từ minX đến maxX theo bước ~0.70m
                    float curX = minX;
                    int rowSubIdx = 0;
                    var rowParent = new GameObject($"DenseRow_P{plotIdx:D2}_Z{z:F1}");
                    rowParent.transform.SetParent(fillerGroup.transform, true);
                    rowParent.transform.position = new Vector3(b.center.x, groundY, z);

                    while (curX <= maxX)
                    {
                        var tuftGO = new GameObject($"Tuft_Fill_{rowSubIdx}");
                        tuftGO.transform.SetParent(rowParent.transform, true);
                        float jx = curX + UnityEngine.Random.Range(-0.08f, 0.08f);
                        float jz = z + UnityEngine.Random.Range(-0.06f, 0.06f);
                        tuftGO.transform.position = new Vector3(jx, groundY, jz);

                        float s = UnityEngine.Random.Range(1.24f, 1.35f);
                        tuftGO.transform.localScale = new Vector3(s, s * 1.08f, s);
                        tuftGO.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                        var mf = tuftGO.AddComponent<MeshFilter>();
                        mf.sharedMesh = riceMesh;

                        var rend = tuftGO.AddComponent<MeshRenderer>();
                        rend.sharedMaterial = riceMat;

                        curX += 0.70f;
                        rowSubIdx++;
                        fillerTuftsCreated++;
                    }
                    fillerRowsCreated++;
                }
            }

            Debug.Log($"<color=#40b8a6>[UnifyPaddySoilAndEnrichRice]</color> Đã thêm {fillerRowsCreated} hàng lúa mới với {fillerTuftsCreated} khóm lúa dày đặc vào các thửa ruộng!");

            // Đánh dấu Scene Dirty để lưu
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=#40b8a6>[UnifyPaddySoilAndEnrichRice]</color> HOÀN TẤT! Đã đồng bộ màu đất toàn bộ và phủ kín lúa vàng óng cho tất cả các thửa ruộng.");
        }
    }
}
