using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Generates the 4 core Cartoon Ghibli / Planet of Lana style 3D Modular Kit pieces
    /// (Meshes, Materials, and Prefabs) with Top-Center (Y=0) pivots and zero visual colliders.
    /// </summary>
    public static class GhibliModularKitGenerator
    {
        private const string ShaderName = "HoaVaMau/GhibliCartoonModular";
        private const string MatFolder = "Assets/Materials/Environment/GhibliModular";
        private const string MeshFolder = "Assets/Models/Environment/GhibliModular";
        private const string PrefabFolder = "Assets/Prefabs/Environment/ModularKit";

        [InitializeOnLoadMethod]
        private static void AutoGenerateOnFirstLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(PrefabFolder + "/PF_Mod_MudBank_Chunk.prefab"))
                {
                    GenerateAllAndSpawnShowcase();
                }
            };
        }

        [MenuItem("Hoa va Mau/Ghibli Modular Kit/1. Generate Ghibli Modular Kit & Preview Showcase")]
        public static void GenerateAllAndSpawnShowcase()
        {
            GenerateAssetsOnly();
            SpawnShowcaseInActiveScene();
        }

        [MenuItem("Hoa va Mau/Ghibli Modular Kit/2. Generate Modular Kit Assets Only")]
        public static void GenerateAssetsOnly()
        {
            EnsureFolders();

            Shader ghibliShader = Shader.Find(ShaderName);
            if (ghibliShader == null)
            {
                ghibliShader = Shader.Find("Universal Render Pipeline/Lit");
                Debug.LogWarning("[GhibliModularKit] Could not find " + ShaderName + ", falling back to URP/Lit.");
            }

            // 1. Create Ghibli Cartoon Materials
            Material mudBankMat = CreateOrUpdateMaterial(
                MatFolder + "/M_Ghibli_MudBank.mat",
                ghibliShader,
                baseCol: new Color(0.56f, 0.36f, 0.22f, 1f),      // Warm terracotta silt
                midCol: new Color(0.43f, 0.26f, 0.17f, 1f),       // Rich clay midtone
                shadowCol: new Color(0.25f, 0.16f, 0.21f, 1f),    // Ghibli warm plum-umber shadow
                useTop: true,
                topCol: new Color(0.38f, 0.66f, 0.25f, 1f),       // Sunlit Ghibli meadow green
                topShadowCol: new Color(0.16f, 0.38f, 0.24f, 1f), // Deep teal-green grass shadow
                topThreshold: 0.58f,
                useWet: true,
                wetCol: new Color(0.16f, 0.19f, 0.15f, 1f),
                rimStrength: 0.18f
            );

            Material grassCapMat = CreateOrUpdateMaterial(
                MatFolder + "/M_Ghibli_GrassCap.mat",
                ghibliShader,
                baseCol: new Color(0.42f, 0.70f, 0.26f, 1f),      // Lush Ghibli sunlit grass
                midCol: new Color(0.28f, 0.54f, 0.22f, 1f),       // Rich emerald midtone
                shadowCol: new Color(0.14f, 0.33f, 0.23f, 1f),    // Cool teal-forest shadow
                useTop: true,
                topCol: new Color(0.52f, 0.78f, 0.30f, 1f),       // Golden-green top highlight
                topShadowCol: new Color(0.22f, 0.46f, 0.24f, 1f),
                topThreshold: 0.45f,
                useWet: false,
                wetCol: new Color(0.14f, 0.28f, 0.18f, 1f),
                rimStrength: 0.26f
            );

            Material rootMat = CreateOrUpdateMaterial(
                MatFolder + "/M_Ghibli_MangroveRoot.mat",
                ghibliShader,
                baseCol: new Color(0.46f, 0.33f, 0.23f, 1f),      // Warm mangrove bark
                midCol: new Color(0.34f, 0.23f, 0.17f, 1f),
                shadowCol: new Color(0.20f, 0.14f, 0.18f, 1f),
                useTop: true,
                topCol: new Color(0.33f, 0.52f, 0.23f, 1f),       // Mossy bark top
                topShadowCol: new Color(0.16f, 0.30f, 0.19f, 1f),
                topThreshold: 0.65f,
                useWet: true,
                wetCol: new Color(0.13f, 0.17f, 0.14f, 1f),
                rimStrength: 0.22f
            );

            Material pierPlankMat = CreateOrUpdateMaterial(
                MatFolder + "/M_Ghibli_PierPlank.mat",
                ghibliShader,
                baseCol: new Color(0.64f, 0.45f, 0.27f, 1f),      // Warm hand-hewn honey cedar
                midCol: new Color(0.49f, 0.33f, 0.20f, 1f),
                shadowCol: new Color(0.28f, 0.19f, 0.21f, 1f),
                useTop: true,
                topCol: new Color(0.72f, 0.53f, 0.33f, 1f),       // Sun-bleached wood top
                topShadowCol: new Color(0.42f, 0.28f, 0.18f, 1f),
                topThreshold: 0.60f,
                useWet: false,
                wetCol: new Color(0.18f, 0.16f, 0.14f, 1f),
                rimStrength: 0.25f
            );

            Material pierStiltMat = CreateOrUpdateMaterial(
                MatFolder + "/M_Ghibli_PierStilt.mat",
                ghibliShader,
                baseCol: new Color(0.48f, 0.34f, 0.22f, 1f),      // Weathered timber piling
                midCol: new Color(0.36f, 0.24f, 0.16f, 1f),
                shadowCol: new Color(0.21f, 0.15f, 0.18f, 1f),
                useTop: true,
                topCol: new Color(0.58f, 0.43f, 0.28f, 1f),
                topShadowCol: new Color(0.32f, 0.22f, 0.15f, 1f),
                topThreshold: 0.70f,
                useWet: true,
                wetCol: new Color(0.12f, 0.18f, 0.14f, 1f),       // Dark mossy waterline piling
                rimStrength: 0.20f
            );

            Material ropeMat = CreateOrUpdateMaterial(
                MatFolder + "/M_Ghibli_Rope.mat",
                ghibliShader,
                baseCol: new Color(0.76f, 0.62f, 0.38f, 1f),      // Warm hemp rope
                midCol: new Color(0.58f, 0.45f, 0.26f, 1f),
                shadowCol: new Color(0.35f, 0.25f, 0.18f, 1f),
                useTop: false,
                topCol: Color.white,
                topShadowCol: Color.gray,
                topThreshold: 0.8f,
                useWet: false,
                wetCol: Color.black,
                rimStrength: 0.28f
            );

            Material pebbleMat = CreateOrUpdateMaterial(
                MatFolder + "/M_Ghibli_PebbleRock.mat",
                ghibliShader,
                baseCol: new Color(0.52f, 0.50f, 0.46f, 1f),      // Warm river stone
                midCol: new Color(0.38f, 0.37f, 0.35f, 1f),
                shadowCol: new Color(0.22f, 0.22f, 0.26f, 1f),    // Cool slate shadow
                useTop: true,
                topCol: new Color(0.38f, 0.62f, 0.26f, 1f),       // Mossy stone cap
                topShadowCol: new Color(0.18f, 0.36f, 0.22f, 1f),
                topThreshold: 0.55f,
                useWet: true,
                wetCol: new Color(0.15f, 0.18f, 0.16f, 1f),
                rimStrength: 0.20f
            );

            // 2. Generate Procedural Ghibli Meshes and Save as .asset
            Mesh mudBodyMesh = SaveMeshAsset(BuildGhibliMudBankBodyMesh(), MeshFolder + "/SM_Ghibli_MudBank_Body.asset");
            Mesh grassLipMesh = SaveMeshAsset(BuildGhibliGrassLipMesh(), MeshFolder + "/SM_Ghibli_MudBank_GrassLip.asset");
            Mesh pebbleMesh = SaveMeshAsset(BuildGhibliPebbleMesh(), MeshFolder + "/SM_Ghibli_Pebble.asset");
            Mesh rootsMesh = SaveMeshAsset(BuildGhibliMangroveRootsMesh(), MeshFolder + "/SM_Ghibli_MangroveRoots.asset");
            Mesh pierPlanksMesh = SaveMeshAsset(BuildGhibliPierPlanksMesh(), MeshFolder + "/SM_Ghibli_PierPlanks.asset");
            Mesh pierStiltMesh = SaveMeshAsset(BuildGhibliPierStiltMesh(), MeshFolder + "/SM_Ghibli_PierStilt.asset");
            Mesh ropeCoilMesh = SaveMeshAsset(BuildGhibliRopeCoilMesh(), MeshFolder + "/SM_Ghibli_RopeCoil.asset");

            // 3. Build the 4 Core Modular Prefabs (Pivot strictly at Y = 0 Top-Center, Collider = None)
            CreateMudBankPrefab(mudBodyMesh, grassLipMesh, pebbleMesh, mudBankMat, grassCapMat, pebbleMat);
            CreateMangroveRootPrefab(rootsMesh, pebbleMesh, rootMat, pebbleMat);
            CreatePierPlankSectionPrefab(pierPlanksMesh, pierPlankMat, pierStiltMat);
            CreatePierStiltPostPrefab(pierStiltMesh, ropeCoilMesh, pierStiltMat, ropeMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#6be385>[GhibliModularKit]</color> Successfully generated 4 Ghibli Cartoon Modular 3D Prefabs in " + PrefabFolder);
        }

        private static void EnsureFolders()
        {
            if (!Directory.Exists(MatFolder)) Directory.CreateDirectory(MatFolder);
            if (!Directory.Exists(MeshFolder)) Directory.CreateDirectory(MeshFolder);
            if (!Directory.Exists(PrefabFolder)) Directory.CreateDirectory(PrefabFolder);
        }

        private static Material CreateOrUpdateMaterial(
            string path,
            Shader shader,
            Color baseCol,
            Color midCol,
            Color shadowCol,
            bool useTop,
            Color topCol,
            Color topShadowCol,
            float topThreshold,
            bool useWet,
            Color wetCol,
            float rimStrength)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", baseCol);
            mat.SetColor("_MidtoneColor", midCol);
            mat.SetColor("_ShadowColor", shadowCol);
            mat.SetFloat("_UseTopOverlay", useTop ? 1f : 0f);
            mat.SetColor("_TopColor", topCol);
            mat.SetColor("_TopShadowColor", topShadowCol);
            mat.SetFloat("_TopNormalThreshold", topThreshold);
            mat.SetFloat("_UseWetness", useWet ? 1f : 0f);
            mat.SetColor("_WetColor", wetCol);
            mat.SetFloat("_RimStrength", rimStrength);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh SaveMeshAsset(Mesh mesh, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
        }

        // =====================================================================
        // PREFAB 1: PF_Mod_MudBank_Chunk (Pivot Y = 0 at Top Walk Surface)
        // =====================================================================
        private static void CreateMudBankPrefab(
            Mesh mudBodyMesh,
            Mesh grassLipMesh,
            Mesh pebbleMesh,
            Material mudBankMat,
            Material grassCapMat,
            Material pebbleMat)
        {
            GameObject root = new GameObject("PF_Mod_MudBank_Chunk");
            root.transform.position = Vector3.zero;

            // 1. Sculpted Mud Bank Body (Y: 0 down to -1.85m)
            GameObject bodyGO = CreateMeshChild(root.transform, "MudBank_Body", mudBodyMesh, mudBankMat);
            bodyGO.transform.localPosition = Vector3.zero;

            // 2. Puffy Overhanging Ghibli Grass Cap Lip (Y: +0.02 down to -0.32m)
            GameObject lipGO = CreateMeshChild(root.transform, "Ghibli_GrassOverhangLip", grassLipMesh, grassCapMat);
            lipGO.transform.localPosition = Vector3.zero;

            // 3. Embedded Chubby Pebbles on the slope
            Vector3[] pebblePositions = new Vector3[]
            {
                new Vector3(-0.78f, -0.52f, -0.92f),
                new Vector3( 0.62f, -0.78f, -0.98f),
                new Vector3( 0.12f, -0.38f, -0.88f),
                new Vector3(-0.32f, -1.15f, -1.06f)
            };
            Vector3[] pebbleScales = new Vector3[]
            {
                new Vector3(0.38f, 0.26f, 0.32f),
                new Vector3(0.46f, 0.32f, 0.36f),
                new Vector3(0.28f, 0.20f, 0.25f),
                new Vector3(0.52f, 0.35f, 0.40f)
            };

            for (int i = 0; i < pebblePositions.Length; i++)
            {
                GameObject peb = CreateMeshChild(root.transform, "Bank_Pebble_" + (i + 1), pebbleMesh, pebbleMat);
                peb.transform.localPosition = pebblePositions[i];
                peb.transform.localRotation = Quaternion.Euler(i * 19f, i * 47f, i * 13f);
                peb.transform.localScale = pebbleScales[i];
            }

            // 4. Attach 3D Grass Tufts on top edge if PF_StylizedGrassTuft_3D exists
            GameObject grassTuftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Foliage/PF_StylizedGrassTuft_3D.prefab");
            if (grassTuftPrefab != null)
            {
                Vector3[] tuftOffsets = new Vector3[]
                {
                    new Vector3(-0.85f, 0.0f, -0.62f),
                    new Vector3(-0.15f, 0.0f, -0.72f),
                    new Vector3( 0.65f, 0.0f, -0.66f),
                    new Vector3( 0.25f, 0.0f,  0.55f)
                };
                for (int i = 0; i < tuftOffsets.Length; i++)
                {
                    GameObject tuft = (GameObject)PrefabUtility.InstantiatePrefab(grassTuftPrefab, root.transform);
                    tuft.name = "Top_GrassTuft_" + (i + 1);
                    tuft.transform.localPosition = tuftOffsets[i];
                    tuft.transform.localRotation = Quaternion.Euler(0f, i * 65f, 0f);
                    tuft.transform.localScale = Vector3.one * (0.75f + (i % 3) * 0.15f);
                    StripCollidersRecursively(tuft);
                }
            }

            StripCollidersRecursively(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/PF_Mod_MudBank_Chunk.prefab");
            Object.DestroyImmediate(root);
        }

        // =====================================================================
        // PREFAB 2: PF_Mod_MangroveRoot_Cluster (Pivot Y = 0 at Bank Rim)
        // =====================================================================
        private static void CreateMangroveRootPrefab(
            Mesh rootsMesh,
            Mesh pebbleMesh,
            Material rootMat,
            Material pebbleMat)
        {
            GameObject root = new GameObject("PF_Mod_MangroveRoot_Cluster");
            root.transform.position = Vector3.zero;

            GameObject tendrilsGO = CreateMeshChild(root.transform, "Mangrove_RootTendrils", rootsMesh, rootMat);
            tendrilsGO.transform.localPosition = Vector3.zero;

            // Add 2 mossy stones nestled among the roots
            GameObject stoneA = CreateMeshChild(root.transform, "Root_MossStone_A", pebbleMesh, pebbleMat);
            stoneA.transform.localPosition = new Vector3(-0.28f, -1.22f, -0.45f);
            stoneA.transform.localRotation = Quaternion.Euler(8f, 32f, -10f);
            stoneA.transform.localScale = new Vector3(0.48f, 0.34f, 0.42f);

            GameObject stoneB = CreateMeshChild(root.transform, "Root_MossStone_B", pebbleMesh, pebbleMat);
            stoneB.transform.localPosition = new Vector3(0.35f, -0.85f, -0.32f);
            stoneB.transform.localRotation = Quaternion.Euler(-12f, 110f, 6f);
            stoneB.transform.localScale = new Vector3(0.36f, 0.26f, 0.32f);

            StripCollidersRecursively(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/PF_Mod_MangroveRoot_Cluster.prefab");
            Object.DestroyImmediate(root);
        }

        // =====================================================================
        // PREFAB 3: PF_Mod_Pier_PlankSection (Pivot Y = 0 at Top Deck Surface)
        // =====================================================================
        private static void CreatePierPlankSectionPrefab(
            Mesh pierPlanksMesh,
            Material pierPlankMat,
            Material beamMat)
        {
            GameObject root = new GameObject("PF_Mod_Pier_PlankSection");
            root.transform.position = Vector3.zero;

            // Beveled hand-hewn planks (Top surface sits right at Y = 0)
            GameObject planksGO = CreateMeshChild(root.transform, "Pier_BeveledPlanks", pierPlanksMesh, pierPlankMat);
            planksGO.transform.localPosition = Vector3.zero;

            // Two longitudinal support stringer beams under the planks
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = (side < 0) ? "SupportStringer_Front" : "SupportStringer_Back";
                beam.transform.SetParent(root.transform, false);
                beam.transform.localPosition = new Vector3(0f, -0.14f, side * 0.72f);
                beam.transform.localScale = new Vector3(2.04f, 0.14f, 0.16f);
                var mr = beam.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = beamMat;
            }

            StripCollidersRecursively(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/PF_Mod_Pier_PlankSection.prefab");
            Object.DestroyImmediate(root);
        }

        // =====================================================================
        // PREFAB 4: PF_Mod_Pier_StiltPost (Pivot Y = 0 at Deck Level)
        // =====================================================================
        private static void CreatePierStiltPostPrefab(
            Mesh stiltMesh,
            Mesh ropeCoilMesh,
            Material stiltMat,
            Material ropeMat)
        {
            GameObject root = new GameObject("PF_Mod_Pier_StiltPost");
            root.transform.position = Vector3.zero;

            GameObject postGO = CreateMeshChild(root.transform, "Stilt_TimberPost", stiltMesh, stiltMat);
            postGO.transform.localPosition = Vector3.zero;

            GameObject ropeGO = CreateMeshChild(root.transform, "Stilt_RopeLashing", ropeCoilMesh, ropeMat);
            ropeGO.transform.localPosition = new Vector3(0f, -0.08f, 0f);

            StripCollidersRecursively(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/PF_Mod_Pier_StiltPost.prefab");
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateMeshChild(Transform parent, string name, Mesh mesh, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        private static void StripCollidersRecursively(GameObject root)
        {
            var cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                Object.DestroyImmediate(cols[i]);
            }
        }

        // =====================================================================
        // PROCEDURAL GHIBLI MESH BUILDERS
        // =====================================================================

        /// <summary>
        /// Builds a chubby, organic Ghibli riverbank chunk (Width 2.6m, Height 1.85m, Depth 2.1m)
        /// with Top-Center at Y = 0, bulging terraced clay front slope, and tapered overlap ends.
        /// </summary>
        private static Mesh BuildGhibliMudBankBodyMesh()
        {
            int segX = 24;
            int segY = 14;
            int segZ = 14;

            float width = 2.6f;   // X: -1.3 to +1.3 (extra 0.05m per side for seamless overlap)
            float height = 1.85f; // Y: 0.0 down to -1.85
            float depth = 2.1f;   // Z: -0.95 (front slope) to +1.15 (back)

            Mesh mesh = BuildSubdividedBox(segX, segY, segZ, width, height, depth);
            Vector3[] verts = mesh.vertices;
            Color[] colors = new Color[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                // Shift so top face is at Y = 0 (v.y was in [-height/2, +height/2])
                v.y -= height * 0.5f;

                // Normalized depth factor tY: 0 at top (Y=0), 1 at bottom (Y=-height)
                float tY = Mathf.Clamp01(-v.y / height);
                // Normalized X factor from center: 0 at center, 1 at ends
                float tX = Mathf.Abs(v.x) / (width * 0.5f);

                // 1. Slant the front face (-Z) outward as it goes down, with 2 Ghibli rounded terrace bulges
                if (v.z < 0f)
                {
                    float frontFactor = Mathf.Clamp01(-v.z / (depth * 0.5f));
                    float slopePush = tY * 0.42f;
                    // Two soft Ghibli clay strata bulges
                    float terraceBulge = Mathf.Sin(tY * Mathf.PI * 2.2f) * 0.11f + Mathf.Sin(tY * Mathf.PI) * 0.14f;
                    v.z -= (slopePush + terraceBulge) * frontFactor;
                }

                // 2. Round off the top-front and top-back edges so it looks like hand-molded clay
                if (tY < 0.22f)
                {
                    float lipRound = 1f - Mathf.Sin((tY / 0.22f) * (Mathf.PI * 0.5f));
                    v.z *= (1f - lipRound * 0.12f);
                }

                // 3. Organic 3D Perlin displacement (stronger on front slope, nearly flat at Y=0 walk center)
                float n1 = Mathf.PerlinNoise(v.x * 1.35f + 11.3f, v.y * 1.65f + 7.1f) - 0.5f;
                float n2 = Mathf.PerlinNoise(v.z * 1.45f + 3.7f, v.x * 1.85f + 19.4f) - 0.5f;

                float topFlatMask = Mathf.SmoothStep(0.02f, 0.25f, tY);
                v.z += (n1 * 0.18f + n2 * 0.08f) * topFlatMask;
                v.x += n2 * 0.10f * topFlatMask;

                // Keep top surface gentle (within +/- 2cm) so character feet never clip weirdly
                v.y += n1 * 0.04f * (1f - topFlatMask) + n2 * 0.10f * topFlatMask;

                // Slightly taper ends in Z so adjacent rotated/scaled chunks blend smoothly
                if (tX > 0.75f)
                {
                    float endTaper = (tX - 0.75f) / 0.25f;
                    v.z *= (1f - endTaper * 0.08f);
                }

                verts[i] = v;
                float shadeVar = Mathf.Clamp01(0.5f + n1 * 0.45f - tY * 0.25f);
                colors[i] = new Color(shadeVar, 1f - tY, 0f, 1f);
            }

            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "SM_Ghibli_MudBank_Body";
            return mesh;
        }

        /// <summary>
        /// Builds the signature Ghibli puffy overhanging grass cap with a wavy scalloped front curtain
        /// that drapes over the top rim of the mud bank.
        /// </summary>
        private static Mesh BuildGhibliGrassLipMesh()
        {
            int segX = 28;
            int segY = 8;
            int segZ = 12;

            float width = 2.66f;
            float height = 0.34f;
            float depth = 2.24f;

            Mesh mesh = BuildSubdividedBox(segX, segY, segZ, width, height, depth);
            Vector3[] verts = mesh.vertices;
            Color[] colors = new Color[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                // Top sits at Y = +0.02m, bottom hangs down to Y = -0.32m
                v.y = v.y - height * 0.5f + 0.02f;

                float tY = Mathf.Clamp01(-v.y / height); // 0 at top, 1 at bottom of lip

                // Puffy rounded cushion profile
                float puff = Mathf.Sin(tY * Mathf.PI) * 0.10f;
                float zSign = Mathf.Sign(v.z);
                if (Mathf.Abs(v.z) > 0.3f)
                {
                    v.z += zSign * puff;
                }

                // Scalloped wavy Ghibli grass fringe along the bottom edge (especially in front -Z)
                float wave = Mathf.Sin(v.x * 4.8f) * 0.055f
                           + Mathf.Sin(v.x * 9.5f + 1.2f) * 0.035f
                           + (Mathf.PerlinNoise(v.x * 2.4f + 5.2f, v.z * 2.4f + 8.3f) - 0.5f) * 0.06f;

                if (tY > 0.45f)
                {
                    float fringeMask = (tY - 0.45f) / 0.55f;
                    v.y += wave * fringeMask * (v.z < 0f ? 1.35f : 0.75f);
                }

                // Gentle pillowy dome on top
                if (tY < 0.25f)
                {
                    float edgeDistZ = Mathf.Abs(v.z) / (depth * 0.5f);
                    float dome = (1f - edgeDistZ * edgeDistZ) * 0.035f;
                    v.y += dome * (1f - tY * 4f);
                }

                verts[i] = v;
                colors[i] = new Color(Mathf.Clamp01(0.55f - tY * 0.3f + wave * 2f), 1f, 0f, 1f);
            }

            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "SM_Ghibli_MudBank_GrassLip";
            return mesh;
        }

        /// <summary>
        /// Builds a chubby Ghibli rounded stone/pebble mesh.
        /// </summary>
        private static Mesh BuildGhibliPebbleMesh()
        {
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh src = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] srcVerts = src.vertices;
            int[] srcTris = src.triangles;
            Object.DestroyImmediate(tempSphere);

            Mesh mesh = new Mesh();
            Vector3[] verts = new Vector3[srcVerts.Length];
            Color[] colors = new Color[srcVerts.Length];

            for (int i = 0; i < srcVerts.Length; i++)
            {
                Vector3 v = srcVerts[i];
                // Flatten bottom slightly, bevel sides like a river stone
                if (v.y < 0f) v.y *= 0.65f;
                float n = Mathf.PerlinNoise(v.x * 2.5f + 4.1f, v.z * 2.5f + 9.2f) - 0.5f;
                v += v.normalized * (n * 0.14f);
                verts[i] = v;
                colors[i] = new Color(Mathf.Clamp01(0.5f + v.y * 0.5f + n * 0.4f), 0f, 0f, 1f);
            }

            mesh.vertices = verts;
            mesh.triangles = srcTris;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "SM_Ghibli_Pebble";
            return mesh;
        }

        /// <summary>
        /// Builds a multi-tendril gnarled Mangrove Root cluster arching from Y=0 down to Y=-1.65m.
        /// </summary>
        private static Mesh BuildGhibliMangroveRootsMesh()
        {
            List<CombineInstance> combines = new List<CombineInstance>();

            // 5 main arching mangrove prop roots + 3 secondary offshoots
            Vector3[][] rootCurves = new Vector3[][]
            {
                new Vector3[] {
                    new Vector3(-0.35f, -0.05f, -0.15f),
                    new Vector3(-0.55f, -0.35f, -0.58f),
                    new Vector3(-0.68f, -0.95f, -0.85f),
                    new Vector3(-0.72f, -1.62f, -0.95f)
                },
                new Vector3[] {
                    new Vector3( 0.00f, -0.02f, -0.10f),
                    new Vector3(-0.08f, -0.28f, -0.65f),
                    new Vector3(-0.12f, -0.88f, -1.02f),
                    new Vector3(-0.10f, -1.65f, -1.12f)
                },
                new Vector3[] {
                    new Vector3( 0.32f, -0.06f, -0.12f),
                    new Vector3( 0.52f, -0.40f, -0.60f),
                    new Vector3( 0.64f, -0.98f, -0.88f),
                    new Vector3( 0.68f, -1.60f, -0.96f)
                },
                new Vector3[] {
                    new Vector3(-0.18f, -0.15f, -0.25f),
                    new Vector3( 0.15f, -0.55f, -0.72f),
                    new Vector3( 0.28f, -1.12f, -0.92f),
                    new Vector3( 0.25f, -1.64f, -1.00f)
                },
                new Vector3[] {
                    new Vector3( 0.18f, -0.20f, -0.22f),
                    new Vector3(-0.22f, -0.62f, -0.68f),
                    new Vector3(-0.38f, -1.18f, -0.82f),
                    new Vector3(-0.42f, -1.62f, -0.88f)
                }
            };

            float[] startRadii = new float[] { 0.13f, 0.15f, 0.13f, 0.11f, 0.10f };
            float[] endRadii   = new float[] { 0.06f, 0.07f, 0.06f, 0.05f, 0.05f };

            for (int r = 0; r < rootCurves.Length; r++)
            {
                Mesh tube = BuildOrganicTubeAlongBezier(
                    rootCurves[r][0],
                    rootCurves[r][1],
                    rootCurves[r][2],
                    rootCurves[r][3],
                    startRadii[r],
                    endRadii[r],
                    lengthSegments: 16,
                    radialSegments: 9,
                    seed: r * 17.3f
                );

                CombineInstance ci = new CombineInstance
                {
                    mesh = tube,
                    transform = Matrix4x4.identity
                };
                combines.Add(ci);
            }

            Mesh finalMesh = new Mesh();
            finalMesh.CombineMeshes(combines.ToArray(), true, false);
            finalMesh.RecalculateNormals();
            finalMesh.RecalculateBounds();
            finalMesh.name = "SM_Ghibli_MangroveRoots";
            return finalMesh;
        }

        /// <summary>
        /// Builds 6 chubby, beveled hand-hewn wooden pier planks across a 2.0m span (Pivot Y = 0 at top deck).
        /// </summary>
        private static Mesh BuildGhibliPierPlanksMesh()
        {
            int plankCount = 6;
            float sectionLengthX = 2.0f;
            float plankStepX = sectionLengthX / plankCount;
            float plankWidthX = plankStepX * 0.88f; // Natural gap between planks
            float plankThicknessY = 0.09f;
            float plankDepthZ = 2.15f;

            List<CombineInstance> combines = new List<CombineInstance>();

            for (int i = 0; i < plankCount; i++)
            {
                float centerX = -sectionLengthX * 0.5f + (i + 0.5f) * plankStepX;
                // Deterministic hand-crafted variation per plank
                float seed = i * 31.7f + 5.3f;
                float heightJitter = (Mathf.Sin(seed) * 0.008f);
                float yawJitter = Mathf.Cos(seed * 1.7f) * 1.4f;
                float rollJitter = Mathf.Sin(seed * 2.3f) * 0.8f;
                float lengthScale = 0.96f + Mathf.Abs(Mathf.Sin(seed * 0.9f)) * 0.08f;
                float shadeTone = 0.35f + (i % 3) * 0.22f + Mathf.Sin(seed) * 0.1f;

                Mesh singlePlank = BuildBeveledPlankMesh(
                    plankWidthX,
                    plankThicknessY,
                    plankDepthZ * lengthScale,
                    shadeTone,
                    seed
                );

                Quaternion rot = Quaternion.Euler(rollJitter, yawJitter, 0f);
                Vector3 pos = new Vector3(centerX, heightJitter, 0f);

                CombineInstance ci = new CombineInstance
                {
                    mesh = singlePlank,
                    transform = Matrix4x4.TRS(pos, rot, Vector3.one)
                };
                combines.Add(ci);
            }

            Mesh combined = new Mesh();
            combined.CombineMeshes(combines.ToArray(), true, true);
            combined.RecalculateNormals();
            combined.RecalculateBounds();
            combined.name = "SM_Ghibli_PierPlanks";
            return combined;
        }

        private static Mesh BuildBeveledPlankMesh(float sizeX, float sizeY, float sizeZ, float shadeTone, float seed)
        {
            Mesh m = BuildSubdividedBox(4, 3, 10, sizeX, sizeY, sizeZ);
            Vector3[] verts = m.vertices;
            Color[] colors = new Color[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                // Top surface sits at Y = 0
                v.y -= sizeY * 0.5f;

                // Bevel edges slightly
                float nx = Mathf.Abs(v.x) / (sizeX * 0.5f);
                float nz = Mathf.Abs(v.z) / (sizeZ * 0.5f);
                if (nx > 0.82f && v.y > -sizeY * 0.35f)
                {
                    v.y -= (nx - 0.82f) * 0.06f;
                }
                if (nz > 0.90f && v.y > -sizeY * 0.35f)
                {
                    v.y -= (nz - 0.90f) * 0.08f;
                }

                // Subtle cartoon bow along Z
                float zNorm = v.z / (sizeZ * 0.5f);
                v.y -= (zNorm * zNorm) * 0.012f;
                v.x += Mathf.Sin(zNorm * Mathf.PI + seed) * 0.008f;

                verts[i] = v;
                colors[i] = new Color(Mathf.Clamp01(shadeTone), 0f, 0f, 1f);
            }

            m.vertices = verts;
            m.colors = colors;
            m.RecalculateNormals();
            return m;
        }

        /// <summary>
        /// Builds a Ghibli tapered timber stilt post (Top cap at Y = +0.42m, base at Y = -3.08m, Deck at Y = 0).
        /// </summary>
        private static Mesh BuildGhibliPierStiltMesh()
        {
            Vector3 p0 = new Vector3(0f,  0.42f, 0f);
            Vector3 p1 = new Vector3(0.02f, -0.65f, -0.01f);
            Vector3 p2 = new Vector3(-0.02f, -1.85f, 0.01f);
            Vector3 p3 = new Vector3(0f, -3.08f, 0f);

            Mesh post = BuildOrganicTubeAlongBezier(
                p0, p1, p2, p3,
                startRadius: 0.125f,
                endRadius: 0.095f,
                lengthSegments: 18,
                radialSegments: 10,
                seed: 42.1f,
                chamferTopCap: true
            );
            post.name = "SM_Ghibli_PierStilt";
            return post;
        }

        /// <summary>
        /// Builds a 4-turn 3D hemp rope lashing coil around the stilt post.
        /// </summary>
        private static Mesh BuildGhibliRopeCoilMesh()
        {
            int turns = 4;
            int segsPerTurn = 18;
            int totalRingSegs = turns * segsPerTurn;
            int tubeSides = 6;
            float coilRadius = 0.132f;
            float ropeTubeRadius = 0.022f;
            float verticalSpan = 0.16f;

            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();

            for (int i = 0; i <= totalRingSegs; i++)
            {
                float t = (float)i / totalRingSegs;
                float angle = t * turns * Mathf.PI * 2f;
                float yCenter = (0.5f - t) * verticalSpan;

                Vector3 ringCenter = new Vector3(Mathf.Cos(angle) * coilRadius, yCenter, Mathf.Sin(angle) * coilRadius);
                Vector3 radialOut = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 upDir = Vector3.up;

                for (int s = 0; s < tubeSides; s++)
                {
                    float sAngle = (float)s / tubeSides * Mathf.PI * 2f;
                    Vector3 offset = (radialOut * Mathf.Cos(sAngle) + upDir * Mathf.Sin(sAngle)) * ropeTubeRadius;
                    verts.Add(ringCenter + offset);
                    colors.Add(new Color(0.5f + 0.25f * Mathf.Sin(angle * 3f), 0f, 0f, 1f));
                }
            }

            for (int i = 0; i < totalRingSegs; i++)
            {
                int rowA = i * tubeSides;
                int rowB = (i + 1) * tubeSides;
                for (int s = 0; s < tubeSides; s++)
                {
                    int nextS = (s + 1) % tubeSides;
                    tris.Add(rowA + s);
                    tris.Add(rowB + s);
                    tris.Add(rowB + nextS);

                    tris.Add(rowA + s);
                    tris.Add(rowB + nextS);
                    tris.Add(rowA + nextS);
                }
            }

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            m.name = "SM_Ghibli_RopeCoil";
            return m;
        }

        // =====================================================================
        // GEOMETRY HELPER FUNCTIONS
        // =====================================================================
        private static Mesh BuildOrganicTubeAlongBezier(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float startRadius,
            float endRadius,
            int lengthSegments,
            int radialSegments,
            float seed,
            bool chamferTopCap = false)
        {
            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();

            for (int i = 0; i <= lengthSegments; i++)
            {
                float t = (float)i / lengthSegments;
                Vector3 center = EvaluateCubicBezier(p0, p1, p2, p3, t);
                Vector3 tangent = EvaluateCubicBezierDerivative(p0, p1, p2, p3, t).normalized;
                if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.down;

                Vector3 refRight = Mathf.Abs(Vector3.Dot(tangent, Vector3.forward)) > 0.9f ? Vector3.right : Vector3.forward;
                Vector3 normal = Vector3.Cross(tangent, refRight).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

                float radius = Mathf.Lerp(startRadius, endRadius, t);
                // Add slight cartoon bulge in the middle
                radius *= (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);

                if (chamferTopCap && i == 0)
                {
                    radius *= 0.72f;
                }

                for (int r = 0; r < radialSegments; r++)
                {
                    float angle = (float)r / radialSegments * Mathf.PI * 2f;
                    float knotNoise = (Mathf.PerlinNoise(t * 3.5f + seed, r * 0.7f + seed) - 0.5f) * 0.18f;
                    float localR = radius * (1f + knotNoise);

                    Vector3 radialDir = normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                    verts.Add(center + radialDir * localR);
                    colors.Add(new Color(Mathf.Clamp01(0.65f - t * 0.35f + knotNoise), 0f, 0f, 1f));
                }
            }

            for (int i = 0; i < lengthSegments; i++)
            {
                int ringA = i * radialSegments;
                int ringB = (i + 1) * radialSegments;
                for (int r = 0; r < radialSegments; r++)
                {
                    int nextR = (r + 1) % radialSegments;
                    tris.Add(ringA + r);
                    tris.Add(ringB + nextR);
                    tris.Add(ringB + r);

                    tris.Add(ringA + r);
                    tris.Add(ringA + nextR);
                    tris.Add(ringB + nextR);
                }
            }

            // Cap top
            int topCenterIdx = verts.Count;
            verts.Add(p0 + Vector3.up * 0.02f);
            colors.Add(new Color(0.8f, 0f, 0f, 1f));
            for (int r = 0; r < radialSegments; r++)
            {
                int nextR = (r + 1) % radialSegments;
                tris.Add(topCenterIdx);
                tris.Add(nextR);
                tris.Add(r);
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0
                 + 3f * u * u * t * p1
                 + 3f * u * t * t * p2
                 + t * t * t * p3;
        }

        private static Vector3 EvaluateCubicBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return 3f * u * u * (p1 - p0)
                 + 6f * u * t * (p2 - p1)
                 + 3f * t * t * (p3 - p2);
        }

        private static Mesh BuildSubdividedBox(int segX, int segY, int segZ, float sizeX, float sizeY, float sizeZ)
        {
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float hx = sizeX * 0.5f;
            float hy = sizeY * 0.5f;
            float hz = sizeZ * 0.5f;

            // Top (+Y)
            AddGridFace(verts, uvs, tris, new Vector3(-hx, hy, -hz), new Vector3(sizeX, 0, 0), new Vector3(0, 0, sizeZ), segX, segZ, false);
            // Bottom (-Y)
            AddGridFace(verts, uvs, tris, new Vector3(-hx, -hy, hz), new Vector3(sizeX, 0, 0), new Vector3(0, 0, -sizeZ), segX, segZ, false);
            // Front (-Z)
            AddGridFace(verts, uvs, tris, new Vector3(-hx, -hy, -hz), new Vector3(sizeX, 0, 0), new Vector3(0, sizeY, 0), segX, segY, false);
            // Back (+Z)
            AddGridFace(verts, uvs, tris, new Vector3(hx, -hy, hz), new Vector3(-sizeX, 0, 0), new Vector3(0, sizeY, 0), segX, segY, false);
            // Left (-X)
            AddGridFace(verts, uvs, tris, new Vector3(-hx, -hy, hz), new Vector3(0, 0, -sizeZ), new Vector3(0, sizeY, 0), segZ, segY, false);
            // Right (+X)
            AddGridFace(verts, uvs, tris, new Vector3(hx, -hy, -hz), new Vector3(0, 0, sizeZ), new Vector3(0, sizeY, 0), segZ, segY, false);

            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return m;
        }

        private static void AddGridFace(
            List<Vector3> verts,
            List<Vector2> uvs,
            List<int> tris,
            Vector3 origin,
            Vector3 axisU,
            Vector3 axisV,
            int segU,
            int segV,
            bool flip)
        {
            int baseIdx = verts.Count;
            for (int v = 0; v <= segV; v++)
            {
                float tv = (float)v / segV;
                for (int u = 0; u <= segU; u++)
                {
                    float tu = (float)u / segU;
                    verts.Add(origin + axisU * tu + axisV * tv);
                    uvs.Add(new Vector2(tu, tv));
                }
            }

            int stride = segU + 1;
            for (int v = 0; v < segV; v++)
            {
                for (int u = 0; u < segU; u++)
                {
                    int i0 = baseIdx + v * stride + u;
                    int i1 = i0 + 1;
                    int i2 = i0 + stride;
                    int i3 = i2 + 1;

                    if (!flip)
                    {
                        tris.Add(i0);
                        tris.Add(i2);
                        tris.Add(i1);

                        tris.Add(i1);
                        tris.Add(i2);
                        tris.Add(i3);
                    }
                    else
                    {
                        tris.Add(i0);
                        tris.Add(i1);
                        tris.Add(i2);

                        tris.Add(i1);
                        tris.Add(i3);
                        tris.Add(i2);
                    }
                }
            }
        }

        // =====================================================================
        // SCENE SHOWCASE SPAWNER (FOR VISUAL INSPECTION)
        // =====================================================================
        public static void SpawnShowcaseInActiveScene()
        {
            const string ShowcaseName = "_GHIBLI_MODULAR_KIT_SHOWCASE_";
            GameObject existing = GameObject.Find(ShowcaseName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject root = new GameObject(ShowcaseName);
            Undo.RegisterCreatedObjectUndo(root, "Spawn Ghibli Modular Kit Showcase");

            // Place near the player start or at a clean spot in front of the camera
            Vector3 basePos = new Vector3(-12f, 1.2f, -3.2f);
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                basePos = player.transform.position + new Vector3(3.5f, 0.8f, -2.8f);
            }
            root.transform.position = basePos;

            GameObject mudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_Mod_MudBank_Chunk.prefab");
            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_Mod_MangroveRoot_Cluster.prefab");
            GameObject plankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_Mod_Pier_PlankSection.prefab");
            GameObject stiltPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_Mod_Pier_StiltPost.prefab");

            // 1. Individual Pieces Display Row (Left to Right)
            GameObject individualGroup = new GameObject("01_Individual_Modular_Pieces");
            individualGroup.transform.SetParent(root.transform, false);

            SpawnPrefabInstance(mudPrefab, individualGroup.transform, "1_PF_Mod_MudBank_Chunk", new Vector3(0f, 0f, 0f));
            SpawnPrefabInstance(rootPrefab, individualGroup.transform, "2_PF_Mod_MangroveRoot_Cluster", new Vector3(3.2f, 0f, 0f));
            SpawnPrefabInstance(plankPrefab, individualGroup.transform, "3_PF_Mod_Pier_PlankSection", new Vector3(6.0f, 0f, 0f));
            SpawnPrefabInstance(stiltPrefab, individualGroup.transform, "4_PF_Mod_Pier_StiltPost", new Vector3(8.5f, 0f, 0f));

            // 2. Mini "Tiny Glade" Assembled Demo (3 MudBanks overlapping -> Mangrove Roots -> 2 Pier Sections + 4 Stilts)
            GameObject demoGroup = new GameObject("02_Assembled_Riverbank_To_Pier_Demo");
            demoGroup.transform.SetParent(root.transform, false);
            demoGroup.transform.localPosition = new Vector3(12.5f, 0f, 0f);

            // 3 overlapping mud bank chunks with slight scale/rotation variety
            for (int i = 0; i < 3; i++)
            {
                GameObject chunk = SpawnPrefabInstance(mudPrefab, demoGroup.transform, "Assembled_MudBank_" + (i + 1), new Vector3(i * 2.15f, Mathf.Sin(i) * 0.06f, 0f));
                if (chunk != null)
                {
                    chunk.transform.localScale = new Vector3(1.0f + (i % 2) * 0.12f, 1.0f + (i * 0.05f), 1.0f);
                }
            }

            // Mangrove roots clinging to the bank edge before the water
            SpawnPrefabInstance(rootPrefab, demoGroup.transform, "Assembled_MangroveRoots_A", new Vector3(1.2f, -0.05f, -0.35f));
            SpawnPrefabInstance(rootPrefab, demoGroup.transform, "Assembled_MangroveRoots_B", new Vector3(4.8f, -0.08f, -0.30f));

            // Transition to Wooden Pier (2 sections + 4 stilts)
            for (int p = 0; p < 2; p++)
            {
                float px = 6.4f + p * 1.98f;
                SpawnPrefabInstance(plankPrefab, demoGroup.transform, "Assembled_PierDeck_" + (p + 1), new Vector3(px, -0.04f, 0f));
                SpawnPrefabInstance(stiltPrefab, demoGroup.transform, "Assembled_Stilt_Front_" + (p + 1), new Vector3(px + 0.7f, -0.04f, -0.82f));
                SpawnPrefabInstance(stiltPrefab, demoGroup.transform, "Assembled_Stilt_Back_" + (p + 1), new Vector3(px + 0.7f, -0.04f,  0.82f));
            }

            // Invisible Smooth Walk Collider on top of the assembled demo so the player can walk on it immediately!
            GameObject smoothColliderGO = new GameObject("Invisible_Smooth_Walk_Collider");
            smoothColliderGO.transform.SetParent(demoGroup.transform, false);
            smoothColliderGO.transform.localPosition = new Vector3(4.2f, -0.15f, 0f);
            BoxCollider bc = smoothColliderGO.AddComponent<BoxCollider>();
            bc.size = new Vector3(11.2f, 0.3f, 2.2f);

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private static GameObject SpawnPrefabInstance(GameObject prefab, Transform parent, string name, Vector3 localPos)
        {
            if (prefab == null) return null;
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = name;
            inst.transform.localPosition = localPos;
            return inst;
        }
    }
}
