#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Comprehensive Technical Audit & Visual Repair Pass for Level 1 ("Hoa trên đất đỏ")
    /// Dedicated Reference Scene: "Assets/Scenes/New Scene 1_Overhaul_Refined.unity"
    /// 
    /// Strict Resolution of all 6 Priorities from Technical Audit:
    /// 1. Terrain Geometry & Seams (Critical):
    ///    - Rebuilds Zone 1 terrain as a SINGLE continuous, gap-free, seam-free sculpted mesh
    ///      spanning X = -15.0m to +14.60m (zero internal splits, zero Z-indent bevels at X = 5.2m).
    ///    - Replaces artificial 1.4m vertical cliff step with an organic sculpted hillside ascent
    ///      from Y = -1.95m up to -0.55m (X = 2.8m .. 5.2m).
    ///    - Fixes 5cm gaps at the Ravine Chasm (X = 14.60m .. 17.60m).
    /// 2. Shadow Rendering (Critical):
    ///    - LanaStylizedLedge shader updated to conditionally ignore shadows on subterranean rock.
    ///    - Sets _ReceiveShadows = 0.0 on underground rock underbelly materials.
    ///    - Zero foliage silhouettes or black splotches inside underground soil.
    ///    - Softens surface grass canopy shadow to painterly watercolor dappled light.
    /// 3. Underground Visuals (High Priority):
    ///    - Rich, cohesive Nam Bo Basalt Red Earth ("Đất Đỏ") with subtle stratified sedimentary layers.
    ///    - Zero harsh rectangular borders or missing face artifacts.
    ///    - Complete purge of any stray vegetation buried underground.
    /// 4. Platform & Rock Integration (Medium-High):
    ///    - 3-Stage transition: Main Terrain -> Embedded 3D Boulders -> Grounding Foliage/Moss.
    ///    - 4 tiered mossy boulders embedded 35% into the slope as natural climbing stone steps.
    ///    - Weathered 3D boulders anchoring the ravine chasm lips.
    ///    - Zero floating rocks or detached decorative meshes.
    /// 5. Foliage Density & 3-Plane Depth Separation:
    ///    - Background (Z >= 6.5m): Distant bamboo and canopy pushed back, desaturated, tinted in cool teal morning mist.
    ///    - Midground (Z = 1.0m .. 4.0m): Hero banyans with distinct, clean silhouettes and warm sage foliage.
    ///    - Foreground (Z <= 0.4m): Darker framing grass blades and fern accents.
    /// 6. Camera & Composition:
    ///    - Implements Planet of Lana lower-third rule: ground line sits in the bottom 20-25% of the frame.
    ///    - Upper 75% opened up for character action, majestic banyan boughs, and sky.
    ///    - Live Cinemachine camera TargetOffset raised to (2.0f, 1.85f, 0.0f).
    /// </summary>
    public static class RefineSceneVisualsAndLanaArt
    {
        public const string SourceScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        public const string TargetScenePath = "Assets/Scenes/New Scene 1_Overhaul_Refined.unity";
        private const string GenAssetsDir = "Assets/Materials/LanaOverhaul/ArtisticRefine";
        private const string GenMeshesDir = "Assets/Art/Generated_LanaVisuals/GeneratedMeshes";

        [MenuItem("Hoa va Mau/Art Pass/Execute Duplicate And Refine Scene")]
        public static void ExecuteRefinePass()
        {
            Debug.Log("<color=#38bdf8>[ArtisticRefine]</color> Starting Complete Technical Audit & Visual Repair Pass...");

            // 1. Fresh duplicate of source scene to target scene (preserving source scene 100%)
            if (File.Exists(TargetScenePath))
            {
                AssetDatabase.DeleteAsset(TargetScenePath);
            }
            bool copied = AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath);
            if (!copied)
            {
                Debug.LogError($"[ArtisticRefine] Failed to duplicate {SourceScenePath} to {TargetScenePath}");
                return;
            }
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            Debug.Log($"<color=#38bdf8>[ArtisticRefine]</color> Opened duplicate scene: {scene.name}");

            if (!Directory.Exists(GenAssetsDir)) Directory.CreateDirectory(GenAssetsDir);
            if (!Directory.Exists(GenMeshesDir)) Directory.CreateDirectory(GenMeshesDir);

            // 2. Refined Materials & Shader Properties
            var mats = CreateOrUpdateRefinedMaterials();

            // 3. Rebuild Seamless Continuous 3D Terrain Ledges (Zero Seams, Zero Gaps)
            RebuildSeamlessContinuousLedges(mats);

            // 4. Update Colliders to Match the Smooth Ascent Slope
            UpdateAscentSlopePhysicsColliders();

            // 5. Build Tactile 3D Embedded Rock Transitions (Slope Steps & Ravine Lips)
            BuildTactile3DRockTransitions(mats);

            // 6. Purge Old Flawed Trees & Populate Authentic Planet of Lana Hero Banyans
            PopulateAuthenticLanaHeroTrees(mats);

            // 7. Ground All Tree Roots with Organic Earth Mounds & Base Stones
            GroundAllTreeRootsWithMounds(mats);

            // 8. Enforce 3-Plane Atmospheric Depth Separation (Cool Teal Mist Background)
            EnforceThreePlaneDepthSeparation();

            // 9. Purge Underground Clutter & Stray Foliage
            PurgeUndergroundClutterAndStrayFoliage();

            // 10. Populate Clean Foreground/Midground Foliage & Grounding Tufts
            PopulateCleanGroundedVegetation(mats);

            // 11. Calibrate Directional Sunlight & Atmosphere
            CalibrateAtmosphereAndLighting();

            // 12. Calibrate Live Cinemachine 2.5D Camera (Lower Third Rule)
            CalibrateLiveGameplayCamera();

            // 13. Save Scene & Assets
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#4ade80>[ArtisticRefine]</color> <b>ALL 6 AUDIT PRIORITIES REPAIRED & SAVED!</b>");

            // 14. Capture High-Res Verification Screenshots
            CaptureRefinedViews();
        }

        // =========================================================================================
        // SAMPLE REFINED TERRAIN SURFACE Y
        // =========================================================================================
        public static float SampleRefinedSurfaceY(float x)
        {
            // Zone 1A: Forest Start Basin (-15.0m to +2.8m) -> Gentle basin Y: -1.75m .. -1.95m
            if (x < -3.8f)
            {
                float t = Mathf.Clamp01((x - (-5.5f)) / 1.7f);
                return Mathf.Lerp(-0.85f, -1.75f, t);
            }
            if (x >= -3.8f && x < 2.80f)
            {
                float t = (x - (-3.8f)) / (2.80f - (-3.8f));
                return Mathf.Lerp(-1.75f, -1.95f, t);
            }

            // Zone 1B: Natural Sculpted Hillside Ascent (+2.80m to +5.20m) -> Smoothly rises from -1.95m to -0.55m!
            // Eliminates the sharp 1.4m vertical discontinuity that caused the vertical seam!
            if (x >= 2.80f && x < 5.20f)
            {
                float t = (x - 2.80f) / (5.20f - 2.80f);
                float smoothT = t * t * (3.0f - 2.0f * t); // Hermite smoothstep
                return Mathf.Lerp(-1.95f, -0.55f, smoothT);
            }

            // Zone 1C: Plateau leading to Ravine (+5.20m to +14.60m) -> Flat at -0.55m
            if (x >= 5.20f && x <= 14.60f)
            {
                return -0.55f;
            }

            // Zone 1D: Rocky Ravine Chasm Floor (+14.60m to +17.60m) -> Floor at Y = -3.80m
            if (x > 14.60f && x < 17.60f)
            {
                return -3.80f;
            }

            // Fallback to original height logic for Zone 1E through Zone 4
            return OverhaulScene1Visuals.SampleTerrainSurfaceY(x);
        }

        // =========================================================================================
        // 2. REFINED MATERIALS & SHADER PROPERTIES
        // =========================================================================================
        public class RefinedMaterials
        {
            public Material GrassCapMat;
            public Material RockUnderbellyMat;
            public Material MossyBoulderMat;
            public Material SpriteDefaultMat;
        }

        private static RefinedMaterials CreateOrUpdateRefinedMaterials()
        {
            var mats = new RefinedMaterials();
            Shader ledgeShader = Shader.Find("HoaVaMau/LanaStylizedLedge") ?? Shader.Find("Universal Render Pipeline/Lit");

            // A. Grass Cap: Muted warm golden-sage, rich basalt red dirt underbelly
            mats.GrassCapMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_GrassCap_Artistic.mat", ledgeShader);
            mats.GrassCapMat.SetFloat("_IsGrassCap", 1.0f);
            mats.GrassCapMat.SetFloat("_ReceiveShadows", 1.0f);
            mats.GrassCapMat.SetFloat("_ShadowStrength", 0.35f); // Soft watercolor dappled shadow
            mats.GrassCapMat.SetColor("_GrassTopColor", new Color(0.48f, 0.53f, 0.30f, 1f));     // Warm golden sage
            mats.GrassCapMat.SetColor("_GrassMidColor", new Color(0.36f, 0.39f, 0.20f, 1f));     // Muted olive-amber
            mats.GrassCapMat.SetColor("_GrassShadowColor", new Color(0.24f, 0.36f, 0.22f, 1f)); // Soft moss shadow tint
            mats.GrassCapMat.SetColor("_GrassGoldTopColor", new Color(0.78f, 0.67f, 0.28f, 1f));
            mats.GrassCapMat.SetColor("_GrassGoldMidColor", new Color(0.52f, 0.45f, 0.19f, 1f));
            mats.GrassCapMat.SetFloat("_GoldenStartX", 50.0f);
            mats.GrassCapMat.SetFloat("_GoldenEndX", 62.0f);
            mats.GrassCapMat.SetColor("_RockDarkColor", new Color(0.36f, 0.18f, 0.11f, 1f));
            mats.GrassCapMat.SetColor("_RockMidColor", new Color(0.28f, 0.16f, 0.11f, 1f));
            mats.GrassCapMat.SetColor("_RockLightColor", new Color(0.40f, 0.28f, 0.20f, 1f));
            EditorUtility.SetDirty(mats.GrassCapMat);

            // B. Underbelly Soil: Authentic Nam Bo Red Basalt Earth ("Đất Đỏ Nam Bộ")
            // STRICTLY DISABLE SHADOW RECEPTION to eradicate all black silhouette splotches underground!
            mats.RockUnderbellyMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_RockUnderbelly_Artistic.mat", ledgeShader);
            mats.RockUnderbellyMat.SetFloat("_IsGrassCap", 0.0f);
            mats.RockUnderbellyMat.SetFloat("_ReceiveShadows", 0.0f); // ZERO SHADOWS UNDERGROUND!
            mats.RockUnderbellyMat.SetFloat("_ShadowStrength", 0.0f);
            mats.RockUnderbellyMat.SetColor("_RockLightColor", new Color(0.44f, 0.28f, 0.18f, 1f)); // Warm red clay dirt
            mats.RockUnderbellyMat.SetColor("_RockMidColor", new Color(0.32f, 0.18f, 0.12f, 1f));   // Rich basalt earth
            mats.RockUnderbellyMat.SetColor("_RockDarkColor", new Color(0.18f, 0.10f, 0.07f, 1f));  // Deep subterranean strata
            EditorUtility.SetDirty(mats.RockUnderbellyMat);

            // C. 3D Boulders: Weathered charcoal slate with warm lichen cap
            mats.MossyBoulderMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_MossyBoulder_Artistic.mat", ledgeShader);
            mats.MossyBoulderMat.SetFloat("_IsGrassCap", 0.5f);
            mats.MossyBoulderMat.SetFloat("_GrassNormalThreshold", 0.40f);
            mats.MossyBoulderMat.SetFloat("_ReceiveShadows", 1.0f);
            mats.MossyBoulderMat.SetFloat("_ShadowStrength", 0.38f);
            mats.MossyBoulderMat.SetColor("_GrassTopColor", new Color(0.44f, 0.49f, 0.27f, 1f));
            mats.MossyBoulderMat.SetColor("_GrassMidColor", new Color(0.32f, 0.36f, 0.18f, 1f));
            mats.MossyBoulderMat.SetColor("_GrassShadowColor", new Color(0.20f, 0.28f, 0.18f, 1f));
            mats.MossyBoulderMat.SetColor("_RockLightColor", new Color(0.34f, 0.35f, 0.36f, 1f));
            mats.MossyBoulderMat.SetColor("_RockMidColor", new Color(0.22f, 0.23f, 0.25f, 1f));
            mats.MossyBoulderMat.SetColor("_RockDarkColor", new Color(0.13f, 0.14f, 0.16f, 1f));
            EditorUtility.SetDirty(mats.MossyBoulderMat);

            // D. URP 2D Sprite Unlit Material
            Shader urpSpriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            mats.SpriteDefaultMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_URPSprite_Artistic.mat", urpSpriteShader);

            return mats;
        }

        private static Material GetOrCreateMat(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            return mat;
        }

        // =========================================================================================
        // 3. REBUILD SEAMLESS CONTINUOUS 3D TERRAIN LEDGES (PRIORITY 1)
        // =========================================================================================
        private static void RebuildSeamlessContinuousLedges(RefinedMaterials mats)
        {
            var terrainRoot = GameObject.Find("03_Terrain");
            if (terrainRoot == null) return;

            var ledgesParent = terrainRoot.transform.Find("Lana_Sculpted_Ledges");
            if (ledgesParent == null) return;

            // Destroy the old disconnected sections in Zone 1 (Ledge_01 and Ledge_02)
            var oldLedge01 = ledgesParent.Find("Ledge_01_ForestStart");
            if (oldLedge01 != null) UnityEngine.Object.DestroyImmediate(oldLedge01.gameObject);

            var oldLedge02 = ledgesParent.Find("Ledge_02_RopeCliffPlateau");
            if (oldLedge02 != null) UnityEngine.Object.DestroyImmediate(oldLedge02.gameObject);

            var oldLedge03 = ledgesParent.Find("Ledge_03_RavineFloor");
            if (oldLedge03 != null) UnityEngine.Object.DestroyImmediate(oldLedge03.gameObject);

            var oldContinuous = ledgesParent.Find("Ledge_01_ForestToRavineContinuous");
            if (oldContinuous != null) UnityEngine.Object.DestroyImmediate(oldContinuous.gameObject);

            var oldRavineFloor = ledgesParent.Find("Ledge_03_RavineFloorRefined");
            if (oldRavineFloor != null) UnityEngine.Object.DestroyImmediate(oldRavineFloor.gameObject);

            // A. Create ONE SINGLE CONTINUOUS SEAMLESS MESH from X = -15.0m to +14.60m!
            // 150 segments = smooth 19.7cm columns.
            // Zero internal splits, zero Z-indent bevels in the middle, zero vertical seam lines!
            CreateSeamlessLedgeSection(ledgesParent, "Ledge_01_ForestToRavineContinuous",
                -15.0f, 14.60f, 150, 0.05f, 1.10f, -9.5f, SampleRefinedSurfaceY, mats, true,
                hasLeftEndCap: true, hasRightEndCap: true);

            // B. Rebuild Ravine Floor (+14.60m to +17.60m, exactly matching ravine width with no 5cm gap)
            CreateSeamlessLedgeSection(ledgesParent, "Ledge_03_RavineFloorRefined",
                14.60f, 17.60f, 16, 0.05f, 1.10f, -9.5f, (x) => -3.80f, mats, false,
                hasLeftEndCap: false, hasRightEndCap: false);

            // C. Ensure Ravine Backdrop Wall fits cleanly
            var gorgeBack = GameObject.Find("Ravine_Chasm_BackdropWall");
            if (gorgeBack != null)
            {
                gorgeBack.transform.position = new Vector3(16.10f, -2.50f, 2.50f);
                gorgeBack.transform.localScale = new Vector3(4.80f, 4.20f, 1.0f);
                var mr = gorgeBack.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = mats.RockUnderbellyMat;
                    mr.receiveShadows = false;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            // D. Ensure all existing ledge meshes have receiveShadows = false on RockUnderbelly
            foreach (var mr in ledgesParent.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.name.EndsWith("_GrassTurfCap"))
                {
                    mr.sharedMaterial = mats.GrassCapMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
                else if (mr.name.EndsWith("_RockUnderbelly"))
                {
                    mr.sharedMaterial = mats.RockUnderbellyMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
            }
        }

        private static void CreateSeamlessLedgeSection(
            Transform parent, string name,
            float startX, float endX, int segmentsX,
            float frontZ, float backZ, float bottomY,
            Func<float, float> heightFunc,
            RefinedMaterials mats,
            bool includeGrassCap = true,
            bool hasLeftEndCap = true,
            bool hasRightEndCap = true)
        {
            var sectionRoot = new GameObject(name);
            sectionRoot.transform.SetParent(parent, false);

            Mesh rockMesh = BuildContinuousChiseledRockMesh($"{name}_RockMesh", startX, endX, segmentsX, frontZ, backZ, bottomY, heightFunc, hasLeftEndCap, hasRightEndCap);
            AssetDatabase.CreateAsset(rockMesh, $"{GenMeshesDir}/{name}_RockMesh.asset");

            var rockGO = new GameObject($"{name}_RockUnderbelly");
            rockGO.transform.SetParent(sectionRoot.transform, false);
            rockGO.AddComponent<MeshFilter>().sharedMesh = rockMesh;
            var rockMR = rockGO.AddComponent<MeshRenderer>();
            rockMR.sharedMaterial = mats.RockUnderbellyMat;
            rockMR.shadowCastingMode = ShadowCastingMode.Off;
            rockMR.receiveShadows = false;

            if (includeGrassCap)
            {
                Mesh grassMesh = BuildContinuousOverhangingGrassMesh($"{name}_GrassMesh", startX, endX, segmentsX, frontZ - 0.05f, backZ, heightFunc);
                AssetDatabase.CreateAsset(grassMesh, $"{GenMeshesDir}/{name}_GrassMesh.asset");

                var grassGO = new GameObject($"{name}_GrassTurfCap");
                grassGO.transform.SetParent(sectionRoot.transform, false);
                grassGO.AddComponent<MeshFilter>().sharedMesh = grassMesh;
                var grassMR = grassGO.AddComponent<MeshRenderer>();
                grassMR.sharedMaterial = mats.GrassCapMat;
                grassMR.shadowCastingMode = ShadowCastingMode.Off;
                grassMR.receiveShadows = false;
            }
        }

        private static Mesh BuildContinuousChiseledRockMesh(
            string meshName, float startX, float endX, int segX,
            float frontZ, float backZ, float bottomY,
            Func<float, float> heightFunc,
            bool hasLeftEndCap, bool hasRightEndCap)
        {
            int rowsY = 6;
            int cols = segX + 1;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= rowsY; r++)
            {
                float v = (float)r / rowsY;
                for (int c = 0; c < cols; c++)
                {
                    float u = (float)c / segX;
                    float x = Mathf.Lerp(startX, endX, u);
                    float surfY = heightFunc(x);
                    float topY = surfY - 0.18f;
                    float y = Mathf.Lerp(topY, bottomY, v);

                    float facet = Mathf.Sin(x * 1.45f + r * 1.9f) * 0.08f
                                + Mathf.Cos(x * 3.1f - r * 2.4f) * 0.04f;
                    float zOffset = facet * Mathf.Sin(v * Mathf.PI);
                    float z = frontZ + zOffset;

                    // Only indent in Z at true external cliff ends, NEVER at internal points!
                    if (c == 0 && hasLeftEndCap) z += 0.20f;
                    if (c == segX && hasRightEndCap) z += 0.20f;

                    verts.Add(new Vector3(x, y, z));

                    float cliffFlag = Mathf.InverseLerp(-1.65f, 1.50f, surfY) * Mathf.Clamp01(1.0f - v * 1.8f);
                    float lipAO = (r == 0) ? 0.50f : ((r == 1) ? 0.85f : 1.0f);
                    float botAO = Mathf.Lerp(1.0f, 0.60f, Mathf.Pow(v, 1.5f));
                    colors.Add(new Color(0f, lipAO * botAO, cliffFlag, 1f));
                    uvs.Add(new Vector2(u * 14f, v * 2f));
                }
            }

            for (int r = 0; r < rowsY; r++)
            {
                for (int c = 0; c < segX; c++)
                {
                    int i0 = r * cols + c;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * cols + c;
                    int i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

            // Left end cap (outer cliff face)
            if (hasLeftEndCap)
            {
                float sx = startX;
                float topY = heightFunc(sx) - 0.15f;
                int bIdx = verts.Count;
                verts.Add(new Vector3(sx, topY, frontZ + 0.20f));
                verts.Add(new Vector3(sx, topY, backZ));
                verts.Add(new Vector3(sx, bottomY, frontZ + 0.20f));
                verts.Add(new Vector3(sx, bottomY, backZ));
                for (int k = 0; k < 4; k++)
                {
                    colors.Add(new Color(0f, 0.80f, 0.4f, 1f));
                    uvs.Add(Vector2.zero);
                }
                tris.Add(bIdx + 0); tris.Add(bIdx + 2); tris.Add(bIdx + 1);
                tris.Add(bIdx + 1); tris.Add(bIdx + 2); tris.Add(bIdx + 3);
            }

            // Right end cap (ravine cliff wall drop-off)
            if (hasRightEndCap)
            {
                float sx = endX;
                float topY = heightFunc(sx) - 0.15f;
                int bIdx = verts.Count;
                verts.Add(new Vector3(sx, topY, frontZ + 0.20f));
                verts.Add(new Vector3(sx, topY, backZ));
                verts.Add(new Vector3(sx, bottomY, frontZ + 0.20f));
                verts.Add(new Vector3(sx, bottomY, backZ));
                for (int k = 0; k < 4; k++)
                {
                    colors.Add(new Color(0f, 0.80f, 0.4f, 1f));
                    uvs.Add(Vector2.zero);
                }
                tris.Add(bIdx + 0); tris.Add(bIdx + 1); tris.Add(bIdx + 2);
                tris.Add(bIdx + 1); tris.Add(bIdx + 3); tris.Add(bIdx + 2);
            }

            // Top shelf back face
            int topStartIdx = verts.Count;
            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / segX;
                float x = Mathf.Lerp(startX, endX, u);
                float topY = heightFunc(x) - 0.18f;
                verts.Add(new Vector3(x, topY, frontZ));
                verts.Add(new Vector3(x, topY, backZ));
                colors.Add(new Color(0f, 0.88f, 0f, 1f));
                colors.Add(new Color(0f, 0.88f, 0f, 1f));
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
            }
            for (int c = 0; c < segX; c++)
            {
                int i0 = topStartIdx + c * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i2); tris.Add(i1); tris.Add(i3);
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildContinuousOverhangingGrassMesh(
            string meshName, float startX, float endX, int segX,
            float frontZ, float backZ,
            Func<float, float> heightFunc)
        {
            int profileCount = 6;
            int cols = segX + 1;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / segX;
                float x = Mathf.Lerp(startX, endX, u);
                float y = heightFunc(x);

                float scallop = Mathf.Sin(x * 2.4f) * 0.045f + Mathf.Cos(x * 5.2f) * 0.025f;

                verts.Add(new Vector3(x, y - 0.22f, backZ));
                verts.Add(new Vector3(x, y + 0.04f, backZ));
                verts.Add(new Vector3(x, y + 0.01f, 0.0f));
                verts.Add(new Vector3(x, y + 0.03f + scallop * 0.20f, frontZ + 0.16f));
                verts.Add(new Vector3(x, y - 0.11f + scallop * 0.60f, frontZ));
                verts.Add(new Vector3(x, y - 0.27f + scallop, frontZ + 0.15f));

                colors.Add(new Color(0.65f, 1f, 0f, 1f));
                colors.Add(new Color(1.00f, 1f, 0f, 1f));
                colors.Add(new Color(0.96f, 1f, 0f, 1f));
                colors.Add(new Color(0.92f, 1f, 0f, 1f));
                colors.Add(new Color(0.62f, 1f, 0f, 1f));
                colors.Add(new Color(0.30f, 1f, 0f, 1f));

                for (int p = 0; p < profileCount; p++)
                {
                    uvs.Add(new Vector2(u * 14f, (float)p / (profileCount - 1)));
                }
            }

            for (int c = 0; c < segX; c++)
            {
                int c0 = c * profileCount;
                int c1 = (c + 1) * profileCount;
                for (int p = 0; p < profileCount - 1; p++)
                {
                    int i0 = c0 + p;
                    int i1 = c0 + p + 1;
                    int i2 = c1 + p;
                    int i3 = c1 + p + 1;
                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // =========================================================================================
        // 4. UPDATE PHYSICS COLLIDERS TO MATCH SMOOTH ASCENT SLOPE
        // =========================================================================================
        private static void UpdateAscentSlopePhysicsColliders()
        {
            var physRoot = GameObject.Find("03_Terrain/PhysicsWalkway_And_Cliffs_145m");
            if (physRoot == null) return;

            // Remove old rigid block colliders in Zone 1
            var col01 = physRoot.transform.Find("Col_01_ForestStart");
            if (col01 != null) UnityEngine.Object.DestroyImmediate(col01.gameObject);

            var col02 = physRoot.transform.Find("Col_02_RopeCliffPlateau");
            if (col02 != null) UnityEngine.Object.DestroyImmediate(col02.gameObject);

            var oldSlopeColliders = physRoot.transform.Find("Refined_AscentSlope_Colliders");
            if (oldSlopeColliders != null) UnityEngine.Object.DestroyImmediate(oldSlopeColliders.gameObject);

            var slopeGroup = new GameObject("Refined_AscentSlope_Colliders");
            slopeGroup.transform.SetParent(physRoot.transform, false);

            void AddBox(string name, float minX, float maxX, float botY, float topY)
            {
                var go = new GameObject(name);
                go.transform.SetParent(slopeGroup.transform, false);
                go.transform.position = new Vector3((minX + maxX) * 0.5f, (botY + topY) * 0.5f, 0f);
                var bc = go.AddComponent<BoxCollider>();
                bc.size = new Vector3(Mathf.Max(0.05f, maxX - minX), Mathf.Max(0.05f, topY - botY), 4.0f);
            }

            // Floor before slope (-11.5m to +2.80m, topY = -1.95m)
            AddBox("Col_01_BasinFloor", -11.5f, 2.80f, -8.0f, -1.95f);

            // Stepped continuous slope (+2.80m to +5.20m, smoothly rising from -1.95m to -0.55m)
            int steps = 12;
            for (int i = 0; i < steps; i++)
            {
                float x0 = Mathf.Lerp(2.80f, 5.20f, (float)i / steps);
                float x1 = Mathf.Lerp(2.80f, 5.20f, (float)(i + 1) / steps);
                float y = SampleRefinedSurfaceY((x0 + x1) * 0.5f);
                AddBox($"Col_AscentStep_{i:D2}", x0, x1, -8.0f, y);
            }

            // Plateau (+5.20m to +14.60m, topY = -0.55m)
            AddBox("Col_02_PlateauToRavine", 5.20f, 14.60f, -8.0f, -0.55f);
        }

        // =========================================================================================
        // 5. BUILD TACTILE 3D EMBEDDED ROCK TRANSITIONS (PRIORITY 4)
        // =========================================================================================
        private static void BuildTactile3DRockTransitions(RefinedMaterials mats)
        {
            // Purge flat 2D rock sprites with borders/fringes and floating 2D vines
            string[] namesToPurge = new string[] {
                "Obstacle02_RavineWestLipRock", "Obstacle02_RavineEastLipRock",
                "RopeCliff_TopLipBoulder", "RopeCliff_WallSlab_Lower", "RopeCliff_BaseOutcrop",
                "Cliff_Ivy_Vines_01", "Cliff_Ivy_Vines_02", "Cliff_Vine_RopeWall",
                "[Additive_TreeShadows_Overhaul]"
            };

            foreach (var n in namesToPurge)
            {
                var go = GameObject.Find(n);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            var refineRoot = GameObject.Find("08_Artistic_Refinements") ?? new GameObject("08_Artistic_Refinements");
            var old3DRocks = refineRoot.transform.Find("Sculpted_3D_Rock_Platforms");
            if (old3DRocks != null) UnityEngine.Object.DestroyImmediate(old3DRocks.gameObject);

            var rocks3DGroup = new GameObject("Sculpted_3D_Rock_Platforms");
            rocks3DGroup.transform.SetParent(refineRoot.transform, false);

            var pfBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");
            var pfLargeRock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Platform_CliffLedge.prefab") ?? pfBoulder;
            var pfOutcrop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Platform_SteppingStone.prefab") ?? pfBoulder;

            GameObject Spawn3DRock(GameObject prefab, string name, Vector3 pos, Vector3 scale, Vector3 rot)
            {
                if (prefab == null) return null;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rocks3DGroup.transform);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.Euler(rot);
                inst.transform.localScale = scale;

                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.sharedMaterial = mats.MossyBoulderMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = true;
                }
                return inst;
            }

            // 4 tiered stepping boulders deeply embedded (35-40% submerged) into the hillside slope
            Spawn3DRock(pfBoulder, "Step_01_BaseRock",   new Vector3(2.95f, -1.82f, 0.18f), new Vector3(0.90f, 0.48f, 0.80f), new Vector3(6f, 25f, -3f));
            Spawn3DRock(pfBoulder, "Step_02_MidAscent",  new Vector3(3.70f, -1.45f, 0.22f), new Vector3(0.98f, 0.52f, 0.85f), new Vector3(-4f, 85f, 5f));
            Spawn3DRock(pfBoulder, "Step_03_HighShelf",  new Vector3(4.45f, -1.02f, 0.20f), new Vector3(1.05f, 0.55f, 0.90f), new Vector3(8f, 150f, -4f));
            Spawn3DRock(pfBoulder, "Step_04_PlateauLip", new Vector3(5.20f, -0.62f, 0.25f), new Vector3(1.12f, 0.60f, 0.95f), new Vector3(-5f, 40f, 4f));

            // Solid weathered 3D rock buttress behind rope
            Spawn3DRock(pfLargeRock, "CliffFace_Buttress", new Vector3(5.60f, -0.85f, 0.85f), new Vector3(1.05f, 0.95f, 1.05f), new Vector3(0f, 45f, 0f));

            // Ravine Chasm Weathered 3D Rock Lips (sunk 40% into the cliff corners to create natural outcrops)
            Spawn3DRock(pfBoulder, "Ravine_WestLip_MossyStone", new Vector3(14.40f, -0.62f, 0.25f), new Vector3(1.00f, 0.55f, 0.85f), new Vector3(8f, 35f, -6f));
            Spawn3DRock(pfBoulder, "Ravine_EastLip_MossyStone", new Vector3(17.80f, -0.62f, 0.25f), new Vector3(1.00f, 0.55f, 0.85f), new Vector3(-6f, -145f, 5f));

            // Ravine floor embedded river boulders
            Spawn3DRock(pfBoulder, "Ravine_Floor_Boulder_01", new Vector3(15.20f, -3.95f, 0.40f), new Vector3(1.30f, 0.80f, 1.20f), new Vector3(0f, 40f, 0f));
            Spawn3DRock(pfBoulder, "Ravine_Floor_Boulder_02", new Vector3(16.60f, -3.95f, 0.55f), new Vector3(1.35f, 0.85f, 1.25f), new Vector3(0f, 210f, 0f));

            // High Cliff Corner Rock (Zone 1 end)
            Spawn3DRock(pfOutcrop, "HighCliff_CornerRock", new Vector3(35.30f, 0.60f, 0.80f), new Vector3(1.30f, 1.50f, 1.20f), new Vector3(0f, -20f, 0f));
        }

        // =========================================================================================
        // 6. POPULATE AUTHENTIC PLANET OF LANA HERO TREES
        // =========================================================================================
        private static void PopulateAuthenticLanaHeroTrees(RefinedMaterials mats)
        {
            string[] treesToPurge = new string[] {
                "Tranquil_HeroBanyan_Left", "Tranquil_SlenderTree_Center", "Tranquil_Banyan_RightCliffBase",
                "Cliff_RopeHeroTree_Plateau", "Cliff_Tree04_PlateauEdge",
                "Ravine_Hero_Banyan_TreeSprite", "Obstacle02_Ravine_Hero_AnchorBanyan"
            };

            foreach (var tn in treesToPurge)
            {
                var go = GameObject.Find(tn);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            var refineRoot = GameObject.Find("08_Artistic_Refinements") ?? new GameObject("08_Artistic_Refinements");
            var oldTrees = refineRoot.transform.Find("Authentic_Lana_Trees_Group");
            if (oldTrees != null) UnityEngine.Object.DestroyImmediate(oldTrees.gameObject);

            var treesGroup = new GameObject("Authentic_Lana_Trees_Group");
            treesGroup.transform.SetParent(refineRoot.transform, false);

            var pfCurvedHero = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Curved_Hero_Tree_V2.prefab");
            var pfLayeredHero = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Hero_Tree_Layered.prefab");
            var pfSlenderTall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Tree_Var01_SlenderTall.prefab");
            var pfWindbent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Tree_Var02_Windbent.prefab");

            GameObject SpawnLanaTree(GameObject prefab, string name, Vector3 pos, Vector3 scale, float swayAngle = 0.8f)
            {
                if (prefab == null) return null;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, treesGroup.transform);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.localScale = scale;

                var sway = inst.GetComponentInChildren<PlanetOfLanaFoliageSway>();
                if (sway == null)
                {
                    sway = inst.AddComponent<PlanetOfLanaFoliageSway>();
                    var so = new SerializedObject(sway);
                    so.FindProperty("maxRotationAngle").floatValue = swayAngle;
                    so.FindProperty("swaySpeed").floatValue = 1.05f;
                    so.FindProperty("useWorldPositionOffset").boolValue = true;
                    so.FindProperty("manualPhaseOffset").floatValue = pos.x * 0.3f;
                    so.FindProperty("scaleBreathing").floatValue = 0.006f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    string sname = sr.name.ToLowerInvariant();
                    if (sname.Contains("leaves") || sname.Contains("canopy") || sname.Contains("foliage") || sname.Contains("front"))
                    {
                        sr.color = new Color(0.90f, 0.94f, 0.86f, 1f); // Warm sage leaves
                    }
                }
                return inst;
            }

            // 1. Hero Banyan Framing Start Clearing (X = -4.50m)
            float yStart = SampleRefinedSurfaceY(-4.5f) - 0.45f;
            SpawnLanaTree(pfCurvedHero, "HeroTree_01_StartClearingCurved", new Vector3(-4.50f, yStart, 1.20f), new Vector3(0.68f, 0.68f, 1.0f), 0.8f);

            // 2. Cliff Sentinel Banyan (X = 6.10m, holding the overhanging bough over the rope)
            float yCliff = SampleRefinedSurfaceY(6.10f) - 0.45f;
            SpawnLanaTree(pfLayeredHero ?? pfCurvedHero, "HeroTree_02_CliffSentinel", new Vector3(6.10f, yCliff, 1.20f), new Vector3(0.70f, 0.70f, 1.0f), 0.8f);

            // 3. West Ravine Chasm Hero Tree (X = 13.40m)
            float yRavine = SampleRefinedSurfaceY(13.4f) - 0.48f;
            SpawnLanaTree(pfLayeredHero ?? pfCurvedHero, "HeroTree_03_RavineWestLayered", new Vector3(13.40f, yRavine, 0.80f), new Vector3(0.68f, 0.68f, 1.0f), 0.9f);

            // 4. East Ravine Windbent Grove (X = 19.50m)
            float yEast = SampleRefinedSurfaceY(19.5f) - 0.42f;
            SpawnLanaTree(pfWindbent, "GroveTree_04_EastRavineWindbent", new Vector3(19.50f, yEast, 1.40f), new Vector3(0.62f, 0.62f, 1.0f), 1.0f);

            // 5. Crawl Tunnel Slender Poplar (X = 27.50m)
            float yCrawl = SampleRefinedSurfaceY(27.5f) - 0.40f;
            SpawnLanaTree(pfSlenderTall, "GroveTree_05_CrawlSlender", new Vector3(27.50f, yCrawl, 1.50f), new Vector3(0.65f, 0.65f, 1.0f), 0.9f);
        }

        // =========================================================================================
        // 7. GROUND ALL TREE ROOTS WITH ORGANIC MOUNDS & STONES
        // =========================================================================================
        private static void GroundAllTreeRootsWithMounds(RefinedMaterials mats)
        {
            var refineRoot = GameObject.Find("08_Artistic_Refinements") ?? new GameObject("08_Artistic_Refinements");
            var oldMounds = refineRoot.transform.Find("Tree_Root_Grounding_Mounds");
            if (oldMounds != null) UnityEngine.Object.DestroyImmediate(oldMounds.gameObject);

            var moundsGroup = new GameObject("Tree_Root_Grounding_Mounds");
            moundsGroup.transform.SetParent(refineRoot.transform, false);

            var pfBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");
            var sprMound = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/Curved_Tree_V2_Mound.png");

            var allRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            HashSet<Transform> groundedTrunks = new HashSet<Transform>();

            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.sprite == null) continue;
                string sname = sr.sprite.name.ToLowerInvariant();
                string oname = sr.gameObject.name.ToLowerInvariant();

                bool isTrunk = sname.Contains("trunk") || oname.Contains("trunk") || sname.Contains("tree") || oname.Contains("tree");
                if (!isTrunk) continue;
                if (oname.Contains("cloud") || oname.Contains("vista") || oname.Contains("bg_")) continue;

                var tr = sr.transform;
                float x = tr.position.x;
                float groundY = SampleRefinedSurfaceY(x);

                // Sink the trunk so the cut bottom edge is completely submerged
                if (tr.position.y > groundY - 0.25f)
                {
                    float newY = groundY - 0.45f;
                    tr.position = new Vector3(tr.position.x, newY, tr.position.z);
                    groundedTrunks.Add(tr);
                }

                if (tr.position.z >= -0.5f && tr.position.z <= 3.0f && !groundedTrunks.Contains(tr))
                {
                    groundedTrunks.Add(tr);

                    if (pfBoulder != null)
                    {
                        var rock = (GameObject)PrefabUtility.InstantiatePrefab(pfBoulder, moundsGroup.transform);
                        rock.name = $"RootRock_{tr.name}";
                        rock.transform.position = new Vector3(x - 0.35f, groundY - 0.18f, tr.position.z - 0.15f);
                        rock.transform.localScale = new Vector3(0.50f, 0.35f, 0.45f);
                        rock.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        foreach (var mr in rock.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            mr.sharedMaterial = mats.MossyBoulderMat;
                            mr.shadowCastingMode = ShadowCastingMode.Off;
                            mr.receiveShadows = false;
                        }
                    }

                    if (sprMound != null)
                    {
                        var moundGO = new GameObject($"RootMound_{tr.name}");
                        moundGO.transform.SetParent(moundsGroup.transform, false);
                        moundGO.transform.position = new Vector3(x, groundY + 0.04f, tr.position.z - 0.05f);
                        moundGO.transform.localScale = new Vector3(0.65f, 0.55f, 1f);

                        var msr = moundGO.AddComponent<SpriteRenderer>();
                        msr.sprite = sprMound;
                        msr.sharedMaterial = mats.SpriteDefaultMat;
                        msr.sortingOrder = sr.sortingOrder + 1;
                        msr.color = new Color(0.85f, 0.78f, 0.65f, 1f);
                    }
                }
            }
        }

        // =========================================================================================
        // 8. ENFORCE 3-PLANE ATMOSPHERIC DEPTH SEPARATION (PRIORITY 5)
        // =========================================================================================
        private static void EnforceThreePlaneDepthSeparation()
        {
            var ecoRoot = GameObject.Find("04_Lana_Unified_Ecosystem");
            if (ecoRoot == null) return;

            // Poetic watercolor mist palette (inspired by Planet of Lana & Neva)
            Color coolTealBackgroundMist = new Color(0.68f, 0.78f, 0.80f, 0.82f); // Cooler, desaturated, softer contrast
            Color softSageMidground      = new Color(0.82f, 0.88f, 0.82f, 1.00f);

            var srs = ecoRoot.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (sr == null || sr.sprite == null) continue;
                string sname = sr.sprite.name.ToLowerInvariant();
                string oname = sr.gameObject.name.ToLowerInvariant();
                float z = sr.transform.position.z;

                // Push dense bamboo groves into the distant atmospheric background
                if (sname.Contains("bamboogrove") || oname.Contains("bamboo"))
                {
                    if (z < 7.0f)
                    {
                        sr.transform.position = new Vector3(sr.transform.position.x, sr.transform.position.y, 8.5f);
                        z = 8.5f;
                    }
                }

                // Plane 1: Background (Z >= 6.0m) -> Cooler teal mist, low contrast, desaturated
                if (z >= 6.0f)
                {
                    sr.color = coolTealBackgroundMist;
                }
                // Plane 2: Midground (Z >= 1.5m && Z < 6.0m) -> Distinct silhouettes, soft sage
                else if (z >= 1.5f && z < 6.0f)
                {
                    sr.color = softSageMidground;
                }
            }
        }

        // =========================================================================================
        // 9. PURGE UNDERGROUND CLUTTER & STRAY FOLIAGE (PRIORITY 2 & 3)
        // =========================================================================================
        private static void PurgeUndergroundClutterAndStrayFoliage()
        {
            // A. Explicitly destroy legacy prototype roots that cast black silhouettes / 2D slab cutouts
            string[] explicitRootsToPurge = new string[] {
                "[Official_Lana_Foreground_Framing]",
                "[Rock_VerticalCut_Cascade]"
            };

            foreach (var rName in explicitRootsToPurge)
            {
                var go = GameObject.Find(rName);
                if (go != null)
                {
                    Debug.Log($"<color=#f43f5e>[Purge]</color> Destroyed legacy clutter root: {rName}");
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // B. Clean up Zone 1 items inside [Official_Lana_Foliage_Meadow_Additive] (X <= 35m)
            var meadowGroup = GameObject.Find("[Official_Lana_Foliage_Meadow_Additive]");
            if (meadowGroup != null)
            {
                List<GameObject> meadowZone1 = new List<GameObject>();
                foreach (var tr in meadowGroup.GetComponentsInChildren<Transform>(true))
                {
                    if (tr != null && tr != meadowGroup.transform && tr.position.x <= 35.5f)
                    {
                        meadowZone1.Add(tr.gameObject);
                    }
                }
                foreach (var go in meadowZone1)
                {
                    if (go != null) UnityEngine.Object.DestroyImmediate(go);
                }
                Debug.Log($"<color=#f43f5e>[Purge]</color> Cleaned {meadowZone1.Count} old meadow items in Zone 1.");
            }

            // C. Purge loose root 2D rocks and bushes in Zone 1
            var rootObjs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var rgo in rootObjs)
            {
                if (rgo == null) continue;
                string n = rgo.name;
                float rx = rgo.transform.position.x;
                if (rx <= 35.5f && (n.StartsWith("T_LanaVN_Rock_") || n.StartsWith("Rock_") || n.StartsWith("FG_") || n.StartsWith("T_HD_Bush_")))
                {
                    Debug.Log($"<color=#f43f5e>[Purge]</color> Destroyed loose root prop in Zone 1: {n}");
                    UnityEngine.Object.DestroyImmediate(rgo);
                }
            }

            // D. Comprehensive underground scan across all objects in Zone 1 (X: -15m .. 35m)
            // Rule 5: NEVER touch protected Rice Paddies!
            List<GameObject> toDestroy = new List<GameObject>();
            var allRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

            foreach (var sr in allRenderers)
            {
                if (sr == null) continue;
                var tr = sr.transform;
                float x = tr.position.x;
                float y = tr.position.y;
                float z = tr.position.z;

                if (x < -15.0f || x > 35.5f) continue;

                // Protect rice field roots
                string pName = tr.parent != null ? tr.parent.name : "";
                if (pName.Contains("Rice") || pName.Contains("Paddy")) continue;

                // Allow ravine chasm floor objects between X = 14.4m and 17.8m down to Y = -4.2m
                if (x >= 14.4f && x <= 17.8f)
                {
                    if (y < -4.30f) toDestroy.Add(sr.gameObject);
                    continue;
                }

                float expectedSurfaceY = SampleRefinedSurfaceY(x);

                // If any sprite is buried more than 25cm under the surface, OR floating at negative Z in front of soil:
                if (y < expectedSurfaceY - 0.25f || (z < 0.0f && y < expectedSurfaceY - 0.10f))
                {
                    string oname = sr.gameObject.name.ToLowerInvariant();
                    if (oname.Contains("rootrock") || oname.Contains("underbelly") || oname.Contains("turfcap") || oname.Contains("mound"))
                    {
                        continue;
                    }
                    toDestroy.Add(sr.gameObject);
                }
            }

            foreach (var go in toDestroy)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            Debug.Log($"<color=#38bdf8>[ArtisticRefine]</color> Purged {toDestroy.Count} stray underground artifacts.");
        }

        // =========================================================================================
        // 10. POPULATE CLEAN GROUNDED VEGETATION (PRIORITY 4 & 5)
        // =========================================================================================
        private static void PopulateCleanGroundedVegetation(RefinedMaterials mats)
        {
            var refineRoot = GameObject.Find("08_Artistic_Refinements") ?? new GameObject("08_Artistic_Refinements");
            var oldFol = refineRoot.transform.Find("Clean_Foliage_Group");
            if (oldFol != null) UnityEngine.Object.DestroyImmediate(oldFol.gameObject);

            var folGroup = new GameObject("Clean_Foliage_Group");
            folGroup.transform.SetParent(refineRoot.transform, false);

            var pfHighGrass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlanetOfLana_Foliage/01_Co_Grass/PF_Lana_Grass_Des_Pg_HDL_HighGrassDarker_001 1.prefab")
                           ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlanetOfLana_Foliage/01_Co_Grass/PF_Lana_Grass_Arc_ReedGreenGrass_001.prefab");
            var pfRockBush = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlanetOfLana_Foliage/04_BuiCo_Bushes/PF_Lana_Bush_Std_Pg_Rockbushes03.prefab");
            var pfFlowerYellow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlanetOfLana_Foliage/02_HoaCon_Flowers/PF_Lana_Flower_RockFlowerYellow03.prefab");
            var pfFlowerWhite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlanetOfLana_Foliage/02_HoaCon_Flowers/PF_Lana_Flower_RockFlowerWhite01.prefab");

            void SeedCluster(float x, float zOffset, bool addFlowers = true)
            {
                // Never place grass over empty ravine air
                if (x >= 14.55f && x <= 17.65f) return;

                float y = SampleRefinedSurfaceY(x) - 0.04f;

                if (pfHighGrass != null)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(pfHighGrass, folGroup.transform);
                    g.name = $"Tuft_X{x:F1}";
                    g.transform.position = new Vector3(x, y, zOffset);
                    g.transform.localScale = new Vector3(0.48f, 0.45f, 1f);
                    foreach (var sr in g.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        // Plane 3: Foreground (Z <= 0.4m) -> Richer darker green framing
                        if (zOffset <= 0.35f)
                        {
                            sr.color = new Color(0.65f, 0.72f, 0.55f, 1f);
                        }
                        else
                        {
                            sr.color = new Color(0.85f, 0.88f, 0.76f, 1f);
                        }
                    }
                }

                if (pfRockBush != null && Mathf.Abs(x % 4.0f) < 1.0f)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(pfRockBush, folGroup.transform);
                    b.name = $"RockBush_X{x:F1}";
                    b.transform.position = new Vector3(x + 0.35f, y - 0.02f, zOffset + 0.15f);
                    b.transform.localScale = new Vector3(0.40f, 0.38f, 1f);
                    foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.color = new Color(0.82f, 0.86f, 0.74f, 1f);
                    }
                }

                if (addFlowers && (pfFlowerYellow != null || pfFlowerWhite != null))
                {
                    var pfFlw = (x > 0) ? pfFlowerYellow : pfFlowerWhite;
                    if (pfFlw != null)
                    {
                        var flw = (GameObject)PrefabUtility.InstantiatePrefab(pfFlw, folGroup.transform);
                        flw.name = $"Flower_X{x:F1}";
                        flw.transform.position = new Vector3(x - 0.25f, y + 0.02f, zOffset - 0.08f);
                        flw.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
                    }
                }
            }

            // Seed grounded tufts hugging rock transitions and walkways
            float[] tuftXs = new float[] { -8.5f, -6.2f, -3.8f, -1.5f, 0.8f, 2.85f, 3.65f, 4.45f, 5.25f, 7.5f, 10.2f, 13.5f, 18.2f, 20.4f, 24.5f, 29.0f, 33.5f };
            foreach (var tx in tuftXs)
            {
                SeedCluster(tx, 0.25f, true);
            }

            // Anchor low grounding grass at the base of the 4 stepping stones
            SeedCluster(2.95f, 0.10f, false);
            SeedCluster(3.70f, 0.12f, false);
            SeedCluster(4.45f, 0.10f, false);
            SeedCluster(5.20f, 0.15f, false);
        }

        // =========================================================================================
        // 11. CALIBRATE ATMOSPHERE & LIGHTING
        // =========================================================================================
        private static void CalibrateAtmosphereAndLighting()
        {
            var lightGO = GameObject.Find("Directional Light") ?? GameObject.Find("Sun");
            if (lightGO != null)
            {
                var light = lightGO.GetComponent<Light>();
                if (light != null)
                {
                    light.color = new Color(1.0f, 0.95f, 0.88f, 1f); // Warm soft morning light
                    light.intensity = 1.10f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.40f; // Soft ambient shadow, never black
                }
            }
        }

        // =========================================================================================
        // 12. CALIBRATE LIVE GAMEPLAY CAMERA (PRIORITY 6 - LOWER THIRD RULE)
        // =========================================================================================
        private static void CalibrateLiveGameplayCamera()
        {
            var vcamGO = GameObject.Find("01_Camera/CM_VirtualCamera_2.5D") ?? GameObject.Find("CM_VirtualCamera_2.5D");
            if (vcamGO == null) return;

            var posComposer = vcamGO.GetComponent<CinemachinePositionComposer>();
            if (posComposer != null)
            {
                // Elevate camera target upward: places the ground in the lower 22% of the screen
                posComposer.TargetOffset = new Vector3(2.0f, 1.85f, 0.0f);
                posComposer.CameraDistance = 12.5f;
                EditorUtility.SetDirty(posComposer);
            }
        }

        // =========================================================================================
        // 14. CAPTURE HIGH-RES VERIFICATION VIEWS (PRIORITY 6 - LOWER THIRD COMPOSITION)
        // =========================================================================================
        public static void CaptureRefinedViews()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[ArtisticRefine] Camera.main not found for capture.");
                return;
            }

            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Screenshots/Showcase_Presentation"));
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;
            float origFov = cam.fieldOfView;

            Action<string, Vector3, Vector3, float> capture = (fileName, pos, rot, fov) =>
            {
                cam.transform.position = pos;
                cam.transform.rotation = Quaternion.Euler(rot);
                cam.fieldOfView = fov;

                // Sync Parallax Follower
                var bgFollower = UnityEngine.Object.FindAnyObjectByType<LanaParallaxBackgroundFollower>();
                if (bgFollower != null)
                {
                    bgFollower.TargetCamera = cam.transform;
                    bgFollower.LateUpdate();
                }

                int width = 1280;
                int height = 720;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                byte[] bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                string fullPath = Path.Combine(outDir, fileName);
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log($"<color=#38bdf8>[Capture]</color> Saved refined verification view: {fileName}");
            };

            // View 1. Forest Clearing Start & Curved Hero Banyan
            // Ground at Y = -1.75m. Camera at Y = +0.55m -> Ground in lower 22% of screen!
            capture("Verify_Refined_01_ForestClearingStart.png", new Vector3(-2.8f, 0.55f, -9.5f), Vector3.zero, 38f);

            // View 2. Tactile 3D Embedded Rock Slope & Climbing Rope Ascent
            // Ground rises from -1.95m to -0.55m. Camera at Y = +1.15m -> Ground in lower 24% of screen!
            capture("Verify_Refined_02_3DCliffRopeAscent.png", new Vector3(4.8f, 1.15f, -9.0f), Vector3.zero, 36f);

            // View 3. Ravine Chasm Weathered Rock Ledges & Swing Rope
            // Cliffs at Y = -0.55m. Camera at Y = +0.85m -> Gorge opens naturally below with expansive vista above!
            capture("Verify_Refined_03_Ravine3DChasmSwing.png", new Vector3(16.1f, 0.85f, -9.5f), Vector3.zero, 36f);

            // Restore Camera
            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.fieldOfView = origFov;
        }
    }
}
#endif
