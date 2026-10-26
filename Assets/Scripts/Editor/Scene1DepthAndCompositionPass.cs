#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Level Design Polish Pass: Fixes ground edge flatness, creates foreground framing (repoussoir),
    /// adds atmospheric depth staging to trees, and tunes URP Post-Processing Volume.
    /// </summary>
    public static class Scene1DepthAndCompositionPass
    {
        private const string ScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string PackDir = "Assets/Prefabs/Environment/Lana_VN_Pack";

        [MenuItem("Hoa va Mau/Level Design/Apply Depth & Composition Staging Pass")]
        public static void ApplyStagingPass()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log("<color=#38bdf8>[StagingPass]</color> Applying Depth & Composition Staging to Scene 1...");

            // 1. Configure URP Global Volume for painterly atmosphere
            TuneAtmosphericPostProcessing();

            // 2. Break flat terrain edge with mossy boulders, slabs, and rim grass tufts
            BreakFlatTerrainEdge();

            // 3. Create foreground framing silhouettes (Repoussoir)
            SetupForegroundFramingSilhouettes();

            // 4. Staging Midground & Distant Trees with Atmospheric Haze
            StageTreesWithAtmosphericHaze();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("<color=#4ade80>[StagingPass]</color> <b>Staging Pass applied to active scene!</b>");
        }

        private static void TuneAtmosphericPostProcessing()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/VolumeProfile_Scene1_CaMau1954.asset");
            if (profile == null)
            {
                Debug.LogWarning("[StagingPass] VolumeProfile_Scene1_CaMau1954.asset not found.");
                return;
            }

            profile.components.Clear();

            // Color Adjustments: soften harsh neon greens, add rich midtone contrast
            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.08f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 14f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = -14f;
            colorAdj.colorFilter.overrideState = true;
            colorAdj.colorFilter.value = new Color(0.97f, 1.0f, 0.98f);

            // Vignette: gentle painterly edge darkening
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.28f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.42f;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.02f, 0.06f, 0.09f);

            // Bloom: subtle golden atmospheric glow
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.02f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.32f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.72f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(1.0f, 0.96f, 0.88f);

            // Neutral Tonemapping
            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.overrideState = true;
            tonemap.mode.value = TonemappingMode.Neutral;

            // Depth of Field: soft cinematic background falloff behind player (player is at Z = 0)
            var dof = profile.Add<DepthOfField>(true);
            dof.mode.overrideState = true;
            dof.mode.value = DepthOfFieldMode.Gaussian;
            dof.gaussianStart.overrideState = true;
            dof.gaussianStart.value = 7.5f;
            dof.gaussianEnd.overrideState = true;
            dof.gaussianEnd.value = 22.0f;

            EditorUtility.SetDirty(profile);
            Debug.Log("[StagingPass] URP Atmospheric Volume configured.");
        }

        private static void BreakFlatTerrainEdge()
        {
            var terrainGroup = GameObject.Find("03_Terrain");
            if (terrainGroup == null) return;

            var edgeDress = terrainGroup.transform.Find("OpeningEdge_RockAndGrassDressing");
            if (edgeDress != null) Object.DestroyImmediate(edgeDress.gameObject);

            var dressGo = new GameObject("OpeningEdge_RockAndGrassDressing");
            dressGo.transform.SetParent(terrainGroup.transform, false);

            var rock01Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Rocks/PF_LanaVN_Rock_01_LargeOutcrop.prefab");
            var rock02Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Rocks/PF_LanaVN_Rock_02_MossyMediumTop.prefab");
            var rock04Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Rocks/PF_LanaVN_Rock_04_FlatSlab.prefab");
            var rock05Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Rocks/PF_LanaVN_Rock_05_SmallMossy.prefab");

            var grassTallPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Grass_Vines/PF_LanaVN_GrassCarpet_02_Tall.prefab");
            var grassDensePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Grass_Vines/PF_LanaVN_GrassCarpet_05_Dense.prefab");
            var reedsPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Wildflowers/PF_LanaVN_Wildflower_01_Reeds.prefab");
            var daisyPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Wildflowers/PF_LanaVN_Wildflower_04_DaisySprig.prefab");

            void SpawnRock(GameObject prefab, Vector3 pos, Vector3 scale, float rotZ)
            {
                if (prefab == null) return;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, dressGo.transform);
                inst.transform.position = pos;
                inst.transform.localScale = scale;
                inst.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            }

            void SpawnFoliage(GameObject prefab, Vector3 pos, Vector3 scale, float rotZ)
            {
                if (prefab == null) return;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, dressGo.transform);
                inst.transform.position = pos;
                inst.transform.localScale = scale;
                inst.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            }

            // Stagger rocks overlapping the edge (Y = -1.85 is top of flat ground)
            // Left boulder cluster breaking left corner
            SpawnRock(rock01Prefab, new Vector3(-6.20f, -2.15f, -0.45f), new Vector3(0.95f, 0.60f, 0.85f), 12f);
            SpawnRock(rock02Prefab, new Vector3(-5.30f, -2.05f, -0.40f), new Vector3(0.85f, 0.55f, 0.80f), -8f);
            SpawnRock(rock05Prefab, new Vector3(-4.60f, -1.95f, -0.35f), new Vector3(0.75f, 0.50f, 0.75f), 5f);

            // Mid boulders along player path (slightly behind player path or hugging edge)
            SpawnRock(rock04Prefab, new Vector3(-2.80f, -2.02f, -0.40f), new Vector3(0.90f, 0.48f, 0.85f), -4f);
            SpawnRock(rock02Prefab, new Vector3(-1.10f, -2.10f, -0.45f), new Vector3(0.85f, 0.55f, 0.80f), 15f);
            SpawnRock(rock05Prefab, new Vector3( 0.40f, -1.98f, -0.35f), new Vector3(0.75f, 0.50f, 0.70f), -10f);
            SpawnRock(rock01Prefab, new Vector3( 2.20f, -2.18f, -0.45f), new Vector3(0.95f, 0.60f, 0.85f), 8f);
            SpawnRock(rock04Prefab, new Vector3( 3.80f, -2.05f, -0.40f), new Vector3(0.85f, 0.50f, 0.80f), -6f);

            // Overhanging lush grass tufts breaking the horizontal line
            SpawnFoliage(grassTallPrefab,  new Vector3(-6.50f, -1.85f, -0.50f), new Vector3(1.05f, 0.95f, 1f), 0f);
            SpawnFoliage(grassDensePrefab, new Vector3(-5.70f, -1.84f, -0.45f), new Vector3(1.00f, 0.90f, 1f), 0f);
            SpawnFoliage(reedsPrefab,      new Vector3(-4.90f, -1.82f, -0.35f), new Vector3(0.95f, 0.95f, 1f), 0f);
            SpawnFoliage(daisyPrefab,      new Vector3(-4.20f, -1.80f, -0.30f), new Vector3(0.85f, 0.85f, 1f), 0f);
            SpawnFoliage(grassTallPrefab,  new Vector3(-2.20f, -1.84f, -0.40f), new Vector3(0.95f, 0.90f, 1f), 0f);
            SpawnFoliage(grassDensePrefab, new Vector3(-0.50f, -1.85f, -0.45f), new Vector3(1.00f, 0.95f, 1f), 0f);
            SpawnFoliage(reedsPrefab,      new Vector3( 1.10f, -1.83f, -0.35f), new Vector3(0.95f, 0.90f, 1f), 0f);
            SpawnFoliage(grassTallPrefab,  new Vector3( 2.90f, -1.85f, -0.45f), new Vector3(1.05f, 0.95f, 1f), 0f);

            Debug.Log("[StagingPass] Edge rock and grass dressing populated.");
        }

        private static void SetupForegroundFramingSilhouettes()
        {
            var fgGroup = GameObject.Find("04_Foreground");
            if (fgGroup == null) return;

            var framing = fgGroup.transform.Find("FramingRepoussoir_Polish");
            if (framing != null) Object.DestroyImmediate(framing.gameObject);

            var framingGo = new GameObject("FramingRepoussoir_Polish");
            framingGo.transform.SetParent(fgGroup.transform, false);

            var bush01 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Bushes/PF_LanaVN_Bush_Broadleaf_01.prefab");
            var bush02 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Bushes/PF_LanaVN_Bush_Low_01.prefab");
            var tree01 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackDir}/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");

            // Helper to tint and layer foreground elements as dark silhouettes
            void AddRepoussoir(GameObject prefab, Vector3 pos, Vector3 scale, float rotZ, Color tint, int order)
            {
                if (prefab == null) return;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, framingGo.transform);
                inst.transform.position = pos;
                inst.transform.localScale = scale;
                inst.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

                foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>())
                {
                    sr.color = tint;
                    sr.sortingOrder = order;
                }
            }

            // Dark silhouette colors matching painterly foreground
            Color darkSilhouette = new Color(0.12f, 0.18f, 0.16f, 0.98f);
            Color branchSilhouette = new Color(0.08f, 0.13f, 0.12f, 0.98f);

            // 1. Bottom-Left Bush: positioned at X = -7.50 to frame corner, well clear of player at X = -4.0
            AddRepoussoir(bush01, new Vector3(-7.40f, -2.15f, -1.85f), new Vector3(1.10f, 1.05f, 1.0f), 8f, darkSilhouette, 30);
            AddRepoussoir(bush02, new Vector3(-6.70f, -2.25f, -1.75f), new Vector3(0.95f, 0.90f, 1.0f), -4f, darkSilhouette, 29);

            // 2. Top-Left Arching Canopy Silhouette: frames the sky view from above left
            AddRepoussoir(tree01, new Vector3(-6.50f, 4.40f, -1.80f), new Vector3(0.60f, 0.60f, 1.0f), -20f, branchSilhouette, 31);

            // 3. Top-Right Arching Branch Silhouette
            AddRepoussoir(tree01, new Vector3(2.80f, 4.30f, -1.80f), new Vector3(0.52f, 0.52f, 1.0f), 25f, branchSilhouette, 31);

            // 4. Bottom-Right Subtle Framing Shrub
            AddRepoussoir(bush02, new Vector3(2.40f, -2.35f, -1.80f), new Vector3(0.90f, 0.85f, 1.0f), 12f, darkSilhouette, 29);

            Debug.Log("[StagingPass] Foreground repoussoir silhouettes configured.");
        }

        private static void StageTreesWithAtmosphericHaze()
        {
            var treesGroup = GameObject.Find("05_Midground/Lana_HandDrawn_Ecosystem/HandDrawn_HeroTrees");
            if (treesGroup == null) return;

            // Desaturated blue-green haze (#7FA0A5: RGB 0.50, 0.63, 0.65)
            Color hazeColor = new Color(0.50f, 0.63f, 0.65f, 0.85f);
            Color deepHazeColor = new Color(0.42f, 0.56f, 0.62f, 0.72f);

            for (int i = 0; i < treesGroup.transform.childCount; i++)
            {
                var c = treesGroup.transform.GetChild(i);
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (c.name.StartsWith("Forest_BG_Tree"))
                {
                    sr.color = hazeColor;
                    var pos = c.position;
                    pos.z = Mathf.Max(pos.z, 6.8f);
                    c.position = pos;
                }
            }

            Debug.Log("[StagingPass] Midground trees staged with atmospheric haze.");
        }
    }
}
#endif
