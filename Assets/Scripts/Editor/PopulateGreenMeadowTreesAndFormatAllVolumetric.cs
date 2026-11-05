using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HoaVaMau.EditorScripts
{
    public static class PopulateGreenMeadowTreesAndFormatAllVolumetric
    {
        private static readonly float[] VolumetricAngles = new float[]
        {
            22.5f, 45.0f, 67.5f, 90.0f, 112.5f, 135.0f, 157.5f
        };

        private static readonly Color[] VolumetricShadings = new Color[]
        {
            new Color(0.97f, 0.97f, 0.97f, 1f),
            new Color(0.93f, 0.94f, 0.92f, 1f),
            new Color(0.90f, 0.91f, 0.89f, 1f),
            new Color(0.87f, 0.88f, 0.86f, 1f),
            new Color(0.91f, 0.92f, 0.90f, 1f),
            new Color(0.94f, 0.95f, 0.93f, 1f),
            new Color(0.98f, 0.98f, 0.97f, 1f)
        };

        [MenuItem("Tools/Hoa Do/Populate Meadow Trees And Format All Volumetric")]
        public static void Execute()
        {
            Debug.Log("<color=cyan>=== [STEP 1/4] GLOBAL MAP TREE VOLUMETRIC CONVERSION & OPTIMIZATION ===</color>");
            int convertedCount = ConvertAllExistingTreesToVolumetricAndOptimize();

            Debug.Log("<color=cyan>=== [STEP 2/4] POPULATE NORTH GREEN MEADOW TREES (8-DIRECTIONAL VOLUMETRIC) ===</color>");
            int newTreesCount = PopulateNorthMeadowTrees();

            Debug.Log("<color=cyan>=== [STEP 3/4] OPTIMIZE MATERIALS & SHADOWS FOR PERFORMANCE ===</color>");
            OptimizeAllFoliageMaterialsAndShadows();

            Debug.Log("<color=cyan>=== [STEP 4/4] CAPTURE QA IMAGES & SAVE SCENE ===</color>");
            CaptureQAImages();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=green>=== [HOA DO SUCCESS] Converted {convertedCount} existing trees, added {newTreesCount} authentic volumetric trees, optimized performance! ===</color>");
        }

        private static int ConvertAllExistingTreesToVolumetricAndOptimize()
        {
            var allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            var treeRoots = new List<SpriteRenderer>();

            foreach (var sr in allSrs)
            {
                if (sr == null || sr.sprite == null) continue;
                var go = sr.gameObject;
                if (go.name.StartsWith("Card_Volumetric")) continue;

                string n = go.name.ToLower();
                bool isTreeLike = n.Contains("tree") || n.Contains("bamboo") || n.Contains("coconut") ||
                                  n.Contains("palm") || n.Contains("banana") || n.Contains("canopy") ||
                                  n.Contains("banyan");

                // Also check parent
                if (!isTreeLike && go.transform.parent != null)
                {
                    string pn = go.transform.parent.name.ToLower();
                    if (pn.Contains("vegetation") || pn.Contains("foliage") || pn.Contains("trees") || pn.Contains("orchard"))
                    {
                        if (n.Contains("leaf") || n.Contains("stem") || n.Contains("branch")) continue;
                        isTreeLike = true;
                    }
                }

                if (isTreeLike)
                {
                    treeRoots.Add(sr);
                }
            }

            int upgradedCount = 0;
            foreach (var rootSr in treeRoots)
            {
                var t = rootSr.transform;
                int existingCards = 0;
                for (int i = 0; i < t.childCount; i++)
                {
                    if (t.GetChild(i).name.StartsWith("Card_Volumetric")) existingCards++;
                }

                if (existingCards < 7)
                {
                    FormatTransformTo8CardVolumetric(t, rootSr.sprite, rootSr.sharedMaterial);
                    upgradedCount++;
                }
                else
                {
                    // Ensure shadow settings on root and child cards
                    rootSr.shadowCastingMode = ShadowCastingMode.Off;
                    rootSr.receiveShadows = false;
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
            }

            Debug.Log($"Upgraded {upgradedCount} flat trees into 8-card volumetric format. Total tree roots evaluated: {treeRoots.Count}");
            return upgradedCount;
        }

        private static int PopulateNorthMeadowTrees()
        {
            var env = GameObject.Find("CaMau_1954_Village/Environment");
            if (env == null)
            {
                var village = GameObject.Find("CaMau_1954_Village");
                if (village != null)
                {
                    env = new GameObject("Environment");
                    env.transform.SetParent(village.transform, false);
                }
            }

            var meadowRoot = GameObject.Find("CaMau_1954_Village/Environment/North_Meadow_Foliage_Cluster");
            if (meadowRoot != null)
            {
                Undo.DestroyObjectImmediate(meadowRoot);
            }

            meadowRoot = new GameObject("North_Meadow_Foliage_Cluster");
            if (env != null) meadowRoot.transform.SetParent(env.transform, false);

            var westParent = new GameObject("West_Meadow_Foliage");
            westParent.transform.SetParent(meadowRoot.transform, false);

            var eastParent = new GameObject("East_Meadow_Foliage");
            eastParent.transform.SetParent(meadowRoot.transform, false);

            // Load authentic sprites
            string[] paths = new string[]
            {
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_01_LargeCanopy.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_02_Medium.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_03_SmallBushTree.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_04_SlenderTall.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_05_Banana.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png"
            };

            var sprites = new Sprite[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                if (sprites[i] == null)
                {
                    Debug.LogWarning($"Could not load tree sprite at {paths[i]}");
                }
            }

            // Material
            var defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Sprite.mat");
            if (defaultMat == null)
            {
                var unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (unlitShader != null) defaultMat = new Material(unlitShader);
            }

            int count = 0;

            // -----------------------------------------------------------------------------------------
            // WEST MEADOW (Orange Curve Area & Green Fields)
            // Strictly Z >= 183.0m, X <= -11.0m (Completely outside rice paddy Z <= 181.56m and road X ~= -7m)
            // -----------------------------------------------------------------------------------------
            var westTreeSpecs = new List<(int spriteIdx, Vector3 pos, float scale, string name)>
            {
                // Along the upper cliff base / orange curve (Z: 198 - 208, X: -34 to -14)
                (6, new Vector3(-33.0f, 0, 204.0f), 1.35f, "West_Bamboo_RockBase_01"), // Bamboo
                (6, new Vector3(-29.5f, 0, 206.5f), 1.40f, "West_Bamboo_RockBase_02"),
                (3, new Vector3(-26.0f, 0, 207.0f), 1.45f, "West_SlenderTall_RockBase_01"),
                (5, new Vector3(-22.0f, 0, 206.0f), 1.30f, "West_Coconut_RockBase_01"), // Coconut
                (6, new Vector3(-18.5f, 0, 205.5f), 1.35f, "West_Bamboo_RockBase_03"),
                (0, new Vector3(-15.0f, 0, 204.5f), 1.25f, "West_LargeCanopy_RockBase_01"), // Large Canopy
                (3, new Vector3(-13.0f, 0, 201.0f), 1.20f, "West_SlenderTall_NearGate_01"),

                // Mid-tier green slope (Z: 191 - 197, X: -32 to -12)
                (0, new Vector3(-31.5f, 0, 196.0f), 1.30f, "West_LargeCanopy_Mid_01"),
                (1, new Vector3(-27.0f, 0, 195.0f), 1.25f, "West_Medium_Mid_01"),
                (5, new Vector3(-23.5f, 0, 197.0f), 1.35f, "West_Coconut_Mid_01"),
                (4, new Vector3(-20.5f, 0, 194.5f), 1.15f, "West_Banana_Clump_01"), // Banana
                (1, new Vector3(-17.0f, 0, 195.5f), 1.20f, "West_Medium_Mid_02"),
                (5, new Vector3(-13.5f, 0, 196.0f), 1.30f, "West_Coconut_GateFlank_01"),

                // Lower tier near rice border (Z: 184 - 190, X: -33 to -12)
                (5, new Vector3(-32.0f, 0, 187.0f), 1.25f, "West_Coconut_Lower_01"),
                (6, new Vector3(-28.0f, 0, 188.5f), 1.30f, "West_Bamboo_Lower_01"),
                (0, new Vector3(-24.0f, 0, 186.0f), 1.30f, "West_LargeCanopy_Lower_01"),
                (4, new Vector3(-19.5f, 0, 188.0f), 1.15f, "West_Banana_Lower_01"),
                (1, new Vector3(-16.0f, 0, 186.5f), 1.25f, "West_Medium_Lower_01"),
                (5, new Vector3(-12.5f, 0, 189.0f), 1.35f, "West_Coconut_LowerFlank_01"),
                (4, new Vector3(-11.5f, 0, 185.0f), 1.10f, "West_Banana_LowerFlank_02"),

                // Outer edge filler (Z: 185 - 205, X: -36 to -34)
                (3, new Vector3(-36.0f, 0, 192.0f), 1.40f, "West_SlenderTall_Outer_01"),
                (6, new Vector3(-35.0f, 0, 199.0f), 1.35f, "West_Bamboo_Outer_01"),
                (1, new Vector3(-34.5f, 0, 184.5f), 1.20f, "West_Medium_Outer_01")
            };

            foreach (var spec in westTreeSpecs)
            {
                if (spec.pos.z < 182.5f) continue; // STRICT RULE 5 GUARDRAIL
                var sp = sprites[Mathf.Clamp(spec.spriteIdx, 0, sprites.Length - 1)];
                SpawnVolumetricTree(westParent.transform, sp, defaultMat, spec.pos, spec.scale, spec.name);
                count++;
            }

            // -----------------------------------------------------------------------------------------
            // EAST MEADOW (East Green Field balancing the scene)
            // Strictly Z >= 183.0m, X >= -2.0m (Completely outside rice paddy Z <= 181.56m and road X ~= -7m)
            // -----------------------------------------------------------------------------------------
            var eastTreeSpecs = new List<(int spriteIdx, Vector3 pos, float scale, string name)>
            {
                // Along the east rock base / cliff ridge (Z: 198 - 208, X: 4 to 26)
                (3, new Vector3(-1.0f, 0, 202.0f), 1.25f, "East_SlenderTall_NearGate_01"),
                (0, new Vector3(3.0f, 0, 204.5f), 1.30f, "East_LargeCanopy_RockBase_01"),
                (6, new Vector3(7.5f, 0, 206.0f), 1.35f, "East_Bamboo_RockBase_01"),
                (5, new Vector3(12.0f, 0, 207.0f), 1.40f, "East_Coconut_RockBase_01"),
                (6, new Vector3(16.5f, 0, 206.5f), 1.40f, "East_Bamboo_RockBase_02"),
                (0, new Vector3(21.0f, 0, 204.5f), 1.35f, "East_LargeCanopy_RockBase_02"),
                (3, new Vector3(25.0f, 0, 203.0f), 1.45f, "East_SlenderTall_RockBase_02"),

                // Mid-tier green slope (Z: 191 - 197, X: -2 to 24)
                (5, new Vector3(-0.5f, 0, 196.0f), 1.30f, "East_Coconut_GateFlank_01"),
                (1, new Vector3(3.5f, 0, 195.0f), 1.25f, "East_Medium_Mid_01"),
                (4, new Vector3(7.0f, 0, 194.5f), 1.15f, "East_Banana_Clump_01"),
                (5, new Vector3(11.0f, 0, 196.5f), 1.35f, "East_Coconut_Mid_01"),
                (0, new Vector3(15.5f, 0, 195.5f), 1.30f, "East_LargeCanopy_Mid_01"),
                (1, new Vector3(19.5f, 0, 194.5f), 1.25f, "East_Medium_Mid_02"),
                (6, new Vector3(23.5f, 0, 196.0f), 1.35f, "East_Bamboo_Mid_01"),

                // Lower tier near rice border (Z: 184 - 190, X: -2 to 24)
                (4, new Vector3(-1.8f, 0, 186.0f), 1.10f, "East_Banana_LowerFlank_01"),
                (5, new Vector3(1.0f, 0, 188.5f), 1.30f, "East_Coconut_LowerFlank_01"),
                (1, new Vector3(5.5f, 0, 186.5f), 1.25f, "East_Medium_Lower_01"),
                (4, new Vector3(9.5f, 0, 188.0f), 1.15f, "East_Banana_Lower_01"),
                (0, new Vector3(13.5f, 0, 186.0f), 1.30f, "East_LargeCanopy_Lower_01"),
                (6, new Vector3(18.0f, 0, 188.5f), 1.35f, "East_Bamboo_Lower_01"),
                (5, new Vector3(22.0f, 0, 187.0f), 1.30f, "East_Coconut_Lower_01"),

                // Outer edge filler (Z: 185 - 205, X: 26 to 28)
                (3, new Vector3(27.0f, 0, 192.0f), 1.40f, "East_SlenderTall_Outer_01"),
                (6, new Vector3(26.5f, 0, 199.0f), 1.35f, "East_Bamboo_Outer_01"),
                (1, new Vector3(26.0f, 0, 184.5f), 1.20f, "East_Medium_Outer_01")
            };

            foreach (var spec in eastTreeSpecs)
            {
                if (spec.pos.z < 182.5f) continue; // STRICT RULE 5 GUARDRAIL
                var sp = sprites[Mathf.Clamp(spec.spriteIdx, 0, sprites.Length - 1)];
                SpawnVolumetricTree(eastParent.transform, sp, defaultMat, spec.pos, spec.scale, spec.name);
                count++;
            }

            Debug.Log($"Successfully spawned {count} authentic 8-directional volumetric trees in North Meadow!");
            return count;
        }

        private static GameObject SpawnVolumetricTree(Transform parent, Sprite sprite, Material mat, Vector3 groundPlanarPos, float uniformScale, string name)
        {
            if (sprite == null) return null;

            var treeGo = new GameObject(name);
            treeGo.transform.SetParent(parent, false);

            float groundY = GetTerrainSurfaceY(groundPlanarPos.x, groundPlanarPos.z);
            float targetLowestRootWorldY = groundY - 0.25f;
            float targetPosY = targetLowestRootWorldY - (sprite.bounds.min.y * uniformScale);

            treeGo.transform.position = new Vector3(groundPlanarPos.x, targetPosY, groundPlanarPos.z);
            treeGo.transform.localScale = Vector3.one * uniformScale;

            FormatTransformTo8CardVolumetric(treeGo.transform, sprite, mat);
            return treeGo;
        }

        private static void FormatTransformTo8CardVolumetric(Transform t, Sprite sprite, Material mat)
        {
            if (sprite == null) return;

            // Clean any existing card objects
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                if (child.name.StartsWith("Card_Volumetric") || child.name.Contains("Card") ||
                    child.name.Contains("Dome") || child.name.Contains("Fork") || child.name.Contains("SolidCore"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            // Root Card (0 degrees)
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat != null) sr.sharedMaterial = mat;
            sr.color = Color.white;
            sr.sortingOrder = 0;
            sr.shadowCastingMode = ShadowCastingMode.Off;
            sr.receiveShadows = false;

            // 7 Radial Child Cards (22.5, 45, 67.5, 90, 112.5, 135, 157.5 degrees)
            for (int k = 0; k < VolumetricAngles.Length; k++)
            {
                var cardObj = new GameObject($"Card_Volumetric_{VolumetricAngles[k]:F1}Deg");
                cardObj.transform.SetParent(t, false);
                cardObj.transform.localPosition = Vector3.zero;
                cardObj.transform.localRotation = Quaternion.Euler(0f, VolumetricAngles[k], 0f);
                cardObj.transform.localScale = Vector3.one;

                var cardSr = cardObj.AddComponent<SpriteRenderer>();
                cardSr.sprite = sprite;
                if (mat != null) cardSr.sharedMaterial = mat;
                cardSr.color = VolumetricShadings[k];
                cardSr.sortingOrder = 0;
                cardSr.shadowCastingMode = ShadowCastingMode.Off;
                cardSr.receiveShadows = false;
            }
        }

        private static float GetTerrainSurfaceY(float x, float z)
        {
            // Raycast down from Y=60m
            Ray ray = new Ray(new Vector3(x, 60.0f, z), Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100.0f);

            float highestTerrainY = 0.50f;
            bool foundGround = false;

            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                string cName = h.collider.gameObject.name.ToLower();
                // Avoid hitting gate or wooden structures
                if (cName.Contains("gate") || cName.Contains("roof") || cName.Contains("post") || cName.Contains("beam"))
                    continue;

                if (cName.Contains("terrain") || cName.Contains("rock") || cName.Contains("ground") ||
                    cName.Contains("soil") || cName.Contains("road") || cName.Contains("cliff"))
                {
                    if (!foundGround || h.point.y > highestTerrainY)
                    {
                        highestTerrainY = h.point.y;
                        foundGround = true;
                    }
                }
            }

            if (!foundGround)
            {
                // Fallback default smooth slope approximation between z=182.5 and z=205
                float t = Mathf.Clamp01((z - 182.5f) / 22.5f);
                highestTerrainY = Mathf.Lerp(0.50f, 4.0f, t);
            }

            return highestTerrainY;
        }

        private static void OptimizeAllFoliageMaterialsAndShadows()
        {
            // Enable GPU Instancing on all sprite materials and turn off shadow casting on all 2D sprite renderers
            var allSrs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            var matsSet = new HashSet<Material>();

            int shadowDisabledCount = 0;
            foreach (var sr in allSrs)
            {
                if (sr == null) continue;
                if (sr.shadowCastingMode != ShadowCastingMode.Off)
                {
                    sr.shadowCastingMode = ShadowCastingMode.Off;
                    shadowDisabledCount++;
                }
                sr.receiveShadows = false;

                if (sr.sharedMaterial != null)
                {
                    matsSet.Add(sr.sharedMaterial);
                }
            }

            int instancedMats = 0;
            foreach (var mat in matsSet)
            {
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    instancedMats++;
                }
            }

            Debug.Log($"Optimization complete: Disabled shadows on {shadowDisabledCount} SpriteRenderers. Enabled GPU Instancing on {instancedMats} materials. Total SpriteRenderers in scene: {allSrs.Length}");
        }

        private static void CaptureQAImages()
        {
            string artifactDir = @"C:\Users\ACER\.gemini\antigravity\brain\0b06ba00-d73d-4df0-adee-d4635db3297d";
            Directory.CreateDirectory(artifactDir);

            // 1. Top-Down Scene Perspective matching media_1793849995399_183e0ed0.jpg
            RenderCameraShot(
                new Vector3(-6.85f, 32.0f, 175.0f),
                Quaternion.Euler(55.0f, 0.0f, 0.0f),
                65.0f,
                Path.Combine(artifactDir, "North_Meadow_Trees_TopDown_QA.jpg")
            );

            // 2. Eye-level shot in front of North Gate looking at the lush meadow
            RenderCameraShot(
                new Vector3(-6.85f, 2.0f, 186.0f),
                Quaternion.Euler(5.0f, 0.0f, 0.0f),
                60.0f,
                Path.Combine(artifactDir, "North_Meadow_Trees_EyeLevel_QA.jpg")
            );

            // 3. Vista Shot from Shot 5 (Mountain Cliff overlooking rice fields and north meadow)
            RenderCameraShot(
                new Vector3(-13.0f, 16.5f, 194.0f),
                Quaternion.Euler(28.0f, 145.0f, 0.0f),
                60.0f,
                Path.Combine(artifactDir, "North_Meadow_Trees_Vista_QA.jpg")
            );
        }

        private static void RenderCameraShot(Vector3 pos, Quaternion rot, float fov, string outputPath)
        {
            var camGo = new GameObject("TempQACam");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.rotation = rot;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;

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
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);

            Debug.Log($"Saved QA capture to: {outputPath}");
        }
    }
}
