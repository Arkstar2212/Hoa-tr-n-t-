using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaDo.Editor
{
    public static class PlaceHousesDirectlyInPlayableVillageMap
    {
        [MenuItem("Tools/Hoa Do/Place Houses Directly In Playable Village Map")]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.LogWarning("[PlaceHouses] Active scene is not CaMau_1954_Village. Opening scene...");
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("[PlaceHouses] Placing authentic upright houses directly in walkable village map...");

            RemoveOldHouseInstances();
            PlaceVillageHousesInPlayableAreas();
            SetupNorthernMistyMountainBackground();
            AlignVillagePathsToHouses();

            Physics.SyncTransforms();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[PlaceHouses] Houses placed successfully in playable village map!");
        }

        private static void RemoveOldHouseInstances()
        {
            string[] houseNames = new string[]
            {
                "House_An_StiltedHouse_3D",
                "Nha_HangXom_NhaAn",
                "Nha_BacBa_KhuLayNuoc",
                "Nha_XomVuon_BacHai",
                "Nha_XomVuon_Go",
                "Nha_NongDan_CanhDong",
                "WaterHut_RiverEdge_3D",
                "Test_BlenderOptimized",
                "Test_Wood",
                "Test_Wood_Rot"
            };

            foreach (var n in houseNames)
            {
                var go = GameObject.Find(n);
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void PlaceVillageHousesInPlayableAreas()
        {
            // Use BlenderOptimized FBX models: verified 100% upright, correct Euler axes, no child rotation skew
            var fbxThatched = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_StiltedHouse_Thatched_BlenderOptimized.fbx");
            var fbxWood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_StiltedHouse_Wood_BlenderOptimized.fbx");

            var matThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat");
            var matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat");

            if (matThatched != null)
            {
                matThatched.SetFloat("_Cull", 0f); // Two-sided
                EditorUtility.SetDirty(matThatched);
            }
            if (matWood != null)
            {
                matWood.SetFloat("_Cull", 0f); // Two-sided
                EditorUtility.SetDirty(matWood);
            }

            // 1. Nhà An (An's Thatched Stilted House) - Playable village courtyard center
            // X = 0.50m, Z = 10.50m. Y = 3.33m places stilts firmly on ground (bounds.min.y = 0.00m)
            var areaNhaAn = GameObject.Find("CaMau_1954_Village/Village_Areas/01_Nha_An");
            if (fbxThatched != null && areaNhaAn != null)
            {
                var houseAn = CreateHouse(fbxThatched, matThatched, "House_An_StiltedHouse_3D", areaNhaAn.transform,
                    new Vector3(0.50f, 3.33f, 10.50f), Quaternion.Euler(0f, 0f, 0f), new Vector3(3.5f, 3.5f, 3.5f));

                var motherAn = GameObject.Find("NPC_Mother_An");
                if (motherAn != null)
                {
                    motherAn.transform.position = new Vector3(0.50f, 0.00f, 7.50f);
                    motherAn.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    EditorUtility.SetDirty(motherAn);
                }
            }

            // 2. Nhà Hàng Xóm (Neighbor's Thatched Stilted House) - West side of village street
            if (fbxThatched != null && areaNhaAn != null)
            {
                CreateHouse(fbxThatched, matThatched, "Nha_HangXom_NhaAn", areaNhaAn.transform,
                    new Vector3(-7.00f, 3.14f, 9.50f), Quaternion.Euler(0f, 15f, 0f), new Vector3(3.3f, 3.3f, 3.3f));
            }

            // 3. Nhà Bác Ba (Uncle Ba's Wooden Stilted House) - At Village Well plaza & bridge approach
            var areaKhuLayNuoc = GameObject.Find("CaMau_1954_Village/Village_Areas/02_Khu_Lay_Nuoc");
            if (fbxWood != null && areaKhuLayNuoc != null)
            {
                CreateHouse(fbxWood, matWood, "Nha_BacBa_KhuLayNuoc", areaKhuLayNuoc.transform,
                    new Vector3(12.50f, 2.33f, 10.50f), Quaternion.Euler(0f, -15f, 0f), new Vector3(3.5f, 3.5f, 3.5f));

                var uncleBa = GameObject.Find("NPC_Elderly_UncleBa");
                if (uncleBa != null)
                {
                    uncleBa.transform.position = new Vector3(13.60f, 0.00f, 7.50f);
                    uncleBa.transform.rotation = Quaternion.Euler(0f, 240f, 0f);
                    EditorUtility.SetDirty(uncleBa);
                }
            }

            // 4. Nhà Bác Hai (Bac Hai's Thatched Orchard House) - In the Fruit Orchard clearing
            var areaVuonCay = GameObject.Find("CaMau_1954_Village/Village_Areas/05_Vuon_Cay");
            if (fbxThatched != null && areaVuonCay != null)
            {
                CreateHouse(fbxThatched, matThatched, "Nha_XomVuon_BacHai", areaVuonCay.transform,
                    new Vector3(-16.00f, 3.14f, 7.50f), Quaternion.Euler(0f, 30f, 0f), new Vector3(3.3f, 3.3f, 3.3f));

                var gardener = GameObject.Find("NPC_Villager_Gardener");
                if (gardener != null)
                {
                    gardener.transform.position = new Vector3(-16.00f, 0.00f, 5.00f);
                    gardener.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
                    EditorUtility.SetDirty(gardener);
                }
            }

            // 5. Nhà Gỗ Xóm Vườn (Orchard Wooden Stilted House) - In the West Orchard
            if (fbxWood != null && areaVuonCay != null)
            {
                CreateHouse(fbxWood, matWood, "Nha_XomVuon_Go", areaVuonCay.transform,
                    new Vector3(-22.50f, 2.13f, 4.50f), Quaternion.Euler(0f, 60f, 0f), new Vector3(3.2f, 3.2f, 3.2f));

                var toddler = GameObject.Find("NPC_Baby_Toddler");
                if (toddler != null)
                {
                    toddler.transform.position = new Vector3(-20.50f, 0.00f, 3.50f);
                    EditorUtility.SetDirty(toddler);
                }
            }

            // 6. Nhà Nông Dân Cánh Đồng (Farmer's Thatched Stilted House) - Across canal bridge along the field dike
            var areaCanhDong = GameObject.Find("CaMau_1954_Village/Village_Areas/06_Canh_Dong");
            if (fbxThatched != null && areaCanhDong != null)
            {
                CreateHouse(fbxThatched, matThatched, "Nha_NongDan_CanhDong", areaCanhDong.transform,
                    new Vector3(21.00f, 3.14f, 14.50f), Quaternion.Euler(0f, -45f, 0f), new Vector3(3.3f, 3.3f, 3.3f));
            }

            // 7. Chòi Ven Sông (Riverside Stilted Wooden Hut) - Along the riverbank boardwalk
            var areaBoSong = GameObject.Find("CaMau_1954_Village/Village_Areas/07_Bo_Song");
            if (fbxWood != null && areaBoSong != null)
            {
                CreateHouse(fbxWood, matWood, "WaterHut_RiverEdge_3D", areaBoSong.transform,
                    new Vector3(-14.00f, 1.87f, -7.50f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.8f, 2.8f, 2.8f));
            }
        }

        private static GameObject CreateHouse(GameObject fbxSource, Material mat, string houseName, Transform parent,
            Vector3 worldPos, Quaternion rotation, Vector3 scale)
        {
            var instance = UnityEngine.Object.Instantiate(fbxSource, parent);
            instance.name = houseName;
            instance.transform.position = worldPos;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;

            // Assign proper material with two-sided rendering
            var renderers = instance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (mat != null) r.sharedMaterial = mat;
            }

            // Add well-fit BoxCollider around house body so it acts as solid obstacle,
            // without blocking player walking up to front ladder
            var col = instance.GetComponent<BoxCollider>();
            if (col == null) col = instance.AddComponent<BoxCollider>();

            if (fbxSource.name.Contains("Thatched"))
            {
                col.center = new Vector3(0f, 0f, 0.25f);
                col.size = new Vector3(1.2f, 1.8f, 1.3f);
            }
            else
            {
                col.center = new Vector3(0f, 0.1f, 0.25f);
                col.size = new Vector3(1.0f, 1.3f, 1.5f);
            }

            EditorUtility.SetDirty(instance);
            return instance;
        }

        private static void AlignVillagePathsToHouses()
        {
            var pathsGroup = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (pathsGroup == null) return;

            // Align house aprons directly under each house's front stairs
            var apronAn = pathsGroup.transform.Find("Path_NhaAn_Apron");
            if (apronAn != null)
            {
                apronAn.position = new Vector3(0.50f, 0.015f, 8.50f);
                apronAn.localScale = new Vector3(6.0f, 0.02f, 3.5f);
                EditorUtility.SetDirty(apronAn);
            }

            var apronNeighbor = pathsGroup.transform.Find("Path_NeighborHouse_Apron");
            if (apronNeighbor != null)
            {
                apronNeighbor.position = new Vector3(-7.00f, 0.015f, 7.50f);
                apronNeighbor.localScale = new Vector3(5.0f, 0.02f, 3.0f);
                EditorUtility.SetDirty(apronNeighbor);
            }

            var apronBacHai = pathsGroup.transform.Find("Path_Orchard_House_Apron_BacHai");
            if (apronBacHai != null)
            {
                apronBacHai.position = new Vector3(-16.00f, 0.015f, 5.50f);
                apronBacHai.localScale = new Vector3(5.0f, 0.02f, 3.0f);
                EditorUtility.SetDirty(apronBacHai);
            }

            var pathGo = pathsGroup.transform.Find("Path_Orchard_To_House_Go");
            if (pathGo != null)
            {
                pathGo.position = new Vector3(-20.50f, 0.015f, 3.50f);
                EditorUtility.SetDirty(pathGo);
            }

            var pathFarmer = pathsGroup.transform.Find("Path_Dike_To_Farmer_House");
            if (pathFarmer != null)
            {
                pathFarmer.position = new Vector3(20.50f, 0.015f, 12.00f);
                EditorUtility.SetDirty(pathFarmer);
            }
        }

        private static void SetupNorthernMistyMountainBackground()
        {
            // Adhere strictly to user request:
            // "Còn background thì chỉ có phần thửa ruộng mới có background kiểu đồi núi. còn lại thì giữ nguyên bầu trời không cần background."
            // Position the misty mountain backdrop exclusively behind the northern rice field expanse (X: 10 to 55m),
            // while leaving behind the village and everywhere else as open 360 sky.
            Texture2D mtnTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/RiceFieldLayers/Layer4_MistyMountains_Alpha.png");
            if (mtnTex == null)
            {
                mtnTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/karst_mountains_far.png");
            }

            var bgMtn = GameObject.Find("CaMau_1954_Village/Environment/09_Natural_Painterly_Background/BG_Layer_01_MistyMountains_Panorama");
            if (bgMtn != null && mtnTex != null)
            {
                bgMtn.transform.position = new Vector3(35.00f, 8.00f, 84.00f); // Behind rice fields
                bgMtn.transform.localScale = new Vector3(80.00f, 22.00f, 1.00f); // Width covering rice fields only

                var mr = bgMtn.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                    mr.sharedMaterial.mainTexture = mtnTex;
                    mr.sharedMaterial.color = new Color(0.96f, 0.93f, 0.88f, 0.85f);
                    EditorUtility.SetDirty(bgMtn);
                }
            }

            // Deactivate all untextured white cloud layers so sky renders cleanly
            string[] layersToDeactivate = new string[]
            {
                "CaMau_1954_Village/Environment/09_Natural_Painterly_Background/BG_Layer_02_KarstPeaks_West",
                "CaMau_1954_Village/Environment/09_Natural_Painterly_Background/BG_Layer_02_KarstPeaks_East",
                "CaMau_1954_Village/Environment/09_Natural_Painterly_Background/BG_Layer_03_GoldenClouds_Center",
                "CaMau_1954_Village/Environment/09_Natural_Painterly_Background/BG_Layer_03_DistantClouds_Top"
            };

            foreach (var l in layersToDeactivate)
            {
                var go = GameObject.Find(l);
                if (go != null)
                {
                    go.SetActive(false);
                    EditorUtility.SetDirty(go);
                }
            }
        }
    }
}
