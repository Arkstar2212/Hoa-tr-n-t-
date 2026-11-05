using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class RevertFlatPlaneAndApplyGreenPebbleSoil
{
    private const int TexSize = 512;
    private const string TextureDir = "Assets/Textures/Ground_Tiles";
    private const string GreenPebbleTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_GreenGrassEarth_SparsePebbles_Seamless.png";
    private const string GreenPebbleNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_GreenGrassEarth_SparsePebbles_Normal.png";

    private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
    private const string DikeEWMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_EW.mat";
    private const string DikeNSMatPath = "Assets/Materials/Environment/M_Ground_Grass_PaddyDike_NS.mat";
    private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";
    private const string VillageSoilMatPath = "Assets/Materials/Environment/M_Ground_Grass_VillageSoil.mat";

    private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

    public struct PlotDef
    {
        public int id;
        public string suffix;
        public float posX;
        public float posZ;
        public float scaleX;
        public float scaleZ;
        public float rotY;
    }

    [MenuItem("Tools/Hoa Do/Revert Flat Plane And Apply Green Pebble Soil")]
    public static void Execute()
    {
        Debug.Log("[RevertFlat] Generating green soil with sparse rock pebbles and flattening terrain...");

        // 1. GENERATE NEW TEXTURE: Green grass earth with sparse rock-colored pebbles
        GenerateGreenSoilWithSparseRockPebbles();

        // 2. CONFIGURE TEXTURE IMPORTERS & APPLY MATERIALS
        ApplyMaterials();

        // 3. FLATTEN PADDY PLOTS TO FLAT PLANES & DEACTIVATE CUBE DIKES
        FlattenPlotsAndRemoveCubeDikes();

        // 4. ENSURE SMOOTH FLAT ROAD
        EnsureFlatWindingRoad();

        // 5. VERIFY PLAYER AT NORTH GATE
        VerifyPlayer();

        // 6. SAVE ASSETS & SCENE
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );

        // 7. CAPTURE QA VERIFICATION SNAPSHOTS
        CaptureQASnapshots();

        Debug.Log("[RevertFlat] Completed successfully!");
    }

    private static void GenerateGreenSoilWithSparseRockPebbles()
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

        var colTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        var normTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        float[,] heightMap = new float[TexSize, TexSize];
        Color[] colors = new Color[TexSize * TexSize];

        // Green grass earth color palette (analogous to the brown earth, but green):
        // Deep rich green loam:
        Color deepGreen = new Color(0.20f, 0.34f, 0.14f);
        // Base vibrant earthy grass:
        Color baseGreen = new Color(0.30f, 0.48f, 0.20f);
        // Sun-kissed grass crest:
        Color lightGreen = new Color(0.42f, 0.60f, 0.27f);

        // Base seamless organic ground grain
        for (int y = 0; y < TexSize; y++)
        {
            float cv = cosA[y], sv = sinA[y];
            for (int x = 0; x < TexSize; x++)
            {
                float cu = cosA[x], su = sinA[x];
                float n0 = Mathf.PerlinNoise(cu * 1.5f + 14f, cv * 1.5f + 14f) * 0.45f;
                float n1 = Mathf.PerlinNoise(su * 4.0f + 28f, sv * 4.0f + 28f) * 0.25f;
                float n2 = Mathf.PerlinNoise(cu * 12.0f + 42f, cv * 12.0f + 42f) * 0.15f;

                // Fine stipple
                float fineGravel = Mathf.PerlinNoise(cu * 28.0f + 56f, cv * 28.0f + 56f) * 0.12f;
                float grain = (Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1f;

                float h = Mathf.Clamp01(n0 + n1 + n2 + fineGravel + (grain - 0.5f) * 0.03f);
                heightMap[x, y] = h;

                Color c = Color.Lerp(deepGreen, baseGreen, Mathf.SmoothStep(0.20f, 0.55f, h));
                c = Color.Lerp(c, lightGreen, Mathf.SmoothStep(0.50f, 0.85f, h));
                colors[y * TexSize + x] = c;
            }
        }

        // Add SPARSE rock-colored pebbles ("phần sỏi thì màu giống núi đá và chỉ ít sỏi thôi")
        // Colors sampled directly from mountain rock material (M_Mountain_Crag_Rock):
        // Midtone slate: (0.42, 0.49, 0.45), Highlight: (0.64, 0.67, 0.58), Shadow: (0.25, 0.32, 0.28)
        var rng = new System.Random(1954);
        int numPebbles = 24; // Only 24 sparse pebbles across the entire 512x512 tile!

        for (int i = 0; i < numPebbles; i++)
        {
            int cx = rng.Next(0, TexSize);
            int cy = rng.Next(0, TexSize);
            int rx = rng.Next(2, 5); // Small delicate pebbles (2 to 4 pixels)
            int ry = rng.Next(2, 4);

            float tone = (float)rng.NextDouble();
            // Rock color: warm limestone slate grey
            Color pCol = Color.Lerp(new Color(0.38f, 0.45f, 0.42f), new Color(0.62f, 0.66f, 0.58f), tone);
            if (rng.NextDouble() > 0.65) pCol = Color.Lerp(pCol, new Color(0.26f, 0.33f, 0.29f), 0.4f);

            for (int dy = -ry - 1; dy <= ry + 1; dy++)
            {
                int py = (cy + dy + TexSize) % TexSize;
                for (int dx = -rx - 1; dx <= rx + 1; dx++)
                {
                    int px = (cx + dx + TexSize) % TexSize;
                    float distSq = ((float)dx * dx) / (rx * rx) + ((float)dy * dy) / (ry * ry);
                    if (distSq < 1.0f)
                    {
                        float alpha = Mathf.SmoothStep(1.0f, 0.0f, distSq);
                        int idx = py * TexSize + px;
                        colors[idx] = Color.Lerp(colors[idx], pCol, alpha * 0.82f);
                        heightMap[px, py] = Mathf.Max(heightMap[px, py], 0.55f + alpha * 0.22f);
                    }
                }
            }
        }

        colTex.SetPixels(colors);
        colTex.Apply();
        File.WriteAllBytes(GreenPebbleTexPath, colTex.EncodeToPNG());
        Object.DestroyImmediate(colTex);

        // Sobel Normal Map
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
        File.WriteAllBytes(GreenPebbleNormPath, normTex.EncodeToPNG());
        Object.DestroyImmediate(normTex);

        AssetDatabase.ImportAsset(GreenPebbleTexPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(GreenPebbleNormPath, ImportAssetOptions.ForceUpdate);

        ConfigureImporter(GreenPebbleTexPath, false);
        ConfigureImporter(GreenPebbleNormPath, true);
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

    private static void ApplyMaterials()
    {
        var greenSoilTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GreenPebbleTexPath);
        var greenSoilNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(GreenPebbleNormPath);

        // Apply new green pebble soil to:
        // 1. Valley base
        var villageSoilMat = AssetDatabase.LoadAssetAtPath<Material>(VillageSoilMatPath);
        if (villageSoilMat != null && greenSoilTex != null)
        {
            ApplyMat(villageSoilMat, greenSoilTex, greenSoilNorm, Color.white, new Vector2(24.0f, 24.0f), 0.04f);
        }

        // 2. Rice field plots
        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
        if (mudMat != null && greenSoilTex != null)
        {
            ApplyMat(mudMat, greenSoilTex, greenSoilNorm, Color.white, new Vector2(8.0f, 4.0f), 0.04f);
        }

        // 3. Dikes / field boundaries
        var dikeEWMat = AssetDatabase.LoadAssetAtPath<Material>(DikeEWMatPath);
        if (dikeEWMat != null && greenSoilTex != null)
        {
            ApplyMat(dikeEWMat, greenSoilTex, greenSoilNorm, Color.white, new Vector2(10.0f, 1.0f), 0.04f);
        }

        var dikeNSMat = AssetDatabase.LoadAssetAtPath<Material>(DikeNSMatPath);
        if (dikeNSMat != null && greenSoilTex != null)
        {
            ApplyMat(dikeNSMat, greenSoilTex, greenSoilNorm, Color.white, new Vector2(1.0f, 6.0f), 0.04f);
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
            mat.SetFloat("_BumpScale", 1.0f);
        }
        mat.SetColor("_BaseColor", col);
        mat.SetFloat("_Smoothness", smoothness);
        mat.mainTextureScale = scale;
        EditorUtility.SetDirty(mat);
    }

    private static void FlattenPlotsAndRemoveCubeDikes()
    {
        var root = GameObject.Find("Paddy_5X_Extension_Root");
        if (root == null) return;

        // Create standard flat quad mesh
        Mesh flatQuadMesh = CreateFlatQuadMesh();

        // 1. Deactivate all cube dikes ("_DikeN", "_DikeS", "_DikeE", "_DikeW")
        // This completely eliminates any "cube đè lên" box appearance!
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var c = root.transform.GetChild(i);
            if (c.name.Contains("_Dike"))
            {
                var mr = c.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }

        // Asymmetric, organic, staggered plot definitions at clean uniform flat elevation (Y = 0.504m)
        PlotDef[] plots = new PlotDef[]
        {
            new PlotDef { id = 1,  suffix = "_Golden", posX = -42.0f, posZ = 60.0f,  scaleX = 23.0f, scaleZ = 12.5f, rotY = -3.0f },
            new PlotDef { id = 2,  suffix = "_Golden", posX = -20.5f, posZ = 61.5f,  scaleX = 18.5f, scaleZ = 11.5f, rotY = +2.2f },
            new PlotDef { id = 3,  suffix = "_Golden", posX = 6.5f,   posZ = 64.0f,  scaleX = 17.0f, scaleZ = 11.0f, rotY = -2.5f },
            new PlotDef { id = 4,  suffix = "_Green",  posX = 26.5f,  posZ = 63.0f,  scaleX = 21.0f, scaleZ = 12.5f, rotY = +3.5f },
            new PlotDef { id = 5,  suffix = "_Green",  posX = -33.5f, posZ = 46.5f,  scaleX = 40.0f, scaleZ = 11.5f, rotY = +1.8f },
            new PlotDef { id = 6,  suffix = "_Green",  posX = 15.5f,  posZ = 48.0f,  scaleX = 35.0f, scaleZ = 11.0f, rotY = -2.5f },

            // West Plots
            new PlotDef { id = 7,  suffix = "_Golden", posX = -32.0f, posZ = 75.0f,  scaleX = 39.0f, scaleZ = 12.0f, rotY = -3.8f },
            new PlotDef { id = 8,  suffix = "_Green",  posX = -35.5f, posZ = 91.0f,  scaleX = 45.0f, scaleZ = 13.5f, rotY = +3.0f },
            new PlotDef { id = 11, suffix = "_Green",  posX = -31.5f, posZ = 107.0f, scaleX = 38.0f, scaleZ = 11.5f, rotY = -3.2f },
            new PlotDef { id = 12, suffix = "_Golden", posX = -36.5f, posZ = 122.5f, scaleX = 46.0f, scaleZ = 13.0f, rotY = +4.0f },
            new PlotDef { id = 15, suffix = "_Golden", posX = -32.5f, posZ = 138.5f, scaleX = 40.0f, scaleZ = 12.0f, rotY = -2.5f },
            new PlotDef { id = 16, suffix = "_Green",  posX = -36.0f, posZ = 154.0f, scaleX = 44.5f, scaleZ = 13.5f, rotY = +3.5f },
            new PlotDef { id = 19, suffix = "_Green",  posX = -31.0f, posZ = 168.0f, scaleX = 37.0f, scaleZ = 11.0f, rotY = -3.5f },
            new PlotDef { id = 20, suffix = "_Golden", posX = -35.0f, posZ = 181.5f, scaleX = 43.0f, scaleZ = 12.0f, rotY = +2.5f },

            // East Plots
            new PlotDef { id = 9,  suffix = "_Green",  posX = 16.0f,  posZ = 81.0f,  scaleX = 35.0f, scaleZ = 12.5f, rotY = +3.2f },
            new PlotDef { id = 10, suffix = "_Golden", posX = 19.5f,  posZ = 97.0f,  scaleX = 41.5f, scaleZ = 13.0f, rotY = -4.0f },
            new PlotDef { id = 13, suffix = "_Golden", posX = 16.5f,  posZ = 113.0f, scaleX = 36.0f, scaleZ = 11.5f, rotY = +2.8f },
            new PlotDef { id = 14, suffix = "_Green",  posX = 20.0f,  posZ = 129.0f, scaleX = 42.0f, scaleZ = 13.5f, rotY = -3.5f },
            new PlotDef { id = 17, suffix = "_Green",  posX = 16.5f,  posZ = 145.0f, scaleX = 37.0f, scaleZ = 12.0f, rotY = +3.0f },
            new PlotDef { id = 18, suffix = "_Golden", posX = 19.5f,  posZ = 161.0f, scaleX = 41.0f, scaleZ = 12.5f, rotY = -2.8f },
            new PlotDef { id = 21, suffix = "_Golden", posX = 16.0f,  posZ = 174.5f, scaleX = 35.0f, scaleZ = 11.5f, rotY = +3.5f },
            new PlotDef { id = 22, suffix = "_Green",  posX = 18.5f,  posZ = 186.0f, scaleX = 39.0f, scaleZ = 11.0f, rotY = -2.5f }
        };

        var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);

        foreach (var p in plots)
        {
            string plotName = $"Paddy_Plot_{p.id:D2}{p.suffix}";
            var plotObj = GameObject.Find(plotName);
            if (plotObj == null) continue;

            // Flat plane at Y = 0.503m (3mm above valley floor base Y = 0.500m)
            plotObj.transform.position = new Vector3(p.posX, 0.503f, p.posZ);
            plotObj.transform.rotation = Quaternion.Euler(90f, p.rotY, 0f);
            plotObj.transform.localScale = new Vector3(p.scaleX, p.scaleZ, 1.0f);

            var mf = plotObj.GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = flatQuadMesh;

            var mr = plotObj.GetComponent<MeshRenderer>();
            if (mr != null && mudMat != null)
            {
                mr.sharedMaterial = mudMat;
                mr.enabled = true;
            }

            EditorUtility.SetDirty(plotObj);
        }

        // Anchor all rice plants at flat ground level Y = 0.505f
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var c = root.transform.GetChild(i);
            if (c.name.Contains("R0") || c.name.Contains("Tuft"))
            {
                Vector3 p = c.position;
                p.y = 0.505f;
                c.position = p;
            }
        }
    }

    private static Mesh CreateFlatQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "FlatQuad_Procedural";
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.normals = new Vector3[]
        {
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void EnsureFlatWindingRoad()
    {
        var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
        var roadObj = GameObject.Find("Winding_Country_Dirt_Road");
        if (roadObj == null) return;

        roadObj.transform.SetParent(null);
        roadObj.transform.position = Vector3.zero;
        roadObj.transform.rotation = Quaternion.identity;
        roadObj.transform.localScale = Vector3.one;

        var mf = roadObj.GetComponent<MeshFilter>();
        var mr = roadObj.GetComponent<MeshRenderer>();
        if (mr != null && roadMat != null)
        {
            mr.sharedMaterial = roadMat;
            mr.enabled = true;
        }

        // Re-generate smooth road at uniform flat height Y = 0.508m (overlaying the 0.500m ground and 0.503m plots)
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

            float curveOffset = Mathf.Sin(t * Mathf.PI * 2.0f) * 0.95f;
            float centerX = -7.0f + curveOffset;

            float width = 7.0f;
            if (t < 0.10f) width = Mathf.Lerp(8.0f, 7.0f, t / 0.10f);
            else if (t > 0.82f)
            {
                float plazaBlend = (t - 0.82f) / 0.18f;
                width = Mathf.Lerp(7.0f, 14.0f, plazaBlend * plazaBlend);
            }

            // Uniform flat height cleanly overlaying ground
            float y = 0.508f;

            float halfW = width * 0.5f;
            int vIndex = i * 2;

            vertices[vIndex]     = new Vector3(centerX - halfW, y, z);
            vertices[vIndex + 1] = new Vector3(centerX + halfW, y, z);

            float vCoord = t * (totalLength / 5.0f);
            uvs[vIndex]     = new Vector2(0f, vCoord);
            uvs[vIndex + 1] = new Vector2(1f, vCoord);

            normals[vIndex]     = Vector3.up;
            normals[vIndex + 1] = Vector3.up;

            if (i < segments)
            {
                int triIndex = i * 6;
                triangles[triIndex]     = vIndex;
                triangles[triIndex + 1] = vIndex + 1;
                triangles[triIndex + 2] = vIndex + 2;

                triangles[triIndex + 3] = vIndex + 2;
                triangles[triIndex + 4] = vIndex + 1;
                triangles[triIndex + 5] = vIndex + 3;
            }
        }

        Mesh roadMesh = new Mesh();
        roadMesh.name = "M_WindingCountryRoad_FlatProcedural";
        roadMesh.vertices = vertices;
        roadMesh.uv = uvs;
        roadMesh.normals = normals;
        roadMesh.triangles = triangles;
        roadMesh.RecalculateBounds();

        if (mf != null) mf.sharedMesh = roadMesh;
        EditorUtility.SetDirty(roadObj);
    }

    private static void VerifyPlayer()
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
            Path.Combine(ArtifactDir, "CaMau_FlatGreenSoil_SpawnView_QA.jpg")
        );

        // 2. Elevated Overview of Entire Valley & Road
        CaptureCameraView(
            new Vector3(-4.0f, 42.0f, 215.0f),
            Quaternion.Euler(32.0f, 180.0f, 0.0f),
            48.0f,
            Path.Combine(ArtifactDir, "CaMau_FlatGreenSoil_ElevatedOverview_QA.jpg")
        );

        // 3. Close Perspective of the Flat Green Soil with Sparse Rock-Colored Pebbles
        CaptureCameraView(
            new Vector3(-25.0f, 8.0f, 130.0f),
            Quaternion.Euler(18.0f, 120.0f, 0.0f),
            60.0f,
            Path.Combine(ArtifactDir, "CaMau_FlatGreenSoil_FieldPerspective_QA.jpg")
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
