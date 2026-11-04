using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class OverhaulOrganicDirtRoadsAndPaddies
{
    private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

    // Textures
    private const string BrownSoilTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownEarth_Pebbles_Seamless.png";
    private const string BrownSoilNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownEarth_Pebbles_Normal.png";
    private const string SandTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_AuthenticEarth_Seamless.png";
    private const string SandNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_AuthenticEarth_Normal.png";
    private const string GrassTexPath = "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_baseColor_2k.png";
    private const string GrassNormPath = "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_normal_gl_2k.png";
    private const string MudTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownPaddyMud_Seamless.png";
    private const string MudNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownPaddyMud_Normal.png";

    // Materials
    private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
    private const string DikeEWMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_EW.mat";
    private const string DikeNSMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_NS.mat";
    private const string VillageSoilMatPath = "Assets/Materials/Environment/M_Ground_Grass_VillageSoil.mat";
    private const string SeabedSandMatPath = "Assets/Materials/M_Seabed_Sand.mat";
    private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";

    public struct PlotDefinition
    {
        public int id;
        public string suffix;
        public float posX;
        public float posZ;
        public float scaleX;
        public float scaleZ;
        public float rotY;
        public float targetY;
    }

    [MenuItem("Tools/Hoa Do/Overhaul Organic Dirt Roads And Paddies")]
    public static void Execute()
    {
        Debug.Log("[OverhaulOrganic] Starting overhaul: Brown dirt paths, lush green landscape, organic non-square layout...");

        // 1. CONFIGURE MATERIALS ACCORDING TO USER'S EXACT RULES
        SetupMaterials();

        // 2. CLEAN UP JAGGED / OBSOLETE OBJECTS
        CleanObsoleteObjects();

        // 3. DE-SQUARE AND ASYMMETRICALLY STAGGER PADDY PLOTS
        DeSquarePaddyPlots();

        // 4. BUILD SMOOTH ORGANIC WINDING DIRT ROAD
        BuildSmoothWindingRoad();

        // 5. ENSURE VILLAGE ALLEYS & PLAZA ARE BROWN DIRT
        SetupVillageAlleysAndPlaza();

        // 6. VERIFY PLAYER SPAWN AT NORTH GATE
        VerifyPlayerSpawn();

        // 7. SAVE ALL CHANGES
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        // 8. CAPTURE QA SNAPSHOTS
        CaptureQASnapshots();

        Debug.Log("[OverhaulOrganic] Completed successfully!");
    }

    private static void SetupMaterials()
    {
        var brownSoilTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownSoilTexPath);
        var brownSoilNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownSoilNormPath);
        var sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SandTexPath);
        var sandNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(SandNormPath);
        var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexPath);
        var grassNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassNormPath);
        var mudTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MudTexPath);
        var mudNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(MudNormPath);

        // A. MAIN COUNTRY ROAD: Brown earth with pebbles
        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
        if (roadMat != null && brownSoilTex != null)
        {
            ApplyMaterialProperties(roadMat, brownSoilTex, brownSoilNorm, Color.white, new Vector2(3.0f, 35.0f), 0.04f);
        }

        // B. PATHS BETWEEN RICE FIELDS (BỜ RUỘNG / ĐƯỜNG ĐI TỪNG THỬA RUỘNG):
        // User request: "phần đường đi từng thửa ruộng có màu xanh thì không hợp lí lắm. Chỉnh lại phần đó thành màu đất nâu có texture sỏi đá nhẹ một chút."
        var dikeEWMat = AssetDatabase.LoadAssetAtPath<Material>(DikeEWMatPath);
        if (dikeEWMat != null && brownSoilTex != null)
        {
            ApplyMaterialProperties(dikeEWMat, brownSoilTex, brownSoilNorm, new Color(0.96f, 0.92f, 0.88f, 1.0f), new Vector2(10.0f, 1.0f), 0.04f);
        }

        var dikeNSMat = AssetDatabase.LoadAssetAtPath<Material>(DikeNSMatPath);
        if (dikeNSMat != null && brownSoilTex != null)
        {
            ApplyMaterialProperties(dikeNSMat, brownSoilTex, brownSoilNorm, new Color(0.96f, 0.92f, 0.88f, 1.0f), new Vector2(1.0f, 5.0f), 0.04f);
        }

        // C. REST OF ENVIRONMENT: "Còn lại vẫn giữ màu xanh"
        // Valley floor, hill skirts, open spaces stay rich lush green grass!
        var villageSoilMat = AssetDatabase.LoadAssetAtPath<Material>(VillageSoilMatPath);
        if (villageSoilMat != null && grassTex != null)
        {
            Color lushGrassColor = new Color(0.48f, 0.72f, 0.30f, 1.0f);
            ApplyMaterialProperties(villageSoilMat, grassTex, grassNorm, lushGrassColor, new Vector2(18.0f, 18.0f), 0.04f);
        }

        // D. BEACH SHORE / SEABED: "Lấy cái này làm cát ngoài bờ biển"
        var seabedSandMat = AssetDatabase.LoadAssetAtPath<Material>(SeabedSandMatPath);
        if (seabedSandMat != null && sandTex != null)
        {
            ApplyMaterialProperties(seabedSandMat, sandTex, sandNorm, Color.white, new Vector2(10.0f, 6.0f), 0.05f);
        }

        // E. PADDY MUD (Under rice plants): River mud
        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        if (mudMat != null && mudTex != null)
        {
            ApplyMaterialProperties(mudMat, mudTex, mudNorm, Color.white, new Vector2(4.0f, 2.0f), 0.12f);
        }
    }

    private static void ApplyMaterialProperties(Material mat, Texture2D tex, Texture2D norm, Color color, Vector2 scale, float smoothness)
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader != null && mat.shader != litShader) mat.shader = litShader;

        mat.SetTexture("_BaseMap", tex);
        if (norm != null)
        {
            mat.SetTexture("_BumpMap", norm);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 1.0f);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.mainTextureScale = scale;
        EditorUtility.SetDirty(mat);
    }

    private static void CleanObsoleteObjects()
    {
        // Remove giant brown substrate if any exists
        var terracedRoot = GameObject.Find("Terraced_Earthen_Substrate_Root");
        if (terracedRoot != null) Object.DestroyImmediate(terracedRoot);

        var valleySubstrate = GameObject.Find("Valley_Earthen_Ground_Substrate");
        if (valleySubstrate != null) Object.DestroyImmediate(valleySubstrate);

        // Remove jagged stair-step quads
        var bermsRoot = GameObject.Find("Road_Organic_Berms_Root");
        if (bermsRoot != null) Object.DestroyImmediate(bermsRoot);

        // Hide straight runway quad if winding road is used
        var straightRoad = GameObject.Find("Road_North_StraightPaddyAisle");
        if (straightRoad != null)
        {
            // Disable straight road renderer so smooth winding road renders cleanly
            var mr = straightRoad.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        // Clean up any rogue quad at North Gate (Z between 175 and 188, X < -10)
        var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var mr in allRenderers)
        {
            if (mr.name.StartsWith("Terrain_Patch_") || mr.name.StartsWith("Gate_Patch_"))
            {
                Object.DestroyImmediate(mr.gameObject);
            }
        }
    }

    private static void DeSquarePaddyPlots()
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

        // ORGANIC, ASYMMETRICAL, NON-SQUARE PADDY TERRACES
        // 1. Z-stagger between West and East sides by 5m - 8m (breaks horizontal parallel bars!)
        // 2. Varied widths and depths (scaleX from 31m to 46m, scaleZ from 10.5m to 14.5m)
        // 3. Gentle natural valley curvature (rotY from -4.5° to +4.5°)
        // 4. Subtle terraced terrace height step (targetY ~ 0.47m to 0.52m)
        PlotDefinition[] plots = new PlotDefinition[]
        {
            // Southern transitional plots (near village boundary)
            new PlotDefinition { id = 1,  suffix = "_Golden", posX = -42.0f, posZ = 60.0f,  scaleX = 23.0f, scaleZ = 12.5f, rotY = -3.0f, targetY = 0.470f },
            new PlotDefinition { id = 2,  suffix = "_Golden", posX = -20.5f, posZ = 61.5f,  scaleX = 18.5f, scaleZ = 11.5f, rotY = +2.2f, targetY = 0.470f },
            new PlotDefinition { id = 3,  suffix = "_Golden", posX = 6.5f,   posZ = 64.0f,  scaleX = 17.0f, scaleZ = 11.0f, rotY = -2.5f, targetY = 0.470f },
            new PlotDefinition { id = 4,  suffix = "_Green",  posX = 26.5f,  posZ = 63.0f,  scaleX = 21.0f, scaleZ = 12.5f, rotY = +3.5f, targetY = 0.470f },
            new PlotDefinition { id = 5,  suffix = "_Green",  posX = -33.5f, posZ = 46.5f,  scaleX = 40.0f, scaleZ = 11.5f, rotY = +1.8f, targetY = 0.465f },
            new PlotDefinition { id = 6,  suffix = "_Green",  posX = 15.5f,  posZ = 48.0f,  scaleX = 35.0f, scaleZ = 11.0f, rotY = -2.5f, targetY = 0.465f },

            // West Plots (Flank West Mountain, Z = 74 to 175)
            new PlotDefinition { id = 7,  suffix = "_Golden", posX = -32.0f, posZ = 75.0f,  scaleX = 39.0f, scaleZ = 12.0f, rotY = -3.8f, targetY = 0.485f },
            new PlotDefinition { id = 8,  suffix = "_Green",  posX = -35.5f, posZ = 91.0f,  scaleX = 45.0f, scaleZ = 13.5f, rotY = +3.0f, targetY = 0.495f },
            new PlotDefinition { id = 11, suffix = "_Green",  posX = -31.5f, posZ = 107.0f, scaleX = 38.0f, scaleZ = 11.5f, rotY = -3.2f, targetY = 0.505f },
            new PlotDefinition { id = 12, suffix = "_Golden", posX = -36.5f, posZ = 122.5f, scaleX = 46.0f, scaleZ = 13.0f, rotY = +4.0f, targetY = 0.515f },
            new PlotDefinition { id = 15, suffix = "_Golden", posX = -32.5f, posZ = 138.5f, scaleX = 40.0f, scaleZ = 12.0f, rotY = -2.5f, targetY = 0.520f },
            new PlotDefinition { id = 16, suffix = "_Green",  posX = -36.0f, posZ = 154.0f, scaleX = 44.5f, scaleZ = 13.5f, rotY = +3.5f, targetY = 0.525f },
            new PlotDefinition { id = 19, suffix = "_Green",  posX = -31.0f, posZ = 168.0f, scaleX = 37.0f, scaleZ = 11.0f, rotY = -3.5f, targetY = 0.530f },
            new PlotDefinition { id = 20, suffix = "_Golden", posX = -35.0f, posZ = 181.5f, scaleX = 43.0f, scaleZ = 12.0f, rotY = +2.5f, targetY = 0.535f },

            // East Plots (Flank East Mountain, Z STAGGERED by +6m to +9m from West side!)
            // Notice Z coordinates: 81, 97, 113, 129, 145, 161, 175 (No more straight horizontal bars!)
            new PlotDefinition { id = 9,  suffix = "_Green",  posX = 16.0f,  posZ = 81.0f,  scaleX = 35.0f, scaleZ = 12.5f, rotY = +3.2f, targetY = 0.485f },
            new PlotDefinition { id = 10, suffix = "_Golden", posX = 19.5f,  posZ = 97.0f,  scaleX = 41.5f, scaleZ = 13.0f, rotY = -4.0f, targetY = 0.495f },
            new PlotDefinition { id = 13, suffix = "_Golden", posX = 16.5f,  posZ = 113.0f, scaleX = 36.0f, scaleZ = 11.5f, rotY = +2.8f, targetY = 0.505f },
            new PlotDefinition { id = 14, suffix = "_Green",  posX = 20.0f,  posZ = 129.0f, scaleX = 42.0f, scaleZ = 13.5f, rotY = -3.5f, targetY = 0.515f },
            new PlotDefinition { id = 17, suffix = "_Green",  posX = 16.5f,  posZ = 145.0f, scaleX = 37.0f, scaleZ = 12.0f, rotY = +3.0f, targetY = 0.520f },
            new PlotDefinition { id = 18, suffix = "_Golden", posX = 19.5f,  posZ = 161.0f, scaleX = 41.0f, scaleZ = 12.5f, rotY = -2.8f, targetY = 0.525f },
            new PlotDefinition { id = 21, suffix = "_Golden", posX = 16.0f,  posZ = 174.5f, scaleX = 35.0f, scaleZ = 11.5f, rotY = +3.5f, targetY = 0.530f },
            new PlotDefinition { id = 22, suffix = "_Green",  posX = 18.5f,  posZ = 186.0f, scaleX = 39.0f, scaleZ = 11.0f, rotY = -2.5f, targetY = 0.535f }
        };

        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        var dikeEWMat = AssetDatabase.LoadAssetAtPath<Material>(DikeEWMatPath);
        var dikeNSMat = AssetDatabase.LoadAssetAtPath<Material>(DikeNSMatPath);

        foreach (var p in plots)
        {
            string plotName = $"Paddy_Plot_{p.id:D2}{p.suffix}";
            if (!childMap.TryGetValue(plotName, out var plotObj)) continue;

            Vector3 origPos = plotObj.position;
            Vector3 newPos = new Vector3(p.posX, p.targetY, p.posZ);
            Quaternion newRot = Quaternion.Euler(90f, p.rotY, 0f);
            Vector3 delta = newPos - origPos;

            plotObj.position = newPos;
            plotObj.rotation = newRot;
            plotObj.localScale = new Vector3(p.scaleX, p.scaleZ, 1.0f);

            var mr = plotObj.GetComponent<MeshRenderer>();
            if (mr != null && mudMat != null) mr.sharedMaterial = mudMat;

            // Update surrounding earthen dikes (bờ ruộng đất nâu)
            UpdatePlotEarthenDikes(p, childMap, newPos, p.rotY, p.scaleX, p.scaleZ, p.targetY, dikeEWMat, dikeNSMat);

            // Shift all rice plants inside this plot so they stay rooted in the soil
            if (plotChildrenGroups.TryGetValue(plotName, out var relatedList))
            {
                for (int j = 0; j < relatedList.Count; j++)
                {
                    var r = relatedList[j];
                    if (r == plotObj || r.name.Contains("Dike")) continue;
                    r.position += delta;
                    Vector3 rp = r.position;
                    rp.y = p.targetY;
                    r.position = rp;
                }
            }

            EditorUtility.SetDirty(plotObj);
        }
    }

    private static void UpdatePlotEarthenDikes(PlotDefinition p, Dictionary<string, Transform> childMap, Vector3 plotPos, float rotY, float scaleX, float scaleZ, float targetY, Material ewMat, Material nsMat)
    {
        string prefix = $"Paddy_Plot_{p.id:D2}{p.suffix}_Dike";
        float halfX = scaleX * 0.5f;
        float halfZ = scaleZ * 0.5f;
        float dikeY = targetY + 0.035f;

        // Path width in Z (walking path along edge)
        float pathWidthZ = 2.4f;
        float roadMarginX = 2.6f;

        Quaternion dikeRot = Quaternion.Euler(0f, rotY, 0f);

        // North Dike (Lối đi bờ ruộng phía Bắc)
        if (childMap.TryGetValue(prefix + "N", out var dikeN))
        {
            Vector3 offset = dikeRot * new Vector3(0f, 0f, halfZ + pathWidthZ * 0.5f);
            Vector3 pos = plotPos + offset;
            pos.y = dikeY;
            dikeN.position = pos;
            dikeN.rotation = dikeRot;
            dikeN.localScale = new Vector3(scaleX + roadMarginX, 0.08f, pathWidthZ);
            var mr = dikeN.GetComponent<MeshRenderer>();
            if (mr != null && ewMat != null) mr.sharedMaterial = ewMat;
        }

        // South Dike (Lối đi bờ ruộng phía Nam)
        if (childMap.TryGetValue(prefix + "S", out var dikeS))
        {
            Vector3 offset = dikeRot * new Vector3(0f, 0f, -halfZ - pathWidthZ * 0.5f);
            Vector3 pos = plotPos + offset;
            pos.y = dikeY;
            dikeS.position = pos;
            dikeS.rotation = dikeRot;
            dikeS.localScale = new Vector3(scaleX + roadMarginX, 0.08f, pathWidthZ);
            var mr = dikeS.GetComponent<MeshRenderer>();
            if (mr != null && ewMat != null) mr.sharedMaterial = ewMat;
        }

        // East Dike (Lối đi bờ ruộng phía Đông)
        if (childMap.TryGetValue(prefix + "E", out var dikeE))
        {
            bool isFacingRoad = (plotPos.x < -7.0f);
            float w = isFacingRoad ? roadMarginX : 1.5f;
            Vector3 offset = dikeRot * new Vector3(halfX + w * 0.5f, 0f, 0f);
            Vector3 pos = plotPos + offset;
            pos.y = dikeY;
            dikeE.position = pos;
            dikeE.rotation = dikeRot;
            dikeE.localScale = new Vector3(w, 0.08f, scaleZ + pathWidthZ * 1.5f);
            var mr = dikeE.GetComponent<MeshRenderer>();
            if (mr != null && nsMat != null) mr.sharedMaterial = nsMat;
        }

        // West Dike (Lối đi bờ ruộng phía Tây)
        if (childMap.TryGetValue(prefix + "W", out var dikeW))
        {
            bool isFacingRoad = (plotPos.x > -7.0f);
            float w = isFacingRoad ? roadMarginX : 1.5f;
            Vector3 offset = dikeRot * new Vector3(-halfX - w * 0.5f, 0f, 0f);
            Vector3 pos = plotPos + offset;
            pos.y = dikeY;
            dikeW.position = pos;
            dikeW.rotation = dikeRot;
            dikeW.localScale = new Vector3(w, 0.08f, scaleZ + pathWidthZ * 1.5f);
            var mr = dikeW.GetComponent<MeshRenderer>();
            if (mr != null && nsMat != null) mr.sharedMaterial = nsMat;
        }
    }

    private static void BuildSmoothWindingRoad()
    {
        // Continuous smooth ribbon mesh from North Gate (Z=186) down to Village Plaza (Z=35)
        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);

        var roadObj = GameObject.Find("Winding_Country_Dirt_Road");
        if (roadObj == null)
        {
            roadObj = new GameObject("Winding_Country_Dirt_Road");
        }

        // CRITICAL: Ensure roadObj is at root with zero offset so local mesh vertices match world space
        roadObj.transform.SetParent(null);
        roadObj.transform.position = Vector3.zero;
        roadObj.transform.rotation = Quaternion.identity;
        roadObj.transform.localScale = Vector3.one;

        // Remove old children if any
        while (roadObj.transform.childCount > 0)
        {
            Object.DestroyImmediate(roadObj.transform.GetChild(0).gameObject);
        }

        var mf = roadObj.GetComponent<MeshFilter>();
        if (mf == null) mf = roadObj.AddComponent<MeshFilter>();

        var mr = roadObj.GetComponent<MeshRenderer>();
        if (mr == null) mr = roadObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = roadMat;
        mr.enabled = true;

        // Generate smooth procedural mesh with 60 cross sections
        int segments = 60;
        float startZ = 188.0f;
        float endZ = 35.0f;

        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];
        Vector3[] normals = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];

        float totalLength = Mathf.Abs(startZ - endZ);

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float z = Mathf.Lerp(startZ, endZ, t);

            // Natural gentle S-curve through the valley:
            // Gate (t=0): X = -7.0m
            // Mid-upper (t=0.3): gentle drift to -7.8m
            // Mid-lower (t=0.7): gentle curve to -6.4m
            // Plaza (t=1.0): X = -7.0m, expanding into wide plaza
            float curveOffset = Mathf.Sin(t * Mathf.PI * 2.0f) * 0.95f;
            float centerX = -7.0f + curveOffset;

            // Width: 7.5m at gate -> 7.0m along fields -> expands to 14m at plaza
            float width = 7.0f;
            if (t < 0.10f)
            {
                width = Mathf.Lerp(8.0f, 7.0f, t / 0.10f);
            }
            else if (t > 0.82f)
            {
                float plazaBlend = (t - 0.82f) / 0.18f;
                width = Mathf.Lerp(7.0f, 14.0f, plazaBlend * plazaBlend);
            }

            // Height sits crisply at Y = 0.518m down to 0.508m, cleanly overlaying the green base (Y = 0.500m)
            float y = Mathf.Lerp(0.518f, 0.508f, t);

            float halfW = width * 0.5f;
            int vIndex = i * 2;

            vertices[vIndex]     = new Vector3(centerX - halfW, y, z);
            vertices[vIndex + 1] = new Vector3(centerX + halfW, y, z);

            float vCoord = t * (totalLength / 5.0f); // Tiling along length
            uvs[vIndex]     = new Vector2(0f, vCoord);
            uvs[vIndex + 1] = new Vector2(1f, vCoord);

            normals[vIndex]     = Vector3.up;
            normals[vIndex + 1] = Vector3.up;

            if (i < segments)
            {
                int triIndex = i * 6;
                // Correct upward-facing winding order (Normal points +Y towards sky):
                triangles[triIndex]     = vIndex;
                triangles[triIndex + 1] = vIndex + 1;
                triangles[triIndex + 2] = vIndex + 2;

                triangles[triIndex + 3] = vIndex + 2;
                triangles[triIndex + 4] = vIndex + 1;
                triangles[triIndex + 5] = vIndex + 3;
            }
        }

        Mesh roadMesh = new Mesh();
        roadMesh.name = "M_WindingCountryRoad_Procedural";
        roadMesh.vertices = vertices;
        roadMesh.uv = uvs;
        roadMesh.normals = normals;
        roadMesh.triangles = triangles;
        roadMesh.RecalculateBounds();

        mf.sharedMesh = roadMesh;

        EditorUtility.SetDirty(roadObj);
    }

    private static void SetupVillageAlleysAndPlaza()
    {
        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
        if (roadMat == null) return;

        // Village plaza ground and alleyways
        var plazaObj = GameObject.Find("Village_Plaza_Ground");
        if (plazaObj != null)
        {
            var mr = plazaObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = roadMat;
        }

        // Search for all alley and path meshes inside the village
        var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var mr in allRenderers)
        {
            if (mr.name.Contains("Alley") || mr.name.Contains("VillagePath") || mr.name.Contains("Plaza_Dirt"))
            {
                mr.sharedMaterial = roadMat;
                EditorUtility.SetDirty(mr);
            }
        }
    }

    private static void VerifyPlayerSpawn()
    {
        var player = GameObject.Find("Player_An_3D");
        if (player == null) player = GameObject.Find("Player_An");
        if (player != null)
        {
            // Player standing at North Gate, facing South towards village
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
            Path.Combine(ArtifactDir, "CaMau_OrganicRoad_SpawnView_QA.jpg")
        );

        // 2. Elevated Overview of Entire Valley & Road
        CaptureCameraView(
            new Vector3(-4.0f, 42.0f, 215.0f),
            Quaternion.Euler(32.0f, 180.0f, 0.0f),
            48.0f,
            Path.Combine(ArtifactDir, "CaMau_OrganicRoad_ElevatedOverview_QA.jpg")
        );

        // 3. Diagonal Field Perspective across staggered organic rice plots
        CaptureCameraView(
            new Vector3(-25.0f, 8.0f, 130.0f),
            Quaternion.Euler(18.0f, 120.0f, 0.0f),
            60.0f,
            Path.Combine(ArtifactDir, "CaMau_OrganicRoad_FieldPerspective_QA.jpg")
        );

        // 4. Beach shore / Coastline view
        CaptureCameraView(
            new Vector3(-6.0f, 9.0f, 10.0f),
            Quaternion.Euler(12.0f, 180.0f, 0.0f),
            60.0f,
            Path.Combine(ArtifactDir, "CaMau_OrganicRoad_BeachCoast_QA.jpg")
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
