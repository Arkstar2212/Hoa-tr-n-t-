using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class OverhaulAuthenticEarthenEnvironment
{
    private const int TexSize = 256;
    private const string TextureDir = "Assets/Textures/Ground_Tiles";
    
    // Texture paths
    public const string SoilTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_AuthenticEarth_Seamless.png";
    public const string SoilNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_AuthenticEarth_Normal.png";
    public const string MudTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_AuthenticMud_Seamless.png";
    public const string MudNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_AuthenticMud_Normal.png";

    // Material paths
    private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
    private const string DikeEWMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_EW.mat";
    private const string DikeNSMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_NS.mat";
    private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";

    public struct RoadNode
    {
        public float z;
        public float x;
        public float width;
        public float y;
    }

    public struct DeSquaredPlot
    {
        public int id;
        public string suffix;
        public float posX;
        public float posZ;
        public float scaleX;
        public float scaleZ;
        public float rotY;
    }

    public struct FloraSpot
    {
        public Vector3 pos;
        public float rotY;
        public float scale;
        public int type; // 0=palm, 1=banana, 2=bamboo, 3=bush, 4=rock
    }

    [MenuItem("Tools/Hoa Do/Overhaul Authentic Earthen Environment")]
    public static void Execute()
    {
        Debug.Log("[OverhaulEarth] === STARTING AUTHENTIC EARTHEN ENVIRONMENT OVERHAUL ===");

        // STEP 1: Fast & genuine seamless earthen dirt & mud textures (NO polka dots, NO green grass tint!)
        GenerateTexturesFast();

        // STEP 2: Configure URP Lit materials with unified, authentic warm earth
        ConfigureMaterials();

        // STEP 3: Create full earthen substrate (ensures NO green grass under paths or paddies)
        EnsureEarthenSubstrate();

        // STEP 4: Build organic winding earthen country road (replaces rigid straight quad)
        BuildWindingEarthenRoad();

        // STEP 5: De-square rice paddies (asymmetric stagger, varied depths, organic angles, intermediate dikes)
        DeSquareRicePaddies();

        // STEP 6: Add authentic tropical roadside flora & mountain skirt vegetation
        AddRoadsideFlora();

        // STEP 7: Verify Player An spawn at North Gate
        VerifyPlayerSpawn();

        Debug.Log("[OverhaulEarth] === OVERHAUL COMPLETE SUCCESSFULLY! ===");
    }

    private static void GenerateTexturesFast()
    {
        if (!Directory.Exists(TextureDir)) Directory.CreateDirectory(TextureDir);

        float[] cosA = new float[TexSize];
        float[] sinA = new float[TexSize];
        for (int i = 0; i < TexSize; i++)
        {
            float a = (float)i / TexSize * Mathf.PI * 2f;
            cosA[i] = Mathf.Cos(a);
            sinA[i] = Mathf.Sin(a);
        }

        // 1A. Genuine Warm Earthen Dirt Texture
        var colTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        var normTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        float[,] heightMap = new float[TexSize, TexSize];
        Color[] soilColors = new Color[TexSize * TexSize];

        Color baseEarth = new Color(0.68f, 0.54f, 0.40f); // Warm natural golden brown #AE8A66
        Color sandDust  = new Color(0.76f, 0.64f, 0.50f); // Sandy trodden earth #C2A380
        Color deepLoam  = new Color(0.56f, 0.42f, 0.29f); // Rich organic loam #8F6B4A

        for (int y = 0; y < TexSize; y++)
        {
            float cv = cosA[y], sv = sinA[y];
            for (int x = 0; x < TexSize; x++)
            {
                float cu = cosA[x], su = sinA[x];
                float n0 = Mathf.PerlinNoise(cu * 1.5f + 12f, cv * 1.5f + 12f) * 0.50f;
                float n1 = Mathf.PerlinNoise(su * 4.0f + 25f, sv * 4.0f + 25f) * 0.30f;
                float n2 = Mathf.PerlinNoise(cu * 12.0f + 40f, cv * 12.0f + 40f) * 0.15f;
                
                float grain = (Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1f;
                float h = Mathf.Clamp01(n0 + n1 + n2 + (grain - 0.5f) * 0.05f);
                heightMap[x, y] = h;

                Color c = Color.Lerp(deepLoam, baseEarth, Mathf.SmoothStep(0.25f, 0.58f, h));
                c = Color.Lerp(c, sandDust, Mathf.SmoothStep(0.55f, 0.88f, h));
                soilColors[y * TexSize + x] = c;
            }
        }
        colTex.SetPixels(soilColors);
        colTex.Apply();
        File.WriteAllBytes(SoilTexPath, colTex.EncodeToPNG());
        Object.DestroyImmediate(colTex);

        // Fast Sobel Normal Map
        Color[] normPixels = new Color[TexSize * TexSize];
        for (int y = 0; y < TexSize; y++)
        {
            int ym = (y - 1 + TexSize) % TexSize;
            int yp = (y + 1) % TexSize;
            for (int x = 0; x < TexSize; x++)
            {
                int xm = (x - 1 + TexSize) % TexSize;
                int xp = (x + 1) % TexSize;
                float dX = (heightMap[xp, y] - heightMap[xm, y]);
                float dY = (heightMap[x, yp] - heightMap[x, ym]);
                Vector3 n = new Vector3(-dX * 1.5f, -dY * 1.5f, 1.0f).normalized;
                normPixels[y * TexSize + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
            }
        }
        normTex.SetPixels(normPixels);
        normTex.Apply();
        File.WriteAllBytes(SoilNormPath, normTex.EncodeToPNG());
        Object.DestroyImmediate(normTex);

        // 1B. Genuine Dark Alluvial Mud Texture (harmonized with warm earth tone)
        var mudColTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        var mudNormTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        float[,] mudHeight = new float[TexSize, TexSize];
        Color[] mudColors = new Color[TexSize * TexSize];

        Color baseMud    = new Color(0.58f, 0.46f, 0.34f); // Warm moist alluvial earth
        Color wetClay    = new Color(0.64f, 0.52f, 0.39f); // Damp river silt
        Color darkPuddle = new Color(0.48f, 0.36f, 0.26f); // Low water depression

        for (int y = 0; y < TexSize; y++)
        {
            float cv = cosA[y], sv = sinA[y];
            for (int x = 0; x < TexSize; x++)
            {
                float cu = cosA[x], su = sinA[x];
                float m0 = Mathf.PerlinNoise(cu * 1.2f + 50f, cv * 1.2f + 50f) * 0.60f;
                float m1 = Mathf.PerlinNoise(su * 3.5f + 65f, sv * 3.5f + 65f) * 0.30f;
                float h = Mathf.Clamp01(m0 + m1);
                mudHeight[x, y] = h;

                Color mc = Color.Lerp(darkPuddle, baseMud, Mathf.SmoothStep(0.2f, 0.6f, h));
                mc = Color.Lerp(mc, wetClay, Mathf.SmoothStep(0.6f, 0.95f, h));
                mudColors[y * TexSize + x] = mc;
            }
        }
        mudColTex.SetPixels(mudColors);
        mudColTex.Apply();
        File.WriteAllBytes(MudTexPath, mudColTex.EncodeToPNG());
        Object.DestroyImmediate(mudColTex);

        // Fast Mud Normal Map
        Color[] mudNormPixels = new Color[TexSize * TexSize];
        for (int y = 0; y < TexSize; y++)
        {
            int ym = (y - 1 + TexSize) % TexSize;
            int yp = (y + 1) % TexSize;
            for (int x = 0; x < TexSize; x++)
            {
                int xm = (x - 1 + TexSize) % TexSize;
                int xp = (x + 1) % TexSize;
                float dX = (mudHeight[xp, y] - mudHeight[xm, y]);
                float dY = (mudHeight[x, yp] - mudHeight[x, ym]);
                Vector3 n = new Vector3(-dX * 0.8f, -dY * 0.8f, 1.0f).normalized;
                mudNormPixels[y * TexSize + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
            }
        }
        mudNormTex.SetPixels(mudNormPixels);
        mudNormTex.Apply();
        File.WriteAllBytes(MudNormPath, mudNormTex.EncodeToPNG());
        Object.DestroyImmediate(mudNormTex);

        AssetDatabase.ImportAsset(SoilTexPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(SoilNormPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MudTexPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MudNormPath, ImportAssetOptions.ForceUpdate);

        ConfigureImporter(SoilTexPath, false);
        ConfigureImporter(SoilNormPath, true);
        ConfigureImporter(MudTexPath, false);
        ConfigureImporter(MudNormPath, true);
    }

    private static void ConfigureImporter(string path, bool isNormal)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        if (isNormal)
        {
            importer.textureType = TextureImporterType.NormalMap;
        }
        else
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
        }
        importer.SaveAndReimport();
    }

    private static void ConfigureMaterials()
    {
        var soilTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilTexPath);
        var soilNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilNormPath);
        var mudTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MudTexPath);
        var mudNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(MudNormPath);

        // Roads & village paths: Pure natural warm earth
        ApplyToMat(RoadMatPath, soilTex, soilNorm, Color.white, new Vector2(3.0f, 35.0f), 0.04f);
        ApplyToMat(DikeEWMatPath, soilTex, soilNorm, Color.white, new Vector2(8.0f, 1.0f), 0.04f);
        ApplyToMat(DikeNSMatPath, soilTex, soilNorm, Color.white, new Vector2(1.0f, 5.0f), 0.04f);

        // Rice paddy basins: Synchronized warm alluvial earth (zero harsh contrast boxes!)
        ApplyToMat(MudMatPath, mudTex, mudNorm, Color.white, new Vector2(5.0f, 3.0f), 0.05f);

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
    }

    private static void ApplyToMat(string path, Texture2D tex, Texture2D norm, Color col, Vector2 scale, float smoothness)
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
            mat.SetFloat("_BumpScale", 1.0f);
        }
        mat.SetColor("_BaseColor", col);
        mat.SetFloat("_Smoothness", smoothness);
        mat.mainTextureScale = scale;
        EditorUtility.SetDirty(mat);
    }

    private static void EnsureEarthenSubstrate()
    {
        var pathsRoot = GameObject.Find("Paths_3D_Network");
        if (pathsRoot == null) return;

        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
        var substrate = GameObject.Find("Valley_Earthen_Ground_Substrate");
        if (substrate == null)
        {
            substrate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            substrate.name = "Valley_Earthen_Ground_Substrate";
            substrate.transform.SetParent(pathsRoot.transform, true);
            var col = substrate.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        // Continuous dirt substrate under the whole valley (Y = 0.490m)
        substrate.transform.position = new Vector3(-8.0f, 0.490f, 112.0f);
        substrate.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        substrate.transform.localScale = new Vector3(110.0f, 155.0f, 1.0f);

        var mr = substrate.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = roadMat;
        EditorUtility.SetDirty(substrate);
    }

    private static void BuildWindingEarthenRoad()
    {
        var pathsRoot = GameObject.Find("Paths_3D_Network");
        if (pathsRoot == null) return;

        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);

        var oldStraightRoad = GameObject.Find("Road_North_StraightPaddyAisle");
        if (oldStraightRoad != null) oldStraightRoad.SetActive(false);

        var oldBerms = GameObject.Find("Road_Organic_Berms_Root");
        if (oldBerms != null) Object.DestroyImmediate(oldBerms);

        var windingRoadRoot = GameObject.Find("Winding_Country_Dirt_Road");
        if (windingRoadRoot != null) Object.DestroyImmediate(windingRoadRoot);
        windingRoadRoot = new GameObject("Winding_Country_Dirt_Road");
        windingRoadRoot.transform.SetParent(pathsRoot.transform, true);

        RoadNode[] nodes = new RoadNode[]
        {
            new RoadNode { z = 186.0f, x = -7.0f, width = 7.5f, y = 0.540f }, // North Gate
            new RoadNode { z = 172.0f, x = -7.4f, width = 6.8f, y = 0.535f },
            new RoadNode { z = 156.0f, x = -7.8f, width = 6.5f, y = 0.530f },
            new RoadNode { z = 140.0f, x = -7.5f, width = 6.6f, y = 0.525f },
            new RoadNode { z = 124.0f, x = -7.0f, width = 6.4f, y = 0.520f },
            new RoadNode { z = 108.0f, x = -6.5f, width = 6.6f, y = 0.515f },
            new RoadNode { z = 92.0f,  x = -6.2f, width = 6.8f, y = 0.510f },
            new RoadNode { z = 76.0f,  x = -6.5f, width = 7.0f, y = 0.505f },
            new RoadNode { z = 60.0f,  x = -6.7f, width = 7.4f, y = 0.502f },
            new RoadNode { z = 46.0f,  x = -6.6f, width = 8.2f, y = 0.500f },
            new RoadNode { z = 35.0f,  x = -6.5f, width = 9.5f, y = 0.498f }
        };

        for (int i = 0; i < nodes.Length - 1; i++)
        {
            var nA = nodes[i];
            var nB = nodes[i + 1];

            Vector3 midPos = new Vector3((nA.x + nB.x) * 0.5f, (nA.y + nB.y) * 0.5f, (nA.z + nB.z) * 0.5f);
            float lengthZ = Mathf.Abs(nA.z - nB.z) + 1.2f;
            float avgWidth = (nA.width + nB.width) * 0.5f;
            float angleY = Mathf.Atan2(nB.x - nA.x, nB.z - nA.z) * Mathf.Rad2Deg;

            var seg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            seg.name = $"Road_Segment_{i:D2}_Z{(int)midPos.z}";
            seg.transform.SetParent(windingRoadRoot.transform, false);

            var col = seg.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            seg.transform.position = midPos;
            seg.transform.rotation = Quaternion.Euler(90f, angleY, 0f);
            seg.transform.localScale = new Vector3(avgWidth, lengthZ, 1.0f);

            var mr = seg.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = roadMat;
        }

        EditorUtility.SetDirty(windingRoadRoot);
    }

    private static void DeSquareRicePaddies()
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

        // ORGANIC, ASYMMETRICAL, NON-SQUARE PLOT LAYOUT
        DeSquaredPlot[] plots = new DeSquaredPlot[]
        {
            // South plots (village transition)
            new DeSquaredPlot { id = 1,  suffix = "_Golden", posX = -43.0f, posZ = 60.5f,  scaleX = 22.0f, scaleZ = 12.0f, rotY = -3.0f },
            new DeSquaredPlot { id = 2,  suffix = "_Golden", posX = -20.0f, posZ = 61.5f,  scaleX = 18.0f, scaleZ = 11.5f, rotY = +2.5f },
            new DeSquaredPlot { id = 3,  suffix = "_Golden", posX = 6.0f,   posZ = 62.5f,  scaleX = 16.5f, scaleZ = 11.0f, rotY = -2.5f },
            new DeSquaredPlot { id = 4,  suffix = "_Green",  posX = 26.5f,  posZ = 61.5f,  scaleX = 20.0f, scaleZ = 12.0f, rotY = +3.5f },
            new DeSquaredPlot { id = 5,  suffix = "_Green",  posX = -33.0f, posZ = 47.0f,  scaleX = 39.0f, scaleZ = 11.5f, rotY = +2.0f },
            new DeSquaredPlot { id = 6,  suffix = "_Green",  posX = 15.0f,  posZ = 48.0f,  scaleX = 34.0f, scaleZ = 11.0f, rotY = -3.0f },

            // West Plots (asymmetrical terracing along West Mountain)
            new DeSquaredPlot { id = 7,  suffix = "_Golden", posX = -31.0f, posZ = 74.0f,  scaleX = 36.0f, scaleZ = 12.0f, rotY = -4.5f },
            new DeSquaredPlot { id = 8,  suffix = "_Green",  posX = -35.0f, posZ = 89.0f,  scaleX = 42.0f, scaleZ = 14.0f, rotY = +3.5f },
            new DeSquaredPlot { id = 11, suffix = "_Green",  posX = -30.5f, posZ = 105.5f, scaleX = 35.0f, scaleZ = 11.0f, rotY = -5.0f },
            new DeSquaredPlot { id = 12, suffix = "_Golden", posX = -36.0f, posZ = 120.0f, scaleX = 44.0f, scaleZ = 13.5f, rotY = +4.0f },
            new DeSquaredPlot { id = 15, suffix = "_Golden", posX = -31.0f, posZ = 136.0f, scaleX = 36.0f, scaleZ = 12.0f, rotY = -3.5f },
            new DeSquaredPlot { id = 16, suffix = "_Green",  posX = -35.0f, posZ = 151.0f, scaleX = 43.0f, scaleZ = 13.0f, rotY = +4.5f },
            new DeSquaredPlot { id = 19, suffix = "_Green",  posX = -29.5f, posZ = 165.5f, scaleX = 34.0f, scaleZ = 11.0f, rotY = -4.0f },
            new DeSquaredPlot { id = 20, suffix = "_Golden", posX = -33.5f, posZ = 178.5f, scaleX = 41.0f, scaleZ = 11.5f, rotY = +3.0f },

            // East Plots (staggered Z by 4m-7m from West side, hugging East Mountain)
            new DeSquaredPlot { id = 9,  suffix = "_Green",  posX = 13.5f,  posZ = 70.0f,  scaleX = 32.0f, scaleZ = 11.0f, rotY = +4.0f },
            new DeSquaredPlot { id = 10, suffix = "_Golden", posX = 17.5f,  posZ = 84.5f,  scaleX = 39.0f, scaleZ = 13.0f, rotY = -4.5f },
            new DeSquaredPlot { id = 13, suffix = "_Golden", posX = 14.0f,  posZ = 100.0f, scaleX = 33.0f, scaleZ = 12.0f, rotY = +3.5f },
            new DeSquaredPlot { id = 14, suffix = "_Green",  posX = 18.5f,  posZ = 115.5f, scaleX = 40.0f, scaleZ = 14.0f, rotY = -4.0f },
            new DeSquaredPlot { id = 17, suffix = "_Green",  posX = 15.0f,  posZ = 131.5f, scaleX = 34.0f, scaleZ = 12.0f, rotY = +4.5f },
            new DeSquaredPlot { id = 18, suffix = "_Golden", posX = 18.0f,  posZ = 146.5f, scaleX = 39.0f, scaleZ = 13.0f, rotY = -3.5f },
            new DeSquaredPlot { id = 21, suffix = "_Golden", posX = 14.5f,  posZ = 161.0f, scaleX = 33.0f, scaleZ = 11.5f, rotY = +4.0f },
            new DeSquaredPlot { id = 22, suffix = "_Green",  posX = 17.5f,  posZ = 175.0f, scaleX = 38.0f, scaleZ = 12.0f, rotY = -3.0f }
        };

        foreach (var p in plots)
        {
            string plotName = $"Paddy_Plot_{p.id:D2}{p.suffix}";
            if (!childMap.TryGetValue(plotName, out var plotObj)) continue;

            Vector3 origPos = plotObj.position;
            float tZ = Mathf.InverseLerp(45f, 180f, p.posZ);
            float targetY = Mathf.Lerp(0.495f, 0.540f, tZ);

            Vector3 newPos = new Vector3(p.posX, targetY, p.posZ);
            Quaternion newRot = Quaternion.Euler(90f, p.rotY, 0f);
            Vector3 delta = newPos - origPos;

            plotObj.position = newPos;
            plotObj.rotation = newRot;
            plotObj.localScale = new Vector3(p.scaleX, p.scaleZ, 1.0f);

            // Update Dikes (walking paths) around plot
            UpdatePlotDikes(p, childMap, newPos, newRot, p.scaleX, p.scaleZ, targetY);

            // Shift rice tufts
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

        // Add intermediate dividing dikes (breaks wide 40m slabs into two natural terraced plots!)
        AddIntermediateTerraceDikes();

        EditorUtility.SetDirty(root);
    }

    private static void UpdatePlotDikes(DeSquaredPlot p, Dictionary<string, Transform> childMap, Vector3 plotPos, Quaternion plotRot, float scaleX, float scaleZ, float targetY)
    {
        string prefix = $"Paddy_Plot_{p.id:D2}{p.suffix}_Dike";
        float halfX = scaleX * 0.5f;
        float halfZ = scaleZ * 0.5f;
        float dikeY = targetY + 0.030f;

        float pathWidthZ = 2.4f;
        float roadMarginX = 3.0f;

        // North Dike
        if (childMap.TryGetValue(prefix + "N", out var dikeN))
        {
            Vector3 worldPos = plotPos + (plotRot * new Vector3(0f, halfZ + pathWidthZ * 0.5f, 0f));
            worldPos.y = dikeY;
            dikeN.position = worldPos;
            dikeN.rotation = Quaternion.Euler(0f, p.rotY, 0f);
            dikeN.localScale = new Vector3(scaleX + roadMarginX, 0.08f, pathWidthZ);
        }

        // South Dike
        if (childMap.TryGetValue(prefix + "S", out var dikeS))
        {
            Vector3 worldPos = plotPos + (plotRot * new Vector3(0f, -halfZ - pathWidthZ * 0.5f, 0f));
            worldPos.y = dikeY;
            dikeS.position = worldPos;
            dikeS.rotation = Quaternion.Euler(0f, p.rotY, 0f);
            dikeS.localScale = new Vector3(scaleX + roadMarginX, 0.08f, pathWidthZ);
        }

        // East Dike
        if (childMap.TryGetValue(prefix + "E", out var dikeE))
        {
            bool isFacingRoad = (plotPos.x < -7.0f);
            float w = isFacingRoad ? roadMarginX : 1.6f;
            Vector3 worldPos = plotPos + (plotRot * new Vector3(halfX + w * 0.5f, 0f, 0f));
            worldPos.y = dikeY;
            dikeE.position = worldPos;
            dikeE.rotation = Quaternion.Euler(0f, p.rotY, 0f);
            dikeE.localScale = new Vector3(w, 0.08f, scaleZ + pathWidthZ * 1.5f);
        }

        // West Dike
        if (childMap.TryGetValue(prefix + "W", out var dikeW))
        {
            bool isFacingRoad = (plotPos.x > -7.0f);
            float w = isFacingRoad ? roadMarginX : 1.6f;
            Vector3 worldPos = plotPos + (plotRot * new Vector3(-halfX - w * 0.5f, 0f, 0f));
            worldPos.y = dikeY;
            dikeW.position = worldPos;
            dikeW.rotation = Quaternion.Euler(0f, p.rotY, 0f);
            dikeW.localScale = new Vector3(w, 0.08f, scaleZ + pathWidthZ * 1.5f);
        }
    }

    private static void AddIntermediateTerraceDikes()
    {
        var root = GameObject.Find("Paddy_5X_Extension_Root");
        if (root == null) return;

        var intermRoot = GameObject.Find("Intermediate_Terrace_Dikes_Root");
        if (intermRoot != null) Object.DestroyImmediate(intermRoot);
        intermRoot = new GameObject("Intermediate_Terrace_Dikes_Root");
        intermRoot.transform.SetParent(root.transform, true);

        var dikeNSMat = AssetDatabase.LoadAssetAtPath<Material>(DikeNSMatPath);

        // Subdivides the wide West slabs (X ~ -32m) into 2 organic square-like fields
        float[] westDikeZs = new float[] { 74f, 89f, 105.5f, 120f, 136f, 151f, 165.5f, 178.5f };
        for (int i = 0; i < westDikeZs.Length; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"West_Intermediate_Dike_{i:D2}";
            go.transform.SetParent(intermRoot.transform, false);

            float tZ = Mathf.InverseLerp(45f, 180f, westDikeZs[i]);
            float y = Mathf.Lerp(0.495f, 0.540f, tZ) + 0.035f;

            float xOffset = (i % 2 == 0) ? -32.5f : -34.0f;
            go.transform.position = new Vector3(xOffset, y, westDikeZs[i]);
            go.transform.rotation = Quaternion.Euler(0f, (i % 2 == 0 ? 3.5f : -4.0f), 0f);
            go.transform.localScale = new Vector3(1.8f, 0.08f, 13.5f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = dikeNSMat;
        }

        // Subdivides the wide East slabs (X ~ +15m) into 2 organic fields
        float[] eastDikeZs = new float[] { 70f, 84.5f, 100f, 115.5f, 131.5f, 146.5f, 161f, 175f };
        for (int i = 0; i < eastDikeZs.Length; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"East_Intermediate_Dike_{i:D2}";
            go.transform.SetParent(intermRoot.transform, false);

            float tZ = Mathf.InverseLerp(45f, 180f, eastDikeZs[i]);
            float y = Mathf.Lerp(0.495f, 0.540f, tZ) + 0.035f;

            float xOffset = (i % 2 == 0) ? 16.5f : 18.0f;
            go.transform.position = new Vector3(xOffset, y, eastDikeZs[i]);
            go.transform.rotation = Quaternion.Euler(0f, (i % 2 == 0 ? -3.0f : 4.2f), 0f);
            go.transform.localScale = new Vector3(1.8f, 0.08f, 13.5f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = dikeNSMat;
        }

        EditorUtility.SetDirty(intermRoot);
    }

    private static void AddRoadsideFlora()
    {
        var vegRoot = GameObject.Find("Vegetation_3D");
        if (vegRoot == null) return;

        var floraRoot = GameObject.Find("Road_Organic_Flora_Root");
        if (floraRoot != null) Object.DestroyImmediate(floraRoot);
        floraRoot = new GameObject("Road_Organic_Flora_Root");
        floraRoot.transform.SetParent(vegRoot.transform, true);

        var palmPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
        var bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");
        var bambooPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
        var bushPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Bushes/PF_LanaVN_Bush_Low_01.prefab");
        var rockPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Rocks/PF_LanaVN_Rock_05_SmallMossy.prefab");

        FloraSpot[] spots = new FloraSpot[]
        {
            // Gate approach & upper valley
            new FloraSpot { pos = new Vector3(-11.5f, 0.54f, 175.0f), rotY = 45f, scale = 1.1f, type = 0 },
            new FloraSpot { pos = new Vector3(-3.2f,  0.54f, 171.0f), rotY = 120f, scale = 0.95f, type = 1 },
            new FloraSpot { pos = new Vector3(-12.0f, 0.53f, 158.0f), rotY = 80f, scale = 1.0f, type = 3 },
            new FloraSpot { pos = new Vector3(-2.8f,  0.53f, 152.0f), rotY = 195f, scale = 1.1f, type = 0 },
            
            // Mid-valley terrace corners & roadside
            new FloraSpot { pos = new Vector3(-2.8f,  0.52f, 138.0f), rotY = 210f, scale = 1.05f, type = 0 },
            new FloraSpot { pos = new Vector3(-11.8f, 0.52f, 126.0f), rotY = 15f,  scale = 1.0f, type = 1 },
            new FloraSpot { pos = new Vector3(-2.5f,  0.51f, 108.0f), rotY = 60f,  scale = 1.0f, type = 2 },
            new FloraSpot { pos = new Vector3(-11.2f, 0.51f, 96.0f),  rotY = 135f, scale = 1.1f, type = 0 },
            new FloraSpot { pos = new Vector3(-2.4f,  0.51f, 90.0f),  rotY = 160f, scale = 1.0f, type = 1 },

            // Lower terrace & village approach
            new FloraSpot { pos = new Vector3(-2.6f,  0.50f, 82.0f),  rotY = 280f, scale = 1.0f, type = 1 },
            new FloraSpot { pos = new Vector3(-11.0f, 0.50f, 68.0f),  rotY = 30f,  scale = 0.9f, type = 3 },
            new FloraSpot { pos = new Vector3(-2.2f,  0.49f, 52.0f),  rotY = 190f, scale = 1.15f, type = 0 },
            new FloraSpot { pos = new Vector3(-11.5f, 0.49f, 44.0f),  rotY = 75f,  scale = 1.0f, type = 2 },

            // Mountain skirt encroaching vegetation (breaks outer mountain linear edges)
            new FloraSpot { pos = new Vector3(-53.0f, 0.53f, 160.0f), rotY = 40f,  scale = 1.3f, type = 0 },
            new FloraSpot { pos = new Vector3(-54.0f, 0.52f, 125.0f), rotY = 110f, scale = 1.2f, type = 1 },
            new FloraSpot { pos = new Vector3(-52.0f, 0.51f, 95.0f),  rotY = 230f, scale = 1.4f, type = 2 },
            new FloraSpot { pos = new Vector3(37.0f,  0.53f, 155.0f), rotY = 85f,  scale = 1.3f, type = 0 },
            new FloraSpot { pos = new Vector3(39.0f,  0.52f, 120.0f), rotY = 175f, scale = 1.25f, type = 1 },
            new FloraSpot { pos = new Vector3(36.0f,  0.51f, 85.0f),  rotY = 310f, scale = 1.35f, type = 2 }
        };

        foreach (var sp in spots)
        {
            GameObject prefab = bushPrefab;
            if (sp.type == 0 && palmPrefab != null) prefab = palmPrefab;
            else if (sp.type == 1 && bananaPrefab != null) prefab = bananaPrefab;
            else if (sp.type == 2 && bambooPrefab != null) prefab = bambooPrefab;
            else if (sp.type == 4 && rockPrefab != null) prefab = rockPrefab;

            if (prefab == null) continue;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, floraRoot.transform);
            inst.transform.position = sp.pos;
            inst.transform.rotation = Quaternion.Euler(0f, sp.rotY, 0f);
            inst.transform.localScale = Vector3.one * sp.scale;
        }

        EditorUtility.SetDirty(floraRoot);
    }

    private static void VerifyPlayerSpawn()
    {
        var player = GameObject.Find("Player_An_3D");
        if (player == null) player = GameObject.Find("Player_An");
        if (player != null)
        {
            player.transform.position = new Vector3(-7.0f, 0.55f, 182.0f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            EditorUtility.SetDirty(player);
        }
    }
}
