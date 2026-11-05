using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HoaVaMau.EditorScripts
{
    public static class FixFloatingTreesAndDeconflictVillageHouses
    {
        private const string SpecificUserAngleQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_Grounded_WestRock_And_Coast_QA.jpg";
        private const string CoastalQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_Coastal_Trees_Grounded_QA.jpg";
        private const string VillageClearanceQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_Village_Houses_Clearance_QA.jpg";
        private const string NorthMeadowQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_North_Meadow_Grounded_QA.jpg";

        [MenuItem("Tools/Hoa Do/Fix Floating Trees And Deconflict Village Houses")]
        public static void Execute()
        {
            Debug.Log("<color=cyan>=== [STEP 1/5] GROUND ALL ROCKS AND MOUNTAIN CRAGS ===</color>");
            GroundAllRocksAndCliffs();

            Debug.Log("<color=cyan>=== [STEP 2/5] GROUND ALL COASTAL PALMS AND BEACH TREES ===</color>");
            GroundAllCoastalTrees();

            Debug.Log("<color=cyan>=== [STEP 3/5] DECONFLICT VILLAGE HOUSES AND GROUND VILLAGE TREES ===</color>");
            DeconflictVillageHousesAndGroundTrees();

            Debug.Log("<color=cyan>=== [STEP 4/5] ENSURE HILL MESH COLLIDERS & GROUND ALL REMAINING TREES ===</color>");
            AddMissingMeshCollidersToHills();
            int mapGrounded = PreciseGroundingAllRemainingTrees();

            Debug.Log("<color=cyan>=== [STEP 5/5] CAPTURE QA IMAGES & SAVE SCENE ===</color>");
            CaptureQAImages();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=green>=== [HOA DO SUCCESS] All floating rocks & trees grounded! Processed: {mapGrounded} ===</color>");
        }

        public static void GroundAllRocksAndCliffs()
        {
            // Lower WestMtn_Rock_01 and EastMtn_Rock_01 so their base meshes sink deeply into the flanking hills
            var rockW = GameObject.Find("WestMtn_Rock_01");
            if (rockW != null)
            {
                rockW.transform.position = new Vector3(-74.0f, 3.0f, 8.0f);
                Debug.Log("Grounded WestMtn_Rock_01 to Y=3.0 (embedded in hill)");
            }

            var rockE = GameObject.Find("EastMtn_Rock_01");
            if (rockE != null)
            {
                rockE.transform.position = new Vector3(74.0f, 3.0f, 8.0f);
                Debug.Log("Grounded EastMtn_Rock_01 to Y=3.0 (embedded in hill)");
            }
        }

        public static void GroundAllCoastalTrees()
        {
            // Set coastal coconut trees so their base circular root mounds sink 0.55m into the ground/sand plane,
            // concealing the flat bottom edge below the terrain surface so the trunk emerges naturally.
            var coastalSpecs = new (string name, Vector3 pos)[]
            {
                ("Coconut_Beach_West_01", new Vector3(-62.0f, 0.05f, -8.0f)),
                ("Coconut_Beach_West_02", new Vector3(-44.0f, 0.05f, -6.5f)),
                ("Coconut_Beach_West_03", new Vector3(-28.0f, 0.05f, -5.5f)),
                ("CoastTree_Palm_West_01", new Vector3(-52.0f, 0.05f, -10.0f)),
                ("CoastTree_Palm_East_01", new Vector3(52.0f, 0.05f, -10.0f)),
                ("CoastTree_Palm_East_02", new Vector3(62.0f, 0.05f, -8.5f)),
                ("Coconut_Beach_East_01", new Vector3(28.0f, 0.05f, -5.5f)),
                ("Coconut_Beach_East_02", new Vector3(46.0f, 0.05f, -6.5f)),
                ("Coconut_Beach_East_03", new Vector3(64.0f, 0.05f, -8.0f))
            };

            foreach (var (name, pos) in coastalSpecs)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    go.transform.position = pos;
                    Debug.Log($"Firmly grounded coastal tree {name} to {pos}");
                }
            }
        }

        public static void DeconflictVillageHousesAndGroundTrees()
        {
            // 1. Move FruitTree_House07_Corner out of House_09_DiHaiGieng to open front garden
            var fruitCorner = GameObject.Find("FruitTree_House07_Corner");
            if (fruitCorner != null)
            {
                fruitCorner.transform.position = new Vector3(23.5f, 0.05f, -3.0f);
            }

            // 2. Move FruitTree_House02_Side away from House_08 to outer yard (outside rice paddy)
            var fruitSide02 = GameObject.Find("FruitTree_House02_Side");
            if (fruitSide02 != null)
            {
                fruitSide02.transform.position = new Vector3(-37.0f, 0.46f, 30.5f);
            }

            // 3. Move Banana_House07_Side away from House_05
            var bananaSide07 = GameObject.Find("Banana_House07_Side");
            if (bananaSide07 != null)
            {
                bananaSide07.transform.position = new Vector3(30.5f, 0.47f, 5.0f);
            }

            // 4. Ground BushTree_House06_Side in authentic garden corner away from camera view
            var bushSide06 = GameObject.Find("BushTree_House06_Side");
            if (bushSide06 != null)
            {
                bushSide06.transform.position = new Vector3(-36.0f, 0.25f, -1.5f);
                Debug.Log("Grounded BushTree_House06_Side to (-36.0, 0.25, -1.5)");
            }
        }

        public static void AddMissingMeshCollidersToHills()
        {
            string[] hillNames = new string[]
            {
                "Hill_West_RiceFlank", "Hill_West_DeepPeak_01", "Hill_West_DeepPeak_02", "Hill_West_MidRidge_01", "Hill_West_MidRidge_02",
                "Hill_East_RiceFlank", "Hill_East_DeepPeak_01", "Hill_East_DeepPeak_02", "Hill_East_MidRidge_01", "Hill_East_MidRidge_02"
            };

            foreach (var name in hillNames)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;

                var mc = go.GetComponent<MeshCollider>();
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    if (mc == null)
                    {
                        mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false;
                    }
                    else if (mc.sharedMesh == null)
                    {
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
            }
        }

        public static int PreciseGroundingAllRemainingTrees()
        {
            var allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            var treeRoots = new List<GameObject>();

            foreach (var sr in allSrs)
            {
                if (sr == null || sr.sprite == null) continue;
                var go = sr.gameObject;
                if (go.name.StartsWith("Card_Volumetric")) continue;

                string n = go.name.ToLower();
                bool isTree = n.Contains("tree") || n.Contains("bamboo") || n.Contains("coconut") ||
                              n.Contains("palm") || n.Contains("banana") || n.Contains("canopy") ||
                              n.Contains("banyan");

                if (!isTree && go.transform.parent != null)
                {
                    string pn = go.transform.parent.name.ToLower();
                    if (pn.Contains("vegetation") || pn.Contains("foliage") || pn.Contains("trees") || pn.Contains("orchard"))
                    {
                        if (!n.Contains("leaf") && !n.Contains("stem") && !n.Contains("branch")) isTree = true;
                    }
                }

                if (isTree) treeRoots.Add(go);
            }

            int count = 0;
            foreach (var tree in treeRoots)
            {
                if (tree == null) continue;
                var t = tree.transform;
                Vector3 pos = t.position;

                // Don't override our handcrafted coastal & village trees
                if (t.name.Contains("Beach") || t.name.Contains("CoastTree") ||
                    t.name.Contains("House06_Side") || t.name.Contains("House07_Corner") ||
                    t.name.Contains("House02_Side") || t.name.Contains("House07_Side"))
                {
                    continue;
                }

                Ray ray = new Ray(new Vector3(pos.x, 80.0f, pos.z), Vector3.down);
                RaycastHit[] hits = Physics.RaycastAll(ray, 100.0f);

                float groundY = -999f;
                bool hitGround = false;

                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    var hitSr = h.collider.GetComponentInParent<SpriteRenderer>();
                    if (hitSr != null) continue;

                    string cn = h.collider.name.ToLower();
                    if (cn.Contains("gate") || cn.Contains("roof") || cn.Contains("post") || cn.Contains("beam") ||
                        cn.Contains("bridge") || cn.Contains("arch") || cn.Contains("reel") || cn.Contains("camera") ||
                        cn.Contains("player") || cn.Contains("an") || cn.Contains("npc") || cn.Contains("trigger") ||
                        cn.Contains("cloth") || cn.Contains("basket") || cn.Contains("pier") || cn.Contains("deck") ||
                        cn.Contains("piling") || cn.Contains("water_surface") || cn.Contains("ocean_turquoise"))
                    {
                        continue;
                    }

                    if (pos.z >= 183f && pos.z <= 210f && Mathf.Abs(pos.x) < 35f)
                    {
                        if ((cn.Contains("flankwall") || cn.Contains("uppertower") || cn.Contains("canyon")) && h.point.y > 6.0f)
                            continue;
                    }

                    if (h.point.y > groundY)
                    {
                        groundY = h.point.y;
                        hitGround = true;
                    }
                }

                if (!hitGround)
                {
                    if (pos.z >= 182.5f && pos.z <= 210f)
                    {
                        float factor = Mathf.Clamp01((pos.z - 182.5f) / 22.5f);
                        groundY = Mathf.Lerp(0.50f, 4.0f, factor);
                    }
                    else if (pos.z < -5.0f)
                    {
                        groundY = Mathf.Lerp(0.50f, -0.20f, Mathf.Clamp01((-pos.z - 5f) / 15f));
                    }
                    else
                    {
                        groundY = 0.50f;
                    }
                }

                var sr = t.GetComponent<SpriteRenderer>();
                float spriteBottomLocal = (sr != null && sr.sprite != null) ? sr.sprite.bounds.min.y : -0.5f;

                float targetRootY = groundY - 0.25f;
                float targetPosY = targetRootY - (spriteBottomLocal * t.localScale.y);

                if (Mathf.Abs(t.position.y - targetPosY) > 0.05f)
                {
                    t.position = new Vector3(pos.x, targetPosY, pos.z);
                    count++;
                }

                if (sr != null)
                {
                    sr.shadowCastingMode = ShadowCastingMode.Off;
                    sr.receiveShadows = false;
                }
            }

            return count;
        }

        public static void CaptureQAImages()
        {
            // 1. Exact view matching user uploaded images (media_1793853475736_71204c6c & media_1793851489216_53c625a9)
            RenderView(
                new Vector3(-36.0f, 1.3f, 4.0f),
                Quaternion.Euler(2.0f, 250.0f, 0.0f),
                50.0f,
                SpecificUserAngleQAImagePath
            );

            // 2. Coastal Beach View
            RenderView(
                new Vector3(-5.0f, 1.2f, 12.0f),
                Quaternion.Euler(5.0f, 185.0f, 0.0f),
                48.0f,
                CoastalQAImagePath
            );

            // 3. Village Houses Clearance View
            RenderView(
                new Vector3(0.0f, 25.0f, -8.0f),
                Quaternion.Euler(45.0f, 0.0f, 0.0f),
                60.0f,
                VillageClearanceQAImagePath
            );

            // 4. North Meadow Grounded View
            RenderView(
                new Vector3(-6.0f, 12.0f, 170.0f),
                Quaternion.Euler(15.0f, 0.0f, 0.0f),
                60.0f,
                NorthMeadowQAImagePath
            );
        }

        private static void RenderView(Vector3 pos, Quaternion rot, float fov, string outputPath)
        {
            var tempGo = new GameObject("Temp_QA_Camera");
            var cam = tempGo.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.rotation = rot;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;

            var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.Default);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToJPG(80);
            File.WriteAllBytes(outputPath, bytes);

            RenderTexture.active = null;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(tempGo);

            Debug.Log($"Captured QA View: {outputPath}");
        }
    }
}
