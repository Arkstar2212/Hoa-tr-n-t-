using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class SculptOrganicPaddyTerrain
{
    private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

    private const string GrassTexPath = "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_baseColor_2k.png";
    private const string GrassNormPath = "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_normal_gl_2k.png";
    private const string BrownSoilTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownEarth_Pebbles_Seamless.png";
    private const string BrownSoilNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownEarth_Pebbles_Normal.png";

    private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
    private const string DikeEWMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_EW.mat";
    private const string DikeNSMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_NS.mat";
    private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";
    private const string VillageSoilMatPath = "Assets/Materials/Environment/M_Ground_Grass_VillageSoil.mat";

    public struct PlotDef
    {
        public int id;
        public string suffix;
        public float posX;
        public float posZ;
        public float scaleX;
        public float scaleZ;
        public float rotY;
        public float baseY;
    }

    [MenuItem("Tools/Hoa Do/Sculpt Organic Rugged Paddy Terrain")]
    public static void Execute()
    {
        Debug.Log("[SculptTerrain] Sculpting genuine 3D rugged rice terraces and organic sloped embankments...");

        // 1. CONFIGURE MATERIALS: Lush green for plots and banks, brown earth for main road
        SetupMaterials();

        // 2. SCULPT RICE PLOTS AND ORGANIC SLOPED BERMS (Kéo lên gồ ghề)
        SculptPlotsAndBerms();

        // 3. RE-VERIFY PLAYER POSITION AT NORTH GATE
        VerifyPlayerPosition();

        // 4. SAVE ASSETS & MARK SCENE DIRTY
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );

        // 5. CAPTURE QA VERIFICATION SNAPSHOTS
        CaptureQASnapshots();

        Debug.Log("[SculptTerrain] Sculpting completed successfully!");
    }

    private static void SetupMaterials()
    {
        var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexPath);
        var grassNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassNormPath);
        var brownSoilTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownSoilTexPath);
        var brownSoilNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownSoilNormPath);

        Color lushGrassColor = new Color(0.48f, 0.72f, 0.30f, 1.0f);
        // Slightly richer, denser green for the embankments/dikes so they have clear visual definition
        Color embankmentGreen = new Color(0.42f, 0.68f, 0.26f, 1.0f);

        // Main road: Brown earth with pebbles
        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
        if (roadMat != null && brownSoilTex != null)
        {
            ApplyMat(roadMat, brownSoilTex, brownSoilNorm, Color.white, new Vector2(3.0f, 35.0f), 0.04f);
        }

        // Dikes / Embankments: Rich sculpted grassy banks with normal relief
        var dikeEWMat = AssetDatabase.LoadAssetAtPath<Material>(DikeEWMatPath);
        if (dikeEWMat != null && grassTex != null)
        {
            ApplyMat(dikeEWMat, grassTex, grassNorm, embankmentGreen, new Vector2(12.0f, 2.0f), 0.06f);
        }

        var dikeNSMat = AssetDatabase.LoadAssetAtPath<Material>(DikeNSMatPath);
        if (dikeNSMat != null && grassTex != null)
        {
            ApplyMat(dikeNSMat, grassTex, grassNorm, embankmentGreen, new Vector2(2.0f, 10.0f), 0.06f);
        }

        // Paddy Mud / Beds: Vibrant paddy turf
        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        if (mudMat != null && grassTex != null)
        {
            ApplyMat(mudMat, grassTex, grassNorm, lushGrassColor, new Vector2(10.0f, 5.0f), 0.04f);
        }

        var villageSoilMat = AssetDatabase.LoadAssetAtPath<Material>(VillageSoilMatPath);
        if (villageSoilMat != null && grassTex != null)
        {
            ApplyMat(villageSoilMat, grassTex, grassNorm, lushGrassColor, new Vector2(18.0f, 18.0f), 0.04f);
        }
    }

    private static void ApplyMat(Material mat, Texture2D tex, Texture2D norm, Color col, Vector2 scale, float smoothness)
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader != null && mat.shader != litShader) mat.shader = litShader;

        mat.SetTexture("_BaseMap", tex);
        if (norm != null)
        {
            mat.SetTexture("_BumpMap", norm);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 1.25f);
        }
        mat.SetColor("_BaseColor", col);
        mat.SetFloat("_Smoothness", smoothness);
        mat.mainTextureScale = scale;
        EditorUtility.SetDirty(mat);
    }

    private static void SculptPlotsAndBerms()
    {
        var root = GameObject.Find("Paddy_5X_Extension_Root");
        if (root == null) return;

        var childMap = new Dictionary<string, Transform>(root.transform.childCount);
        var plotChildrenGroups = new Dictionary<string, List<Transform>>();

        for (int i = 0; i < root.transform.childCount; i++)
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

        // TANGIBLE TERRACED ELEVATION (Kéo lên gồ ghề: 0.525m -> 0.585m -> 0.655m)
        // Clean elevated steps above the 0.500m valley floor
        PlotDef[] plots = new PlotDef[]
        {
            // Southern transitional plots (Y = 0.520m)
            new PlotDef { id = 1,  suffix = "_Golden", posX = -42.0f, posZ = 60.0f,  scaleX = 23.0f, scaleZ = 12.5f, rotY = -3.0f, baseY = 0.520f },
            new PlotDef { id = 2,  suffix = "_Golden", posX = -20.5f, posZ = 61.5f,  scaleX = 18.5f, scaleZ = 11.5f, rotY = +2.2f, baseY = 0.520f },
            new PlotDef { id = 3,  suffix = "_Golden", posX = 6.5f,   posZ = 64.0f,  scaleX = 17.0f, scaleZ = 11.0f, rotY = -2.5f, baseY = 0.520f },
            new PlotDef { id = 4,  suffix = "_Green",  posX = 26.5f,  posZ = 63.0f,  scaleX = 21.0f, scaleZ = 12.5f, rotY = +3.5f, baseY = 0.520f },
            new PlotDef { id = 5,  suffix = "_Green",  posX = -33.5f, posZ = 46.5f,  scaleX = 40.0f, scaleZ = 11.5f, rotY = +1.8f, baseY = 0.515f },
            new PlotDef { id = 6,  suffix = "_Green",  posX = 15.5f,  posZ = 48.0f,  scaleX = 35.0f, scaleZ = 11.0f, rotY = -2.5f, baseY = 0.515f },

            // West Plots (Tier 1 to Tier 4: Y = 0.535m up to 0.585m)
            new PlotDef { id = 7,  suffix = "_Golden", posX = -32.0f, posZ = 75.0f,  scaleX = 39.0f, scaleZ = 12.0f, rotY = -3.8f, baseY = 0.535f },
            new PlotDef { id = 8,  suffix = "_Green",  posX = -35.5f, posZ = 91.0f,  scaleX = 45.0f, scaleZ = 13.5f, rotY = +3.0f, baseY = 0.545f },
            new PlotDef { id = 11, suffix = "_Green",  posX = -31.5f, posZ = 107.0f, scaleX = 38.0f, scaleZ = 11.5f, rotY = -3.2f, baseY = 0.555f },
            new PlotDef { id = 12, suffix = "_Golden", posX = -36.5f, posZ = 122.5f, scaleX = 46.0f, scaleZ = 13.0f, rotY = +4.0f, baseY = 0.565f },
            new PlotDef { id = 15, suffix = "_Golden", posX = -32.5f, posZ = 138.5f, scaleX = 40.0f, scaleZ = 12.0f, rotY = -2.5f, baseY = 0.575f },
            new PlotDef { id = 16, suffix = "_Green",  posX = -36.0f, posZ = 154.0f, scaleX = 44.5f, scaleZ = 13.5f, rotY = +3.5f, baseY = 0.585f },
            new PlotDef { id = 19, suffix = "_Green",  posX = -31.0f, posZ = 168.0f, scaleX = 37.0f, scaleZ = 11.0f, rotY = -3.5f, baseY = 0.590f },
            new PlotDef { id = 20, suffix = "_Golden", posX = -35.0f, posZ = 181.5f, scaleX = 43.0f, scaleZ = 12.0f, rotY = +2.5f, baseY = 0.595f },

            // East Plots (Z staggered by +6m to +9m, Y = 0.535m up to 0.595m)
            new PlotDef { id = 9,  suffix = "_Green",  posX = 16.0f,  posZ = 81.0f,  scaleX = 35.0f, scaleZ = 12.5f, rotY = +3.2f, baseY = 0.535f },
            new PlotDef { id = 10, suffix = "_Golden", posX = 19.5f,  posZ = 97.0f,  scaleX = 41.5f, scaleZ = 13.0f, rotY = -4.0f, baseY = 0.545f },
            new PlotDef { id = 13, suffix = "_Golden", posX = 16.5f,  posZ = 113.0f, scaleX = 36.0f, scaleZ = 11.5f, rotY = +2.8f, baseY = 0.555f },
            new PlotDef { id = 14, suffix = "_Green",  posX = 20.0f,  posZ = 129.0f, scaleX = 42.0f, scaleZ = 13.5f, rotY = -3.5f, baseY = 0.565f },
            new PlotDef { id = 17, suffix = "_Green",  posX = 16.5f,  posZ = 145.0f, scaleX = 37.0f, scaleZ = 12.0f, rotY = +3.0f, baseY = 0.575f },
            new PlotDef { id = 18, suffix = "_Golden", posX = 19.5f,  posZ = 161.0f, scaleX = 41.0f, scaleZ = 12.5f, rotY = -2.8f, baseY = 0.585f },
            new PlotDef { id = 21, suffix = "_Golden", posX = 16.0f,  posZ = 174.5f, scaleX = 35.0f, scaleZ = 11.5f, rotY = +3.5f, baseY = 0.590f },
            new PlotDef { id = 22, suffix = "_Green",  posX = 18.5f,  posZ = 186.0f, scaleX = 39.0f, scaleZ = 11.0f, rotY = -2.5f, baseY = 0.595f }
        };

        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        var dikeEWMat = AssetDatabase.LoadAssetAtPath<Material>(DikeEWMatPath);
        var dikeNSMat = AssetDatabase.LoadAssetAtPath<Material>(DikeNSMatPath);

        foreach (var p in plots)
        {
            string plotName = $"Paddy_Plot_{p.id:D2}{p.suffix}";
            if (!childMap.TryGetValue(plotName, out var plotObj)) continue;

            // 1. PROCEDURAL 3D SCULPTED PLOT BED (Subdivided surface with rugged organic relief)
            var mfPlot = plotObj.GetComponent<MeshFilter>();
            if (mfPlot != null)
            {
                Mesh plotMesh = CreateSculptedPlotBedMesh(p.scaleX, p.scaleZ, p.id);
                mfPlot.sharedMesh = plotMesh;
            }
            plotObj.position = new Vector3(p.posX, p.baseY, p.posZ);
            plotObj.rotation = Quaternion.Euler(0f, p.rotY, 0f);
            plotObj.localScale = Vector3.one;

            var mrPlot = plotObj.GetComponent<MeshRenderer>();
            if (mrPlot != null && mudMat != null) mrPlot.sharedMaterial = mudMat;

            // 2. SCULPT TANGIBLE 3D ROUNDED SLOPED BERMS (35cm high, 2.5m wide sloped embankments)
            UpdateSculptedBerms(p, childMap, dikeEWMat, dikeNSMat);

            // 3. Anchor rice plants firmly onto the undulating bed
            if (plotChildrenGroups.TryGetValue(plotName, out var relatedList))
            {
                for (int j = 0; j < relatedList.Count; j++)
                {
                    var r = relatedList[j];
                    if (r == plotObj || r.name.Contains("Dike")) continue;
                    Vector3 rp = r.position;
                    // Follow terrain height
                    rp.y = p.baseY + Mathf.Sin((rp.x + rp.z) * 0.4f) * 0.02f;
                    r.position = rp;
                }
            }

            EditorUtility.SetDirty(plotObj);
        }
    }

    private static void UpdateSculptedBerms(PlotDef p, Dictionary<string, Transform> childMap, Material ewMat, Material nsMat)
    {
        string prefix = $"Paddy_Plot_{p.id:D2}{p.suffix}_Dike";
        float halfX = p.scaleX * 0.5f;
        float halfZ = p.scaleZ * 0.5f;
        Quaternion plotRot = Quaternion.Euler(0f, p.rotY, 0f);

        // Substantial, visible 3D earthen berm dimensions:
        // Height: 0.35m (clearly raised ridge above plot bed and floor)
        // Width: 2.6m (gentle sloped natural bank)
        float bermWidth = 2.6f;
        float bermHeight = 0.35f;

        // North Berm (EW orientation along North edge)
        if (childMap.TryGetValue(prefix + "N", out var dikeN))
        {
            Vector3 worldPos = new Vector3(p.posX, p.baseY, p.posZ) + (plotRot * new Vector3(0f, 0f, halfZ));
            dikeN.position = worldPos;
            dikeN.rotation = plotRot;
            dikeN.localScale = Vector3.one;

            var mf = dikeN.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = CreateSculptedBermMesh(p.scaleX + 2.4f, bermWidth, bermHeight, p.id * 17);
            }
            var mr = dikeN.GetComponent<MeshRenderer>();
            if (mr != null && ewMat != null) mr.sharedMaterial = ewMat;
            EditorUtility.SetDirty(dikeN);
        }

        // South Berm (EW orientation along South edge)
        if (childMap.TryGetValue(prefix + "S", out var dikeS))
        {
            Vector3 worldPos = new Vector3(p.posX, p.baseY, p.posZ) + (plotRot * new Vector3(0f, 0f, -halfZ));
            dikeS.position = worldPos;
            dikeS.rotation = plotRot;
            dikeS.localScale = Vector3.one;

            var mf = dikeS.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = CreateSculptedBermMesh(p.scaleX + 2.4f, bermWidth, bermHeight, p.id * 23);
            }
            var mr = dikeS.GetComponent<MeshRenderer>();
            if (mr != null && ewMat != null) mr.sharedMaterial = ewMat;
            EditorUtility.SetDirty(dikeS);
        }

        // East Berm (NS orientation along East edge)
        if (childMap.TryGetValue(prefix + "E", out var dikeE))
        {
            Vector3 worldPos = new Vector3(p.posX, p.baseY, p.posZ) + (plotRot * new Vector3(halfX, 0f, 0f));
            dikeE.position = worldPos;
            dikeE.rotation = plotRot * Quaternion.Euler(0f, 90f, 0f);
            dikeE.localScale = Vector3.one;

            var mf = dikeE.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = CreateSculptedBermMesh(p.scaleZ + 2.0f, bermWidth * 0.9f, bermHeight, p.id * 31);
            }
            var mr = dikeE.GetComponent<MeshRenderer>();
            if (mr != null && nsMat != null) mr.sharedMaterial = nsMat;
            EditorUtility.SetDirty(dikeE);
        }

        // West Berm (NS orientation along West edge)
        if (childMap.TryGetValue(prefix + "W", out var dikeW))
        {
            Vector3 worldPos = new Vector3(p.posX, p.baseY, p.posZ) + (plotRot * new Vector3(-halfX, 0f, 0f));
            dikeW.position = worldPos;
            dikeW.rotation = plotRot * Quaternion.Euler(0f, 90f, 0f);
            dikeW.localScale = Vector3.one;

            var mf = dikeW.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mf.sharedMesh = CreateSculptedBermMesh(p.scaleZ + 2.0f, bermWidth * 0.9f, bermHeight, p.id * 37);
            }
            var mr = dikeW.GetComponent<MeshRenderer>();
            if (mr != null && nsMat != null) mr.sharedMaterial = nsMat;
            EditorUtility.SetDirty(dikeW);
        }
    }

    /// <summary>
    /// Generates a smooth, sloped earthen berm with undulating crest (gồ ghề tự nhiên).
    /// Uses 6 cross-segments (7 points) for soft beveled slopes and rounded crest.
    /// </summary>
    private static Mesh CreateSculptedBermMesh(float length, float width, float baseHeight, int seed)
    {
        int lengthSegs = 24;
        int crossSegs = 6; // 7 points across width

        int vertCount = (lengthSegs + 1) * (crossSegs + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        int[] triangles = new int[lengthSegs * crossSegs * 6];

        float halfL = length * 0.5f;
        float halfW = width * 0.5f;

        // Smooth curved trapezoidal profile:
        // k=0: Left toe (Y=0, X=-1.0*halfW)
        // k=1: Lower left slope (Y=0.30*H, X=-0.70*halfW)
        // k=2: Upper left shoulder (Y=0.85*H, X=-0.35*halfW)
        // k=3: Center crest crown (Y=1.00*H, X=0.0)
        // k=4: Upper right shoulder (Y=0.85*H, X=+0.35*halfW)
        // k=5: Lower right slope (Y=0.30*H, X=+0.70*halfW)
        // k=6: Right toe (Y=0, X=+1.0*halfW)
        float[] profileX = { -1.0f, -0.70f, -0.35f, 0.0f, +0.35f, +0.70f, +1.0f };
        float[] profileY = {  0.0f,  0.30f,  0.85f, 1.0f,  0.85f,  0.30f,  0.0f };

        for (int i = 0; i <= lengthSegs; i++)
        {
            float t = (float)i / lengthSegs;
            float z = Mathf.Lerp(-halfL, halfL, t);

            // True organic undulating height ("gồ ghề"):
            // Combines primary and secondary terrain harmonics + seed
            float wave1 = Mathf.Sin(t * Mathf.PI * 3.2f + seed * 0.2f) * 0.20f;
            float wave2 = Mathf.Cos(t * Mathf.PI * 6.5f + seed * 0.35f) * 0.10f;
            float heightFactor = 1.0f + wave1 + wave2;

            // Smooth end taper so berm gently melts into the ground
            float endTaper = Mathf.Sin(t * Mathf.PI);
            float currentH = baseHeight * heightFactor * Mathf.Clamp01(endTaper * 2.2f);

            // Subtle lateral organic meandering (bờ ruộng uốn lượn nhẹ)
            float lateralDrift = Mathf.Sin(t * Mathf.PI * 2.5f + seed * 0.15f) * 0.22f;

            for (int k = 0; k <= crossSegs; k++)
            {
                int vIdx = i * (crossSegs + 1) + k;

                float x = profileX[k] * halfW + lateralDrift;
                float y = profileY[k] * currentH;

                vertices[vIdx] = new Vector3(x, y, z);
                uvs[vIdx] = new Vector2((float)k / crossSegs, t * (length / 4.0f));
            }
        }

        // Triangles with upward normal
        int triIdx = 0;
        for (int i = 0; i < lengthSegs; i++)
        {
            for (int k = 0; k < crossSegs; k++)
            {
                int rowA = i * (crossSegs + 1);
                int rowB = (i + 1) * (crossSegs + 1);

                int v0 = rowA + k;
                int v1 = rowA + k + 1;
                int v2 = rowB + k;
                int v3 = rowB + k + 1;

                triangles[triIdx++] = v0;
                triangles[triIdx++] = v1;
                triangles[triIdx++] = v2;

                triangles[triIdx++] = v2;
                triangles[triIdx++] = v1;
                triangles[triIdx++] = v3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = $"SculptedBerm_{length:F0}m_S{seed}";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Generates a subdivided undulating ground mesh for rice plot beds (thửa ruộng gồ ghề).
    /// </summary>
    private static Mesh CreateSculptedPlotBedMesh(float sizeX, float sizeZ, int seed)
    {
        int segsX = 14;
        int segsZ = 10;

        int vertCount = (segsX + 1) * (segsZ + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[segsX * segsZ * 6];

        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;

        for (int j = 0; j <= segsZ; j++)
        {
            float tz = (float)j / segsZ;
            float z = Mathf.Lerp(-halfZ, halfZ, tz);

            for (int i = 0; i <= segsX; i++)
            {
                float tx = (float)i / segsX;
                float x = Mathf.Lerp(-halfX, halfX, tx);

                int vIdx = j * (segsX + 1) + i;

                // Organic ground topography inside plot bed ("gồ ghề"):
                // Edge factor ensures boundary lines blend with berm feet
                float edgeFactor = Mathf.Sin(tx * Mathf.PI) * Mathf.Sin(tz * Mathf.PI);
                // Subtle natural hummocks and hollows
                float hummock1 = Mathf.Sin(tx * Mathf.PI * 4.0f + seed) * 0.035f;
                float hummock2 = Mathf.Cos(tz * Mathf.PI * 3.0f + seed * 0.5f) * 0.025f;
                float y = (hummock1 + hummock2) * edgeFactor;

                vertices[vIdx] = new Vector3(x, y, z);
                uvs[vIdx] = new Vector2(tx * (sizeX / 6.0f), tz * (sizeZ / 6.0f));
            }
        }

        int triIdx = 0;
        for (int j = 0; j < segsZ; j++)
        {
            for (int i = 0; i < segsX; i++)
            {
                int rowA = j * (segsX + 1);
                int rowB = (j + 1) * (segsX + 1);

                int v0 = rowA + i;
                int v1 = rowA + i + 1;
                int v2 = rowB + i;
                int v3 = rowB + i + 1;

                triangles[triIdx++] = v0;
                triangles[triIdx++] = v2;
                triangles[triIdx++] = v1;

                triangles[triIdx++] = v1;
                triangles[triIdx++] = v2;
                triangles[triIdx++] = v3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = $"SculptedPlotBed_{sizeX:F0}x{sizeZ:F0}_S{seed}";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static void VerifyPlayerPosition()
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

    private static void CaptureQASnapshots()
    {
        // 1. Spawn View (An at North Gate looking down into village)
        CaptureCameraView(
            new Vector3(-7.0f, 2.1f, 186.5f),
            Quaternion.Euler(14.0f, 180.0f, 0.0f),
            55.0f,
            Path.Combine(ArtifactDir, "CaMau_Sculpted_SpawnView_QA.jpg")
        );

        // 2. Elevated Overview of Entire Valley & Terraced Rugged Paddies
        CaptureCameraView(
            new Vector3(-4.0f, 42.0f, 215.0f),
            Quaternion.Euler(32.0f, 180.0f, 0.0f),
            48.0f,
            Path.Combine(ArtifactDir, "CaMau_Sculpted_ElevatedOverview_QA.jpg")
        );

        // 3. Close Perspective of the Rugged, Sloped Terraces & Rice Crops
        CaptureCameraView(
            new Vector3(-25.0f, 8.0f, 130.0f),
            Quaternion.Euler(18.0f, 120.0f, 0.0f),
            60.0f,
            Path.Combine(ArtifactDir, "CaMau_Sculpted_FieldPerspective_QA.jpg")
        );
    }

    private static void CaptureCameraView(Vector3 pos, Quaternion rot, float fov, string outputPath)
    {
        int width = 1280;
        int height = 720;

        GameObject camObj = new GameObject("Temp_QA_Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camObj.transform.position = pos;
        camObj.transform.rotation = rot;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;

        cam.Render();

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = null;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camObj);

        byte[] jpgBytes = tex.EncodeToJPG(80);
        Object.DestroyImmediate(tex);

        File.WriteAllBytes(outputPath, jpgBytes);
        Debug.Log($"[CaptureCameraView] Saved {outputPath}");
    }
}
