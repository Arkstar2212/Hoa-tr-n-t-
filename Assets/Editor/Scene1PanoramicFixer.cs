using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace HoaVaMau.EditorTools
{
    public static class Scene1PanoramicFixer
    {
        [MenuItem("Tools/Fix Scene 1 Layout")]
        public static void FixScene1Layout()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var expGo = GameObject.Find("Scene1_Expansion_115m");

            // Clean up any old duplicate or bad helper objects
            var oldBase = GameObject.Find("Black_Ground_Base_Unified");
            if (oldBase != null) Object.DestroyImmediate(oldBase);

            // =========================================================================
            // 1. WATER & 1954 SHIP (X = 75m -> 115m)
            // =========================================================================
            var mWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_RiverWater_Canh1.mat");
            if (mWater == null) mWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_OriWater_CaMau.mat");

            var waterObj = GameObject.Find("River_Water_Surface");
            if (waterObj == null) waterObj = GameObject.Find("Water_Plane");
            if (waterObj != null)
            {
                waterObj.transform.rotation = Quaternion.identity;
                // Extend water plane from X = 86m to 136m (width 50m)
                // Z from -9.0m to +29.0m (length 38m, center Z = 10.0m) to surround pier completely
                waterObj.transform.position = new Vector3(111.0f, -1.85f, 10.0f);
                waterObj.transform.localScale = new Vector3(5.0f, 1.0f, 3.8f);

                var mrWater = waterObj.GetComponent<MeshRenderer>();
                if (mrWater != null && mWater != null) mrWater.sharedMaterial = mWater;
            }

            // Riverbed muddy floor underneath canal water
            var mMud = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_CaMau_RiverMud.mat");
            if (mMud == null) mMud = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Seabed_Sand.mat");

            var riverBed = GameObject.Find("RiverBed_Canal_Floor");
            if (riverBed == null)
            {
                riverBed = GameObject.CreatePrimitive(PrimitiveType.Plane);
                riverBed.name = "RiverBed_Canal_Floor";
                if (waterObj != null && waterObj.transform.parent != null)
                    riverBed.transform.SetParent(waterObj.transform.parent, true);
                var col = riverBed.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            riverBed.transform.rotation = Quaternion.identity;
            riverBed.transform.position = new Vector3(111.0f, -3.20f, 10.0f);
            riverBed.transform.localScale = new Vector3(5.0f, 1.0f, 3.8f);
            if (mMud != null) riverBed.GetComponent<MeshRenderer>().sharedMaterial = mMud;

            // Ship placed at X = 108m, Y = -1.2m, Z = 28m so entire hull is visible floating on water
            var ship = GameObject.Find("TransportShip_1954_Horizon");
            if (ship != null)
            {
                ship.transform.position = new Vector3(108.0f, -1.2f, 28.0f);
                ship.transform.rotation = Quaternion.identity;
                ship.transform.localScale = new Vector3(2.0f, 2.0f, 1.0f);
                var sr = ship.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 0;
                    sr.color = Color.white;
                }
            }

            // =========================================================================
            // 2. EXTEND BLACK GROUND BASE / UNDERBELLY FROM X = -10m TO 125m
            // =========================================================================
            var ledges = GameObject.Find("Lana_Sculpted_Ledges");
            var mUnderbelly = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_RockUnderbelly.mat");
            if (mUnderbelly == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                mUnderbelly = new Material(s);
                mUnderbelly.color = new Color(0.04f, 0.05f, 0.06f, 1.0f);
            }

            var underbellyRoot = GameObject.Find("Black_Ground_Base_Extended");
            if (underbellyRoot == null)
            {
                var go = new GameObject("Black_Ground_Base_Extended");
                if (ledges != null) go.transform.SetParent(ledges.transform, true);
                underbellyRoot = go;
            }
            for (int i = underbellyRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(underbellyRoot.transform.GetChild(i).gameObject);
            }

            // Section 1: Under Village (X = 72m to 93m)
            var bVillage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bVillage.name = "Underbelly_Village_X72_93";
            bVillage.transform.SetParent(underbellyRoot.transform, false);
            bVillage.transform.position = new Vector3(82.5f, -8.75f, 0.0f);
            bVillage.transform.localScale = new Vector3(21.0f, 12.5f, 12.0f); // Top at Y = -2.5m
            var colV = bVillage.GetComponent<Collider>(); if (colV != null) Object.DestroyImmediate(colV);
            bVillage.GetComponent<MeshRenderer>().sharedMaterial = mUnderbelly;

            // Section 2: Under Canal & Pier (X = 93m to 125m)
            var bDock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bDock.name = "Underbelly_Dock_X93_125";
            bDock.transform.SetParent(underbellyRoot.transform, false);
            bDock.transform.position = new Vector3(109.0f, -9.5f, 0.0f);
            bDock.transform.localScale = new Vector3(32.0f, 12.5f, 20.0f); // Top at Y = -3.25m
            var colD = bDock.GetComponent<Collider>(); if (colD != null) Object.DestroyImmediate(colD);
            bDock.GetComponent<MeshRenderer>().sharedMaterial = mUnderbelly;

            // =========================================================================
            // 3. FLATTEN VILLAGE GROUND & SPREAD TREES & ASSIGN HOUSE MATERIALS
            // =========================================================================
            var mTrail = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Earthen_Trail.mat");
            var mThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat");
            var mWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat");

            var z3 = expGo != null ? expGo.transform.Find("Zone3_StiltVillage_And_CratePuzzle") : null;
            if (z3 != null)
            {
                // Flatten walking trail at Y = -1.35m
                var fgGround = z3.Find("Visual_Village_Ground_Zone3");
                if (fgGround != null)
                {
                    fgGround.position = new Vector3(82.5f, -2.05f, 0.5f);
                    fgGround.localScale = new Vector3(21.0f, 1.4f, 5.0f);
                    if (mTrail != null) fgGround.GetComponent<MeshRenderer>().sharedMaterial = mTrail;
                }

                // Flatten backdrop earth plane at Y = -1.35m
                var bgGround = z3.Find("Ground_Backdrop_Plane_Zone3");
                if (bgGround != null)
                {
                    bgGround.position = new Vector3(82.5f, -2.05f, 10.0f);
                    bgGround.localScale = new Vector3(21.0f, 1.4f, 14.0f);
                    if (mTrail != null) bgGround.GetComponent<MeshRenderer>().sharedMaterial = mTrail;
                }

                // Spread bamboo & coconut trees evenly across X = 65m to 95m
                var vbd = z3.Find("Village_Backdrop_Dressing");
                if (vbd != null)
                {
                    var distantRoot = vbd.Find("Distant_Backdrop_Treeline");
                    if (distantRoot != null)
                    {
                        var bL = vbd.Find("Bamboo_Backdrop_Left");
                        var cC = vbd.Find("Coconut_Backdrop_Center");
                        var bambooSprite = bL != null ? bL.GetComponent<SpriteRenderer>()?.sprite : null;
                        var coconutSprite = cC != null ? cC.GetComponent<SpriteRenderer>()?.sprite : null;

                        for (int i = distantRoot.childCount - 1; i >= 0; i--)
                            Object.DestroyImmediate(distantRoot.GetChild(i).gameObject);

                        float[] spreadX = { 65.0f, 68.0f, 71.5f, 75.0f, 78.5f, 82.0f, 85.5f, 88.5f, 91.5f, 94.5f };
                        float[] spreadZ = { 11.0f, 13.0f, 10.5f, 12.5f, 11.0f, 13.5f, 11.5f, 13.0f, 11.0f, 12.5f };

                        for (int i = 0; i < spreadX.Length; i++)
                        {
                            var tGo = new GameObject("Spaced_Tree_" + i);
                            tGo.transform.SetParent(distantRoot, false);
                            var sr = tGo.AddComponent<SpriteRenderer>();
                            if (i % 2 == 0 && bambooSprite != null)
                            {
                                sr.sprite = bambooSprite;
                                tGo.transform.position = new Vector3(spreadX[i], -1.15f, spreadZ[i]);
                                tGo.transform.localScale = new Vector3(2.2f, 2.2f, 1.0f);
                            }
                            else if (coconutSprite != null)
                            {
                                sr.sprite = coconutSprite;
                                tGo.transform.position = new Vector3(spreadX[i], -1.05f, spreadZ[i]);
                                tGo.transform.localScale = new Vector3(2.2f, 2.2f, 1.0f);
                            }
                            sr.sortingOrder = 0;
                            sr.color = new Color(0.88f, 0.94f, 0.90f, 1.0f);
                        }
                    }

                    var bLeft = vbd.Find("Bamboo_Backdrop_Left");
                    if (bLeft != null) bLeft.position = new Vector3(72.0f, -1.25f, 5.5f);

                    var cCenter = vbd.Find("Coconut_Backdrop_Center");
                    if (cCenter != null) cCenter.position = new Vector3(79.0f, -1.20f, 6.5f);

                    var bPorch = vbd.Find("Banana_Backdrop_Porch");
                    if (bPorch != null) bPorch.position = new Vector3(84.0f, -1.30f, 4.5f);

                    var cMid = vbd.Find("Coconut_Backdrop_Mid");
                    if (cMid != null) cMid.position = new Vector3(88.0f, -1.20f, 6.0f);

                    var bRight = vbd.Find("Bamboo_Backdrop_Right");
                    if (bRight != null) bRight.position = new Vector3(92.0f, -1.25f, 5.5f);
                }

                // Restore Materials on Stilted Houses
                var hThatched = z3.Find("StiltedHouse_Thatched_Zone3");
                if (hThatched != null)
                {
                    hThatched.position = new Vector3(74.5f, 0.85f, 5.0f);
                    foreach (var mr in hThatched.GetComponentsInChildren<MeshRenderer>())
                        if (mThatched != null) mr.sharedMaterial = mThatched;
                }

                var hWood = z3.Find("StiltedHouse_Wood_Zone3_Proper");
                if (hWood == null) hWood = z3.Find("StiltedHouse_Wood_Zone3");
                if (hWood != null)
                {
                    hWood.position = new Vector3(82.5f, 0.85f, 7.5f);
                    foreach (var mr in hWood.GetComponentsInChildren<MeshRenderer>())
                        if (mWood != null) mr.sharedMaterial = mWood;
                }

                var hVillage3 = z3.Find("StiltedHouse_Village_3");
                if (hVillage3 != null)
                {
                    hVillage3.position = new Vector3(89.5f, 0.85f, 6.5f);
                    foreach (var mr in hVillage3.GetComponentsInChildren<MeshRenderer>())
                        if (mThatched != null) mr.sharedMaterial = mThatched;
                }
            }

            // =========================================================================
            // 4. SLIDE ELEVATION DROP & SEAMLESS CONNECTION (X = 31m -> 45m)
            // =========================================================================
            var z2 = expGo != null ? expGo.transform.Find("Zone2_Slide_And_RicePaddyMeadow") : null;
            if (z2 != null)
            {
                var s1 = z2.Find("SlideSlope_Section_1");
                if (s1 != null)
                {
                    s1.position = new Vector3(34.8f, -0.15f, 0.0f);
                    s1.rotation = Quaternion.Euler(0f, 0f, -11.5f);
                    s1.localScale = new Vector3(1.35f, 1.0f, 1.0f);
                }
                var s2 = z2.Find("SlideSlope_Section_2");
                if (s2 != null)
                {
                    s2.position = new Vector3(41.5f, -0.85f, 0.0f);
                    s2.rotation = Quaternion.Euler(0f, 0f, -7.5f);
                    s2.localScale = new Vector3(1.35f, 1.0f, 1.0f);
                }
                var gMeadow = z2.Find("Visual_Ground_RiceMeadow");
                if (gMeadow != null)
                {
                    gMeadow.position = new Vector3(58.5f, -2.05f, 0.0f);
                    gMeadow.localScale = new Vector3(27.0f, 1.4f, 6.0f);
                }
            }

            // =========================================================================
            // 5. EXTEND & LOWER SKYBOX (X = -20m to 140m)
            // =========================================================================
            var sky = GameObject.Find("Skybox_Unified_PlainsHills");
            if (sky != null)
            {
                sky.transform.position = new Vector3(60.0f, 4.0f, 60.0f);
                sky.transform.localScale = new Vector3(360.0f, 110.0f, 1.0f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // =========================================================================
            // 6. RENDER 1 PANORAMA + 2 IN-GAME CAMERA SCREENSHOTS
            // =========================================================================
            string outDir = "C:/Users/Thanh Tài/.gemini/antigravity/brain/91d03331-ffbc-4dc5-be7e-61bc484b62a5";
            
            bool originalFog = RenderSettings.fog;
            RenderSettings.fog = false;

            // 1. Panoramic Overview (3840 x 1200)
            int wPan = 3840, hPan = 1200;
            var rtPan = new RenderTexture(wPan, hPan, 24, RenderTextureFormat.ARGB32);
            var texPan = new Texture2D(wPan, hPan, TextureFormat.RGB24, false);
            var camGo = new GameObject("Fixer_Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.72f, 0.82f, 0.86f);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 500f;
            cam.targetTexture = rtPan;
            cam.fieldOfView = 34f;
            camGo.transform.position = new Vector3(58.0f, 13.0f, -58.0f);
            camGo.transform.rotation = Quaternion.Euler(11.0f, 0.0f, 0.0f);
            cam.Render();
            RenderTexture.active = rtPan;
            texPan.ReadPixels(new Rect(0, 0, wPan, hPan), 0, 0);
            texPan.Apply();
            File.WriteAllBytes(Path.Combine(outDir, "scene_full_level_panoramic_overview.png"), texPan.EncodeToPNG());

            // 2. Gameplay In-Game Cameras (1920 x 1080)
            int wGame = 1920, hGame = 1080;
            var rtGame = new RenderTexture(wGame, hGame, 24, RenderTextureFormat.ARGB32);
            var texGame = new Texture2D(wGame, hGame, TextureFormat.RGB24, false);
            cam.targetTexture = rtGame;
            camGo.transform.rotation = Quaternion.Euler(5.0f, 0.0f, 0.0f);

            // Village (X = 84m)
            camGo.transform.position = new Vector3(84.0f, 0.8f, -10.8f);
            cam.Render();
            RenderTexture.active = rtGame;
            texGame.ReadPixels(new Rect(0, 0, wGame, hGame), 0, 0);
            texGame.Apply();
            File.WriteAllBytes(Path.Combine(outDir, "expansion_zone3_village_crate_puzzle.png"), texGame.EncodeToPNG());

            // Dock (X = 101.5m)
            camGo.transform.position = new Vector3(101.5f, 0.8f, -10.8f);
            cam.Render();
            RenderTexture.active = rtGame;
            texGame.ReadPixels(new Rect(0, 0, wGame, hGame), 0, 0);
            texGame.Apply();
            File.WriteAllBytes(Path.Combine(outDir, "expansion_zone4_dock_1954_ship.png"), texGame.EncodeToPNG());

            RenderSettings.fog = originalFog;
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rtPan);
            Object.DestroyImmediate(texPan);
            Object.DestroyImmediate(rtGame);
            Object.DestroyImmediate(texGame);
            Object.DestroyImmediate(camGo);

            Debug.Log("[Scene1PanoramicFixer] Completed perfectly in 1 shot!");
        }
    }
}
