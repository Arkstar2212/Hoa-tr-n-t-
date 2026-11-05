using UnityEngine;
using UnityEditor;
using System.IO;

public static class GenerateAuthenticEarthenSoilTextures
{
    private const int TexSize = 512;
    private const string TextureDir = "Assets/Textures/Ground_Tiles";
    private const string SoilTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_EarthenSoil_Pebbles_Seamless.png";
    private const string SoilNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_EarthenSoil_Normal.png";
    private const string MudTexPath = "Assets/Textures/Ground_Tiles/T_NamBo_PaddyMud_Seamless.png";
    private const string MudNormPath = "Assets/Textures/Ground_Tiles/T_NamBo_PaddyMud_Normal.png";

    [MenuItem("Tools/Hoa Do/Generate Authentic Earthen Soil Textures")]
    public static void Execute()
    {
        Debug.Log("[GenerateSoilTextures] Generating authentic seamless dirt and mud textures (fast pass)...");

        if (!Directory.Exists(TextureDir)) Directory.CreateDirectory(TextureDir);

        GenerateSoilWithPebblesFast(SoilTexPath, SoilNormPath);
        GeneratePaddyMudFast(MudTexPath, MudNormPath);

        AssetDatabase.Refresh();

        ConfigureTextureImporter(SoilTexPath, false);
        ConfigureTextureImporter(SoilNormPath, true);
        ConfigureTextureImporter(MudTexPath, false);
        ConfigureTextureImporter(MudNormPath, true);

        Debug.Log("[GenerateSoilTextures] Successfully generated and imported genuine soil & mud textures!");
    }

    private static void GenerateSoilWithPebblesFast(string colorPath, string normPath)
    {
        var colTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        var normTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        float[,] heightMap = new float[TexSize, TexSize];
        Color[] colors = new Color[TexSize * TexSize];

        Color darkSoil = new Color(0.32f, 0.20f, 0.12f);
        Color midEarth = new Color(0.48f, 0.33f, 0.22f);
        Color dryCrust = new Color(0.58f, 0.42f, 0.28f);

        // 1. Generate base seamless soil noise
        for (int y = 0; y < TexSize; y++)
        {
            float v = (float)y / TexSize;
            float angleV = v * Mathf.PI * 2f;
            float nz = Mathf.Cos(angleV) * 1.5f;
            float nw = Mathf.Sin(angleV) * 1.5f;

            for (int x = 0; x < TexSize; x++)
            {
                float u = (float)x / TexSize;
                float angleU = u * Mathf.PI * 2f;
                float nx = Mathf.Cos(angleU) * 1.5f;
                float ny = Mathf.Sin(angleU) * 1.5f;

                float baseNoise = Mathf.PerlinNoise(nx + 10f, nz + 10f) * 0.55f
                                + Mathf.PerlinNoise(ny * 2.5f + 20f, nw * 2.5f + 20f) * 0.30f
                                + Mathf.PerlinNoise(nx * 6f + 30f, nz * 6f + 30f) * 0.15f;

                float h = Mathf.Clamp01(baseNoise);
                heightMap[x, y] = h;

                Color c = Color.Lerp(darkSoil, midEarth, Mathf.SmoothStep(0.2f, 0.65f, h));
                c = Color.Lerp(c, dryCrust, Mathf.SmoothStep(0.6f, 0.95f, h));
                colors[y * TexSize + x] = c;
            }
        }

        // 2. Splat pebbles directly on bounding boxes (fast O(pebbles * r^2))
        Random.InitState(1954);
        Color stonePebble = new Color(0.58f, 0.53f, 0.46f);
        Color darkPebble = new Color(0.36f, 0.30f, 0.25f);
        int numPebbles = 65;

        for (int p = 0; p < numPebbles; p++)
        {
            int cx = Random.Range(0, TexSize);
            int cy = Random.Range(0, TexSize);
            int radius = Random.Range(6, 16);
            Color pCol = Color.Lerp(stonePebble, darkPebble, Random.value * 0.6f);

            for (int dy = -radius - 2; dy <= radius + 2; dy++)
            {
                int py = (cy + dy + TexSize) % TexSize;
                for (int dx = -radius - 2; dx <= radius + 2; dx++)
                {
                    int px = (cx + dx + TexSize) % TexSize;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius)
                    {
                        float profile = Mathf.Sqrt(1.0f - (dist / radius));
                        int idx = py * TexSize + px;
                        colors[idx] = Color.Lerp(colors[idx], pCol, profile * 0.9f);
                        heightMap[px, py] = Mathf.Max(heightMap[px, py], 0.55f + profile * 0.4f);
                    }
                    else if (dist <= radius + 2)
                    {
                        int idx = py * TexSize + px;
                        colors[idx] *= 0.65f; // shadow ring
                        heightMap[px, py] *= 0.85f;
                    }
                }
            }
        }

        colTex.SetPixels(colors);
        colTex.Apply();

        // 3. Fast Sobel Normal Map
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
                Vector3 n = new Vector3(-dX * 3.0f, -dY * 3.0f, 1.0f).normalized;
                normPixels[y * TexSize + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
            }
        }
        normTex.SetPixels(normPixels);
        normTex.Apply();

        File.WriteAllBytes(colorPath, colTex.EncodeToPNG());
        File.WriteAllBytes(normPath, normTex.EncodeToPNG());
        Object.DestroyImmediate(colTex);
        Object.DestroyImmediate(normTex);
    }

    private static void GeneratePaddyMudFast(string colorPath, string normPath)
    {
        var colTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        var normTex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
        float[,] heightMap = new float[TexSize, TexSize];
        Color[] colors = new Color[TexSize * TexSize];

        Color deepMud = new Color(0.20f, 0.13f, 0.08f);
        Color moistMud = new Color(0.28f, 0.19f, 0.12f);
        Color wetSilt = new Color(0.36f, 0.25f, 0.16f);

        for (int y = 0; y < TexSize; y++)
        {
            float v = (float)y / TexSize;
            float angleV = v * Mathf.PI * 2f;
            float nz = Mathf.Cos(angleV) * 2.0f;
            float nw = Mathf.Sin(angleV) * 2.0f;

            for (int x = 0; x < TexSize; x++)
            {
                float u = (float)x / TexSize;
                float angleU = u * Mathf.PI * 2f;
                float nx = Mathf.Cos(angleU) * 2.0f;
                float ny = Mathf.Sin(angleU) * 2.0f;

                float mudNoise = Mathf.PerlinNoise(nx + 5f, nz + 5f) * 0.65f
                               + Mathf.PerlinNoise(ny * 3.5f + 15f, nw * 3.5f + 15f) * 0.35f;

                float h = Mathf.Clamp01(mudNoise);
                heightMap[x, y] = h;

                Color c = Color.Lerp(deepMud, moistMud, Mathf.SmoothStep(0.15f, 0.6f, h));
                c = Color.Lerp(c, wetSilt, Mathf.SmoothStep(0.55f, 0.9f, h));
                colors[y * TexSize + x] = c;
            }
        }

        colTex.SetPixels(colors);
        colTex.Apply();

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
                Vector3 n = new Vector3(-dX * 2.0f, -dY * 2.0f, 1.0f).normalized;
                normPixels[y * TexSize + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
            }
        }
        normTex.SetPixels(normPixels);
        normTex.Apply();

        File.WriteAllBytes(colorPath, colTex.EncodeToPNG());
        File.WriteAllBytes(normPath, normTex.EncodeToPNG());
        Object.DestroyImmediate(colTex);
        Object.DestroyImmediate(normTex);
    }

    private static void ConfigureTextureImporter(string path, bool isNormal)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
    }
}
