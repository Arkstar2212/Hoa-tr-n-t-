using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace HoaDo.Editor
{
    /// <summary>
    /// Adds dense rocky mountains, stepped cliff platforms, and lush tropical trees
    /// (coconut palms, bamboo groves, large canopy banyans, banana clumps) flanking both sides
    /// of the expanded 5X rice fields in CaMau_1954_Village.
    /// Highly optimized for rapid execution without physics bake delay.
    /// </summary>
    public static class ExpandNorthernRiceFields5X
    {
        private const float UNIFORM_GROUND_Y = 0.50f;

        [MenuItem("Tools/Hoa Do/Add Dense Mountains and Trees On Both Sides")]
        public static void BuildFlankingMountainsAndTreesOnly()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log("<color=#4CAF50>[RiceExpansion5X]</color> Adding dense mountains and trees on both sides...");

            // Resolve Materials
            Material matGrass = null;
            var sampleDike = GameObject.Find("Paddy_Plot_01_Golden_DikeN");
            if (sampleDike != null) matGrass = sampleDike.GetComponent<MeshRenderer>()?.sharedMaterial;
            if (matGrass == null) matGrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Ground_Grass_Stylized.mat");

            Material matRock = null;
            var sampleRock = GameObject.Find("West_Platform_01");
            if (sampleRock != null) matRock = sampleRock.GetComponentInChildren<MeshRenderer>()?.sharedMaterial;

            // Resolve Prefabs
            var rockPlatformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Platform_CliffLedge.prefab");
            if (rockPlatformPrefab == null) rockPlatformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Platform_01.fbx");
            var rockCragPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/rock_outcrop_3d_model/rock_outcrop_3d_model.fbx");
            var boulderPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx");
            if (boulderPrefab == null) boulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx");
            var coconutPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            var bambooPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
            var canopyPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");
            var bananaPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");

            // =========================================================================
            // A. WEST FLANK MOUNTAINS & DENSE TROPICAL FOREST (Dãy Núi Đá & Rừng Tây)
            // =========================================================================
            var westRange = GameObject.Find("MountainRange_West_Flank");
            if (westRange != null)
            {
                var oldWestExt = westRange.transform.Find("Mountain_5X_West_Ext");
                if (oldWestExt != null) Object.DestroyImmediate(oldWestExt.gameObject);

                var westExt = new GameObject("Mountain_5X_West_Ext");
                westExt.transform.parent = westRange.transform;
                westExt.transform.position = Vector3.zero;

                // 1. High Backdrop Mountain Ridges (Layer 1 - Cao nhất, che kín đường chân trời)
                CreateOrganicHillMesh(westExt.transform, "Hill_West_DeepPeak_01", new Vector3(-88.0f, UNIFORM_GROUND_Y, 110.0f), 55.0f, 75.0f, 15.0f, matGrass);
                CreateOrganicHillMesh(westExt.transform, "Hill_West_DeepPeak_02", new Vector3(-90.0f, UNIFORM_GROUND_Y, 165.0f), 58.0f, 80.0f, 16.5f, matGrass);
                CreateOrganicHillMesh(westExt.transform, "Hill_West_MidRidge_01",  new Vector3(-78.0f, UNIFORM_GROUND_Y, 105.0f), 48.0f, 65.0f,  9.0f, matGrass);
                CreateOrganicHillMesh(westExt.transform, "Hill_West_MidRidge_02",  new Vector3(-78.0f, UNIFORM_GROUND_Y, 160.0f), 50.0f, 65.0f,  9.5f, matGrass);

                // 2. Towering 3D Rock Crags / Monoliths (Vách núi đá Karst sừng sững)
                // Layer sâu:
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Deep_01", new Vector3(-86.0f, 5.5f,  95.0f), new Vector3(10.0f, 16.0f,  9.0f), new Vector3(0f,  15f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Deep_02", new Vector3(-88.0f, 6.0f, 130.0f), new Vector3(11.0f, 17.5f, 10.0f), new Vector3(0f, -35f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Deep_03", new Vector3(-90.0f, 6.5f, 165.0f), new Vector3(12.0f, 18.5f, 10.5f), new Vector3(0f,  45f, 0f), matRock);
                // Layer sườn giữa:
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Mid_01",  new Vector3(-72.0f, 3.8f,  85.0f), new Vector3( 8.5f, 13.0f,  8.0f), new Vector3(0f, -25f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Mid_02",  new Vector3(-74.0f, 4.2f, 110.0f), new Vector3( 9.0f, 14.0f,  8.5f), new Vector3(0f,  60f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Mid_03",  new Vector3(-71.0f, 4.0f, 135.0f), new Vector3( 8.5f, 13.5f,  8.0f), new Vector3(0f, -40f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Mid_04",  new Vector3(-73.0f, 4.5f, 155.0f), new Vector3( 9.0f, 14.5f,  8.5f), new Vector3(0f,  20f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Mid_05",  new Vector3(-75.0f, 4.2f, 175.0f), new Vector3( 9.5f, 15.0f,  9.0f), new Vector3(0f, -70f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Pass_01", new Vector3(-68.0f, 3.5f, 190.0f), new Vector3( 8.5f, 13.0f,  8.0f), new Vector3(0f,  30f, 0f), matRock);

                // 3. Volumetric Rock Platforms & Cliff Terraces (Thềm đá khối bậc thang)
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_01", new Vector3(-61.0f, 1.2f,  85.0f), new Vector3(3.6f, 2.8f, 3.2f), new Vector3(0f,  25f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_02", new Vector3(-63.0f, 1.6f, 100.0f), new Vector3(4.0f, 3.0f, 3.5f), new Vector3(0f, -15f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_03", new Vector3(-60.0f, 1.3f, 115.0f), new Vector3(3.5f, 2.6f, 3.0f), new Vector3(0f,  40f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_04", new Vector3(-64.0f, 1.8f, 130.0f), new Vector3(4.2f, 3.2f, 3.6f), new Vector3(0f, -30f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_05", new Vector3(-61.0f, 1.4f, 145.0f), new Vector3(3.8f, 2.8f, 3.2f), new Vector3(0f,  50f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_06", new Vector3(-63.0f, 1.7f, 160.0f), new Vector3(4.0f, 3.0f, 3.4f), new Vector3(0f, -20f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_07", new Vector3(-59.0f, 1.2f, 175.0f), new Vector3(3.5f, 2.5f, 3.0f), new Vector3(0f,  35f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_08", new Vector3(-62.0f, 1.5f, 188.0f), new Vector3(3.8f, 2.8f, 3.2f), new Vector3(0f, -45f, 0f), matRock);

                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_Terrace_01", new Vector3(-68.0f, 2.8f,  95.0f), new Vector3(4.5f, 3.5f, 4.0f), new Vector3(0f,  10f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_Terrace_02", new Vector3(-70.0f, 3.2f, 120.0f), new Vector3(4.8f, 3.6f, 4.2f), new Vector3(0f, -55f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_Terrace_03", new Vector3(-69.0f, 3.0f, 150.0f), new Vector3(4.6f, 3.5f, 4.0f), new Vector3(0f,  20f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Plat_Terrace_04", new Vector3(-71.0f, 3.4f, 170.0f), new Vector3(4.8f, 3.6f, 4.2f), new Vector3(0f, -35f, 0f), matRock);

                // 4. Large Footing Boulders along field edge
                float[] westBoulderZs = new float[] { 78f, 92f, 105f, 118f, 132f, 146f, 158f, 172f, 185f };
                for (int i = 0; i < westBoulderZs.Length; i++)
                {
                    float bz = westBoulderZs[i];
                    float bx = -58.0f + (i % 2 == 0 ? -1.0f : 0.8f);
                    InstantiateRock(boulderPrefab, westExt.transform, $"West_Boulder_Ext_{i:D2}", new Vector3(bx, UNIFORM_GROUND_Y + 0.3f, bz), new Vector3(2.6f, 2.0f, 2.4f), new Vector3(5f * i, 35f * i, 0f));
                }

                // 5. Dense Coconut Palms (Rặng Dừa Phía Tây)
                if (coconutPrefab != null)
                {
                    float[] westPalmZs = new float[] { 72f, 80f, 90f, 98f, 108f, 116f, 126f, 134f, 144f, 152f, 162f, 170f, 180f, 188f };
                    for (int i = 0; i < westPalmZs.Length; i++)
                    {
                        float pz = westPalmZs[i];
                        float px = -54.0f - (i % 3) * 1.2f;
                        float scale = 1.20f + (i % 4) * 0.08f;
                        InstantiateTree(coconutPrefab, westExt.transform, $"West_Palm_Border_{i:D2}", new Vector3(px, UNIFORM_GROUND_Y, pz), scale);
                    }

                    InstantiateTree(coconutPrefab, westExt.transform, "West_Palm_Hill_01", new Vector3(-64.0f, 2.2f,  88.0f), 1.30f);
                    InstantiateTree(coconutPrefab, westExt.transform, "West_Palm_Hill_02", new Vector3(-67.0f, 3.0f, 105.0f), 1.40f);
                    InstantiateTree(coconutPrefab, westExt.transform, "West_Palm_Hill_03", new Vector3(-65.0f, 2.5f, 125.0f), 1.25f);
                    InstantiateTree(coconutPrefab, westExt.transform, "West_Palm_Hill_04", new Vector3(-68.0f, 3.2f, 145.0f), 1.35f);
                    InstantiateTree(coconutPrefab, westExt.transform, "West_Palm_Hill_05", new Vector3(-66.0f, 2.8f, 165.0f), 1.30f);
                    InstantiateTree(coconutPrefab, westExt.transform, "West_Palm_Hill_06", new Vector3(-65.0f, 2.4f, 185.0f), 1.25f);
                }

                // 6. Dense Bamboo Groves (Lũy Tre Làng Phía Tây)
                if (bambooPrefab != null)
                {
                    float[] westBambooZs = new float[] { 75f, 85f, 95f, 105f, 115f, 125f, 135f, 145f, 155f, 165f, 175f, 185f };
                    for (int i = 0; i < westBambooZs.Length; i++)
                    {
                        float bz = westBambooZs[i];
                        float bx = -59.0f - (i % 2) * 2.5f;
                        InstantiateTree(bambooPrefab, westExt.transform, $"West_Bamboo_Grove_{i:D2}", new Vector3(bx, UNIFORM_GROUND_Y + 0.2f, bz), 1.35f + (i % 3) * 0.08f);
                    }
                }

                // 7. Large Canopy Banyan Trees (Cây Cổ Thụ Đỉnh Núi Tây)
                if (canopyPrefab != null)
                {
                    InstantiateTree(canopyPrefab, westExt.transform, "West_Canopy_Peak_01", new Vector3(-76.0f, 5.2f,  85.0f), 1.60f);
                    InstantiateTree(canopyPrefab, westExt.transform, "West_Canopy_Peak_02", new Vector3(-78.0f, 6.0f, 110.0f), 1.70f);
                    InstantiateTree(canopyPrefab, westExt.transform, "West_Canopy_Peak_03", new Vector3(-80.0f, 6.5f, 140.0f), 1.75f);
                    InstantiateTree(canopyPrefab, westExt.transform, "West_Canopy_Peak_04", new Vector3(-79.0f, 6.2f, 168.0f), 1.65f);
                    InstantiateTree(canopyPrefab, westExt.transform, "West_Canopy_Peak_05", new Vector3(-72.0f, 4.5f, 192.0f), 1.50f);
                }

                // 8. Banana Clumps (Bụi Chuối Ven Đồi Tây)
                if (bananaPrefab != null)
                {
                    float[] westBananaZs = new float[] { 74f, 88f, 104f, 122f, 138f, 154f, 172f, 186f };
                    for (int i = 0; i < westBananaZs.Length; i++)
                    {
                        float bz = westBananaZs[i];
                        float bx = -56.0f - (i % 2) * 1.5f;
                        InstantiateTree(bananaPrefab, westExt.transform, $"West_Banana_Clump_{i:D2}", new Vector3(bx, UNIFORM_GROUND_Y + 0.1f, bz), 1.30f);
                    }
                }
            }

            // =========================================================================
            // B. EAST FLANK MOUNTAINS & DENSE TROPICAL FOREST (Dãy Núi Đá & Rừng Đông)
            // =========================================================================
            var eastRange = GameObject.Find("MountainRange_East_Flank");
            if (eastRange != null)
            {
                var oldEastExt = eastRange.transform.Find("Mountain_5X_East_Ext");
                if (oldEastExt != null) Object.DestroyImmediate(oldEastExt.gameObject);

                var eastExt = new GameObject("Mountain_5X_East_Ext");
                eastExt.transform.parent = eastRange.transform;
                eastExt.transform.position = Vector3.zero;

                // 1. High Backdrop Mountain Ridges (Layer 1 - Cao nhất, che kín đường chân trời)
                CreateOrganicHillMesh(eastExt.transform, "Hill_East_DeepPeak_01", new Vector3(88.0f, UNIFORM_GROUND_Y, 110.0f), 55.0f, 75.0f, 15.0f, matGrass);
                CreateOrganicHillMesh(eastExt.transform, "Hill_East_DeepPeak_02", new Vector3(90.0f, UNIFORM_GROUND_Y, 165.0f), 58.0f, 80.0f, 16.5f, matGrass);
                CreateOrganicHillMesh(eastExt.transform, "Hill_East_MidRidge_01",  new Vector3(78.0f, UNIFORM_GROUND_Y, 105.0f), 48.0f, 65.0f,  9.0f, matGrass);
                CreateOrganicHillMesh(eastExt.transform, "Hill_East_MidRidge_02",  new Vector3(78.0f, UNIFORM_GROUND_Y, 160.0f), 50.0f, 65.0f,  9.5f, matGrass);

                // 2. Towering 3D Rock Crags / Monoliths (Vách núi đá Karst sừng sững)
                // Layer sâu:
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Deep_01", new Vector3(86.0f, 5.5f,  95.0f), new Vector3(10.0f, 16.0f,  9.0f), new Vector3(0f, -15f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Deep_02", new Vector3(88.0f, 6.0f, 130.0f), new Vector3(11.0f, 17.5f, 10.0f), new Vector3(0f,  35f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Deep_03", new Vector3(90.0f, 6.5f, 165.0f), new Vector3(12.0f, 18.5f, 10.5f), new Vector3(0f, -45f, 0f), matRock);
                // Layer sườn giữa:
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Mid_01",  new Vector3(72.0f, 3.8f,  85.0f), new Vector3( 8.5f, 13.0f,  8.0f), new Vector3(0f,  25f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Mid_02",  new Vector3(74.0f, 4.2f, 110.0f), new Vector3( 9.0f, 14.0f,  8.5f), new Vector3(0f, -60f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Mid_03",  new Vector3(71.0f, 4.0f, 135.0f), new Vector3( 8.5f, 13.5f,  8.0f), new Vector3(0f,  40f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Mid_04",  new Vector3(73.0f, 4.5f, 155.0f), new Vector3( 9.0f, 14.5f,  8.5f), new Vector3(0f, -20f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Mid_05",  new Vector3(75.0f, 4.2f, 175.0f), new Vector3( 9.5f, 15.0f,  9.0f), new Vector3(0f,  70f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Pass_01", new Vector3(68.0f, 3.5f, 190.0f), new Vector3( 8.5f, 13.0f,  8.0f), new Vector3(0f, -30f, 0f), matRock);

                // 3. Volumetric Rock Platforms & Cliff Terraces (Thềm đá khối bậc thang)
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_01", new Vector3(58.0f, 1.2f,  85.0f), new Vector3(3.6f, 2.8f, 3.2f), new Vector3(0f, -25f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_02", new Vector3(60.0f, 1.6f, 100.0f), new Vector3(4.0f, 3.0f, 3.5f), new Vector3(0f,  15f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_03", new Vector3(57.0f, 1.3f, 115.0f), new Vector3(3.5f, 2.6f, 3.0f), new Vector3(0f, -40f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_04", new Vector3(61.0f, 1.8f, 130.0f), new Vector3(4.2f, 3.2f, 3.6f), new Vector3(0f,  30f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_05", new Vector3(58.0f, 1.4f, 145.0f), new Vector3(3.8f, 2.8f, 3.2f), new Vector3(0f, -50f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_06", new Vector3(60.0f, 1.7f, 160.0f), new Vector3(4.0f, 3.0f, 3.4f), new Vector3(0f,  20f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_07", new Vector3(56.0f, 1.2f, 175.0f), new Vector3(3.5f, 2.5f, 3.0f), new Vector3(0f, -35f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_08", new Vector3(59.0f, 1.5f, 188.0f), new Vector3(3.8f, 2.8f, 3.2f), new Vector3(0f,  45f, 0f), matRock);

                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_Terrace_01", new Vector3(66.0f, 2.8f,  95.0f), new Vector3(4.5f, 3.5f, 4.0f), new Vector3(0f, -10f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_Terrace_02", new Vector3(68.0f, 3.2f, 120.0f), new Vector3(4.8f, 3.6f, 4.2f), new Vector3(0f,  55f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_Terrace_03", new Vector3(67.0f, 3.0f, 150.0f), new Vector3(4.6f, 3.5f, 4.0f), new Vector3(0f, -20f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Plat_Terrace_04", new Vector3(69.0f, 3.4f, 170.0f), new Vector3(4.8f, 3.6f, 4.2f), new Vector3(0f,  35f, 0f), matRock);

                // 4. Large Footing Boulders along field edge
                float[] eastBoulderZs = new float[] { 78f, 92f, 105f, 118f, 132f, 146f, 158f, 172f, 185f };
                for (int i = 0; i < eastBoulderZs.Length; i++)
                {
                    float bz = eastBoulderZs[i];
                    float bx = 55.0f + (i % 2 == 0 ? 1.0f : -0.8f);
                    InstantiateRock(boulderPrefab, eastExt.transform, $"East_Boulder_Ext_{i:D2}", new Vector3(bx, UNIFORM_GROUND_Y + 0.3f, bz), new Vector3(2.6f, 2.0f, 2.4f), new Vector3(-5f * i, -35f * i, 0f));
                }

                // 5. Dense Coconut Palms (Rặng Dừa Phía Đông)
                if (coconutPrefab != null)
                {
                    float[] eastPalmZs = new float[] { 72f, 80f, 90f, 98f, 108f, 116f, 126f, 134f, 144f, 152f, 162f, 170f, 180f, 188f };
                    for (int i = 0; i < eastPalmZs.Length; i++)
                    {
                        float pz = eastPalmZs[i];
                        float px = 38.0f + (i % 3) * 1.5f;
                        float scale = 1.20f + (i % 4) * 0.08f;
                        InstantiateTree(coconutPrefab, eastExt.transform, $"East_Palm_Border_{i:D2}", new Vector3(px, UNIFORM_GROUND_Y, pz), scale);
                    }

                    InstantiateTree(coconutPrefab, eastExt.transform, "East_Palm_Hill_01", new Vector3(62.0f, 2.2f,  88.0f), 1.30f);
                    InstantiateTree(coconutPrefab, eastExt.transform, "East_Palm_Hill_02", new Vector3(65.0f, 3.0f, 105.0f), 1.40f);
                    InstantiateTree(coconutPrefab, eastExt.transform, "East_Palm_Hill_03", new Vector3(63.0f, 2.5f, 125.0f), 1.25f);
                    InstantiateTree(coconutPrefab, eastExt.transform, "East_Palm_Hill_04", new Vector3(66.0f, 3.2f, 145.0f), 1.35f);
                    InstantiateTree(coconutPrefab, eastExt.transform, "East_Palm_Hill_05", new Vector3(64.0f, 2.8f, 165.0f), 1.30f);
                    InstantiateTree(coconutPrefab, eastExt.transform, "East_Palm_Hill_06", new Vector3(63.0f, 2.4f, 185.0f), 1.25f);
                }

                // 6. Dense Bamboo Groves (Lũy Tre Làng Phía Đông)
                if (bambooPrefab != null)
                {
                    float[] eastBambooZs = new float[] { 75f, 85f, 95f, 105f, 115f, 125f, 135f, 145f, 155f, 165f, 175f, 185f };
                    for (int i = 0; i < eastBambooZs.Length; i++)
                    {
                        float bz = eastBambooZs[i];
                        float bx = 52.0f + (i % 2) * 2.5f;
                        InstantiateTree(bambooPrefab, eastExt.transform, $"East_Bamboo_Grove_{i:D2}", new Vector3(bx, UNIFORM_GROUND_Y + 0.2f, bz), 1.35f + (i % 3) * 0.08f);
                    }
                }

                // 7. Large Canopy Banyan Trees (Cây Cổ Thụ Đỉnh Núi Đông)
                if (canopyPrefab != null)
                {
                    InstantiateTree(canopyPrefab, eastExt.transform, "East_Canopy_Peak_01", new Vector3(76.0f, 5.2f,  85.0f), 1.60f);
                    InstantiateTree(canopyPrefab, eastExt.transform, "East_Canopy_Peak_02", new Vector3(78.0f, 6.0f, 110.0f), 1.70f);
                    InstantiateTree(canopyPrefab, eastExt.transform, "East_Canopy_Peak_03", new Vector3(80.0f, 6.5f, 140.0f), 1.75f);
                    InstantiateTree(canopyPrefab, eastExt.transform, "East_Canopy_Peak_04", new Vector3(79.0f, 6.2f, 168.0f), 1.65f);
                    InstantiateTree(canopyPrefab, eastExt.transform, "East_Canopy_Peak_05", new Vector3(72.0f, 4.5f, 192.0f), 1.50f);
                }

                // 8. Banana Clumps (Bụi Chuối Ven Đầm Đông)
                if (bananaPrefab != null)
                {
                    float[] eastBananaZs = new float[] { 74f, 88f, 104f, 122f, 138f, 154f, 172f, 186f };
                    for (int i = 0; i < eastBananaZs.Length; i++)
                    {
                        float bz = eastBananaZs[i];
                        float bx = 41.0f + (i % 2) * 1.5f;
                        InstantiateTree(bananaPrefab, eastExt.transform, $"East_Banana_Clump_{i:D2}", new Vector3(bx, UNIFORM_GROUND_Y + 0.1f, bz), 1.30f);
                    }
                }
            }

            // =========================================================================
            // C. NORTH GATE PASS ENCLOSURE (Đèo Núi & Hàng Cây Khung Cổng Làng)
            // =========================================================================
            var northBackdrop = GameObject.Find("MountainRange_North_Backdrop");
            if (northBackdrop != null)
            {
                var oldPassExt = northBackdrop.transform.Find("Pass_Enclosure_Ext");
                if (oldPassExt != null) Object.DestroyImmediate(oldPassExt.gameObject);

                var passExt = new GameObject("Pass_Enclosure_Ext");
                passExt.transform.parent = northBackdrop.transform;
                passExt.transform.position = Vector3.zero;

                InstantiateRock(rockCragPrefab, passExt.transform, "Pass_Crag_West_Outer", new Vector3(-28.0f, 3.5f, 192.0f), new Vector3(8.0f, 12.0f, 7.5f), new Vector3(0f,  40f, 0f), matRock);
                InstantiateRock(rockCragPrefab, passExt.transform, "Pass_Crag_West_Inner", new Vector3(-14.0f, 2.8f, 189.0f), new Vector3(7.0f, 10.5f, 6.5f), new Vector3(0f,  15f, 0f), matRock);
                InstantiateRock(rockCragPrefab, passExt.transform, "Pass_Crag_East_Inner", new Vector3( 10.0f, 2.8f, 189.0f), new Vector3(7.0f, 10.5f, 6.5f), new Vector3(0f, -15f, 0f), matRock);
                InstantiateRock(rockCragPrefab, passExt.transform, "Pass_Crag_East_Outer", new Vector3( 24.0f, 3.5f, 192.0f), new Vector3(8.0f, 12.0f, 7.5f), new Vector3(0f, -40f, 0f), matRock);

                if (bambooPrefab != null)
                {
                    InstantiateTree(bambooPrefab, passExt.transform, "Pass_Bamboo_West", new Vector3(-11.5f, UNIFORM_GROUND_Y, 186.0f), 1.35f);
                    InstantiateTree(bambooPrefab, passExt.transform, "Pass_Bamboo_East", new Vector3(  5.5f, UNIFORM_GROUND_Y, 186.0f), 1.35f);
                }
                if (coconutPrefab != null)
                {
                    InstantiateTree(coconutPrefab, passExt.transform, "Pass_Coconut_West", new Vector3(-13.0f, UNIFORM_GROUND_Y, 184.0f), 1.30f);
                    InstantiateTree(coconutPrefab, passExt.transform, "Pass_Coconut_East", new Vector3(  7.0f, UNIFORM_GROUND_Y, 184.0f), 1.30f);
                }
                if (canopyPrefab != null)
                {
                    InstantiateTree(canopyPrefab, passExt.transform, "Pass_Canopy_Backdrop", new Vector3(-7.0f, 5.0f, 205.0f), 1.80f);
                }
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=#4CAF50>[RiceExpansion5X]</color> SUCCESS: Dense rocky mountains, stepped stone platforms, and tropical forest added to both sides!");
        }

        private static GameObject CreateOrganicHillMesh(Transform parent, string name, Vector3 center, float width, float length, float maxHeight, Material mat, float groundY = UNIFORM_GROUND_Y)
        {
            int segmentsX = 12;
            int segmentsZ = 12;
            int numVertices = (segmentsX + 1) * (segmentsZ + 1);
            var vertices = new Vector3[numVertices];
            var uvs = new Vector2[numVertices];
            var triangles = new int[segmentsX * segmentsZ * 6];

            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            for (int z = 0; z <= segmentsZ; z++)
            {
                float tz = (float)z / segmentsZ;
                float posZ = Mathf.Lerp(-halfL, halfL, tz);
                float bellZ = Mathf.Sin(tz * Mathf.PI);

                for (int x = 0; x <= segmentsX; x++)
                {
                    float tx = (float)x / segmentsX;
                    float posX = Mathf.Lerp(-halfW, halfW, tx);
                    float bellX = Mathf.Sin(tx * Mathf.PI);

                    float shapeFactor = Mathf.Pow(Mathf.Clamp01(bellX * bellZ), 0.75f);
                    float y = groundY + (maxHeight * shapeFactor);

                    int idx = z * (segmentsX + 1) + x;
                    vertices[idx] = new Vector3(posX, y - groundY, posZ);
                    uvs[idx] = new Vector2(tx * (width / 16f), tz * (length / 16f));
                }
            }

            int triIndex = 0;
            for (int z = 0; z < segmentsZ; z++)
            {
                for (int x = 0; x < segmentsX; x++)
                {
                    int botLeft = z * (segmentsX + 1) + x;
                    int botRight = botLeft + 1;
                    int topLeft = (z + 1) * (segmentsX + 1) + x;
                    int topRight = topLeft + 1;

                    triangles[triIndex++] = botLeft;
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = botRight;

                    triangles[triIndex++] = botRight;
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = topRight;
                }
            }

            var mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.parent = parent;
            go.transform.position = center;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            return go;
        }

        private static GameObject InstantiateRock(GameObject prefab, Transform parent, string name, Vector3 pos, Vector3 scale, Vector3 rot, Material overrideMat = null)
        {
            if (prefab == null) return null;
            var obj = Object.Instantiate(prefab, parent);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.transform.rotation = Quaternion.Euler(rot);

            if (overrideMat != null)
            {
                foreach (var r in obj.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = overrideMat;
                }
            }
            return obj;
        }

        private static GameObject InstantiateTree(GameObject prefab, Transform parent, string name, Vector3 pos, float scale)
        {
            if (prefab == null) return null;
            var obj = Object.Instantiate(prefab, parent);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * scale;
            obj.transform.rotation = Quaternion.identity;
            return obj;
        }
    }
}
