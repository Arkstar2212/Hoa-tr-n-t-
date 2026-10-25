using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace HoaVaMau.EditorTools
{
    public static class SceneBatchFixer
    {
        [MenuItem("Tools/Apply Scene Fixes")]
        public static void ApplyAllFixesAndCapture()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var expGo = GameObject.Find("Scene1_Expansion_115m");
            if (expGo == null)
            {
                Debug.LogError("Scene1_Expansion_115m not found!");
                return;
            }

            // Authentic Materials & Prefabs
            var mTrail = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Earthen_Trail.mat");
            var pRock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Outcrop.prefab");
            var pMossy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");
            var pMangrove = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/ModularKit/PF_Mod_MangroveRoot_Cluster.prefab");
            var pGrass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_02_TallMeadow.prefab");
            var pHouseWood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Wood.prefab");

            // ====================================================================
            // 1. FIX 1: LOWER RICE LAYERS & SEED CONTINUOUS GROUND & TREES IN ZONE 3
            // ====================================================================
            var z2 = expGo.transform.Find("Zone2_Slide_And_RicePaddyMeadow");
            if (z2 != null)
            {
                var riceRoot = z2.Find("Parallax_RiceField_Layers");
                if (riceRoot != null)
                {
                    foreach (Transform stalk in riceRoot.GetComponentsInChildren<Transform>())
                    {
                        if (stalk != riceRoot && stalk.name.Contains("Rice"))
                        {
                            if (stalk.position.z > 1.5f && stalk.position.y > -1.85f)
                            {
                                Vector3 p = stalk.position;
                                p.y = -2.15f; // Root stalks safely in soil
                                stalk.position = p;
                            }
                        }
                    }
                }
            }

            var z3 = expGo.transform.Find("Zone3_StiltVillage_And_CratePuzzle");
            if (z3 != null)
            {
                // Foreground village ground ends cleanly at X = 86.5m (transition to elevated walkway)
                var fgGround = z3.Find("Visual_Village_Ground_Zone3");
                if (fgGround != null)
                {
                    fgGround.position = new Vector3(79.25f, -2.55f, 2.0f);
                    fgGround.localScale = new Vector3(14.5f, 1.4f, 8.0f);
                }

                // Ground backdrop in Zone 3: Wide and deep (X = 72.5 to 86.5, Z = 1 to 31)
                var bgGround = z3.Find("Ground_Backdrop_Plane_Zone3");
                if (bgGround == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Ground_Backdrop_Plane_Zone3";
                    go.transform.SetParent(z3, false);
                    bgGround = go.transform;
                }
                bgGround.position = new Vector3(79.5f, -2.55f, 16.0f);
                bgGround.localScale = new Vector3(14.0f, 1.4f, 30.0f);
                var mrBg = bgGround.GetComponent<MeshRenderer>();
                if (mrBg != null && mTrail != null) mrBg.sharedMaterial = mTrail;
                var colBg = bgGround.GetComponent<Collider>();
                if (colBg != null) Object.DestroyImmediate(colBg);

                // Lower mid-ground village trees so roots firmly ground into soil
                var vbd = z3.Find("Village_Backdrop_Dressing");
                if (vbd != null)
                {
                    var bL = vbd.Find("Bamboo_Backdrop_Left");
                    if (bL != null) bL.position = new Vector3(75.0f, -1.60f, 6.0f);

                    var cC = vbd.Find("Coconut_Backdrop_Center");
                    if (cC != null) cC.position = new Vector3(80.5f, -1.54f, 7.5f);

                    var bP = vbd.Find("Banana_Backdrop_Porch");
                    if (bP != null) bP.position = new Vector3(84.0f, -1.74f, 5.0f);

                    var cM = vbd.Find("Coconut_Backdrop_Mid");
                    if (cM != null) cM.position = new Vector3(85.5f, -1.50f, 7.5f);

                    var bR = vbd.Find("Bamboo_Backdrop_Right");
                    if (bR != null) bR.position = new Vector3(86.5f, -1.61f, 6.5f);

                    // Add rich backdrop tree canopy across the entire horizon at Z = 12..16m
                    var distantRoot = vbd.Find("Distant_Backdrop_Treeline");
                    if (distantRoot == null)
                    {
                        var go = new GameObject("Distant_Backdrop_Treeline");
                        go.transform.SetParent(vbd, false);
                        distantRoot = go.transform;
                    }
                    for (int i = distantRoot.childCount - 1; i >= 0; i--)
                    {
                        Object.DestroyImmediate(distantRoot.GetChild(i).gameObject);
                    }

                    var bambooSprite = bL != null ? bL.GetComponent<SpriteRenderer>()?.sprite : null;
                    var coconutSprite = cC != null ? cC.GetComponent<SpriteRenderer>()?.sprite : null;

                    float[] bgX = { 72.5f, 74.5f, 76.5f, 78.5f, 80.5f, 82.5f, 84.5f, 86.0f };
                    float[] bgZ = { 13.0f, 15.0f, 13.5f, 16.0f, 14.0f, 15.5f, 13.5f, 15.0f };
                    for (int i = 0; i < bgX.Length; i++)
                    {
                        var treeGo = new GameObject("Back_Tree_" + i);
                        treeGo.transform.SetParent(distantRoot, false);
                        var sr = treeGo.AddComponent<SpriteRenderer>();
                        if (i % 2 == 0 && bambooSprite != null)
                        {
                            sr.sprite = bambooSprite;
                            treeGo.transform.position = new Vector3(bgX[i], -1.20f, bgZ[i]);
                            treeGo.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
                        }
                        else if (coconutSprite != null)
                        {
                            sr.sprite = coconutSprite;
                            treeGo.transform.position = new Vector3(bgX[i], -1.00f, bgZ[i]);
                            treeGo.transform.localScale = new Vector3(2.3f, 2.3f, 1f);
                        }
                        sr.color = new Color(0.85f, 0.92f, 0.88f, 0.95f);
                        sr.sortingOrder = 0;
                    }
                }

                // ====================================================================
                // 2. FIX 2: SCALE CRATE & SCALE HOUSES
                // ====================================================================
                var crate = z3.Find("PF_Movable_CargoCrate_3D");
                if (crate != null)
                {
                    crate.position = new Vector3(81.5f, -1.35f, 0.0f);
                    crate.localScale = Vector3.one;
                    var col = crate.GetComponent<BoxCollider>();
                    if (col != null)
                    {
                        col.size = new Vector3(1.0f, 1.0f, 1.0f);
                        col.center = Vector3.zero;
                    }
                    var meshChild = crate.Find("Mesh_0");
                    if (meshChild != null)
                    {
                        meshChild.localPosition = Vector3.zero;
                        meshChild.localRotation = Quaternion.Euler(270.02f, 0f, 0f);
                        meshChild.localScale = new Vector3(53f, 53f, 53f); // Exactly 1.0m x 1.0m x 1.0m
                    }
                }

                // Thatched House Left: Stilts rest firmly on ground Y = -1.85
                var hThatched = z3.Find("StiltedHouse_Thatched_Zone3");
                if (hThatched != null)
                {
                    hThatched.position = new Vector3(75.5f, 0.40f, 5.5f);
                    hThatched.localScale = new Vector3(2.4f, 2.4f, 2.4f);
                }

                // Wood House Mid: Instantiate if empty, place on stilts
                var hWood = z3.Find("StiltedHouse_Wood_Zone3");
                if (hWood != null && hWood.childCount == 0 && pHouseWood != null)
                {
                    var newWood = (GameObject)PrefabUtility.InstantiatePrefab(pHouseWood, z3);
                    newWood.name = "StiltedHouse_Wood_Zone3_Proper";
                    newWood.transform.position = new Vector3(83.0f, 0.35f, 9.5f);
                    newWood.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
                    Object.DestroyImmediate(hWood.gameObject);
                }

                // Village House 3 (Right edge of village, safely on bank before canal)
                var hVillage3 = z3.Find("StiltedHouse_Village_3");
                if (hVillage3 != null)
                {
                    hVillage3.position = new Vector3(88.5f, 0.35f, 7.0f);
                    hVillage3.localScale = new Vector3(2.3f, 2.3f, 2.3f);
                }
            }

            // ====================================================================
            // 3. FIX 3: 3D NPCS ON PIER (Zone 4)
            // ====================================================================
            var z4 = expGo.transform.Find("Zone4_Dock_1954_Departure");
            if (z4 != null)
            {
                var nFamily = z4.Find("NPC_Family_Waving_Dock");
                if (nFamily != null) Object.DestroyImmediate(nFamily.gameObject);
                var nSoldier = z4.Find("NPC_Soldier_Saluting_Dock");
                if (nSoldier != null) Object.DestroyImmediate(nSoldier.gameObject);

                var npc3DRoot = z4.Find("NPCs_3D_On_Pier");
                if (npc3DRoot != null)
                {
                    var aM = npc3DRoot.Find("NPC_3D_AnMother");
                    if (aM != null) aM.position = new Vector3(97.2f, -1.65f, 0.8f);
                    var eld = npc3DRoot.Find("NPC_3D_Elderly_Man");
                    if (eld != null) eld.position = new Vector3(98.3f, -1.65f, 0.5f);
                    var fem = npc3DRoot.Find("NPC_3D_Female_VN");
                    if (fem != null) fem.position = new Vector3(99.6f, -1.65f, 0.9f);
                    var sol = npc3DRoot.Find("NPC_3D_Soldier_Villager");
                    if (sol != null) sol.position = new Vector3(101.5f, -1.65f, 0.3f);
                }

                // ====================================================================
                // 4. FIX 4: MASK RIVERBANKS, CONTINUOUS WATER & POSITION SHIP
                // ====================================================================
                // Water surface: Spans X = 85.5 to 160.5, Z = -15 to 65, covers canal & open sea
                var water = GameObject.Find("River_Water_Surface");
                if (water != null)
                {
                    water.transform.position = new Vector3(123.0f, -2.25f, 25.0f);
                    water.transform.localScale = new Vector3(7.5f, 1.0f, 8.0f);
                }

                // Right Bank Corner Masking at X = 104..108m
                var bankMask = z4.Find("RiverBank_Corner_RockFoliage_Masking");
                if (bankMask == null)
                {
                    var go = new GameObject("RiverBank_Corner_RockFoliage_Masking");
                    go.transform.SetParent(z4, false);
                    bankMask = go.transform;
                }
                for (int i = bankMask.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(bankMask.GetChild(i).gameObject);
                }

                if (pRock != null)
                {
                    var r1 = (GameObject)PrefabUtility.InstantiatePrefab(pRock, bankMask);
                    r1.transform.position = new Vector3(104.2f, -1.8f, -0.2f);
                    r1.transform.rotation = Quaternion.Euler(10f, 45f, 5f);
                    r1.transform.localScale = new Vector3(1.5f, 1.4f, 1.3f);

                    var r2 = (GameObject)PrefabUtility.InstantiatePrefab(pRock, bankMask);
                    r2.transform.position = new Vector3(104.5f, -1.6f, 1.2f);
                    r2.transform.rotation = Quaternion.Euler(5f, 120f, 0f);
                    r2.transform.localScale = new Vector3(1.6f, 1.5f, 1.4f);

                    var r3 = (GameObject)PrefabUtility.InstantiatePrefab(pRock, bankMask);
                    r3.transform.position = new Vector3(105.0f, -1.7f, 2.2f);
                    r3.transform.rotation = Quaternion.Euler(-10f, 60f, 0f);
                    r3.transform.localScale = new Vector3(1.5f, 1.4f, 1.3f);
                }
                if (pMossy != null)
                {
                    var m1 = (GameObject)PrefabUtility.InstantiatePrefab(pMossy, bankMask);
                    m1.transform.position = new Vector3(104.2f, -2.1f, -0.8f);
                    m1.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                    m1.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

                    var m2 = (GameObject)PrefabUtility.InstantiatePrefab(pMossy, bankMask);
                    m2.transform.position = new Vector3(104.8f, -1.8f, 0.5f);
                    m2.transform.rotation = Quaternion.Euler(15f, 75f, 0f);
                    m2.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

                    var m3 = (GameObject)PrefabUtility.InstantiatePrefab(pMossy, bankMask);
                    m3.transform.position = new Vector3(105.5f, -1.7f, 1.8f);
                    m3.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                    m3.transform.localScale = new Vector3(1.5f, 1.4f, 1.4f);

                    var m4 = (GameObject)PrefabUtility.InstantiatePrefab(pMossy, bankMask);
                    m4.transform.position = new Vector3(106.8f, -1.85f, -0.3f);
                    m4.transform.localScale = new Vector3(1.3f, 1.2f, 1.2f);
                }
                if (pMangrove != null)
                {
                    var mg1 = (GameObject)PrefabUtility.InstantiatePrefab(pMangrove, bankMask);
                    mg1.transform.position = new Vector3(104.3f, -2.1f, 0.0f);
                    mg1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    mg1.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

                    var mg2 = (GameObject)PrefabUtility.InstantiatePrefab(pMangrove, bankMask);
                    mg2.transform.position = new Vector3(104.7f, -2.0f, 1.5f);
                    mg2.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                    mg2.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
                }
                if (pGrass != null)
                {
                    var gBank1 = (GameObject)PrefabUtility.InstantiatePrefab(pGrass, bankMask);
                    gBank1.transform.position = new Vector3(105.2f, -1.75f, 0.3f);
                    gBank1.transform.localScale = new Vector3(1.4f, 1.4f, 1.2f);

                    var gBank2 = (GameObject)PrefabUtility.InstantiatePrefab(pGrass, bankMask);
                    gBank2.transform.position = new Vector3(105.8f, -1.75f, 1.5f);
                    gBank2.transform.localScale = new Vector3(1.4f, 1.4f, 1.2f);

                    var gBank3 = (GameObject)PrefabUtility.InstantiatePrefab(pGrass, bankMask);
                    gBank3.transform.position = new Vector3(106.5f, -1.75f, -0.2f);
                    gBank3.transform.localScale = new Vector3(1.5f, 1.4f, 1.2f);
                }

                // Left Bank Natural Shoreline Masking at X = 86.5..90.0m (Lush riverbank)
                var leftBankMask = z4.Find("LeftBank_RockRoot_Masking");
                if (leftBankMask == null)
                {
                    var go = new GameObject("LeftBank_RockRoot_Masking");
                    go.transform.SetParent(z4, false);
                    leftBankMask = go.transform;
                }
                for (int i = leftBankMask.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(leftBankMask.GetChild(i).gameObject);
                }

                // Bamboo & Coconut trees framing the left riverbank
                var bambooSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/Environment/Lana_Concept_Assets/Trees/T_LanaVN_Tree_07_BambooGrove.png");
                var coconutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/Environment/Lana_Concept_Assets/Trees/T_LanaVN_Tree_06_Coconut.png");

                if (bambooSprite != null)
                {
                    var lTree1 = new GameObject("LeftBank_BambooTree_1");
                    lTree1.transform.SetParent(leftBankMask, false);
                    var sr1 = lTree1.AddComponent<SpriteRenderer>();
                    sr1.sprite = bambooSprite;
                    lTree1.transform.position = new Vector3(86.5f, -1.25f, 5.0f);
                    lTree1.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
                    sr1.sortingOrder = 0;

                    var lTree2 = new GameObject("LeftBank_BambooTree_2");
                    lTree2.transform.SetParent(leftBankMask, false);
                    var sr2 = lTree2.AddComponent<SpriteRenderer>();
                    sr2.sprite = bambooSprite;
                    lTree2.transform.position = new Vector3(87.8f, -1.15f, 11.0f);
                    lTree2.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
                    sr2.sortingOrder = 0;
                }
                if (coconutSprite != null)
                {
                    var lTree3 = new GameObject("LeftBank_CoconutTree_1");
                    lTree3.transform.SetParent(leftBankMask, false);
                    var sr3 = lTree3.AddComponent<SpriteRenderer>();
                    sr3.sprite = coconutSprite;
                    lTree3.transform.position = new Vector3(87.0f, -1.10f, 8.0f);
                    lTree3.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
                    sr3.sortingOrder = 0;
                }

                if (pMangrove != null)
                {
                    var lmg1 = (GameObject)PrefabUtility.InstantiatePrefab(pMangrove, leftBankMask);
                    lmg1.transform.position = new Vector3(87.5f, -2.0f, 1.5f);
                    lmg1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    lmg1.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);

                    var lmg2 = (GameObject)PrefabUtility.InstantiatePrefab(pMangrove, leftBankMask);
                    lmg2.transform.position = new Vector3(87.2f, -2.1f, -0.2f);
                    lmg2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    lmg2.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                }
                if (pMossy != null)
                {
                    var lm1 = (GameObject)PrefabUtility.InstantiatePrefab(pMossy, leftBankMask);
                    lm1.transform.position = new Vector3(87.6f, -1.8f, 2.5f);
                    lm1.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

                    var lm2 = (GameObject)PrefabUtility.InstantiatePrefab(pMossy, leftBankMask);
                    lm2.transform.position = new Vector3(88.0f, -1.9f, 0.6f);
                    lm2.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                }

                // Skybox pushed back safely
                var sky = GameObject.Find("Skybox_Unified_PlainsHills");
                if (sky != null)
                {
                    sky.transform.position = new Vector3(57.5f, 8.0f, 65.0f);
                    sky.transform.localScale = new Vector3(320.0f, 100.0f, 1.0f);
                }

                // 1954 Ship sits naturally in the water on horizon
                var ship = GameObject.Find("TransportShip_1954_Horizon");
                if (ship != null)
                {
                    ship.transform.position = new Vector3(116.0f, 3.65f, 40.0f);
                    ship.transform.localScale = new Vector3(2.0f, 2.0f, 1.0f);
                    var sr = ship.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = 0;
                        sr.color = new Color(0.80f, 0.85f, 0.92f, 0.88f);
                    }
                }
            }

            // ====================================================================
            // 5. FIX 5: SMOOTH JUNCTION AT FOOT OF ROCK SLIDE (X = 40.0..43.5m)
            // ====================================================================
            var seamRoot = expGo.transform.Find("Slide_Seam_Foliage_Masking");
            if (seamRoot == null)
            {
                var go = new GameObject("Slide_Seam_Foliage_Masking");
                go.transform.SetParent(expGo.transform, false);
                seamRoot = go.transform;
            }
            for (int i = seamRoot.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(seamRoot.GetChild(i).gameObject);
            }

            if (pRock != null)
            {
                var rk1 = (GameObject)PrefabUtility.InstantiatePrefab(pRock, seamRoot);
                rk1.transform.position = new Vector3(41.8f, -1.25f, -0.3f);
                rk1.transform.rotation = Quaternion.Euler(10f, 35f, -5f);
                rk1.transform.localScale = new Vector3(1.6f, 1.4f, 1.2f);

                var rk2 = (GameObject)PrefabUtility.InstantiatePrefab(pRock, seamRoot);
                rk2.transform.position = new Vector3(42.6f, -1.55f, 0.1f);
                rk2.transform.rotation = Quaternion.Euler(15f, 120f, 0f);
                rk2.transform.localScale = new Vector3(1.2f, 1.1f, 1.0f);
            }

            if (pMossy != null)
            {
                var ms = (GameObject)PrefabUtility.InstantiatePrefab(pMossy, seamRoot);
                ms.transform.position = new Vector3(41.4f, -1.45f, -0.5f);
                ms.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                ms.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            }

            if (pGrass != null)
            {
                var g1 = (GameObject)PrefabUtility.InstantiatePrefab(pGrass, seamRoot);
                g1.transform.position = new Vector3(41.0f, -1.05f, -0.4f);
                g1.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
                g1.transform.localScale = new Vector3(1.6f, 1.6f, 1.2f);

                var g2 = (GameObject)PrefabUtility.InstantiatePrefab(pGrass, seamRoot);
                g2.transform.position = new Vector3(41.9f, -1.35f, -0.2f);
                g2.transform.rotation = Quaternion.Euler(5f, 180f, 0f);
                g2.transform.localScale = new Vector3(1.7f, 1.7f, 1.2f);

                var g3 = (GameObject)PrefabUtility.InstantiatePrefab(pGrass, seamRoot);
                g3.transform.position = new Vector3(42.8f, -1.70f, 0.1f);
                g3.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
                g3.transform.localScale = new Vector3(1.5f, 1.5f, 1.2f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // ====================================================================
            // 6. RENDER 3 GAMEPLAY-FRAMED SCREENSHOTS
            // ====================================================================
            CaptureAllThreeZones();
            Debug.Log("[SceneBatchFixer] Ultimate batch fixes applied and 3 screenshots updated!");
        }

        public static void CaptureAllThreeZones()
        {
            string baseDir = "C:/Users/Thanh Tài/.gemini/antigravity/brain/91d03331-ffbc-4dc5-be7e-61bc484b62a5";
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

            var tempCamGo = new GameObject("Gameplay_Framing_Cam");
            var cam = tempCamGo.AddComponent<Camera>();
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 34f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
            cam.targetTexture = rt;

            // 1. Zone 1 & 2: Ricefields & slide (X = 45m)
            tempCamGo.transform.position = new Vector3(45.0f, 1.5f, -10.8f);
            tempCamGo.transform.rotation = Quaternion.Euler(5.0f, 0f, 0f);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(baseDir, "expansion_zone1_and_2_ricefields.png"), tex.EncodeToPNG());

            // 2. Zone 3: Village & Crate Puzzle (X = 84m)
            tempCamGo.transform.position = new Vector3(84.0f, 0.8f, -10.8f);
            tempCamGo.transform.rotation = Quaternion.Euler(5.0f, 0f, 0f);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(baseDir, "expansion_zone3_village_crate_puzzle.png"), tex.EncodeToPNG());

            // 3. Zone 4: Dock & 1954 Departure Ship (X = 101.5m)
            tempCamGo.transform.position = new Vector3(101.5f, 0.8f, -10.8f);
            tempCamGo.transform.rotation = Quaternion.Euler(5.0f, 0f, 0f);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(baseDir, "expansion_zone4_dock_1954_ship.png"), tex.EncodeToPNG());

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(tempCamGo);
        }
    }
}