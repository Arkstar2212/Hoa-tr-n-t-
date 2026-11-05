using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HoaVaMau.EditorScripts
{
    public static class BuildAuthenticNorthGateAndCanyonPass
    {
        private const string RootName = "North_Gate_Canyon_Passage";
        private const string RockLargeFbx = "Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx";
        private const string RockMediumFbx = "Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx";
        private const string RockMatPath = "Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat";
        private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
        private const string GodRayMatPath = "Assets/Materials/VFX/M_StylizedGodRays.mat";

        private const string PrefabBamboo = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab";
        private const string PrefabCoconut = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab";
        private const string PrefabHeroTreeLayered = "Assets/Prefabs/Environment/Trees/PF_Lana_Hero_Tree_Layered.prefab";
        private const string PrefabHeroTreeSmall = "Assets/Prefabs/Environment/Trees/PF_Lana_Hero_Tree_Small.prefab";
        private const string PrefabBushBroad1 = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab";
        private const string PrefabBushBroad2 = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_02.prefab";
        private const string PrefabBushLow = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_01.prefab";

        private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

        [MenuItem("Tools/Hoa Do/Build Authentic North Gate And Canyon Pass")]
        public static void Build()
        {
            Debug.Log("<color=#40b8a6>[BuildAuthenticNorthGateAndCanyonPass]</color> Khởi động tái thiết Cổng Đá & Hẻm Núi (Hình 2) và Khu Vực Cửa Ải (Hình 1)...");

            var activeScene = SceneManager.GetActiveScene();

            // 1. Load Meshes & Materials
            var rockLargeMesh = LoadMeshFromFbx(RockLargeFbx);
            var rockMediumMesh = LoadMeshFromFbx(RockMediumFbx);
            var rockMat = AssetDatabase.LoadAssetAtPath<Material>(RockMatPath);
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
            var godRayMat = AssetDatabase.LoadAssetAtPath<Material>(GodRayMatPath);

            var pBamboo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBamboo);
            var pCoconut = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCoconut);
            var pHeroLayered = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabHeroTreeLayered);
            var pHeroSmall = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabHeroTreeSmall);
            var pBushBroad1 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBushBroad1);
            var pBushBroad2 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBushBroad2);
            var pBushLow = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBushLow);

            // 2. Clean up old conflicting objects
            DestroyIfExists("Gate_North_Cave_Entrance");
            DestroyIfExists("NorthMtn_Rock_Center_Spire");
            DestroyIfExists("NorthMtn_Rock_West_SaddleSealer");
            DestroyIfExists("NorthMtn_Rock_East_SaddleSealer");
            DestroyIfExists("Gate_Village_Environment");
            DestroyIfExists(RootName);

            // 3. Create Master Hierarchy
            var masterRoot = new GameObject(RootName);
            var gate = GameObject.Find("Gate_Village_North");
            if (gate != null)
            {
                masterRoot.transform.SetParent(gate.transform.parent, true);
            }
            masterRoot.transform.position = new Vector3(-7.0f, 0.505f, 184.0f);

            var groupGateCliffs = CreateGroup(masterRoot.transform, "Gate_Portal_Cliffs_Image2");
            var groupCanyonPass = CreateGroup(masterRoot.transform, "Canyon_Passage_Corridor");
            var groupGodRays = CreateGroup(masterRoot.transform, "Canyon_GodRays");
            var groupCanopy = CreateGroup(masterRoot.transform, "Overhead_Canopy_Vines");
            var groupTransition = CreateGroup(masterRoot.transform, "Transition_Valley_Foliage_Image1");
            var groupMountainRidges = CreateGroup(masterRoot.transform, "Mountain_Pass_Ridges_Image1");
            var groupRoad = CreateGroup(masterRoot.transform, "Earthen_Road_Network");

            // =========================================================================
            // 4. EARTHEN ROAD NETWORK (Con đường đất liền mạch, tự nhiên như Hình 2)
            // =========================================================================
            CreateRoadPlane(groupRoad.transform, "Road_Approach_PaddyToGate",
                new Vector3(-7.0f, 0.506f, 175.0f), new Vector3(9.0f, 1.0f, 20.0f), roadMat);

            CreateRoadPlane(groupRoad.transform, "Road_Canyon_Corridor",
                new Vector3(-7.0f, 0.508f, 203.0f), new Vector3(5.2f, 1.0f, 38.0f), roadMat);

            // =========================================================================
            // 5. GATE PORTAL CLIFFS (Vách đá ôm sát 2 bên cổng làng chuẩn xác theo HÌNH 2)
            // Cột trái gỗ: X = -8.5m | Cột phải gỗ: X = -5.5m | Đèn lồng: X = -4.1m
            // Hai cột gỗ lộ trọn vẹn 100% từ chân đế tròn lên mái ngói, không bị che khuất!
            // =========================================================================
            // A. VÁCH ĐÁ TÂY (Bên trái cổng làng)
            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_Base_Ground", rockLargeMesh, rockMat,
                new Vector3(-13.0f, 1.8f, 185.2f),
                Quaternion.Euler(-2f, 45f, 0f),
                new Vector3(4.0f, 6.5f, 4.0f));

            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_MainTower_Image2", rockLargeMesh, rockMat,
                new Vector3(-13.2f, 4.8f, 185.5f),
                Quaternion.Euler(5f, 25f, -6f),
                new Vector3(4.2f, 10.0f, 4.2f));

            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_UpperCliff_Image2", rockLargeMesh, rockMat,
                new Vector3(-14.2f, 9.8f, 186.0f),
                Quaternion.Euler(12f, 35f, -8f),
                new Vector3(4.5f, 11.5f, 4.5f));

            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_FlankWall_Image2", rockLargeMesh, rockMat,
                new Vector3(-17.0f, 4.5f, 183.0f),
                Quaternion.Euler(0f, 15f, 0f),
                new Vector3(5.0f, 10.0f, 5.0f));

            // B. VÁCH ĐÁ ĐÔNG (Bên phải cổng làng)
            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_Base_Ground", rockLargeMesh, rockMat,
                new Vector3(-0.8f, 1.8f, 185.2f),
                Quaternion.Euler(-4f, -50f, 0f),
                new Vector3(3.6f, 6.5f, 3.8f));

            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_MainCrag_Image2", rockLargeMesh, rockMat,
                new Vector3(-0.5f, 4.5f, 185.5f),
                Quaternion.Euler(-5f, -40f, 6f),
                new Vector3(3.8f, 9.5f, 4.0f));

            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_UpperSpire_Image2", rockLargeMesh, rockMat,
                new Vector3(0.2f, 9.0f, 186.0f),
                Quaternion.Euler(10f, -50f, 10f),
                new Vector3(4.0f, 10.5f, 4.0f));

            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_FlankWall_Image2", rockLargeMesh, rockMat,
                new Vector3(3.5f, 4.5f, 183.0f),
                Quaternion.Euler(0f, -20f, 0f),
                new Vector3(5.0f, 10.0f, 5.0f));

            // =========================================================================
            // 6. CANYON PASSAGE CORRIDOR (Hẻm đá tự nhiên sâu thẳm khép kín, Z = 187m -> 214m)
            // Lòng hẻm hẹp 3.0m - 3.8m, vách đá dựng đứng 2 bên, không có lỗ hổng lộ trời!
            // =========================================================================
            // Vách Tây hẻm núi liên hoàn
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_01", rockLargeMesh, rockMat,
                new Vector3(-10.2f, 3.2f, 188.0f), Quaternion.Euler(5f, 40f, 0f), new Vector3(3.6f, 8.0f, 4.2f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_02", rockLargeMesh, rockMat,
                new Vector3(-9.8f, 3.8f, 194.0f), Quaternion.Euler(8f, 30f, -5f), new Vector3(3.8f, 8.8f, 4.5f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_03", rockLargeMesh, rockMat,
                new Vector3(-9.4f, 4.5f, 201.0f), Quaternion.Euler(-5f, 25f, 0f), new Vector3(4.0f, 9.5f, 4.8f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_04", rockLargeMesh, rockMat,
                new Vector3(-9.0f, 5.2f, 208.0f), Quaternion.Euler(0f, 35f, 5f), new Vector3(4.2f, 10.5f, 5.0f));

            // Vách Đông hẻm núi liên hoàn
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_01", rockLargeMesh, rockMat,
                new Vector3(-3.8f, 3.2f, 188.0f), Quaternion.Euler(5f, -40f, 0f), new Vector3(3.6f, 8.0f, 4.2f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_02", rockLargeMesh, rockMat,
                new Vector3(-4.2f, 3.8f, 194.0f), Quaternion.Euler(8f, -30f, 5f), new Vector3(3.8f, 8.8f, 4.5f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_03", rockLargeMesh, rockMat,
                new Vector3(-4.6f, 4.5f, 201.0f), Quaternion.Euler(-5f, -25f, 0f), new Vector3(4.0f, 9.5f, 4.8f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_04", rockLargeMesh, rockMat,
                new Vector3(-5.0f, 5.2f, 208.0f), Quaternion.Euler(0f, -35f, -5f), new Vector3(4.2f, 10.5f, 5.0f));

            // Vách đá uốn cong chặn đáy hẻm núi (Z = 212m, cắm sâu xuống đất, bịt kín 100% tầm nhìn đáy)
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_Curve_BackWall", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 3.0f, 212.0f), Quaternion.Euler(0f, 80f, 0f), new Vector3(14.0f, 12.0f, 5.0f));

            // Tường núi sừng sững hậu cảnh phía sau hẻm núi (Z = 220m, vươn cao Y = 18m)
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_Backdrop_HighRidge", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 10.0f, 220.0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(20.0f, 15.0f, 6.0f));

            // =========================================================================
            // 7. CANYON GOD RAYS (Tia nắng rọi nghiêng huyền ảo qua hẻm núi như HÌNH 2)
            // Quad mesh nằm trên mặt phẳng XY, scale theo Y là chiều dài tia sáng (10m - 14m)
            // =========================================================================
            if (godRayMat != null)
            {
                CreateGodRayBeam(groupGodRays.transform, "GodRay_Shaft_01",
                    new Vector3(-5.6f, 3.8f, 192.0f), Quaternion.Euler(58f, -28f, 0f), new Vector3(2.2f, 12.0f, 1.0f), godRayMat);
                CreateGodRayBeam(groupGodRays.transform, "GodRay_Shaft_02",
                    new Vector3(-6.5f, 4.2f, 198.0f), Quaternion.Euler(56f, -22f, 0f), new Vector3(2.6f, 14.0f, 1.0f), godRayMat);
                CreateGodRayBeam(groupGodRays.transform, "GodRay_Shaft_03",
                    new Vector3(-6.0f, 3.2f, 186.5f), Quaternion.Euler(54f, -32f, 0f), new Vector3(1.8f, 10.0f, 1.0f), godRayMat);
            }

            // =========================================================================
            // 8. OVERHEAD CANOPY & HANGING VINES (Vòm lá xanh & dây leo rủ xuống mái cổng 100% như HÌNH 2)
            // Bụi cây chân đá nằm gọn bên ngoài cột gỗ, KHÔNG ĐƯỢC CHE CỘT GỖ!
            // =========================================================================
            if (pBushBroad1 != null)
            {
                // Dây leo & lá bám vách đá trái rủ xuống mép mái ngói trái
                SpawnDetailPrefab(pBushBroad1, groupCanopy.transform, "Vines_RoofTop_Left",
                    new Vector3(-8.6f, 4.9f, 184.2f), Quaternion.Euler(25f, 30f, 15f), new Vector3(0.55f, 0.55f, 0.55f));

                // Lá rủ vắt ngang đỉnh mái cổng gỗ
                SpawnDetailPrefab(pBushBroad1, groupCanopy.transform, "Vines_RoofTop_Center",
                    new Vector3(-7.0f, 5.2f, 184.3f), Quaternion.Euler(35f, 0f, 0f), new Vector3(0.65f, 0.65f, 0.65f));

                // Dây leo bám mép mái ngói phải
                SpawnDetailPrefab(pBushBroad1, groupCanopy.transform, "Vines_RoofTop_Right",
                    new Vector3(-5.4f, 4.9f, 184.2f), Quaternion.Euler(25f, -30f, -15f), new Vector3(0.55f, 0.55f, 0.55f));

                // Cụm lá xanh leo trên vách đá đứng bên trái
                SpawnDetailPrefab(pBushBroad1, groupCanopy.transform, "Vines_LeftCliff_Wall",
                    new Vector3(-9.8f, 3.8f, 184.6f), Quaternion.Euler(15f, 45f, 10f), new Vector3(0.6f, 0.6f, 0.6f));

                // Vòm cây xanh rủ từ góc trên bên trái khung hình (chuẩn xác như Hình 2)
                SpawnDetailPrefab(pBushBroad1, groupCanopy.transform, "Canopy_TopLeft_Image2",
                    new Vector3(-8.8f, 4.8f, 179.0f), Quaternion.Euler(15f, 40f, 0f), new Vector3(0.75f, 0.75f, 0.75f));

                // Vòm cây xanh rủ từ góc trên bên phải khung hình (chuẩn xác như Hình 2)
                SpawnDetailPrefab(pBushBroad1, groupCanopy.transform, "Canopy_TopRight_Image2",
                    new Vector3(-5.0f, 4.8f, 179.0f), Quaternion.Euler(15f, -40f, 0f), new Vector3(0.75f, 0.75f, 0.75f));
            }

            // BỤI CÂY LÁ XANH CHÂN VÁCH ĐÁ PHẢI (Chuẩn xác 100% như HÌNH 2: Nằm bên phải cột gỗ phải, không chạm vào cột!)
            var bushRightPrefab = pBushLow ?? pBushBroad2 ?? pBushBroad1;
            if (bushRightPrefab != null)
            {
                SpawnDetailPrefab(bushRightPrefab, groupCanopy.transform, "Bushes_RightBase_01",
                    new Vector3(-2.8f, 0.52f, 184.6f), Quaternion.Euler(0f, -25f, 0f), new Vector3(0.45f, 0.45f, 0.45f));
                SpawnDetailPrefab(bushRightPrefab, groupCanopy.transform, "Bushes_RightBase_02",
                    new Vector3(-1.6f, 0.52f, 184.0f), Quaternion.Euler(0f, -45f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
                SpawnDetailPrefab(bushRightPrefab, groupCanopy.transform, "Bushes_RightBase_03",
                    new Vector3(-0.6f, 0.52f, 184.5f), Quaternion.Euler(0f, -65f, 0f), new Vector3(0.45f, 0.45f, 0.45f));

                // Bụi cây chân vách đá trái (nằm ở X <= -11.2m, cách xa cột trái X = -8.5m)
                SpawnDetailPrefab(bushRightPrefab, groupCanopy.transform, "Bushes_LeftBase_01",
                    new Vector3(-11.2f, 0.52f, 184.6f), Quaternion.Euler(0f, 35f, 0f), new Vector3(0.45f, 0.45f, 0.45f));
                SpawnDetailPrefab(bushRightPrefab, groupCanopy.transform, "Bushes_LeftBase_02",
                    new Vector3(-12.4f, 0.52f, 184.0f), Quaternion.Euler(0f, 55f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
            }

            if (pBushLow != null)
            {
                SpawnDetailPrefab(pBushLow, groupCanopy.transform, "Grasses_Edge_Left",
                    new Vector3(-9.2f, 0.51f, 181.5f), Quaternion.identity, new Vector3(0.35f, 0.35f, 0.35f));
                SpawnDetailPrefab(pBushLow, groupCanopy.transform, "Grasses_Edge_Right",
                    new Vector3(-4.8f, 0.51f, 181.5f), Quaternion.identity, new Vector3(0.35f, 0.35f, 0.35f));
            }

            // =========================================================================
            // 9. REBUILD MOUNTAIN PASS RIDGES (Khắc phục núi bẹp ở HÌNH 1 -> Tạo Hẻm Đèo Tự Nhiên Hùng Vĩ)
            // Hai dãy núi Tây và Đông mở rộng sừng sững hình chữ V, tạo hẻm đèo tự nhiên nhìn thấy bầu trời
            // =========================================================================
            // A. DÃY NÚI TÂY (West Mountain Ridge)
            SpawnRockPiece(groupMountainRidges.transform, "WestRidge_Peak_Near", rockLargeMesh, rockMat,
                new Vector3(-22.0f, 16.0f, 215.0f), Quaternion.Euler(10f, 35f, -8f), new Vector3(22.0f, 32.0f, 20.0f));
            SpawnRockPiece(groupMountainRidges.transform, "WestRidge_Peak_Mid", rockLargeMesh, rockMat,
                new Vector3(-40.0f, 26.0f, 230.0f), Quaternion.Euler(5f, 20f, 4f), new Vector3(28.0f, 44.0f, 24.0f));
            SpawnRockPiece(groupMountainRidges.transform, "WestRidge_Peak_High", rockLargeMesh, rockMat,
                new Vector3(-65.0f, 38.0f, 245.0f), Quaternion.Euler(-5f, 15f, 0f), new Vector3(36.0f, 55.0f, 30.0f));

            // B. DÃY NÚI ĐÔNG (East Mountain Ridge)
            SpawnRockPiece(groupMountainRidges.transform, "EastRidge_Peak_Near", rockLargeMesh, rockMat,
                new Vector3(14.0f, 16.0f, 215.0f), Quaternion.Euler(10f, -35f, 8f), new Vector3(22.0f, 32.0f, 20.0f));
            SpawnRockPiece(groupMountainRidges.transform, "EastRidge_Peak_Mid", rockLargeMesh, rockMat,
                new Vector3(32.0f, 26.0f, 230.0f), Quaternion.Euler(5f, -20f, -4f), new Vector3(28.0f, 44.0f, 24.0f));
            SpawnRockPiece(groupMountainRidges.transform, "EastRidge_Peak_High", rockLargeMesh, rockMat,
                new Vector3(55.0f, 38.0f, 245.0f), Quaternion.Euler(5f, -15f, 0f), new Vector3(36.0f, 55.0f, 30.0f));

            // C. ĐỈNH NÚI XA HẬU CẢNH (Distant Misty Summit) - Đặt lùi hẳn ra Z = 290m, đỉnh nhọn vươn cao
            SpawnRockPiece(groupMountainRidges.transform, "Far_Misty_Peak", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 28.0f, 290.0f), Quaternion.Euler(0f, 15f, 0f), new Vector3(28.0f, 40.0f, 20.0f));

            // =========================================================================
            // 10. TRANSITION VALLEY FOLIAGE (Chỉnh sửa khu vực HÌNH 1 đẹp mắt & hợp lý)
            // =========================================================================
            if (pBamboo != null)
            {
                // Cụm rừng tre phía Tây (X <= -13.5m, Z = 168m đến 183m)
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_West_01",
                    new Vector3(-13.5f, 0.505f, 171.0f), Quaternion.Euler(0f, 30f, 0f), new Vector3(1.3f, 1.4f, 1.3f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_West_02",
                    new Vector3(-17.0f, 0.505f, 177.0f), Quaternion.Euler(0f, 75f, 0f), new Vector3(1.35f, 1.45f, 1.35f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_West_03",
                    new Vector3(-21.0f, 0.505f, 170.0f), Quaternion.Euler(0f, 15f, 0f), new Vector3(1.4f, 1.5f, 1.4f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_West_04",
                    new Vector3(-25.0f, 0.505f, 176.0f), Quaternion.Euler(0f, 50f, 0f), new Vector3(1.35f, 1.45f, 1.35f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_West_05",
                    new Vector3(-30.0f, 0.505f, 181.0f), Quaternion.Euler(0f, -20f, 0f), new Vector3(1.4f, 1.5f, 1.4f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_West_06",
                    new Vector3(-36.0f, 0.505f, 174.0f), Quaternion.Euler(0f, 40f, 0f), new Vector3(1.45f, 1.55f, 1.45f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_West_07",
                    new Vector3(-42.0f, 0.505f, 178.0f), Quaternion.Euler(0f, -30f, 0f), new Vector3(1.4f, 1.5f, 1.4f));

                // Cụm rừng tre phía Đông (X >= 0.0m, Z = 168m đến 183m)
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_East_01",
                    new Vector3(0.0f, 0.505f, 171.0f), Quaternion.Euler(0f, -30f, 0f), new Vector3(1.3f, 1.4f, 1.3f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_East_02",
                    new Vector3(3.5f, 0.505f, 177.0f), Quaternion.Euler(0f, -75f, 0f), new Vector3(1.35f, 1.45f, 1.35f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_East_03",
                    new Vector3(7.5f, 0.505f, 170.0f), Quaternion.Euler(0f, -15f, 0f), new Vector3(1.4f, 1.5f, 1.4f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_East_04",
                    new Vector3(12.0f, 0.505f, 176.0f), Quaternion.Euler(0f, -50f, 0f), new Vector3(1.35f, 1.45f, 1.35f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_East_05",
                    new Vector3(16.5f, 0.505f, 181.0f), Quaternion.Euler(0f, 20f, 0f), new Vector3(1.4f, 1.5f, 1.4f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_East_06",
                    new Vector3(22.0f, 0.505f, 174.0f), Quaternion.Euler(0f, -40f, 0f), new Vector3(1.45f, 1.55f, 1.45f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_East_07",
                    new Vector3(28.0f, 0.505f, 178.0f), Quaternion.Euler(0f, 30f, 0f), new Vector3(1.4f, 1.5f, 1.4f));
            }

            if (pCoconut != null)
            {
                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_West_01",
                    new Vector3(-18.0f, 0.505f, 178.5f), Quaternion.Euler(0f, 45f, -4f), new Vector3(1.15f, 1.25f, 1.15f));
                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_West_02",
                    new Vector3(-26.0f, 0.505f, 173.0f), Quaternion.Euler(0f, 85f, 5f), new Vector3(1.2f, 1.3f, 1.2f));
                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_West_03",
                    new Vector3(-34.0f, 0.505f, 182.0f), Quaternion.Euler(0f, 15f, -5f), new Vector3(1.15f, 1.25f, 1.15f));

                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_East_01",
                    new Vector3(6.0f, 0.505f, 178.5f), Quaternion.Euler(0f, -45f, 4f), new Vector3(1.15f, 1.25f, 1.15f));
                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_East_02",
                    new Vector3(14.0f, 0.505f, 173.0f), Quaternion.Euler(0f, -85f, -5f), new Vector3(1.2f, 1.3f, 1.2f));
                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_East_03",
                    new Vector3(21.0f, 0.505f, 182.0f), Quaternion.Euler(0f, -15f, 5f), new Vector3(1.15f, 1.25f, 1.15f));
            }

            if (pHeroLayered != null)
            {
                SpawnDetailPrefab(pHeroLayered, groupTransition.transform, "HeroTree_West_Ridge",
                    new Vector3(-24.0f, 1.2f, 186.0f), Quaternion.Euler(0f, 60f, 0f), new Vector3(1.45f, 1.65f, 1.45f));
                SpawnDetailPrefab(pHeroLayered, groupTransition.transform, "HeroTree_East_Ridge",
                    new Vector3(11.0f, 1.2f, 186.0f), Quaternion.Euler(0f, -60f, 0f), new Vector3(1.45f, 1.65f, 1.45f));
            }

            // =========================================================================
            // 11. UPDATE PLAYER SPAWN (Bé An xuất phát trong lòng hẻm núi)
            // =========================================================================
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(-7.0f, 0.505f, 187.0f);
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EditorUtility.SetDirty(player);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#40b8a6>[BuildAuthenticNorthGateAndCanyonPass]</color> Tái thiết hoàn tất! Đang chụp ảnh kiểm chứng cả 2 góc...");
            CaptureComparisonQA();
        }

        private static Mesh LoadMeshFromFbx(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is Mesh m && !m.name.StartsWith("__preview__"))
                {
                    return m;
                }
            }
            return null;
        }

        private static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject SpawnRockPiece(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;

            if (mesh != null)
            {
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
            }

            return go;
        }

        private static GameObject SpawnDetailPrefab(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (go != null)
            {
                go.name = name;
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.transform.localScale = scale;
            }
            return go;
        }

        private static void CreateRoadPlane(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Plane);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.position = pos;
            quad.transform.rotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(scale.x * 0.1f, 1.0f, scale.z * 0.1f);

            var col = quad.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            var mr = quad.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;
        }

        private static void CreateGodRayBeam(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.position = pos;
            quad.transform.rotation = rot;
            quad.transform.localScale = scale;

            var col = quad.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            var mr = quad.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;
        }

        [MenuItem("Tools/Hoa Do/Capture Gate Comparison QA")]
        public static void CaptureComparisonQA()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            // 1. Góc nhìn Hình 1: Toàn cảnh trên cao nhìn về phía Cổng Làng & Núi Hậu Cảnh (Aerial Pass View)
            Vector3 camPos1 = new Vector3(-7.0f, 24.0f, 135.0f);
            Quaternion camRot1 = Quaternion.Euler(28.0f, 0.0f, 0.0f);
            RenderView(camPos1, camRot1, 60.0f, Path.Combine(ArtifactDir, "Test_Hinh1_Aerial_Overview_QA.jpg"));

            // 2. Góc nhìn Hình 2: Cận cảnh cổng làng & Hẻm núi đá tự nhiên (1:1 Reference Match)
            // Đặt góc máy tại Z = 175.2m, Y = 2.15m để trọn vẹn đường đất không bị lúa che viền, thấy đầy đủ chân đế gỗ, mái ngói và vòm đá 2 bên
            Vector3 camPos2 = new Vector3(-7.0f, 2.15f, 175.2f);
            Quaternion camRot2 = Quaternion.Euler(6.0f, 0.0f, 0.0f);
            RenderView(camPos2, camRot2, 50.0f, Path.Combine(ArtifactDir, "Test_Hinh2_Gate_Gorge_QA.jpg"));

            // 3. Góc nhìn Cận cảnh mặt Bé An bước ra khỏi hẻm núi (Shot 2 Cutscene)
            Vector3 camPos3 = new Vector3(-7.0f, 1.32f, 183.0f);
            Quaternion camRot3 = Quaternion.Euler(1.0f, 0.0f, 0.0f);
            RenderView(camPos3, camRot3, 28.0f, Path.Combine(ArtifactDir, "Test_Hinh2_BéAn_Closeup_QA.jpg"));

            Debug.Log("[CaptureComparisonQA] Đã chụp xong cả 3 góc kiểm chứng!");
        }

        private static void RenderView(Vector3 pos, Quaternion rot, float fov, string outputPath)
        {
            var camGo = new GameObject("Temp_QA_Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.rotation = rot;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;

            int width = 1280;
            int height = 720;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToJPG(80);
            File.WriteAllBytes(outputPath, bytes);

            cam.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[RenderView] Đã lưu ảnh QA: {outputPath}");
        }
    }
}
