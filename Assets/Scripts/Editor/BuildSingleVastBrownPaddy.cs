using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class BuildSingleVastBrownPaddy
{
    private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";
    private const string BrownEarthTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownEarth_Pebbles_Seamless.png";
    private const string BrownEarthNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_BrownEarth_Pebbles_Normal.png";
    private const string RiceMatPath = "Assets/Materials/RicePlants/Mat_Rice_Meshy.mat";
    private const string RiceMeshPath = "Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_0.fbx";
    private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
    private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

    [MenuItem("Tools/Hoa Do/Build Single Vast Brown Paddy")]
    public static void Execute()
    {
        Debug.Log("[VastBrownPaddy] Starting transformation into 1 single vast brown paddy field and thinning trees...");

        // 1. CONFIGURE BROWN PADDY EARTH MATERIAL
        ConfigureBrownPaddyMudMaterial();

        // 2. THIN OUT TREES AROUND THE PADDY FIELD ("hạn chế cây xung quanh đồng lúa")
        ThinTreesAroundPaddy();

        // 3. BUILD 1 SINGLE VAST PADDY FIELD MESH WITH BROWN EARTH (CLOCKWISE WINDING)
        BuildVastPaddyFieldMesh();

        // 4. CLEAN UP ALL OLD FRAGMENTED PLOTS, DIKES, AND OLD RICE
        CleanUpOldPlotsAndRice();

        // 5. POPULATE DENSE, CONTINUOUS RICE CROPS ACROSS THE ENTIRE VAST FIELD
        PopulateContinuousVastRiceCrops();

        // 6. VERIFY ROAD & PLAYER AN
        VerifyRoadAndPlayer();

        // 7. SAVE SCENE & ASSETS
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );

        // 8. CAPTURE QA SNAPSHOTS
        CaptureQASnapshots();

        Debug.Log("[VastBrownPaddy] Completed successfully!");
    }

    private static void ConfigureBrownPaddyMudMaterial()
    {
        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        if (mudMat == null) return;

        var brownTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownEarthTexPath);
        var brownNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownEarthNormPath);

        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader != null && mudMat.shader != litShader) mudMat.shader = litShader;

        if (brownTex != null) mudMat.SetTexture("_BaseMap", brownTex);
        if (brownNorm != null)
        {
            mudMat.SetTexture("_BumpMap", brownNorm);
            mudMat.EnableKeyword("_NORMALMAP");
            mudMat.SetFloat("_BumpScale", 1.15f);
        }

        // Rich warm brown soil color
        mudMat.SetColor("_BaseColor", new Color(0.68f, 0.54f, 0.42f, 1.0f));
        mudMat.SetFloat("_Smoothness", 0.04f);
        mudMat.mainTextureScale = new Vector2(16.0f, 32.0f);
        mudMat.enableInstancing = true;

        EditorUtility.SetDirty(mudMat);
    }

    private static void ThinTreesAroundPaddy()
    {
        int disabledCount = 0;

        // Specific tree parent groups bordering the paddy fields
        string[] targetGroups = new string[]
        {
            "Coconut_Paddy_West_01",
            "Coconut_Paddy_West_02",
            "Coconut_Paddy_East_01",
            "Coconut_Paddy_East_02",
            "West_Flank_Elements",
            "East_Flank_Elements"
        };

        foreach (var gName in targetGroups)
        {
            var go = GameObject.Find(gName);
            if (go != null)
            {
                go.SetActive(false);
                EditorUtility.SetDirty(go);
                disabledCount++;
                Debug.Log($"[ThinTrees] Deactivated border tree group: {gName}");
            }
        }

        // All trees standing directly in or around the paddy field area (Z: 48m to 180m, X: -48m to 42m)
        var allT = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in allT)
        {
            if (t == null) continue;
            string n = t.name.ToLower();

            // Check if this object or its parent is a tree/palm/coconut/banana/bamboo
            bool isFoliage = n.Contains("tree") || n.Contains("palm") || n.Contains("coconut") || 
                             n.Contains("banana") || n.Contains("bamboo");

            if (!isFoliage && t.parent != null)
            {
                string pn = t.parent.name.ToLower();
                if (pn.Contains("tree") || pn.Contains("palm") || pn.Contains("coconut") || 
                    pn.Contains("banana") || pn.Contains("bamboo") || pn == "road_organic_flora_root")
                {
                    isFoliage = true;
                }
            }

            if (isFoliage)
            {
                Vector3 p = t.position;
                // Only thin trees inside the paddy zone (Z 48 to 180, X -48 to 40)
                // Preserves North Gate (Z > 181), South Village (Z < 48), and Mountains (X < -48 or X > 42)
                if (p.z >= 48f && p.z <= 180f && p.x >= -48f && p.x <= 40f)
                {
                    if (t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        EditorUtility.SetDirty(t.gameObject);
                        disabledCount++;
                    }
                }
            }
        }

        Debug.Log($"[ThinTrees] Successfully thinned {disabledCount} foliage objects around the paddy field!");
    }

    private static void BuildVastPaddyFieldMesh()
    {
        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);

        // Find or create the single vast paddy field GameObject
        var vastPaddyObj = GameObject.Find("Vast_Single_Paddy_Field_BrownSoil");
        if (vastPaddyObj == null)
        {
            vastPaddyObj = new GameObject("Vast_Single_Paddy_Field_BrownSoil");
            vastPaddyObj.transform.position = Vector3.zero;
            vastPaddyObj.transform.rotation = Quaternion.identity;
            vastPaddyObj.transform.localScale = Vector3.one;
        }

        var mf = vastPaddyObj.GetComponent<MeshFilter>();
        if (mf == null) mf = vastPaddyObj.AddComponent<MeshFilter>();

        var mr = vastPaddyObj.GetComponent<MeshRenderer>();
        if (mr == null) mr = vastPaddyObj.AddComponent<MeshRenderer>();

        if (mudMat != null) mr.sharedMaterial = mudMat;

        // Generate a continuous subdivided brown soil plane spanning:
        // X: -48.0m (West mountain foot) to +40.0m (East mountain foot)
        // Z: 48.0m (South village edge) to 182.0m (North Gate edge)
        // Height: Y = 0.504m (clean flat plane, flush with 0.500m ground, road sits on top at 0.508m)
        int xSubdivs = 22;
        int zSubdivs = 66;

        float minX = -48.0f;
        float maxX = 40.0f;
        float minZ = 48.0f;
        float maxZ = 182.0f;
        float groundY = 0.504f;

        int vertCount = (xSubdivs + 1) * (zSubdivs + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        int[] triangles = new int[xSubdivs * zSubdivs * 6];

        for (int z = 0; z <= zSubdivs; z++)
        {
            float tz = (float)z / zSubdivs;
            float posZ = Mathf.Lerp(minZ, maxZ, tz);

            for (int x = 0; x <= xSubdivs; x++)
            {
                float tx = (float)x / xSubdivs;
                float posX = Mathf.Lerp(minX, maxX, tx);

                int idx = z * (xSubdivs + 1) + x;
                vertices[idx] = new Vector3(posX, groundY, posZ);
                normals[idx] = Vector3.up;
                uvs[idx] = new Vector2(tx * 16.0f, tz * 32.0f);
            }
        }

        // Triangles with CLOCKWISE winding viewed from above (+Y looking -Y)
        int triIdx = 0;
        for (int z = 0; z < zSubdivs; z++)
        {
            for (int x = 0; x < xSubdivs; x++)
            {
                int botLeft  = z * (xSubdivs + 1) + x;
                int botRight = botLeft + 1;
                int topLeft  = (z + 1) * (xSubdivs + 1) + x;
                int topRight = topLeft + 1;

                // Triangle 1: Top-Left -> Top-Right -> Bottom-Left (Clockwise from +Y)
                triangles[triIdx++] = topLeft;
                triangles[triIdx++] = topRight;
                triangles[triIdx++] = botLeft;

                // Triangle 2: Top-Right -> Bottom-Right -> Bottom-Left (Clockwise from +Y)
                triangles[triIdx++] = topRight;
                triangles[triIdx++] = botRight;
                triangles[triIdx++] = botLeft;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "M_VastSinglePaddy_BrownEarth_Subdivided";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mr.enabled = true;
        EditorUtility.SetDirty(vastPaddyObj);

        Debug.Log($"[VastBrownPaddy] Successfully generated 1 vast brown soil paddy plane with {vertCount} vertices and {triangles.Length / 3} triangles!");
    }

    private static void CleanUpOldPlotsAndRice()
    {
        // Deactivate old 22 plots and dikes in Paddy_5X_Extension_Root
        var root = GameObject.Find("Paddy_5X_Extension_Root");
        if (root != null)
        {
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var c = root.transform.GetChild(i);
                var mr = c.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                c.gameObject.SetActive(false);
            }
        }

        // Deactivate any leftover DenseFiller roots across scene
        var allGO = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGO)
        {
            if (go == null) continue;
            string n = go.name;
            if (n.Contains("_DenseFiller_") || (n.StartsWith("Paddy_Plot_") && n.Contains("_R0")))
            {
                go.SetActive(false);
            }
        }
    }

    private static void PopulateContinuousVastRiceCrops()
    {
        var riceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(RiceMeshPath);
        var riceMat = AssetDatabase.LoadAssetAtPath<Material>(RiceMatPath);

        if (riceMesh == null)
        {
            var sampleTuft = GameObject.Find("Tuft_Row_0");
            if (sampleTuft != null) riceMesh = sampleTuft.GetComponent<MeshFilter>()?.sharedMesh;
        }

        if (riceMesh == null || riceMat == null)
        {
            Debug.LogWarning("[PopulateRice] Could not load rice mesh or material.");
            return;
        }

        // Clean up previous container
        var container = GameObject.Find("Vast_Paddy_Continuous_Crops_Root");
        if (container != null)
        {
            Object.DestroyImmediate(container);
        }
        container = new GameObject("Vast_Paddy_Continuous_Crops_Root");
        container.transform.position = Vector3.zero;

        var rng = new System.Random(1954);
        int rows = 95; // Dense row spacing (~1.35m per row) across the 130m length
        float startZ = 180.0f;
        float endZ = 52.0f;
        float groundY = 0.505f;

        int totalTufts = 0;

        for (int r = 0; r < rows; r++)
        {
            float t = (float)r / (rows - 1);
            float z = Mathf.Lerp(startZ, endZ, t);

            float curveOffset = Mathf.Sin(t * Mathf.PI * 2.0f) * 0.95f;
            float roadCenterX = -7.0f + curveOffset;

            float roadHalfWidth = 3.6f;
            if (t < 0.10f) roadHalfWidth = Mathf.Lerp(4.0f, 3.6f, t / 0.10f);
            else if (t > 0.82f)
            {
                float plazaBlend = (t - 0.82f) / 0.18f;
                roadHalfWidth = Mathf.Lerp(3.6f, 7.0f, plazaBlend * plazaBlend);
            }

            // West flank: X from -45.0m to roadCenterX - roadHalfWidth - 0.8m
            float westMinX = -45.0f;
            float westMaxX = roadCenterX - roadHalfWidth - 0.8f;

            // East flank: X from roadCenterX + roadHalfWidth + 0.8m to +37.0m
            float eastMinX = roadCenterX + roadHalfWidth + 0.8f;
            float eastMaxX = 37.0f;

            // Step in X ~ 1.35m for dense carpet feel
            float stepX = 1.35f;

            // Populate West
            float westWidth = westMaxX - westMinX;
            if (westWidth > 2.0f)
            {
                int clusters = Mathf.Max(2, Mathf.RoundToInt(westWidth / stepX));
                for (int c = 0; c < clusters; c++)
                {
                    float frac = (float)c / (clusters - 1);
                    float posX = Mathf.Lerp(westMinX, westMaxX, frac) + ((float)rng.NextDouble() - 0.5f) * 0.35f;
                    float posZ = z + ((float)rng.NextDouble() - 0.5f) * 0.25f;

                    CreateRiceTuft(container.transform, riceMesh, riceMat, new Vector3(posX, groundY, posZ), rng);
                    totalTufts++;
                }
            }

            // Populate East
            float eastWidth = eastMaxX - eastMinX;
            if (eastWidth > 2.0f)
            {
                int clusters = Mathf.Max(2, Mathf.RoundToInt(eastWidth / stepX));
                for (int c = 0; c < clusters; c++)
                {
                    float frac = (float)c / (clusters - 1);
                    float posX = Mathf.Lerp(eastMinX, eastMaxX, frac) + ((float)rng.NextDouble() - 0.5f) * 0.35f;
                    float posZ = z + ((float)rng.NextDouble() - 0.5f) * 0.25f;

                    CreateRiceTuft(container.transform, riceMesh, riceMat, new Vector3(posX, groundY, posZ), rng);
                    totalTufts++;
                }
            }
        }

        Debug.Log($"[PopulateRice] Successfully created {totalTufts} dense rice tufts across the vast brown paddy field!");
    }

    private static void CreateRiceTuft(Transform parent, Mesh mesh, Material mat, Vector3 pos, System.Random rng)
    {
        var tuft = new GameObject("VastRice_Tuft");
        tuft.transform.SetParent(parent, false);
        tuft.transform.position = pos;

        float rotY = (float)rng.NextDouble() * 360f;
        tuft.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        // Lush, full scale so tufts blend seamlessly into an unbroken golden carpet
        float s = 1.45f + (float)rng.NextDouble() * 0.20f;
        float sy = s * (1.0f + (float)rng.NextDouble() * 0.15f);
        tuft.transform.localScale = new Vector3(s, sy, s);

        var mf = tuft.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = tuft.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
    }

    private static void VerifyRoadAndPlayer()
    {
        var roadObj = GameObject.Find("Winding_Country_Dirt_Road");
        if (roadObj != null)
        {
            var mr = roadObj.GetComponent<MeshRenderer>();
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
            if (mr != null && roadMat != null)
            {
                mr.sharedMaterial = roadMat;
                mr.enabled = true;
            }
        }

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
            Path.Combine(ArtifactDir, "CaMau_VastBrownPaddy_SpawnView_QA.jpg")
        );

        // 2. Elevated Overview of Entire Valley & Road
        CaptureCameraView(
            new Vector3(-4.0f, 42.0f, 215.0f),
            Quaternion.Euler(32.0f, 180.0f, 0.0f),
            48.0f,
            Path.Combine(ArtifactDir, "CaMau_VastBrownPaddy_ElevatedOverview_QA.jpg")
        );

        // 3. Close Perspective of the Vast Brown Soil Paddy with Dense Rice
        CaptureCameraView(
            new Vector3(-25.0f, 8.0f, 130.0f),
            Quaternion.Euler(18.0f, 120.0f, 0.0f),
            60.0f,
            Path.Combine(ArtifactDir, "CaMau_VastBrownPaddy_FieldPerspective_QA.jpg")
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
