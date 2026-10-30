using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace HoaVaMau.EditorTools
{
    public static class FixBridgePassage
    {
        [MenuItem("Tools/Hoa Tren Dat Do/Fix Bridge Passage")]
        public static void FixPassage()
        {
            Debug.Log("[FixBridgePassage] Bắt đầu sửa lỗi không đi qua được tại cầu kênh...");

            // 1. Sửa BoxCollider của Nha_BacBa_KhuLayNuoc khớp chính xác với Mesh_0
            FixUncleBaHouseCollider();

            // 2. Chỉnh Stairs_BacBa_KhuLayNuoc về đúng vị trí chân cầu thang thực tế (Z >= 12.2)
            FixUncleBaStairsCollider();

            // 3. Tạo đường dốc chuyển tiếp mượt mà (Smooth Ramp) giữa đường đất và sàn cầu ván gỗ
            FixBridgeRampTransitions();

            // 4. Kiểm định di chuyển 2 chiều liên tục bằng CharacterController
            bool testResult = VerifyCharacterTraverseFull();

            // 5. Lưu scene
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (testResult)
            {
                Debug.Log("[FixBridgePassage] HOÀN TẤT XUẤT SẮC: Bé An đã có thể đi lại thông suốt 100% hai chiều qua cầu kênh!");
            }
            else
            {
                Debug.LogWarning("[FixBridgePassage] Cần kiểm tra thêm sau khi sửa.");
            }
        }

        private static void FixUncleBaHouseCollider()
        {
            var house = GameObject.Find("Nha_BacBa_KhuLayNuoc");
            if (house == null) return;

            var mr = house.GetComponentInChildren<MeshRenderer>();
            var bc = house.GetComponent<BoxCollider>();
            if (bc == null) bc = house.AddComponent<BoxCollider>();

            if (mr != null)
            {
                Bounds mb = mr.bounds;
                Vector3 localCenter = house.transform.InverseTransformPoint(mb.center);
                Vector3 scale = house.transform.lossyScale;
                Vector3 localSize = new Vector3(
                    mb.size.x / Mathf.Abs(scale.x),
                    mb.size.y / Mathf.Abs(scale.y),
                    mb.size.z / Mathf.Abs(scale.z)
                );

                bc.center = localCenter;
                bc.size = localSize;
                EditorUtility.SetDirty(house);
                Debug.Log($"[FixBridgePassage] Đã chỉnh BoxCollider Nha_BacBa_KhuLayNuoc: bounds = {bc.bounds.min} đến {bc.bounds.max}");
            }
        }

        private static void FixUncleBaStairsCollider()
        {
            var stairs = GameObject.Find("Stairs_BacBa_KhuLayNuoc");
            if (stairs != null)
            {
                // Chân cầu thang thực tế của model nằm ở Z = 12.42, dẫn lên sàn nhà ở Z = 13.8, Y = 1.6
                // Dời stairs về đúng vị trí cầu thang thực tế, giải phóng hoàn toàn vùng lối đi Z < 12.0!
                stairs.transform.position = new Vector3(12.50f, 0.85f, 13.10f);
                stairs.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

                var bc = stairs.GetComponentInChildren<BoxCollider>();
                if (bc != null)
                {
                    bc.size = new Vector3(1.2f, 0.15f, 2.0f);
                    bc.center = Vector3.zero;
                    EditorUtility.SetDirty(bc);
                }
                EditorUtility.SetDirty(stairs);
                Debug.Log($"[FixBridgePassage] Đã dời Stairs_BacBa_KhuLayNuoc về Z = 13.10, giải phóng hoàn toàn hành lang Z = 10.0!");
            }
        }

        private static void FixBridgeRampTransitions()
        {
            var bridge = GameObject.Find("CauVan_Kenh_CanhDong");
            var pathApproach = GameObject.Find("Path_Canal_Approach_West");
            var pathsRoot = GameObject.Find("Paths_3D_Network")?.transform;

            // Đường dẫn phía Tây (Path_Canal_Approach_West):
            // Nối từ mặt đường giếng (X = 13.0, Y = 0.11) lên sàn cầu (X = 14.7, Y = 0.28)
            if (pathApproach != null)
            {
                pathApproach.transform.position = new Vector3(13.90f, 0.195f, 10.0f);
                pathApproach.transform.rotation = Quaternion.Euler(0f, 0f, 6.0f);
                pathApproach.transform.localScale = new Vector3(1.8f, 0.04f, 2.2f);

                var bc = pathApproach.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.size = new Vector3(1f, 1f, 1f);
                    bc.center = Vector3.zero;
                }
                EditorUtility.SetDirty(pathApproach);
            }

            // Tấm ván đệm chuyển tiếp vát mép (Transition Plank Ramp)
            string rampName = "Bridge_Plank_Ramp_West";
            var existingRamp = GameObject.Find(rampName);
            if (existingRamp == null)
            {
                existingRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                existingRamp.name = rampName;
                if (bridge != null) existingRamp.transform.SetParent(bridge.transform, true);
                else if (pathsRoot != null) existingRamp.transform.SetParent(pathsRoot, true);
            }

            existingRamp.transform.position = new Vector3(14.65f, 0.23f, 10.0f);
            existingRamp.transform.rotation = Quaternion.Euler(0f, 0f, 6.5f);
            existingRamp.transform.localScale = new Vector3(0.55f, 0.06f, 2.0f);

            var plankMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Wood_Weathered.mat");
            if (plankMat != null)
            {
                var rend = existingRamp.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = plankMat;
            }

            var rampCol = existingRamp.GetComponent<BoxCollider>();
            if (rampCol != null) rampCol.isTrigger = false;
            EditorUtility.SetDirty(existingRamp);

            Debug.Log("[FixBridgePassage] Đã thiết lập dốc chuyển tiếp mượt mà tại đầu cầu!");
        }

        public static bool VerifyCharacterTraverseFull()
        {
            var an = GameObject.Find("An_Player");
            if (an == null) return false;

            var cc = an.GetComponent<CharacterController>();
            if (cc == null) return false;

            Vector3 origPos = an.transform.position;
            cc.enabled = false;

            // TEST 1: Di chuyển từ Giếng sang Cánh Đồng (X = 11.5 -> X = 19.5, Z = 10.0)
            an.transform.position = new Vector3(11.5f, 0.5f, 10.0f);
            cc.enabled = true;
            Physics.SyncTransforms();

            bool passedEast = true;
            float lastX = an.transform.position.x;
            for (int i = 0; i < 50; i++)
            {
                cc.Move(new Vector3(0.2f, -0.1f, 0f));
                if (an.transform.position.x <= lastX + 0.001f && an.transform.position.x < 19.0f)
                {
                    passedEast = false;
                    Debug.LogWarning($"[FixBridgePassage] Kẹt hướng Đông tại Step {i}: pos={an.transform.position}");
                    break;
                }
                lastX = an.transform.position.x;
                if (an.transform.position.x >= 19.0f) break;
            }

            // TEST 2: Di chuyển ngược lại từ Cánh Đồng về Giếng (X = 19.5 -> X = 11.5, Z = 10.0)
            cc.enabled = false;
            an.transform.position = new Vector3(19.5f, 0.5f, 10.0f);
            cc.enabled = true;
            Physics.SyncTransforms();

            bool passedWest = true;
            lastX = an.transform.position.x;
            for (int i = 0; i < 50; i++)
            {
                cc.Move(new Vector3(-0.2f, -0.1f, 0f));
                if (an.transform.position.x >= lastX - 0.001f && an.transform.position.x > 11.8f)
                {
                    passedWest = false;
                    Debug.LogWarning($"[FixBridgePassage] Kẹt hướng Tây tại Step {i}: pos={an.transform.position}");
                    break;
                }
                lastX = an.transform.position.x;
                if (an.transform.position.x <= 11.8f) break;
            }

            // Khôi phục vị trí An
            cc.enabled = false;
            an.transform.position = origPos;
            cc.enabled = true;
            Physics.SyncTransforms();

            Debug.Log($"[FixBridgePassage] KẾT QUẢ KIỂM ĐỊNH TOÀN DIỆN: Hướng Đông (Sang cánh đồng) = {passedEast}, Hướng Tây (Về giếng/nhà An) = {passedWest}");
            return passedEast && passedWest;
        }
    }
}
