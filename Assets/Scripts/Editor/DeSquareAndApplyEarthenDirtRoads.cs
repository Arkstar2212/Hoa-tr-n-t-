using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class DeSquareAndApplyEarthenDirtRoads
{
    private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
    private const string DikeEWMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_EW.mat";
    private const string DikeNSMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_NS.mat";
    private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";

    private const string SoilTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_EarthenSoil_Pebbles_Seamless.png";
    private const string SoilNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_EarthenSoil_Normal.png";
    private const string MudTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_PaddyMud_Seamless.png";
    private const string MudNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_PaddyMud_Normal.png";

    [MenuItem("Tools/Hoa Do/De-Square And Apply Pure Earthen Dirt Roads")]
    public static void Execute()
    {
        Debug.Log("[DeSquare] Starting overhaul: applying genuine soil textures, breaking rigid squares...");

        var soilTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilTexPath);
        var soilNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilNormPath);
        var mudTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MudTexPath);
        var mudNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(MudNormPath);

        // 1. UPDATE MATERIALS TO PURE EARTHEN SOIL & PEBBLES (NO GRASS!)
        // Paths / Dikes: Pure natural Vietnamese red-brown earth
        Color earthDirtColor = new Color(0.95f, 0.90f, 0.85f, 1.0f); // neutral multiplier on colored soil tex
        ConfigureMat(DikeEWMatPath, soilTex, soilNorm, earthDirtColor, new Vector2(8.0f, 1.0f), 0.04f);
        ConfigureMat(DikeNSMatPath, soilTex, soilNorm, earthDirtColor, new Vector2(1.0f, 4.0f), 0.04f);

        // Village entrance road & all village alleys: Pure dirt path!
        ConfigureMat(RoadMatPath, soilTex, soilNorm, earthDirtColor, new Vector2(2.0f, 45.0f), 0.04f);

        // Paddy field soil: Dark alluvial river mud
        ConfigureMat(MudMatPath, mudTex, mudNorm, Color.white, new Vector2(4.0f, 2.0f), 0.16f);

        // 2. APPLY MUD TO ALL PLOTS
        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        for (int i = 1; i <= 22; i++)
        {
            string[] sfx = { "_Golden", "_Green" };
            foreach (var s in sfx)
            {
                var go = GameObject.Find($"Paddy_Plot_{i:D2}{s}");
                if (go != null)
                {
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = mudMat;
                }
            }
        }

        // 3. BREAK THE BOXES / RECTANGLES (DE-SQUARE):
        // - Asymmetrical Z stagger between West and East sides
        // - Angled / trapezoidal rotations (no more 90-degree boxes)
        // - Varied mountain-contour widths
        // - Continuous wide earthen paths
        DeSquarePlotsAndDikes();

        // 4. SHAPE MEANDERING COUNTRY DIRT ROAD
        ShapeOrganicDirtRoad();

        Debug.Log("[DeSquare] De-squaring and earthen dirt overhaul completed successfully!");
    }

    private static void ConfigureMat(string path, Texture2D tex, Texture2D norm, Color col, Vector2 tiling, float smoothness)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) return;

        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader != null && mat.shader != litShader) mat.shader = litShader;

        if (tex != null) mat.SetTexture("_BaseMap", tex);
        if (norm != null)
        {
            mat.SetTexture("_BumpMap", norm);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 1.1f);
        }

        mat.SetColor("_BaseColor", col);
        mat.SetFloat("_Smoothness", smoothness);
        mat.mainTextureScale = tiling;
        EditorUtility.SetDirty(mat);
    }

    private struct OrganicPlotLayout
    {
        public int id;
        public string suffix;
        public float posX;
        public float posZ;
        public float scaleX;
        public float scaleZ;
        public float rotY; // Angled natural boundary (breaking 90-degree rectangle!)
    }

    private static void DeSquarePlotsAndDikes()
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

        // NON-SQUARE, ASYMMETRIC, ANGLED PLOT LAYOUT
        // Notice: East plots are staggered in Z from West plots by 4m-7m!
        // Rotations are non-zero: -3.5 deg to +4 deg creating organic trapezoidal fields!
        OrganicPlotLayout[] layouts = new OrganicPlotLayout[]
        {
            // Southern transitional plots
            new OrganicPlotLayout { id = 1,  suffix = "_Golden", posX = -42.5f, posZ = 61.5f,  scaleX = 22.0f, scaleZ = 12.0f, rotY = -2.5f },
            new OrganicPlotLayout { id = 2,  suffix = "_Golden", posX = -20.5f, posZ = 62.5f,  scaleX = 18.0f, scaleZ = 11.5f, rotY = +1.8f },
            new OrganicPlotLayout { id = 3,  suffix = "_Golden", posX = 6.2f,   posZ = 63.5f,  scaleX = 16.5f, scaleZ = 11.0f, rotY = -2.0f },
            new OrganicPlotLayout { id = 4,  suffix = "_Green",  posX = 26.0f,  posZ = 62.0f,  scaleX = 20.0f, scaleZ = 12.5f, rotY = +3.0f },
            new OrganicPlotLayout { id = 5,  suffix = "_Green",  posX = -33.0f, posZ = 47.5f,  scaleX = 39.0f, scaleZ = 11.5f, rotY = +1.5f },
            new OrganicPlotLayout { id = 6,  suffix = "_Green",  posX = 15.0f,  posZ = 48.5f,  scaleX = 34.5f, scaleZ = 11.0f, rotY = -2.2f },

            // West Plots: Flank West Mountain (Z = 76 to 171)
            new OrganicPlotLayout { id = 7,  suffix = "_Golden", posX = -30.5f, posZ = 76.5f,  scaleX = 37.0f, scaleZ = 11.0f, rotY = -3.2f },
            new OrganicPlotLayout { id = 8,  suffix = "_Green",  posX = -34.0f, posZ = 90.0f,  scaleX = 44.0f, scaleZ = 12.5f, rotY = +2.5f },
            new OrganicPlotLayout { id = 11, suffix = "_Green",  posX = -31.0f, posZ = 104.5f, scaleX = 38.5f, scaleZ = 11.0f, rotY = -2.8f },
            new OrganicPlotLayout { id = 12, suffix = "_Golden", posX = -35.5f, posZ = 117.5f, scaleX = 45.0f, scaleZ = 12.0f, rotY = +3.5f },
            new OrganicPlotLayout { id = 15, suffix = "_Golden", posX = -31.5f, posZ = 131.0f, scaleX = 39.5f, scaleZ = 11.5f, rotY = -1.8f },
            new OrganicPlotLayout { id = 16, suffix = "_Green",  posX = -34.5f, posZ = 144.5f, scaleX = 43.0f, scaleZ = 12.0f, rotY = +2.8f },
            new OrganicPlotLayout { id = 19, suffix = "_Green",  posX = -30.0f, posZ = 158.0f, scaleX = 36.5f, scaleZ = 11.0f, rotY = -3.0f },
            new OrganicPlotLayout { id = 20, suffix = "_Golden", posX = -34.0f, posZ = 171.5f, scaleX = 44.5f, scaleZ = 12.5f, rotY = +2.0f },

            // East Plots: Flank East Mountain (Z staggered by 4m-7m from West side!)
            new OrganicPlotLayout { id = 9,  suffix = "_Green",  posX = 14.0f,  posZ = 72.0f,  scaleX = 33.5f, scaleZ = 11.5f, rotY = +2.8f },
            new OrganicPlotLayout { id = 10, suffix = "_Golden", posX = 17.5f,  posZ = 85.5f,  scaleX = 39.5f, scaleZ = 12.0f, rotY = -3.5f },
            new OrganicPlotLayout { id = 13, suffix = "_Golden", posX = 14.5f,  posZ = 99.5f,  scaleX = 34.0f, scaleZ = 11.0f, rotY = +2.2f },
            new OrganicPlotLayout { id = 14, suffix = "_Green",  posX = 18.0f,  posZ = 113.5f, scaleX = 40.5f, scaleZ = 12.5f, rotY = -3.0f },
            new OrganicPlotLayout { id = 17, suffix = "_Green",  posX = 15.0f,  posZ = 127.5f, scaleX = 35.0f, scaleZ = 11.0f, rotY = +2.5f },
            new OrganicPlotLayout { id = 18, suffix = "_Golden", posX = 18.5f,  posZ = 141.5f, scaleX = 40.0f, scaleZ = 12.0f, rotY = -2.2f },
            new OrganicPlotLayout { id = 21, suffix = "_Golden", posX = 14.5f,  posZ = 155.0f, scaleX = 34.5f, scaleZ = 11.5f, rotY = +3.2f },
            new OrganicPlotLayout { id = 22, suffix = "_Green",  posX = 18.0f,  posZ = 168.5f, scaleX = 39.0f, scaleZ = 12.0f, rotY = -2.5f }
        };

        foreach (var layout in layouts)
        {
            string plotName = $"Paddy_Plot_{layout.id:D2}{layout.suffix}";
            if (!childMap.TryGetValue(plotName, out var plotObj)) continue;

            Vector3 origPos = plotObj.position;
            Quaternion origRot = plotObj.rotation;

            // Terraced stepped height Y from south (0.495m) to north (0.545m)
            float tZ = Mathf.InverseLerp(45f, 175f, layout.posZ);
            float targetY = Mathf.Lerp(0.495f, 0.545f, tZ);

            Vector3 newPos = new Vector3(layout.posX, targetY, layout.posZ);
            Quaternion newRot = Quaternion.Euler(90f, layout.rotY, 0f);

            Vector3 deltaPos = newPos - origPos;

            plotObj.position = newPos;
            plotObj.rotation = newRot;
            plotObj.localScale = new Vector3(layout.scaleX, layout.scaleZ, 1.0f);

            // Update Dikes around this angled organic plot
            UpdateAngledPlotDikes(layout, childMap, newPos, newRot, layout.scaleX, layout.scaleZ, targetY);

            // Translate all rice tufts belonging to this plot
            if (plotChildrenGroups.TryGetValue(plotName, out var relatedList))
            {
                for (int j = 0; j < relatedList.Count; j++)
                {
                    var r = relatedList[j];
                    if (r == plotObj || r.name.Contains("Dike")) continue;
                    r.position += deltaPos;
                }
            }
        }

        EditorUtility.SetDirty(root);
    }

    private static void UpdateAngledPlotDikes(OrganicPlotLayout layout, Dictionary<string, Transform> childMap, Vector3 plotPos, Quaternion plotRot, float scaleX, float scaleZ, float targetY)
    {
        string prefix = $"Paddy_Plot_{layout.id:D2}{layout.suffix}_Dike";
        float halfX = scaleX * 0.5f;
        float halfZ = scaleZ * 0.5f;
        float dikeY = targetY + 0.035f;

        float pathWidthZ = 2.2f; // wide continuous walking path in Z
        float roadMarginX = 2.4f; // wide earthen access path along the road

        // North Dike (oriented along top edge with plot rotation)
        if (childMap.TryGetValue(prefix + "N", out var dikeN))
        {
            Vector3 localOffset = new Vector3(0f, 0f, halfZ + pathWidthZ * 0.5f);
            Vector3 worldPos = plotPos + (plotRot * new Vector3(0f, halfZ + pathWidthZ * 0.5f, 0f));
            worldPos.y = dikeY;
            dikeN.position = worldPos;
            dikeN.rotation = Quaternion.Euler(0f, layout.rotY, 0f);
            dikeN.localScale = new Vector3(scaleX + roadMarginX, 0.08f, pathWidthZ);
        }

        // South Dike
        if (childMap.TryGetValue(prefix + "S", out var dikeS))
        {
            Vector3 worldPos = plotPos + (plotRot * new Vector3(0f, -halfZ - pathWidthZ * 0.5f, 0f));
            worldPos.y = dikeY;
            dikeS.position = worldPos;
            dikeS.rotation = Quaternion.Euler(0f, layout.rotY, 0f);
            dikeS.localScale = new Vector3(scaleX + roadMarginX, 0.08f, pathWidthZ);
        }

        // East Dike
        if (childMap.TryGetValue(prefix + "E", out var dikeE))
        {
            bool isFacingRoad = (plotPos.x < -7.0f);
            float w = isFacingRoad ? roadMarginX : 1.4f;
            Vector3 worldPos = plotPos + (plotRot * new Vector3(halfX + w * 0.5f, 0f, 0f));
            worldPos.y = dikeY;
            dikeE.position = worldPos;
            dikeE.rotation = Quaternion.Euler(0f, layout.rotY, 0f);
            dikeE.localScale = new Vector3(w, 0.08f, scaleZ + pathWidthZ * 1.5f);
        }

        // West Dike
        if (childMap.TryGetValue(prefix + "W", out var dikeW))
        {
            bool isFacingRoad = (plotPos.x > -7.0f);
            float w = isFacingRoad ? roadMarginX : 1.4f;
            Vector3 worldPos = plotPos + (plotRot * new Vector3(-halfX - w * 0.5f, 0f, 0f));
            worldPos.y = dikeY;
            dikeW.position = worldPos;
            dikeW.rotation = Quaternion.Euler(0f, layout.rotY, 0f);
            dikeW.localScale = new Vector3(w, 0.08f, scaleZ + pathWidthZ * 1.5f);
        }
    }

    private static void ShapeOrganicDirtRoad()
    {
        var mainRoad = GameObject.Find("Road_North_StraightPaddyAisle");
        if (mainRoad == null) return;

        // Position road slightly wider (5.2m) with natural organic berms
        mainRoad.transform.position = new Vector3(-7.0f, 0.505f, 112.0f);
        mainRoad.transform.localScale = new Vector3(5.2f, 153.0f, 1.0f);
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

        // Organic earthen berms connecting road to plot dikes
        float[] sampleZs = new float[] { 45f, 60f, 75f, 90f, 105f, 120f, 135f, 150f, 165f, 178f };
        for (int i = 0; i < sampleZs.Length; i++)
        {
            float z = sampleZs[i];
            float curveOffset = Mathf.Sin(z * 0.045f) * 0.65f;

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
            float widthVarW = 0.8f + (i % 3) * 0.35f;
            westBerm.transform.position = new Vector3(-9.6f + curveOffset - widthVarW * 0.5f, 0.51f, z);
            westBerm.transform.rotation = Quaternion.Euler(90f, (i % 2 == 0 ? 3f : -3f), 0f);
            westBerm.transform.localScale = new Vector3(widthVarW, 7.5f, 1f);

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
            float widthVarE = 0.75f + ((i + 1) % 3) * 0.4f;
            eastBerm.transform.position = new Vector3(-4.4f + curveOffset + widthVarE * 0.5f, 0.51f, z + 2.5f);
            eastBerm.transform.rotation = Quaternion.Euler(90f, (i % 2 == 0 ? -2.5f : 3.5f), 0f);
            eastBerm.transform.localScale = new Vector3(widthVarE, 7.0f, 1f);
        }

        EditorUtility.SetDirty(bermsRoot);
    }
}
