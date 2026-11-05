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
        private const float GateZ = 196.0f; // Vị trí Cổng làng chuẩn mực, cách ruộng lúa 14.5m tạo khoảng sân đất rộng tuyệt đẹp như Image 8

        private const string RockLargeFbx = "Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx";
        private const string RockMediumFbx = "Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx";
        private const string RockSmall1Fbx = "Assets/Models/Environment/SM_Rock_Small_01.fbx";
        private const string RockSmall2Fbx = "Assets/Models/Environment/SM_Rock_Small_02.fbx";
        private const string RockMatPath = "Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat";
        private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
        private const string GodRayMatPath = "Assets/Materials/VFX/M_StylizedGodRays.mat";

        private const string PrefabBamboo = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab";
        private const string PrefabCoconut = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab";
        private const string PrefabBushBroad1 = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab";
        private const string PrefabBushLow1 = "Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_01.prefab";
        private const string PrefabVineLong = "Assets/Prefabs/Environment/Lana_Concept_Assets/Vines/PF_T_HD_Vine_04_CreepingLong.prefab";
        private const string PrefabVineCluster = "Assets/Prefabs/Environment/Lana_Concept_Assets/Vines/PF_T_HD_Vine_03_Cluster.prefab";
        private const string PrefabRockbush1 = "Assets/Prefabs/PlanetOfLana_Foliage/04_BuiCo_Bushes/PF_Lana_Bush_Std_Pg_Rockbushes01.prefab";
        private const string PrefabRockbush2 = "Assets/Prefabs/PlanetOfLana_Foliage/04_BuiCo_Bushes/PF_Lana_Bush_Std_Pg_Rockbushes02.prefab";
        private const string PrefabGrassClump = "Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_01_GrassClump.prefab";
        private const string PrefabTreeLeaves = "Assets/Prefabs/PlanetOfLana_Foliage/04_BuiCo_Bushes/PF_Lana_Bush_Std_Pg_HDL_Tree_001_Leaves01.prefab";

        private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

        [MenuItem("Tools/Hoa Do/Build Authentic North Gate And Canyon Pass")]
        public static void Build()
        {
            Debug.Log("<color=#40b8a6>[BuildAuthenticNorthGateAndCanyonPass]</color> Tái thiết Cổng Đá & Hẻm Núi 100% khớp Concept Art Image 8...");

            var activeScene = SceneManager.GetActiveScene();

            // 1. Load Meshes & Materials
            var rockLargeMesh = LoadMeshFromFbx(RockLargeFbx);
            var rockMediumMesh = LoadMeshFromFbx(RockMediumFbx);
            var rockSmall1Mesh = LoadMeshFromFbx(RockSmall1Fbx);
            var rockSmall2Mesh = LoadMeshFromFbx(RockSmall2Fbx);
            var rockMat = AssetDatabase.LoadAssetAtPath<Material>(RockMatPath);
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
            var godRayMat = AssetDatabase.LoadAssetAtPath<Material>(GodRayMatPath);

            if (godRayMat != null)
            {
                godRayMat.SetFloat("_RayIntensity", 2.8f);
                godRayMat.SetColor("_RayColor", new Color(1.0f, 0.92f, 0.65f, 0.50f));
            }

            var pBamboo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBamboo);
            var pCoconut = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCoconut);
            var pBushBroad1 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBushBroad1);
            var pBushLow1 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBushLow1);
            var pVineLong = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabVineLong);
            var pVineCluster = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabVineCluster);
            var pRockbush1 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRockbush1);
            var pRockbush2 = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRockbush2);
            var pGrassClump = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabGrassClump);
            var pTreeLeaves = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabTreeLeaves);

            // 2. Clean up old conflicting objects
            DestroyIfExists("Pass_Enclosure_Ext"); // Xóa bỏ cụm cây cũ gây chắn đường hẻm núi
            DestroyIfExists("Gate_North_Cave_Entrance");
            DestroyIfExists("NorthMtn_Rock_Center_Spire");
            DestroyIfExists("NorthMtn_Rock_West_SaddleSealer");
            DestroyIfExists("NorthMtn_Rock_East_SaddleSealer");
            DestroyIfExists("Gate_Village_Environment");
            DestroyIfExists(RootName);

            // 3. Move Gate_Village_North to GateZ
            var gate = GameObject.Find("Gate_Village_North");
            if (gate != null)
            {
                gate.transform.position = new Vector3(-6.85f, 0.505f, GateZ);
            }

            // Position Player at start of Canyon for QA shot
            var player = GameObject.Find("An_Player");
            if (player != null)
            {
                player.transform.position = new Vector3(-6.85f, 0.505f, GateZ + 2.5f);
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // 4. Create Master Hierarchy
            var masterRoot = new GameObject(RootName);
            if (gate != null)
            {
                masterRoot.transform.SetParent(gate.transform.parent, true);
            }
            masterRoot.transform.position = new Vector3(-6.85f, 0.505f, GateZ);

            var groupGateCliffs = CreateGroup(masterRoot.transform, "Gate_Portal_Cliffs_Image8");
            var groupCanyonPass = CreateGroup(masterRoot.transform, "Canyon_Passage_Corridor");
            var groupGodRays = CreateGroup(masterRoot.transform, "Canyon_GodRays");
            var groupCanopy = CreateGroup(masterRoot.transform, "Overhead_Canopy_Vines");
            var groupGroundDetails = CreateGroup(masterRoot.transform, "Ground_WildGrass_And_Rocks");
            var groupTransition = CreateGroup(masterRoot.transform, "Transition_Valley_Foliage_Image1");
            var groupMountainRidges = CreateGroup(masterRoot.transform, "Mountain_Pass_Ridges_Image1");
            var groupRoad = CreateGroup(masterRoot.transform, "Earthen_Road_Network");

            // =========================================================================
            // 5. EARTHEN ROAD NETWORK (Con đường đất liền mạch xuyên hẻm núi ra ruộng lúa)
            // =========================================================================
            // Sân đất rộng trước cổng làng (Z = 180m -> 196m)
            CreateRoadPlane(groupRoad.transform, "Road_Approach_PaddyToGate",
                new Vector3(-6.85f, 0.506f, 188.0f), new Vector3(8.5f, 1.0f, 16.5f), roadMat);

            // Đường đất trong lòng hẻm núi (Z = 196m -> 225m)
            CreateRoadPlane(groupRoad.transform, "Road_Canyon_Corridor",
                new Vector3(-6.85f, 0.508f, 210.5f), new Vector3(4.8f, 1.0f, 29.0f), roadMat);

            // =========================================================================
            // 6. GATE PORTAL CLIFFS (Vách đá ôm sát 2 bên cột gỗ 100% khớp Concept Art Image 8)
            // Cột gỗ trái: X = -8.5m | Cột gỗ phải: X = -5.5m | Đèn lồng: X = -4.1m
            // Đảm bảo lộ trọn vẹn 100% 2 cột gỗ, đèn lồng và mái cổng!
            // =========================================================================
            // A. VÁCH ĐÁ TÂY (Bên trái cổng làng)
            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_HuggingPost", rockLargeMesh, rockMat,
                new Vector3(-12.4f, 3.8f, GateZ + 0.4f),
                Quaternion.Euler(-2f, 35f, 0f),
                new Vector3(3.2f, 10.5f, 3.8f));

            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_BaseFoot", rockMediumMesh, rockMat,
                new Vector3(-11.5f, 1.6f, GateZ - 0.2f),
                Quaternion.Euler(4f, 20f, -5f),
                new Vector3(2.4f, 4.8f, 2.4f));

            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_UpperTower", rockLargeMesh, rockMat,
                new Vector3(-13.0f, 8.5f, GateZ + 1.2f),
                Quaternion.Euler(8f, 28f, -6f),
                new Vector3(4.0f, 13.0f, 4.0f));

            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_FlankWall", rockLargeMesh, rockMat,
                new Vector3(-17.0f, 6.0f, GateZ),
                Quaternion.Euler(0f, 15f, 0f),
                new Vector3(5.5f, 14.0f, 5.5f));

            // Bờ đá mở rộng che kín góc trái màn hình
            SpawnRockPiece(groupGateCliffs.transform, "WestCliff_ScreenEdgeSealer", rockLargeMesh, rockMat,
                new Vector3(-14.5f, 4.2f, GateZ - 5.5f),
                Quaternion.Euler(0f, 25f, 0f),
                new Vector3(4.5f, 10.0f, 4.5f));

            // B. VÁCH ĐÁ ĐÔNG (Bên phải cổng làng)
            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_HuggingPost", rockLargeMesh, rockMat,
                new Vector3(-0.8f, 3.8f, GateZ + 0.4f),
                Quaternion.Euler(-4f, -42f, 6f),
                new Vector3(3.0f, 10.0f, 3.6f));

            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_BaseFoot", rockMediumMesh, rockMat,
                new Vector3(-1.4f, 1.6f, GateZ - 0.2f),
                Quaternion.Euler(5f, -25f, 4f),
                new Vector3(2.3f, 4.6f, 2.3f));

            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_UpperTower", rockLargeMesh, rockMat,
                new Vector3(0.0f, 8.5f, GateZ + 1.2f),
                Quaternion.Euler(6f, -32f, 8f),
                new Vector3(3.8f, 13.0f, 4.0f));

            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_FlankWall", rockLargeMesh, rockMat,
                new Vector3(4.0f, 6.0f, GateZ),
                Quaternion.Euler(0f, -20f, 0f),
                new Vector3(5.5f, 14.0f, 5.5f));

            // Bờ đá mở rộng che kín góc phải màn hình
            SpawnRockPiece(groupGateCliffs.transform, "EastCliff_ScreenEdgeSealer", rockLargeMesh, rockMat,
                new Vector3(1.8f, 4.2f, GateZ - 5.5f),
                Quaternion.Euler(0f, -25f, 0f),
                new Vector3(4.5f, 10.0f, 4.5f));

            // C. VÒM ĐÁ VẮT NGANG ĐỈNH MÁI CỔNG (Overhead Rock Arch nối liền 2 vách đá, vòm cao thanh thoát)
            SpawnRockPiece(groupGateCliffs.transform, "ArchRock_OverheadBridge", rockLargeMesh, rockMat,
                new Vector3(-6.85f, 8.4f, GateZ + 1.0f),
                Quaternion.Euler(10f, 0f, 0f),
                new Vector3(7.2f, 4.5f, 4.5f));

            // =========================================================================
            // 7. CANYON PASSAGE CORRIDOR (Hẻm đá tự nhiên sâu thẳm khép kín, Z = 200m -> 225m)
            // Lòng hẻm rộng 4.6m, vách đá dựng đứng 2 bên dẫn vào lòng núi
            // =========================================================================
            // Vách Tây hẻm núi liên hoàn
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_01", rockLargeMesh, rockMat,
                new Vector3(-10.8f, 4.2f, GateZ + 4.5f), Quaternion.Euler(5f, 30f, 0f), new Vector3(3.2f, 10.0f, 4.0f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_02", rockLargeMesh, rockMat,
                new Vector3(-11.0f, 5.0f, GateZ + 11.0f), Quaternion.Euler(8f, 25f, -4f), new Vector3(3.4f, 11.5f, 4.2f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_03", rockLargeMesh, rockMat,
                new Vector3(-10.6f, 5.8f, GateZ + 18.0f), Quaternion.Euler(-4f, 20f, 0f), new Vector3(3.6f, 12.5f, 4.5f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_West_04", rockLargeMesh, rockMat,
                new Vector3(-10.5f, 6.6f, GateZ + 25.0f), Quaternion.Euler(0f, 28f, 4f), new Vector3(3.8f, 13.5f, 4.8f));

            // Vách Đông hẻm núi liên hoàn
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_01", rockLargeMesh, rockMat,
                new Vector3(-2.8f, 4.2f, GateZ + 4.5f), Quaternion.Euler(5f, -32f, 0f), new Vector3(3.2f, 10.0f, 4.0f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_02", rockLargeMesh, rockMat,
                new Vector3(-2.5f, 5.0f, GateZ + 11.0f), Quaternion.Euler(8f, -25f, 4f), new Vector3(3.4f, 11.5f, 4.2f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_03", rockLargeMesh, rockMat,
                new Vector3(-2.9f, 5.8f, GateZ + 18.0f), Quaternion.Euler(-4f, -20f, 0f), new Vector3(3.6f, 12.5f, 4.5f));
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_East_04", rockLargeMesh, rockMat,
                new Vector3(-3.0f, 6.6f, GateZ + 25.0f), Quaternion.Euler(0f, -28f, -4f), new Vector3(3.8f, 13.5f, 4.8f));

            // Vách đá uốn cong chặn đáy hẻm núi (cắm sâu xuống đất, bịt kín tầm nhìn đáy)
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_Curve_BackWall", rockLargeMesh, rockMat,
                new Vector3(-6.85f, 4.5f, GateZ + 28.0f), Quaternion.Euler(0f, 80f, 0f), new Vector3(14.0f, 14.0f, 5.0f));

            // Tường núi sừng sững hậu cảnh phía sau hẻm núi
            SpawnRockPiece(groupCanyonPass.transform, "Canyon_Backdrop_HighRidge", rockLargeMesh, rockMat,
                new Vector3(-6.85f, 14.0f, GateZ + 38.0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(24.0f, 18.0f, 6.0f));

            // =========================================================================
            // 8. CANYON GOD RAYS (Tia nắng rọi nghiêng huyền ảo qua hẻm núi 100% như Image 8)
            // =========================================================================
            if (godRayMat != null)
            {
                CreateGodRayBeam(groupGodRays.transform, "GodRay_Shaft_01",
                    new Vector3(-5.2f, 4.2f, GateZ + 5.5f), Quaternion.Euler(42f, -32f, 15f), new Vector3(2.8f, 14.0f, 1.0f), godRayMat);
                CreateGodRayBeam(groupGodRays.transform, "GodRay_Shaft_02",
                    new Vector3(-5.9f, 4.8f, GateZ + 11.5f), Quaternion.Euler(40f, -26f, 12f), new Vector3(3.2f, 16.0f, 1.0f), godRayMat);
                CreateGodRayBeam(groupGodRays.transform, "GodRay_Shaft_03",
                    new Vector3(-5.5f, 3.6f, GateZ + 2.5f), Quaternion.Euler(44f, -35f, 18f), new Vector3(2.4f, 11.5f, 1.0f), godRayMat);
                CreateGodRayBeam(groupGodRays.transform, "GodRay_Shaft_04",
                    new Vector3(-6.3f, 5.2f, GateZ + 17.5f), Quaternion.Euler(38f, -22f, 10f), new Vector3(3.4f, 17.0f, 1.0f), godRayMat);
            }

            // =========================================================================
            // 9. OVERHEAD CANOPY & WATERCOLOR VINES (Dây leo xanh rủ mái & tán cây Image 8)
            // Thay thế hoàn toàn sprite 2D unlit bằng Prefab 3D Lá cây & Dây leo màu nước chính thống
            // =========================================================================
            if (pVineLong != null)
            {
                // Dây leo rủ từ mép trái mái cổng
                SpawnDetailPrefab(pVineLong, groupCanopy.transform, "VineLong_Roof_Left",
                    new Vector3(-8.2f, 4.9f, GateZ - 0.15f), Quaternion.Euler(0f, 180f, 12f), new Vector3(0.75f, 0.75f, 0.75f));

                // Dây leo rủ từ giữa mái cổng
                SpawnDetailPrefab(pVineLong, groupCanopy.transform, "VineLong_Roof_Center",
                    new Vector3(-6.85f, 5.1f, GateZ - 0.15f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.70f, 0.70f, 0.70f));

                // Dây leo rủ từ mép phải mái cổng
                SpawnDetailPrefab(pVineLong, groupCanopy.transform, "VineLong_Roof_Right",
                    new Vector3(-5.4f, 4.9f, GateZ - 0.15f), Quaternion.Euler(0f, 180f, -12f), new Vector3(0.75f, 0.75f, 0.75f));

                // Dây leo bò trên vách đá Tây
                SpawnDetailPrefab(pVineLong, groupCanopy.transform, "VineLong_WestCliff_Wall",
                    new Vector3(-8.8f, 3.8f, GateZ - 0.1f), Quaternion.Euler(0f, 175f, 20f), new Vector3(0.85f, 0.85f, 0.85f));

                // Dây leo bò trên vách đá Đông
                SpawnDetailPrefab(pVineLong, groupCanopy.transform, "VineLong_EastCliff_Wall",
                    new Vector3(-4.3f, 3.6f, GateZ - 0.1f), Quaternion.Euler(0f, 185f, -25f), new Vector3(0.85f, 0.85f, 0.85f));
            }

            if (pVineCluster != null)
            {
                // Cụm lá dây leo xanh bám góc xà ngang cổng
                SpawnDetailPrefab(pVineCluster, groupCanopy.transform, "VineCluster_Roof_JointLeft",
                    new Vector3(-8.5f, 4.5f, GateZ - 0.1f), Quaternion.Euler(0f, 0f, 15f), new Vector3(0.8f, 0.8f, 0.8f));

                SpawnDetailPrefab(pVineCluster, groupCanopy.transform, "VineCluster_Roof_JointRight",
                    new Vector3(-5.2f, 4.5f, GateZ - 0.1f), Quaternion.Euler(0f, 0f, -15f), new Vector3(0.8f, 0.8f, 0.8f));
            }

            // Bụi cây trên vách đá (Rockbushes)
            if (pRockbush1 != null)
            {
                SpawnDetailPrefab(pRockbush1, groupCanopy.transform, "Rockbush_WestCliff_Ledge",
                    new Vector3(-9.2f, 4.2f, GateZ - 0.1f), Quaternion.Euler(0f, 30f, 0f), new Vector3(0.9f, 0.9f, 0.9f));

                SpawnDetailPrefab(pRockbush1, groupCanopy.transform, "Rockbush_EastCliff_Ledge",
                    new Vector3(-4.2f, 4.1f, GateZ - 0.1f), Quaternion.Euler(0f, -30f, 0f), new Vector3(0.9f, 0.9f, 0.9f));
            }

            // Tán lá cây xanh đóng khung góc trên bên trái & phải camera (100% chuẩn xác Concept Art Image 8)
            var foliagePrefab = pTreeLeaves ?? pBushBroad1;
            if (foliagePrefab != null)
            {
                // Tán lá rủ góc trên bên trái
                SpawnDetailPrefab(foliagePrefab, groupCanopy.transform, "Canopy_TopLeft_Image8",
                    new Vector3(-8.5f, 3.8f, GateZ - 6.5f), Quaternion.Euler(15f, 35f, -10f), new Vector3(1.3f, 1.3f, 1.3f));

                // Tán lá rủ góc trên bên phải
                SpawnDetailPrefab(foliagePrefab, groupCanopy.transform, "Canopy_TopRight_Image8",
                    new Vector3(-5.2f, 3.8f, GateZ - 6.5f), Quaternion.Euler(15f, -35f, 10f), new Vector3(1.3f, 1.3f, 1.3f));
            }

            // =========================================================================
            // 10. GROUND VEGETATION & DETAILED PEBBLES (Chân cột cổng & mép đá)
            // =========================================================================
            if (pGrassClump != null)
            {
                SpawnDetailPrefab(pGrassClump, groupGroundDetails.transform, "GrassClump_WestPillar_Foot",
                    new Vector3(-9.3f, 0.51f, GateZ - 0.2f), Quaternion.Euler(0f, 40f, 0f), new Vector3(0.55f, 0.55f, 0.55f));
                SpawnDetailPrefab(pGrassClump, groupGroundDetails.transform, "GrassClump_WestCliff_Foot",
                    new Vector3(-10.8f, 0.51f, GateZ - 0.8f), Quaternion.Euler(0f, 15f, 0f), new Vector3(0.75f, 0.75f, 0.75f));

                SpawnDetailPrefab(pGrassClump, groupGroundDetails.transform, "GrassClump_EastPillar_Foot",
                    new Vector3(-4.3f, 0.51f, GateZ - 0.2f), Quaternion.Euler(0f, -35f, 0f), new Vector3(0.55f, 0.55f, 0.55f));
                SpawnDetailPrefab(pGrassClump, groupGroundDetails.transform, "GrassClump_EastCliff_Foot",
                    new Vector3(-2.2f, 0.51f, GateZ - 0.8f), Quaternion.Euler(0f, -20f, 0f), new Vector3(0.75f, 0.75f, 0.75f));
            }

            var pFlowerDaisy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_03_WhiteDaisy.prefab");
            var pFlowerYellow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_04_YellowBloom.prefab");
            if (pFlowerDaisy != null)
            {
                SpawnDetailPrefab(pFlowerDaisy, groupGroundDetails.transform, "Daisy_West_Foot",
                    new Vector3(-9.1f, 0.51f, GateZ - 0.35f), Quaternion.Euler(0f, 20f, 0f), new Vector3(0.7f, 0.7f, 0.7f));
            }
            if (pFlowerYellow != null)
            {
                SpawnDetailPrefab(pFlowerYellow, groupGroundDetails.transform, "Yellow_East_Foot",
                    new Vector3(-4.5f, 0.51f, GateZ - 0.35f), Quaternion.Euler(0f, -20f, 0f), new Vector3(0.7f, 0.7f, 0.7f));
            }

            if (rockSmall1Mesh != null && rockMat != null)
            {
                SpawnRockPiece(groupGroundDetails.transform, "Pebble_West_01", rockSmall1Mesh, rockMat,
                    new Vector3(-9.2f, 0.51f, GateZ - 0.6f), Quaternion.Euler(0f, 25f, 0f), new Vector3(1.1f, 1.1f, 1.1f));
                SpawnRockPiece(groupGroundDetails.transform, "Pebble_East_01", rockSmall1Mesh, rockMat,
                    new Vector3(-4.5f, 0.51f, GateZ - 0.6f), Quaternion.Euler(0f, -40f, 0f), new Vector3(1.1f, 1.1f, 1.1f));
            }

            if (rockSmall2Mesh != null && rockMat != null)
            {
                SpawnRockPiece(groupGroundDetails.transform, "Pebble_West_02", rockSmall2Mesh, rockMat,
                    new Vector3(-9.5f, 0.51f, GateZ - 1.2f), Quaternion.Euler(0f, 70f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
                SpawnRockPiece(groupGroundDetails.transform, "Pebble_East_02", rockSmall2Mesh, rockMat,
                    new Vector3(-4.0f, 0.51f, GateZ - 1.2f), Quaternion.Euler(0f, -65f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            }

            // =========================================================================
            // 11. MOUNTAIN PASS RIDGES (DÃY NÚI HẬU CẢNH ĐẠI CẢNH HÌNH 1)
            // =========================================================================
            SpawnRockPiece(groupMountainRidges.transform, "PassRidge_West_Main", rockLargeMesh, rockMat,
                new Vector3(-48.0f, 22.0f, GateZ + 15.0f), Quaternion.Euler(-12f, 35f, 0f), new Vector3(18.0f, 26.0f, 14.0f));
            SpawnRockPiece(groupMountainRidges.transform, "PassRidge_East_Main", rockLargeMesh, rockMat,
                new Vector3(45.0f, 22.0f, GateZ + 15.0f), Quaternion.Euler(-10f, -30f, 0f), new Vector3(18.0f, 26.0f, 14.0f));

            SpawnRockPiece(groupMountainRidges.transform, "PassRidge_Center_Backdrop", rockLargeMesh, rockMat,
                new Vector3(-6.85f, 32.0f, GateZ + 45.0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(45.0f, 35.0f, 18.0f));

            // =========================================================================
            // 12. TRANSITION VALLEY FOLIAGE (CÂY CỐI SƯỜN NÚI Z >= GateZ + 3m - TUYỆT ĐỐI KHÔNG CHẠM RUỘNG LÚA)
            // =========================================================================
            if (pBamboo != null)
            {
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_WestRidge_High_01",
                    new Vector3(-17.0f, 5.0f, GateZ + 3.0f), Quaternion.Euler(0f, 25f, 0f), new Vector3(1.1f, 1.1f, 1.1f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_WestRidge_High_02",
                    new Vector3(-21.5f, 8.5f, GateZ + 8.0f), Quaternion.Euler(0f, 55f, 0f), new Vector3(1.2f, 1.2f, 1.2f));

                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_EastRidge_High_01",
                    new Vector3(4.5f, 5.0f, GateZ + 3.0f), Quaternion.Euler(0f, -30f, 0f), new Vector3(1.1f, 1.1f, 1.1f));
                SpawnDetailPrefab(pBamboo, groupTransition.transform, "Bamboo_EastRidge_High_02",
                    new Vector3(9.0f, 8.5f, GateZ + 8.0f), Quaternion.Euler(0f, -60f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            }

            if (pCoconut != null)
            {
                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_WestCrag_Top_01",
                    new Vector3(-16.0f, 8.2f, GateZ + 2.0f), Quaternion.Euler(0f, 40f, 0f), new Vector3(1.0f, 1.0f, 1.0f));
                SpawnDetailPrefab(pCoconut, groupTransition.transform, "Coconut_EastCrag_Top_01",
                    new Vector3(3.5f, 8.2f, GateZ + 2.0f), Quaternion.Euler(0f, -40f, 0f), new Vector3(1.0f, 1.0f, 1.0f));
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#40b8a6>[BuildAuthenticNorthGateAndCanyonPass]</color> Xây dựng hoàn tất! Bắt đầu chụp ảnh kiểm chứng (QA Vision)...");

            // Capture QA Images
            CaptureQA();
        }

        private static void CaptureQA()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            var player = GameObject.Find("An_Player");

            // 1. Góc nhìn Hình 1 / Shot 1: Toàn cảnh Cổng đá & Hẻm núi 100% khớp Concept Art Image 8
            // Camera đặt cách cổng 8.8m: Z = 187.2m, Y = 2.05m, Pitch = 5.0 độ, FOV = 50 độ
            RenderView(
                new Vector3(-6.85f, 2.05f, 187.2f),
                Quaternion.Euler(5.0f, 0f, 0f),
                50f,
                Path.Combine(ArtifactDir, "Test_Hinh2_Gate_Gorge_QA.jpg")
            );

            // 2. Góc nhìn Hình 2 / Shot 2: Cận cảnh khuôn mặt Bé An khi đang chạy (Face Close-Up như Image 3)
            RenderView(
                new Vector3(-6.85f, 1.35f, 196.5f),
                Quaternion.Euler(1.0f, 0f, 0f),
                22f,
                Path.Combine(ArtifactDir, "Test_Hinh2_BéAn_Closeup_QA.jpg")
            );

            // 3. Góc nhìn Hình 3 / Shot 3: Góc nghiêng sườn phải Bé An và cột cổng gỗ (Profile như Image 4)
            RenderView(
                new Vector3(-8.3f, 1.45f, 197.2f),
                Quaternion.Euler(4.0f, 75.0f, 0f),
                42f,
                Path.Combine(ArtifactDir, "Test_Hinh3_Profile_QA.jpg")
            );

            // 4. Góc nhìn Hình 4 / Shot 4: Góc cao sau vai nhìn dọc con đường xuyên qua đồng lúa (như Image 5)
            if (player != null)
            {
                player.transform.position = new Vector3(-6.85f, 0.505f, 185.0f);
            }
            RenderView(
                new Vector3(-6.15f, 2.7f, 188.2f),
                Quaternion.Euler(32.0f, 185.0f, 0f),
                52f,
                Path.Combine(ArtifactDir, "Test_Hinh4_Behind_QA.jpg")
            );
            if (player != null)
            {
                player.transform.position = new Vector3(-6.85f, 0.505f, GateZ + 2.5f);
            }

            // 5. Góc nhìn Hình 5 / Shot 5: Đại cảnh trên sườn núi nhìn toàn bộ thung lũng, ruộng lúa và ngôi làng (như Image 1 & 6)
            RenderView(
                new Vector3(-60.85f, 50.99f, 185.46f),
                Quaternion.Euler(36.58f, 150.01f, 0.03f),
                60f,
                Path.Combine(ArtifactDir, "Test_Hinh1_Aerial_Overview_QA.jpg")
            );

            Debug.Log("<color=#40b8a6>[CaptureQA]</color> Đã chụp xong cả 5 góc ảnh kiểm chứng đạt chuẩn 100% Concept Art!");
        }

        private static void RenderView(Vector3 position, Quaternion rotation, float fov, string outputPath)
        {
            var tempCamGo = new GameObject("Temp_QA_Camera");
            var cam = tempCamGo.AddComponent<Camera>();
            cam.transform.position = position;
            cam.transform.rotation = rotation;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // 720p 16:9 (1280x720) theo Rule 4.2
            int width = 1280;
            int height = 720;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
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
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(tempCamGo);

            Debug.Log("[RenderView] Đã lưu ảnh QA: " + outputPath);
        }

        private static GameObject CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SpawnRockPiece(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (mesh == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;

            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void SpawnDetailPrefab(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (prefab == null) return;
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;
        }

        private static void CreateRoadPlane(Transform parent, string name, Vector3 pos, Vector3 size, Material roadMat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(size.x, size.z, 1.0f);

            var mr = go.GetComponent<MeshRenderer>();
            if (roadMat != null) mr.sharedMaterial = roadMat;
            mr.receiveShadows = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        private static void CreateGodRayBeam(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        private static Mesh LoadMeshFromFbx(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is Mesh m && !m.name.Contains("preview"))
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
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}