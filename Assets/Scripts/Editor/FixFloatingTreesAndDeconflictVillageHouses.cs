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
        private const string CoastalQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_Coastal_Trees_Grounded_QA.jpg";
        private const string VillageClearanceQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_Village_Houses_Clearance_QA.jpg";
        private const string NorthMeadowQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_North_Meadow_Grounded_QA.jpg";
        private const string FlankHillsQAImagePath = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d/CaMau_Flank_Hills_Grounded_QA.jpg";

        [MenuItem("Tools/Hoa Do/Fix Floating Trees And Deconflict Village Houses")]
        public static void Execute()
        {
            Debug.Log("<color=cyan>=== [STEP 1/4] ENSURE HILL MESH COLLIDERS ===</color>");
            AddMissingMeshCollidersToHills();

            Debug.Log("<color=cyan>=== [STEP 2/4] DECONFLICT VILLAGE TREES FROM HOUSES ===</color>");
            DeconflictVillageHouses();

            Debug.Log("<color=cyan>=== [STEP 3/4] ACCURATELY GROUND ALL TREES ===</color>");
            int groundedCount = GroundAllTreesPrecisely();

            Debug.Log("<color=cyan>=== [STEP 4/4] CAPTURE QA IMAGES & SAVE SCENE ===</color>");
            CaptureQAImages();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=green>=== [HOA DO SUCCESS] Trees accurately grounded: {groundedCount} ===</color>");
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

        public static void DeconflictVillageHouses()
        {
            var houseNetwork = GameObject.Find("CaMau_1954_Village/Environment/Village_Houses_Network");
            if (houseNetwork == null) return;

            var fruitCorner = GameObject.Find("FruitTree_House07_Corner");
            if (fruitCorner != null) fruitCorner.transform.position = new Vector3(23.5f, 0.51f, -3.0f);

            var fruitSide02 = GameObject.Find("FruitTree_House02_Side");
            if (fruitSide02 != null) fruitSide02.transform.position = new Vector3(-37.0f, 0.53f, 30.5f);

            var bushSide06 = GameObject.Find("BushTree_House06_Side");
            if (bushSide06 != null) bushSide06.transform.position = new Vector3(-39.5f, 0.51f, 3.0f);

            var bananaSide07 = GameObject.Find("Banana_House07_Side");
            if (bananaSide07 != null) bananaSide07.transform.position = new Vector3(30.5f, 0.53f, 5.0f);
        }

        public static int GroundAllTreesPrecisely()
        {
            var allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            var treeRoots = new List<GameObject>();

            foreach (var sr in allSrs)
            {
                if (sr == null || sr.sprite == null) continue;
                var go = sr.gameObject;
                if (go.name.StartsWith("Card_Volumetric")) continue;

                string n = go.name.ToLower();
                bool isTreeLike = n.Contains("tree") || n.Contains("bamboo") || n.Contains("coconut") ||
                                  n.Contains("palm") || n.Contains("banana") || n.Contains("canopy") ||
                                  n.Contains("banyan");

                if (!isTreeLike && go.transform.parent != null)
                {
                    string pn = go.transform.parent.name.ToLower();
                    if (pn.Contains("vegetation") || pn.Contains("foliage") || pn.Contains("trees") || pn.Contains("orchard"))
                    {
                        if (!n.Contains("leaf") && !n.Contains("stem") && !n.Contains("branch")) isTreeLike = true;
                    }
                }

                if (isTreeLike) treeRoots.Add(go);
            }

            int adjusted = 0;

            foreach (var tree in treeRoots)
            {
                if (tree == null) continue;
                var t = tree.transform;
                Vector3 pos = t.position;

                // Coastal Palm Shoreline Snap
                if (pos.z < -11.0f)
                {
                    pos.z = -8.5f;
                    t.position = pos;
                }

                // Raycast down from Y=80
                Ray ray = new Ray(new Vector3(pos.x, 80.0f, pos.z), Vector3.down);
                RaycastHit[] hits = Physics.RaycastAll(ray, 100.0f);

                float groundY = -999f;
                bool found = false;

                foreach (var h in hits)
                {
                    // Ignore any hit that belongs to a tree or volumetric card
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

                    // For meadow trees at north gate (Z between 183 and 210, |X| < 35):
                    // Avoid hitting high vertical cliff walls like WestCliff_FlankWall (> 7m)
                    if (pos.z >= 183f && pos.z <= 210f && Mathf.Abs(pos.x) < 35f)
                    {
                        if (cn.Contains("flankwall") || cn.Contains("uppertower") || cn.Contains("canyon"))
                        {
                            // If hit is very high (> 6m), it's the vertical canyon cliff above the meadow ground
                            if (h.point.y > 6.0f) continue;
                        }
                    }

                    if (h.point.y > groundY)
                    {
                        groundY = h.point.y;
                        found = true;
                    }
                }

                if (!found)
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
                    adjusted++;
                }

                // Ensure shadows disabled on root and cards
                if (sr != null)
                {
                    sr.shadowCastingMode = ShadowCastingMode.Off;
                    sr.receiveShadows = false;
                }
                for (int i = 0; i < t.childCount; i++)
                {
                    var c = t.GetChild(i);
                    if (c.name.StartsWith("Card_Volumetric"))
                    {
                        var csr = c.GetComponent<SpriteRenderer>();
                        if (csr != null)
                        {
                            csr.shadowCastingMode = ShadowCastingMode.Off;
                            csr.receiveShadows = false;
                        }
                    }
                }
            }

            return adjusted;
        }

        public static void CaptureQAImages()
        {
            // 1. Coastal Trees Grounded View (matching image 10)
            RenderView(
                new Vector3(-5.0f, 1.2f, 12.0f),
                Quaternion.Euler(5.0f, 185.0f, 0.0f),
                48.0f,
                CoastalQAImagePath
            );

            // 2. Village Houses Clearance View
            RenderView(
                new Vector3(0.0f, 25.0f, -8.0f),
                Quaternion.Euler(45.0f, 0.0f, 0.0f),
                60.0f,
                VillageClearanceQAImagePath
            );

            // 3. North Meadow Grounded View
            RenderView(
                new Vector3(-6.0f, 12.0f, 170.0f),
                Quaternion.Euler(15.0f, 0.0f, 0.0f),
                60.0f,
                NorthMeadowQAImagePath
            );

            // 4. Flank Hills View
            RenderView(
                new Vector3(-6.0f, 18.0f, 90.0f),
                Quaternion.Euler(20.0f, 30.0f, 0.0f),
                55.0f,
                FlankHillsQAImagePath
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
        }
    }
}
