using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Công cụ chuẩn hóa và tối ưu hóa hiệu năng đồ họa cao (60+ FPS) cho Scene 3D CaMau_1954_Village.unity:
    /// 1. Tích hợp mesh tối ưu hóa từ Blender (Quadric Error Decimation + Smooth Shading) bảo toàn độ cong mịn,
    ///    đường nét viền mượt mà của lu nước, thùng gỗ, tảng đá, nhà sàn, giếng nước, trâu, xuồng...
    /// 2. Cập nhật mesh dữ liệu cao vào Assets/Models/Optimized/ và đồng bộ tự động vào các Prefab/Scene.
    /// 3. Tối ưu Shadow Casters trên các prop nhỏ và rút gọn Camera FarClip.
    /// 4. Đảm bảo BẢO TOÀN TUYỆT ĐỐI Player và New Scene 1_Overhaul.unity (Chỉ tác động CaMau_1954_Village).
    /// </summary>
    public static class OptimizeVillage3DScene
    {
        public const string Scene3DPath = "Assets/Scenes/CaMau_1954_Village.unity";
        public const string OptimizedMeshFolder = "Assets/Models/Optimized";

        // Cấu hình đồng bộ giữa FBX gốc, FBX tối ưu từ Blender, và file .asset tối ưu (nếu có)
        private static readonly (string origFbx, string blenderFbx, string assetName)[] ModelMappings = new[]
        {
            ("Assets/Models/Model_Lu.fbx", "Assets/Models/Optimized/SM_Lu_BlenderOptimized.fbx", "M_Lu_Nuoc_Optimized.asset"),
            ("Assets/Models/Props/SM_WoodenCrate.fbx", "Assets/Models/Optimized/SM_WoodenCrate_BlenderOptimized.fbx", "M_WoodenCrate_Optimized.asset"),
            ("Assets/Models/Environment/SM_Rock_Large_01.fbx", "Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx", "M_Rock_Large_Optimized.asset"),
            ("Assets/Models/Environment/SM_MossyBoulder_01.fbx", "Assets/Models/Optimized/SM_MossyBoulder_BlenderOptimized.fbx", "M_MossyBoulder_Optimized.asset"),
            ("Assets/Models/Environment/SM_StiltedHouse_Thatched.fbx", "Assets/Models/Optimized/SM_StiltedHouse_Thatched_BlenderOptimized.fbx", "M_House_Thatched_Optimized.asset"),
            ("Assets/Models/Environment/SM_StiltedHouse_Wood.fbx", "Assets/Models/Optimized/SM_StiltedHouse_Wood_BlenderOptimized.fbx", "M_House_Wood_Optimized.asset"),
            ("Assets/Models/Props/SM_WoodenSlatCube.fbx", "Assets/Models/Optimized/SM_WoodenSlatCube_BlenderOptimized.fbx", "M_WoodenSlatCube_Optimized.asset"),
            ("Assets/Models/Environment/SM_Rock_Medium_02.fbx", "Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx", null),
            ("Assets/Models/Environment/SM_LoneBoulder.fbx", "Assets/Models/Optimized/SM_LoneBoulder_BlenderOptimized.fbx", null),
            ("Assets/Models/GiengNuoc.fbx", "Assets/Models/Optimized/SM_GiengNuoc_BlenderOptimized.fbx", null),
            ("Assets/Models/Buffalo.fbx", "Assets/Models/Optimized/SM_Buffalo_BlenderOptimized.fbx", null),
            ("Assets/Models/Xe_keo.fbx", "Assets/Models/Optimized/SM_Xe_keo_BlenderOptimized.fbx", null),
            ("Assets/Models/Xuong_go.fbx", "Assets/Models/Optimized/SM_Xuong_go_BlenderOptimized.fbx", null),
            ("Assets/Models/Cong_lang.fbx", "Assets/Models/Optimized/SM_Cong_lang_BlenderOptimized.fbx", null),
            ("Assets/Models/Nhà tranh.fbx", "Assets/Models/Optimized/SM_NhaTranh_BlenderOptimized.fbx", null)
        };

        [MenuItem("Tools/Hoa Tren Dat Do/Optimize Ca Mau 3D Village Scene (60 FPS)")]
        public static void OptimizeVillageSceneMenu()
        {
            Debug.Log("<color=#38bdf8>[OptimizeVillage]</color> Bắt đầu quy trình tối ưu hóa chất lượng cao Scene 3D Làng Cà Mau 1954...");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var meshMap = new Dictionary<string, UnityEngine.Mesh>(StringComparer.OrdinalIgnoreCase);

            // 1. Nạp các Blender-optimized meshes và cập nhật vào .asset
            foreach (var item in ModelMappings)
            {
                UnityEngine.Mesh blenderMesh = GetMeshFromFBX(item.blenderFbx);
                if (blenderMesh == null)
                {
                    Debug.LogWarning($"[OptimizeVillage] Chưa tìm thấy mesh trong Blender FBX: {item.blenderFbx}");
                    continue;
                }

                // Đăng ký vào bảng tra cứu cho cả FBX gốc và FBX tối ưu
                meshMap[item.origFbx] = blenderMesh;
                meshMap[item.blenderFbx] = blenderMesh;

                // Nếu có file .asset đại diện, sao chép dữ liệu mesh để giữ nguyên toàn bộ tham chiếu cũ
                if (!string.IsNullOrEmpty(item.assetName))
                {
                    string assetPath = $"{OptimizedMeshFolder}/{item.assetName}";
                    UnityEngine.Mesh existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(assetPath);
                    if (existingAsset != null)
                    {
                        CopyMeshData(blenderMesh, existingAsset);
                        meshMap[assetPath] = existingAsset;
                        Debug.Log($"[OptimizeVillage] Đã nâng cấp {item.assetName} sang dạng mịn Blender QEM ({existingAsset.triangles.Length / 3:N0} tris)");
                    }
                    else
                    {
                        var newAsset = UnityEngine.Object.Instantiate(blenderMesh);
                        newAsset.name = Path.GetFileNameWithoutExtension(item.assetName);
                        AssetDatabase.CreateAsset(newAsset, assetPath);
                        meshMap[assetPath] = newAsset;
                        Debug.Log($"[OptimizeVillage] Đã tạo mới {item.assetName} ({newAsset.triangles.Length / 3:N0} tris)");
                    }
                }
            }

            AssetDatabase.SaveAssets();

            // 2. Cập nhật các Prefab môi trường và prop (TUYỆT ĐỐI KHÔNG ĐỤNG ĐẾN PREFAB PLAYER)
            UpdatePrefabMesh("Assets/Prefabs/Model_Lu.prefab", meshMap, "Assets/Models/Model_Lu.fbx", true);
            UpdatePrefabMesh("Assets/Prefabs/Props/PF_Prop_CargoCrate_3D.prefab", meshMap, "Assets/Models/Props/SM_WoodenCrate.fbx", false);
            UpdatePrefabMesh("Assets/Prefabs/Environment/PF_MossyBoulder.prefab", meshMap, "Assets/Models/Environment/SM_MossyBoulder_01.fbx", false);
            UpdatePrefabMesh("Assets/Prefabs/Environment/PF_Rock_Large_01.prefab", meshMap, "Assets/Models/Environment/SM_Rock_Large_01.fbx", false);
            UpdatePrefabMesh("Assets/Prefabs/Environment/PF_StiltedHouse_Thatched.prefab", meshMap, "Assets/Models/Environment/SM_StiltedHouse_Thatched.fbx", false);

            AssetDatabase.SaveAssets();

            // 3. Tối ưu hóa Scene CaMau_1954_Village.unity
            OptimizeSceneObjects(meshMap);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#4ade80>[OptimizeVillage]</color> Đã hoàn tất nâng cấp mesh Blender chất lượng cao cho Scene 3D Làng Cà Mau 1954!");
        }

        public static UnityEngine.Mesh GetMeshFromFBX(string fbxPath)
        {
            if (!File.Exists(fbxPath)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is UnityEngine.Mesh m && !m.name.Contains("__preview__"))
                {
                    return m;
                }
            }
            return null;
        }

        public static void CopyMeshData(UnityEngine.Mesh src, UnityEngine.Mesh dst)
        {
            dst.Clear();
            dst.indexFormat = src.indexFormat;
            dst.vertices = src.vertices;
            dst.normals = src.normals;
            dst.uv = src.uv;
            dst.uv2 = src.uv2;
            dst.tangents = src.tangents;
            dst.subMeshCount = src.subMeshCount;
            for (int s = 0; s < src.subMeshCount; s++)
            {
                dst.SetTriangles(src.GetTriangles(s), s);
            }
            dst.RecalculateBounds();
            EditorUtility.SetDirty(dst);
        }

        private static void UpdatePrefabMesh(string prefabPath, Dictionary<string, UnityEngine.Mesh> meshMap, string key, bool disableShadow)
        {
            if (!File.Exists(prefabPath) || !meshMap.TryGetValue(key, out var optMesh)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;
            try
            {
                var mf = root.GetComponentInChildren<MeshFilter>(true);
                if (mf != null)
                {
                    mf.sharedMesh = optMesh;
                    var rend = root.GetComponentInChildren<MeshRenderer>(true);
                    if (rend != null && disableShadow)
                    {
                        rend.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Debug.Log($"[OptimizeVillage] Đã cập nhật Prefab: {prefabPath} -> {optMesh.name} ({optMesh.triangles.Length / 3:N0} tris)");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void OptimizeSceneObjects(Dictionary<string, UnityEngine.Mesh> meshMap)
        {
            if (!File.Exists(Scene3DPath)) return;
            bool wasLoaded = false;
            Scene scene = SceneManager.GetSceneByPath(Scene3DPath);
            if (scene.isLoaded) wasLoaded = true;
            else scene = EditorSceneManager.OpenScene(Scene3DPath, OpenSceneMode.Additive);

            if (!scene.IsValid() || !scene.isLoaded) return;

            int replacedMeshes = 0;
            int shadowCastersDisabled = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                // Tuyệt đối không đụng đến đối tượng Player
                if (root.name.Contains("Player") || root.name.Contains("Hero") || root.name.Contains("Boy"))
                {
                    continue;
                }

                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    if (go.name.Contains("Player") || go.name.Contains("Hero") || go.name.Contains("Boy"))
                    {
                        continue;
                    }

                    var mf = go.GetComponent<MeshFilter>();
                    var mr = go.GetComponent<MeshRenderer>();

                    // A. Thay thế các mesh siêu nặng thành mesh tối ưu Blender
                    if (mf != null && mf.sharedMesh != null)
                    {
                        string p = AssetDatabase.GetAssetPath(mf.sharedMesh);
                        if (!string.IsNullOrEmpty(p) && meshMap.TryGetValue(p, out var optMesh))
                        {
                            if (mf.sharedMesh != optMesh)
                            {
                                mf.sharedMesh = optMesh;
                                replacedMeshes++;
                            }
                        }
                    }

                    // B. Tối ưu Shadow Casters trên các prop nhỏ
                    if (mr != null && mr.enabled && mr.shadowCastingMode != ShadowCastingMode.Off)
                    {
                        string n = go.name;
                        bool isFoliageOrSmallProp = n.Contains("LuNuoc") || n.Contains("Lu_") || n.Contains("Model_Lu") ||
                                                    n.Contains("Rice") || n.Contains("Paddy") || n.Contains("LOD") ||
                                                    n.Contains("Pebble") || n.Contains("Grass") || n.Contains("Flower") ||
                                                    n.Contains("MangroveRoots") || n.Contains("Fence") || n.Contains("Chicken");

                        if (isFoliageOrSmallProp)
                        {
                            mr.shadowCastingMode = ShadowCastingMode.Off;
                            shadowCastersDisabled++;
                        }
                    }
                }

                // C. Cấu hình Camera FarClip tối ưu (160m thay vì 5000m)
                var cam = root.GetComponentInChildren<Camera>(true);
                if (cam != null && (cam.name == "Main Camera" || cam.CompareTag("MainCamera")))
                {
                    cam.farClipPlane = 160f;
                    cam.nearClipPlane = 0.15f;
                    EditorUtility.SetDirty(cam);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Scene3DPath);
            Debug.Log($"[OptimizeVillage] Scene CaMau_1954_Village: Đã cập nhật {replacedMeshes} vị trí sang mesh mịn Blender và tắt shadow trên {shadowCastersDisabled} prop nhỏ!");

            if (!wasLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
