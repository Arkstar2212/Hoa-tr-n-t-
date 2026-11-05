using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class OverhaulVillagePaddyRoadsAndLayout
{
    private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
    private const string DikeEWMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_EW.mat";
    private const string DikeNSMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_NS.mat";
    private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";
    private const string SoilTexPath = "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_baseColor_2k.png";
    private const string NormalTexPath = "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_normal_gl_2k.png";

    [MenuItem("Tools/Hoa Do/Overhaul Village Paddy Roads And Layout")]
    public static void Execute()
    {
        Debug.Log("[OverhaulVillagePaddyRoads] Starting full naturalization of paths, soil, and layout...");

        var soilTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilTexPath);
        var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexPath);

        // 1. CONFIGURE MATERIALS: Warm earthen brown with fine gravel/pebble texture
        Color earthenBrown = new Color(0.48f, 0.37f, 0.27f, 1.0f);
        Color mudBrown = new Color(0.26f, 0.18f, 0.12f, 1.0f);

        // Paths / Dikes between rice fields
        UpdateEarthenMaterial(DikeEWMatPath, soilTex, normalTex, earthenBrown, new Vector2(8.0f, 1.0f), 0.04f);
        UpdateEarthenMaterial(DikeNSMatPath, soilTex, normalTex, earthenBrown, new Vector2(1.0f, 4.0f), 0.04f);

        // Village entrance road and all village alleys/lanes
        UpdateEarthenMaterial(RoadMatPath, soilTex, normalTex, earthenBrown, new Vector2(2.0f, 50.0f), 0.04f);

        // Paddy mud soil inside rice plots
        UpdateEarthenMaterial(MudMatPath, soilTex, normalTex, mudBrown, new Vector2(6.0f, 3.0f), 0.14f);

        // 2. APPLY MUD MATERIAL TO ALL 22 PADDY PLOTS (Eliminates green lawn inside rice fields)
        ApplyPaddyMudToPlots();

        // 3. ORGANIC PLOT OFFSETS & SEAMLESS EARTHEN PATHS (Eliminates green lawn between rice fields)
        NaturalizePlotsAndContinuousEarthenPaths();

        // 4. SOFTEN ROAD EDGES WITH EARTHEN BERMS
        NaturalizeMainVillageRoad();

        Debug.Log("[OverhaulVillagePaddyRoads] Completed full naturalization with 0 green paths in rice fields!");
    }

    private static void UpdateEarthenMaterial(string path, Texture2D tex, Texture2D normal, Color col, Vector2 tiling, float smoothness)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) return;

        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader != null && mat.shader != litShader)
        {
            mat.shader = litShader;
        }

        if (tex != null) mat.SetTexture("_BaseMap", tex);
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 1.05f);
        }

        mat.SetColor("_BaseColor", col);
        mat.SetFloat("_Smoothness", smoothness);
        mat.mainTextureScale = tiling;

        EditorUtility.SetDirty(mat);
    }

    private static void ApplyPaddyMudToPlots()
    {
        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        if (mudMat == null) return;

        for (int i = 1; i <= 22; i++)
        {
            string[] suffixes = { "_Golden", "_Green" };
            foreach (var s in suffixes)
            {
                var go = GameObject.Find(string.Format("Paddy_Plot_{0:D2}{1}", i, s));
                if (go != null)
                {
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.sharedMaterial = mudMat;
                        EditorUtility.SetDirty(mr);
                    }
                }
            }
        }
    }

    private struct PlotNaturalSpec
    {
        public int id;
        public string suffix;
        public float deltaX;
        public float scaleX;
        public float deltaZ;
        public float scaleZ;
    }

    private static void NaturalizePlotsAndContinuousEarthenPaths()
    {
        var root = GameObject.Find("Paddy_5X_Extension_Root");
        if (root == null) return;

        int totalChildren = root.transform.childCount;
        var childMap = new Dictionary<string, Transform>(totalChildren);
        var plotChildrenGroups = new Dictionary<string, List<Transform>>();

        for (int i = 0; i < totalChildren; i++)
        {
            var c = root.transform.GetChild(i);
            childMap[c.name] = c;

            int secondUnder = c.name.IndexOf('_', 11);
            if (secondUnder > 0)
            {
                int thirdUnder = c.name.IndexOf('_', secondUnder + 1);
                string key = thirdUnder > 0 ? c.name.Substring(0, thirdUnder) : c.name;
                if (!plotChildrenGroups.TryGetValue(key, out var list))
                {
                    list = new List<Transform>();
                    plotChildrenGroups[key] = list;
                }
                list.Add(c);
            }
        }

        // Varied dimensions: organic mountain flanks, varied depths, subtle step-downs
        PlotNaturalSpec[] specs = new PlotNaturalSpec[]
        {
            new PlotNaturalSpec { id = 1,  suffix = "_Golden", deltaX = -0.5f,  scaleX = 21.0f, deltaZ = -0.2f,  scaleZ = 12.0f },
            new PlotNaturalSpec { id = 2,  suffix = "_Golden", deltaX = +0.4f,  scaleX = 18.2f, deltaZ = +0.1f,  scaleZ = 12.0f },
            new PlotNaturalSpec { id = 3,  suffix = "_Golden", deltaX = -0.3f,  scaleX = 16.5f, deltaZ = -0.15f, scaleZ = 12.0f },
            new PlotNaturalSpec { id = 4,  suffix = "_Green",  deltaX = +0.6f,  scaleX = 19.8f, deltaZ = +0.2f,  scaleZ = 12.0f },
            new PlotNaturalSpec { id = 5,  suffix = "_Green",  deltaX = -0.8f,  scaleX = 39.5f, deltaZ = -0.25f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 6,  suffix = "_Green",  deltaX = +0.5f,  scaleX = 33.2f, deltaZ = +0.15f, scaleZ = 11.0f },

            // West plots (X ~ -55 to -10): varied widths following mountain contours
            new PlotNaturalSpec { id = 7,  suffix = "_Golden", deltaX = +0.75f, scaleX = 38.5f, deltaZ = -0.20f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 8,  suffix = "_Green",  deltaX = -1.00f, scaleX = 42.0f, deltaZ = +0.30f, scaleZ = 11.5f },
            new PlotNaturalSpec { id = 11, suffix = "_Green",  deltaX = +0.50f, scaleX = 39.0f, deltaZ = -0.15f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 12, suffix = "_Golden", deltaX = -1.75f, scaleX = 43.5f, deltaZ = +0.25f, scaleZ = 11.5f },
            new PlotNaturalSpec { id = 15, suffix = "_Golden", deltaX = +0.25f, scaleX = 40.5f, deltaZ = -0.30f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 16, suffix = "_Green",  deltaX = -0.75f, scaleX = 41.5f, deltaZ = +0.20f, scaleZ = 11.5f },
            new PlotNaturalSpec { id = 19, suffix = "_Green",  deltaX = +1.00f, scaleX = 38.0f, deltaZ = -0.25f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 20, suffix = "_Golden", deltaX = -1.25f, scaleX = 42.5f, deltaZ = +0.35f, scaleZ = 11.5f },

            // East plots (X ~ -3 to 36): varied widths following mountain contours
            new PlotNaturalSpec { id = 9,  suffix = "_Green",  deltaX = -0.50f, scaleX = 35.0f, deltaZ = +0.20f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 10, suffix = "_Golden", deltaX = +1.00f, scaleX = 38.0f, deltaZ = -0.25f, scaleZ = 11.5f },
            new PlotNaturalSpec { id = 13, suffix = "_Golden", deltaX = -0.75f, scaleX = 34.5f, deltaZ = +0.15f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 14, suffix = "_Green",  deltaX = +1.50f, scaleX = 39.0f, deltaZ = -0.20f, scaleZ = 11.5f },
            new PlotNaturalSpec { id = 17, suffix = "_Green",  deltaX = -0.25f, scaleX = 35.5f, deltaZ = +0.30f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 18, suffix = "_Golden", deltaX = +0.75f, scaleX = 37.5f, deltaZ = -0.15f, scaleZ = 11.5f },
            new PlotNaturalSpec { id = 21, suffix = "_Golden", deltaX = -0.50f, scaleX = 35.0f, deltaZ = +0.25f, scaleZ = 11.0f },
            new PlotNaturalSpec { id = 22, suffix = "_Green",  deltaX = +1.25f, scaleX = 38.5f, deltaZ = -0.30f, scaleZ = 11.5f }
        };

        foreach (var spec in specs)
        {
            string plotName = string.Format("Paddy_Plot_{0:D2}{1}", spec.id, spec.suffix);
            if (!childMap.TryGetValue(plotName, out var plotObj)) continue;

            Vector3 origPos = plotObj.position;
            Vector3 origScale = plotObj.localScale;

            float tZ = Mathf.InverseLerp(45f, 175f, origPos.z);
            float targetY = Mathf.Lerp(0.495f, 0.545f, tZ);

            Vector3 newPos = new Vector3(origPos.x + spec.deltaX, targetY, origPos.z + spec.deltaZ);
            Vector3 delta = newPos - origPos;

            plotObj.position = newPos;
            plotObj.localScale = new Vector3(spec.scaleX, spec.scaleZ, origScale.z);

            // WIDE EARTHEN PATHS: Dikes now bridge the entire gaps between plots and to the road!
            UpdatePlotEarthenPathsContinuous(spec, childMap, newPos, spec.scaleX, spec.scaleZ, targetY);

            // Translate all rice tufts belonging to this plot
            if (plotChildrenGroups.TryGetValue(plotName, out var relatedList))
            {
                for (int j = 0; j < relatedList.Count; j++)
                {
                    var r = relatedList[j];
                    if (r == plotObj || r.name.Contains("Dike")) continue;
                    r.position += delta;
                }
            }
        }

        EditorUtility.SetDirty(root);
    }

    private static void UpdatePlotEarthenPathsContinuous(PlotNaturalSpec spec, Dictionary<string, Transform> childMap, Vector3 plotPos, float scaleX, float scaleZ, float targetY)
    {
        string prefix = string.Format("Paddy_Plot_{0:D2}{1}_Dike", spec.id, spec.suffix);
        float halfX = scaleX * 0.5f;
        float halfZ = scaleZ * 0.5f;
        float dikeY = targetY + 0.035f;

        // Wide cross-paths in Z (1.8m to 2.4m) bridging between rows so zero green gaps exist
        float crossPathZ = (spec.id % 2 == 0) ? 2.4f : 1.8f;
        // Wide access paths bordering the central road (2.2m)
        float roadBorderPathX = 2.2f;

        // North Dike / Path
        if (childMap.TryGetValue(prefix + "N", out var dikeN))
        {
            dikeN.position = new Vector3(plotPos.x, dikeY, plotPos.z + halfZ + crossPathZ * 0.5f);
            dikeN.localScale = new Vector3(scaleX + roadBorderPathX, 0.08f, crossPathZ);
        }
        // South Dike / Path
        if (childMap.TryGetValue(prefix + "S", out var dikeS))
        {
            dikeS.position = new Vector3(plotPos.x, dikeY, plotPos.z - halfZ - crossPathZ * 0.5f);
            dikeS.localScale = new Vector3(scaleX + roadBorderPathX, 0.08f, crossPathZ);
        }
        // East Dike: for West plots, East Dike borders the central road -> wide earthen road margin!
        if (childMap.TryGetValue(prefix + "E", out var dikeE))
        {
            bool isFacingRoad = (plotPos.x < -7.0f);
            float widthE = isFacingRoad ? roadBorderPathX : 1.2f;
            float posX = plotPos.x + halfX + widthE * 0.5f;
            dikeE.position = new Vector3(posX, dikeY, plotPos.z);
            dikeE.localScale = new Vector3(widthE, 0.08f, scaleZ + crossPathZ);
        }
        // West Dike: for East plots, West Dike borders the central road -> wide earthen road margin!
        if (childMap.TryGetValue(prefix + "W", out var dikeW))
        {
            bool isFacingRoad = (plotPos.x > -7.0f);
            float widthW = isFacingRoad ? roadBorderPathX : 1.2f;
            float posX = plotPos.x - halfX - widthW * 0.5f;
            dikeW.position = new Vector3(posX, dikeY, plotPos.z);
            dikeW.localScale = new Vector3(widthW, 0.08f, scaleZ + crossPathZ);
        }
    }

    private static void NaturalizeMainVillageRoad()
    {
        var mainRoad = GameObject.Find("Road_North_StraightPaddyAisle");
        if (mainRoad == null) return;

        mainRoad.transform.position = new Vector3(-7.0f, 0.505f, 112.0f);
        mainRoad.transform.localScale = new Vector3(5.0f, 153.0f, 1.0f);
        EditorUtility.SetDirty(mainRoad);

        var mfRoad = mainRoad.GetComponent<MeshFilter>();
        Mesh quadMesh = mfRoad != null ? mfRoad.sharedMesh : null;

        var bermsRoot = GameObject.Find("Road_Organic_Berms_Root");
        if (bermsRoot == null)
        {
            bermsRoot = new GameObject("Road_Organic_Berms_Root");
            bermsRoot.transform.SetParent(mainRoad.transform.parent, true);
        }

        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);

        float[] sampleZs = new float[] { 45f, 63f, 81f, 99f, 117f, 135f, 153f, 171f };
        for (int i = 0; i < sampleZs.Length; i++)
        {
            float z = sampleZs[i];
            string westBermName = $"Road_Berm_West_{i:D2}";
            var westBerm = GameObject.Find(westBermName);
            if (westBerm == null)
            {
                westBerm = new GameObject(westBermName);
                westBerm.transform.SetParent(bermsRoot.transform, false);
                var mf = westBerm.AddComponent<MeshFilter>();
                mf.sharedMesh = quadMesh;
                var mr = westBerm.AddComponent<MeshRenderer>();
                mr.sharedMaterial = roadMat;
            }
            float widthVarW = 0.6f + (i % 3) * 0.3f;
            westBerm.transform.position = new Vector3(-9.5f - widthVarW * 0.5f, 0.51f, z);
            westBerm.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            westBerm.transform.localScale = new Vector3(widthVarW, 6.5f, 1f);

            string eastBermName = $"Road_Berm_East_{i:D2}";
            var eastBerm = GameObject.Find(eastBermName);
            if (eastBerm == null)
            {
                eastBerm = new GameObject(eastBermName);
                eastBerm.transform.SetParent(bermsRoot.transform, false);
                var mf = eastBerm.AddComponent<MeshFilter>();
                mf.sharedMesh = quadMesh;
                var mr = eastBerm.AddComponent<MeshRenderer>();
                mr.sharedMaterial = roadMat;
            }
            float widthVarE = 0.55f + ((i + 1) % 3) * 0.35f;
            eastBerm.transform.position = new Vector3(-4.5f + widthVarE * 0.5f, 0.51f, z + 3.0f);
            eastBerm.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            eastBerm.transform.localScale = new Vector3(widthVarE, 6.0f, 1f);
        }

        EditorUtility.SetDirty(bermsRoot);
    }
}
