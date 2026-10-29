using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class RiceFieldLayerExtractor
{
#if UNITY_EDITOR
    private static string outputFolder = "Assets/Textures/RiceFieldLayers";

    [MenuItem("Tools/Rice Field 2.5D Layers/1. Generate All Transparent Layers (.PNG)")]
    public static void GenerateAllTransparentLayers()
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        // 1. Xuất Layer 0A: Bụi cây & Lá chuối tối màu ở 2 góc dưới màn hình (Có Alpha)
        Texture2D fgTex = GenerateForegroundDarkFoliage(2048, 512);
        string fgPath = Path.Combine(outputFolder, "Layer0_Foreground_DarkFoliage.png");
        File.WriteAllBytes(fgPath, fgTex.EncodeToPNG());

        // 2. Xuất Layer 0B: Tia nắng xiên God Rays (Có Alpha)
        Texture2D raysTex = GenerateGodRaysLayer(2048, 1024);
        string raysPath = Path.Combine(outputFolder, "Layer0_GodRays_Overlay.png");
        File.WriteAllBytes(raysPath, raysTex.EncodeToPNG());

        // 3. Xuất Layer 3: Cánh đồng lúa bậc thang + Bờ đê chữ S + Mặt nước (Phần trời phía trên trong suốt)
        Texture2D paddyTex = GenerateMidgroundRicePaddyLayer(2048, 768);
        string paddyPath = Path.Combine(outputFolder, "Layer3_RicePaddy_Terraces_Alpha.png");
        File.WriteAllBytes(paddyPath, paddyTex.EncodeToPNG());

        // 4. Xuất Layer 4: Dãy núi xanh mờ sương + Hàng dừa xa (Phần trời phía trên trong suốt để lộ mây)
        Texture2D mountainTex = GenerateDistantMountainsLayer(2048, 768);
        string mountainPath = Path.Combine(outputFolder, "Layer4_MistyMountains_Alpha.png");
        File.WriteAllBytes(mountainPath, mountainTex.EncodeToPNG());

        AssetDatabase.Refresh();

        ConfigureSpriteImporter(fgPath, TextureWrapMode.Repeat);
        ConfigureSpriteImporter(raysPath, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(paddyPath, TextureWrapMode.Repeat);
        ConfigureSpriteImporter(mountainPath, TextureWrapMode.Repeat);

        Debug.Log("Đã xuất thành công 4 lớp Layer .PNG trong suốt tại: " + outputFolder);
    }

    [MenuItem("Tools/Rice Field 2.5D Layers/2. Remove Gray Background from Selected SpriteSheet")]
    public static void RemoveGrayBackgroundFromSelectedTexture()
    {
        Texture2D selectedTex = Selection.activeObject as Texture2D;
        if (selectedTex == null)
        {
            Debug.LogWarning("Hãy click chọn tấm ảnh Sprite Sheet (có Chòi lá, Đống rơm, Cây cối) trong cửa sổ Project trước khi bấm nút này!");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedTex);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        int w = selectedTex.width;
        int h = selectedTex.height;
        Color[] pixels = selectedTex.GetPixels();
        Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // Lấy màu nền ở góc trên bên trái tấm Sprite Sheet làm màu chuẩn cần xóa
        Color bgSample = pixels[(h - 5) * w + 5];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            float diff = Vector3.Distance(new Vector3(c.r, c.g, c.b), new Vector3(bgSample.r, bgSample.g, bgSample.b));
            float saturation = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));

            // Nếu màu tiệm cận màu xám nền và độ bão hòa màu thấp -> Chuyển thành trong suốt (Alpha)
            if (diff < 0.08f && saturation < 0.06f)
            {
                c.a = 0f;
            }
            else if (diff < 0.14f && saturation < 0.09f)
            {
                // Làm mềm viền (Feather anti-aliasing)
                c.a = Mathf.InverseLerp(0.08f, 0.14f, diff);
            }
            pixels[i] = c;
        }

        outTex.SetPixels(pixels);
        outTex.Apply();

        string outPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_TransparentCutout.png");
        File.WriteAllBytes(outPath, outTex.EncodeToPNG());
        AssetDatabase.Refresh();

        TextureImporter outImporter = AssetImporter.GetAtPath(outPath) as TextureImporter;
        if (outImporter != null)
        {
            outImporter.textureType = TextureImporterType.Sprite;
            outImporter.spriteImportMode = SpriteImportMode.Multiple; // Bật sẵn Multiple để vào Sprite Editor bấm Slice -> Automatic
            outImporter.alphaIsTransparency = true;
            outImporter.maxTextureSize = 2048;
            outImporter.SaveAndReimport();
        }

        Debug.Log("Đã tách nền trong suốt xong tại: " + outPath + " -> Hãy mở Sprite Editor và bấm Slice -> Automatic!");
    }

    [MenuItem("Tools/Rice Field 2.5D Layers/3. Create 6-Layer Hierarchy Setup in Scene")]
    public static void CreateLayerHierarchyInScene()
    {
        GameObject root = GameObject.Find("[SCENE_RICE_FIELD_25D]");
        if (root == null)
        {
            root = new GameObject("[SCENE_RICE_FIELD_25D]");
            Undo.RegisterCreatedObjectUndo(root, "Create 6-Layer Hierarchy");
        }

        CreateLayerGroup(root.transform, "L0_Foreground_Silhouette_And_Rays", -2.5f, 50);
        CreateLayerGroup(root.transform, "L1_PlayableGround_And_Player", 0.0f, 0);
        CreateLayerGroup(root.transform, "L2_NearMidground_Trees_And_Flowers", 2.0f, -10);
        CreateLayerGroup(root.transform, "L3_Midground_RicePaddy_Hut_Haystacks", 5.0f, 2);
        CreateLayerGroup(root.transform, "L4_Distant_MistyMountains_And_Palms", 12.0f, -10);
        CreateLayerGroup(root.transform, "L5_Sky_And_Clouds_Backdrop", 20.0f, -20);

        Selection.activeGameObject = root;
        Debug.Log("Đã tạo xong cấu trúc 6 Layer chuẩn 2.5D trong Hierarchy!");
    }

    [MenuItem("Tools/Rice Field 2.5D Layers/4. Auto-Assign Slices into 6-Layer Scene Setup (1-Click Test)")]
    public static void AutoAssignSlicesIntoScene()
    {
        // 1. Tìm hoặc tạo root [SCENE_RICE_FIELD_25D]
        GameObject root = GameObject.Find("[SCENE_RICE_FIELD_25D]");
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root);
        }
        root = new GameObject("[SCENE_RICE_FIELD_25D]");
        Undo.RegisterCreatedObjectUndo(root, "Setup 6-Layer 2.5D Test Scene");

        // 2. Xác định tọa độ trung tâm để đặt các Layer khớp tầm mắt
        float centerX = 73.0f;
        float centerY = -1.85f;

        if (SceneView.lastActiveSceneView != null)
        {
            centerX = SceneView.lastActiveSceneView.pivot.x;
            centerY = SceneView.lastActiveSceneView.pivot.y - 0.5f;
        }
        else
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("An");
            if (player == null) player = GameObject.Find("Player");
            if (player != null)
            {
                centerX = player.transform.position.x;
                centerY = player.transform.position.y;
            }
        }

        root.transform.position = new Vector3(centerX, centerY, 0f);

        // 3. Tạo 6 nhóm Layer chuẩn Z-depth
        Transform l0 = CreateLayerGroup(root.transform, "L0_Foreground_Silhouette_And_Rays", -2.5f, 50).transform;
        Transform l1 = CreateLayerGroup(root.transform, "L1_PlayableGround_And_Player", 0.0f, 0).transform;
        Transform l2 = CreateLayerGroup(root.transform, "L2_NearMidground_Trees_And_Flowers", 2.0f, -5).transform;
        Transform l3 = CreateLayerGroup(root.transform, "L3_Midground_RicePaddy_Hut_Haystacks", 5.0f, 2).transform;
        Transform l4 = CreateLayerGroup(root.transform, "L4_Distant_MistyMountains_And_Palms", 12.0f, -10).transform;
        Transform l5 = CreateLayerGroup(root.transform, "L5_Sky_And_Clouds_Backdrop", 20.0f, -20).transform;

        Material spriteMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        if (spriteMat.shader == null || spriteMat.shader.name == "Hidden/InternalErrorShader")
        {
            spriteMat = new Material(Shader.Find("Sprites/Default"));
        }

        // 4. Gắn các mảnh Layer đã cắt vào từng tầng
        // L0: Tiền cảnh bụi cỏ tối sát mép dưới
        SpawnLayerSprite("FG_DarkGrass", l0, "Assets/Textures/Concept_Slices/07_TienCanh_BuiCoToi.png",
            new Vector3(0f, -1.2f, 0f), new Vector3(32f, 4.5f, 1f), 50, spriteMat);

        // L3: Toàn cảnh ruộng bậc thang màu nước (Chòi lá, đống rơm, các nếp lúa)
        string ricePath = "Assets/Textures/Concept_Slices/01_RuongBacThang_ToanCanh.png";
        if (!File.Exists(ricePath)) ricePath = "Assets/Textures/RiceFieldLayers/Layer3_RicePaddy_Terraces_Alpha.png";
        SpawnLayerSprite("Midground_RiceTerraces", l3, ricePath,
            new Vector3(0f, 1.4f, 0f), new Vector3(38f, 9.2f, 1f), 2, spriteMat);

        // L4: Dãy núi xanh mờ sương
        string mountainPath = "Assets/Textures/Concept_Slices/04_DayNui_MoSuong.png";
        if (!File.Exists(mountainPath)) mountainPath = "Assets/Textures/RiceFieldLayers/Layer4_MistyMountains_Alpha.png";
        SpawnLayerSprite("BG_MistyMountains", l4, mountainPath,
            new Vector3(0f, 3.2f, 0f), new Vector3(48f, 10.5f, 1f), -10, spriteMat);

        // L5: Bầu trời & Mây trắng
        SpawnLayerSprite("Sky_Clouds_Backdrop", l5, "Assets/Textures/Concept_Slices/05_BauTroi_VaMay.png",
            new Vector3(0f, 6.0f, 0f), new Vector3(58f, 14.0f, 1f), -20, spriteMat);

        // Đạo cụ lẻ: Chòi lá & Đống rơm vào L3
        SpawnLayerSprite("Prop_ChoiLa", l3, "Assets/Textures/Concept_Slices/Prop_ChoiLa_Nho.png",
            new Vector3(-6.5f, 0.4f, -0.1f), new Vector3(3.2f, 3.2f, 1f), 3, spriteMat);

        SpawnLayerSprite("Prop_DongRom", l3, "Assets/Textures/Concept_Slices/Prop_DongRom_Trai.png",
            new Vector3(-9.2f, 0.3f, -0.1f), new Vector3(2.4f, 2.4f, 1f), 3, spriteMat);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        SceneView.RepaintAll();

        Debug.Log("<color=#10b981>[RiceField25D]</color> <b>ĐÃ GẮN ĐẦY ĐỦ CÁC LAYER VÀO [SCENE_RICE_FIELD_25D] ĐỂ TEST!</b>");
    }

    private static void SpawnLayerSprite(string name, Transform parent, string assetPath, Vector3 localPos, Vector3 localScale, int sortingOrder, Material mat)
    {
        if (!File.Exists(assetPath)) return;

        EnsureSpriteImporter(assetPath);
        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (spr == null) return;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = sortingOrder;
        sr.sharedMaterial = mat;
    }

    private static void EnsureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static GameObject CreateLayerGroup(Transform parent, string name, float zPos, int sortingOrder)
    {
        GameObject group = new GameObject(name + " (Order: " + sortingOrder + ")");
        group.transform.SetParent(parent, false);
        group.transform.localPosition = new Vector3(0f, 0f, zPos);
        return group;
    }

    // --- HÀM VẼ LAYER 0A: TIỀN CẢNH CỎ & LÁ CHUỐI TỐI MÀU Ở 2 GÓC DƯỚI ---
    private static Texture2D GenerateForegroundDarkFoliage(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        Color silhouetteBlack = new Color(0.03f, 0.07f, 0.05f, 1f);
        Color darkRimGreen    = new Color(0.09f, 0.20f, 0.11f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;

                // Tập trung bụi cây cao ở góc trái (u < 0.28) và góc phải (u > 0.72), thấp ở giữa để không che nhân vật
                float cornerMask = Mathf.Pow(Mathf.Abs(u - 0.5f) * 2.0f, 1.6f);
                float bladeWave = Mathf.Abs(Mathf.Sin((u * 38f + SeamlessNoise(u, v, 8, 3f, 11.2f) * 1.4f) * Mathf.PI));
                float nCluster = SeamlessFBM(u, v, 6, 4.0f, 29.4f);

                float maxH = Mathf.Lerp(0.18f, 0.82f, cornerMask) + (nCluster - 0.5f) * 0.22f - bladeWave * 0.16f;

                if (v > maxH)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    float rim = Mathf.SmoothStep(maxH - 0.12f, maxH, v);
                    pixels[y * width + x] = Color.Lerp(silhouetteBlack, darkRimGreen, rim * 0.7f);
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // --- HÀM VẼ LAYER 0B: TIA NẮNG XIÊN (GOD RAYS OVERLAY) ---
    private static Texture2D GenerateGodRaysLayer(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];
        Color sunColor = new Color(0.88f, 0.96f, 0.78f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float diag = (u - 0.35f) * 3.2f - (v - 0.5f) * 1.6f;
                float ray1 = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(diag * Mathf.PI)), 6f);
                float ray2 = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((diag + 0.4f) * Mathf.PI * 1.7f)), 8f);

                float fadeV = Mathf.SmoothStep(0.05f, 0.85f, v);
                float fadeU = Mathf.SmoothStep(0.20f, 0.75f, u);
                float alpha = Mathf.Clamp01((ray1 * 0.16f + ray2 * 0.10f) * fadeV * fadeU);

                pixels[y * width + x] = new Color(sunColor.r, sunColor.g, sunColor.b, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // --- HÀM VẼ LAYER 3: CÁNH ĐỒNG LÚA BẬC THANG + BỜ ĐÊ + MẶT NƯỚC (CÓ ALPHA TRÊN ĐỈNH) ---
    private static Texture2D GenerateMidgroundRicePaddyLayer(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        Color horizonMist   = new Color(0.66f, 0.80f, 0.68f, 1f); // Lúa xa mờ sương
        Color paddyGolden   = new Color(0.76f, 0.78f, 0.36f, 1f); // Lúa chín vàng nắng
        Color paddyGreen    = new Color(0.46f, 0.66f, 0.30f, 1f); // Lúa xanh non
        Color dikeSoil      = new Color(0.32f, 0.42f, 0.22f, 1f); // Bờ đê & bờ ruộng
        Color waterReflect  = new Color(0.48f, 0.68f, 0.64f, 1f); // Mặt nước ruộng phản chiếu trời

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;

                float n1 = SeamlessFBM(u, v, 3, 2.0f, 17.4f);
                float n2 = SeamlessFBM(u, v, 9, 6.0f, 53.1f);

                // Đường chân trời của cánh đồng lúa (Phía trên v > horizonLine là trong suốt để lộ núi & trời)
                float horizonLine = 0.66f + (n1 - 0.5f) * 0.04f;
                if (v > horizonLine)
                {
                    pixels[y * width + x] = Color.clear;
                    continue;
                }

                float normV = v / horizonLine; // 0 = sát lưng nhân vật, 1 = xa tít chân núi

                // Tạo các bậc ruộng ngang & đường bờ đê uốn chữ S theo phối cảnh
                float perspectiveRows = Mathf.Pow(1.0f - normV, 0.55f) * 14.0f;
                float curveS = Mathf.Sin(normV * Mathf.PI * 2.2f + u * Mathf.PI * 2.0f) * 0.35f;
                float terraceBand = Mathf.Abs(Mathf.Sin((perspectiveRows + curveS + n1 * 0.4f) * Mathf.PI));

                // Đường đê chữ S chính ở giữa khung hình
                float mainPathDist = Mathf.Abs((u - 0.54f) - Mathf.Sin(normV * Mathf.PI * 1.8f) * 0.08f * (1f - normV));

                Color col;
                if (terraceBand < 0.14f || mainPathDist < 0.018f * (1.2f - normV))
                {
                    // Bờ đê / Bờ ruộng
                    col = dikeSoil;
                }
                else if (normV < 0.45f && n2 > 0.52f)
                {
                    // Ô ruộng ngập nước phản chiếu bầu trời ở gần
                    col = Color.Lerp(waterReflect, paddyGreen, 0.25f);
                }
                else
                {
                    // Thảm lúa xanh xen vàng
                    col = (n2 + normV * 0.2f > 0.55f) ? paddyGolden : paddyGreen;
                }

                // Hòa vào sương mù khi lùi về phía chân núi (normV -> 1)
                col = Color.Lerp(col, horizonMist, Mathf.SmoothStep(0.45f, 1.0f, normV) * 0.78f);

                pixels[y * width + x] = col;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // --- HÀM VẼ LAYER 4: DÃY NÚI XANH SƯƠNG MÙ + HÀNG DỪA XA (CÓ ALPHA TRÊN ĐỈNH) ---
    private static Texture2D GenerateDistantMountainsLayer(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        Color farMountain  = new Color(0.38f, 0.60f, 0.58f, 1f); // Dãy núi xa mờ
        Color nearMountain = new Color(0.28f, 0.50f, 0.44f, 1f); // Dãy núi gần hơn
        Color valleyMist   = new Color(0.68f, 0.82f, 0.74f, 1f); // Sương mù chân núi

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;

                float nFar  = SeamlessFBM(u, 0.5f, 3, 1.5f, 12.1f);
                float nNear = SeamlessFBM(u + 0.35f, 0.5f, 4, 2.0f, 44.8f);

                float farPeak  = 0.48f + (nFar - 0.5f) * 0.38f;
                float nearPeak = 0.36f + (nNear - 0.5f) * 0.32f;

                if (v > Mathf.Max(farPeak, nearPeak))
                {
                    pixels[y * width + x] = Color.clear; // Trong suốt phía trên đỉnh núi để lộ bầu trời và mây
                }
                else
                {
                    Color col = (v <= nearPeak) ? nearMountain : farMountain;
                    // Phủ sương mù trắng xanh ở chân núi để nối mượt với cánh đồng lúa
                    float mistFactor = Mathf.SmoothStep(0.32f, 0.02f, v);
                    col = Color.Lerp(col, valleyMist, mistFactor * 0.85f);
                    pixels[y * width + x] = col;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static void ConfigureSpriteImporter(string path, TextureWrapMode wrapU)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.wrapModeU = wrapU;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static float SeamlessNoise(float u, float v, int periodX, float freqY, float seed)
    {
        float angle = u * Mathf.PI * 2.0f * periodX;
        float nx = Mathf.Cos(angle) * 0.5f + seed;
        float nz = Mathf.Sin(angle) * 0.5f + seed;
        float ny = v * freqY + seed;
        return (Mathf.PerlinNoise(nx + ny * 0.2f, nz + ny) + Mathf.PerlinNoise(nz - ny * 0.2f + 19.1f, nx + ny + 31.7f)) * 0.5f;
    }

    private static float SeamlessFBM(float u, float v, int basePeriodX, float baseFreqY, float seed)
    {
        float total = 0f, amplitude = 0.5f, maxVal = 0f;
        for (int i = 0; i < 3; i++)
        {
            total += SeamlessNoise(u, v, basePeriodX * (1 << i), baseFreqY * (1 << i), seed + i * 15.3f) * amplitude;
            maxVal += amplitude;
            amplitude *= 0.5f;
        }
        return total / maxVal;
    }
#endif
}
