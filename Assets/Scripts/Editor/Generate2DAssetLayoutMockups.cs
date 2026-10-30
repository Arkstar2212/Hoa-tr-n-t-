#pragma warning disable 0219
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Tạo ra các bản vẽ bố cục thị giác (2D Visual Layout Concept Boards) từ chính các asset thực tế của dự án.
    /// Giúp người dùng và đạo diễn nghệ thuật xem trước, đánh giá tỷ lệ và bố cục trước khi implement vào scene.
    /// 100% sử dụng SpriteRenderer 2D unlit, đảm bảo triệt tiêu Z-fighting và cách ly tuyệt đối với scene 3D.
    /// </summary>
    public static class Generate2DAssetLayoutMockups
    {
        private const string OutDir = "Screenshots/Layout_Mockups";
        private const string ArtifactDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\96efc365-f422-4745-bfa5-8d83af1b80e5";

        private static Material _unlitSpriteMat;
        private static Sprite _mistSprite;
        private static Sprite _sunbeamSprite;

        // Tọa độ chuẩn của mặt đất đi bộ (neo ở vị trí 17.7% từ đáy màn hình, chuẩn Cinematic 2.5D Ghibli / Planet of Lana)
        private const float GroundY = -2.7f;
        private const float CamY = 0.4f;
        private const float CamSize = 4.8f; // orthographicSize = 4.8 -> H = 9.6 -> Y in [0.4 - 4.8 = -4.4, 0.4 + 4.8 = 5.2]
        private const float ViewBottomY = CamY - CamSize; // -4.4m
        private const float ScreenHalfW = 8.533f; // 16:9 -> W = 17.066 -> X in [-8.53, +8.53]

        // Procedural Sprite tạo sương mù mềm (Soft Feathered Mist)
        private static Sprite GetOrCreateMistSprite()
        {
            if (_mistSprite != null) return _mistSprite;
            int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / (h - 1);
                float yDist = Mathf.Abs(ty - 0.5f) * 2f;
                float yFade = Mathf.Clamp01(1f - yDist * yDist);
                yFade = yFade * yFade; // Độ mờ viền mềm mại 4 bậc

                for (int x = 0; x < w; x++)
                {
                    float tx = (float)x / (w - 1);
                    float xDist = Mathf.Abs(tx - 0.5f) * 2f;
                    float xFade = Mathf.Clamp01(1f - xDist * xDist);
                    cols[y * w + x] = new Color(1f, 1f, 1f, yFade * xFade);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            _mistSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 128f); // 128 PPU -> Đúng chuẩn 1m x 1m
            return _mistSprite;
        }

        // Procedural Sprite tạo vệt nắng xiên (Volumetric Sunbeam Shaft)
        private static Sprite GetOrCreateSunbeamSprite()
        {
            if (_sunbeamSprite != null) return _sunbeamSprite;
            int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / (h - 1);
                float yFade = Mathf.Pow(ty, 1.4f);
                for (int x = 0; x < w; x++)
                {
                    float tx = (float)x / (w - 1);
                    float dist = Mathf.Abs(tx - 0.5f) * 2f;
                    float xFade = Mathf.Clamp01(1f - dist * dist);
                    xFade = xFade * xFade;
                    cols[y * w + x] = new Color(1f, 1f, 1f, yFade * xFade);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            _sunbeamSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 128f); // 128 PPU -> Đúng chuẩn 1m x 1m
            return _sunbeamSprite;
        }

        // Procedural Sprite mặt cắt lòng đất có gradient độ sâu (Earth Underbelly Gradient)
        private static Sprite _earthCrossSectionSprite;
        private static Sprite GetOrCreateEarthCrossSectionSprite()
        {
            if (_earthCrossSectionSprite != null) return _earthCrossSectionSprite;
            int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color topColor = new Color(0.24f, 0.18f, 0.12f, 1f); // Đất mùn hữu cơ đậm gần mặt cỏ
            Color midColor = new Color(0.15f, 0.11f, 0.07f, 1f); // Tầng đất sét sâu
            Color botColor = new Color(0.05f, 0.04f, 0.02f, 1f); // Bóng tối sâu dưới đáy màn hình
            Color[] cols = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1); // 0 ở đáy, 1 ở đỉnh
                Color c;
                if (t > 0.65f)
                {
                    float f = (t - 0.65f) / 0.35f;
                    c = Color.Lerp(midColor, topColor, f);
                }
                else
                {
                    float f = t / 0.65f;
                    c = Color.Lerp(botColor, midColor, f);
                }
                for (int x = 0; x < w; x++)
                {
                    cols[y * w + x] = c;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            // Pivot đặt tại đáy giữa (0.5f, 0f), 64 PPU -> Kích thước gốc đúng chuẩn 1m x 1m
            _earthCrossSectionSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
            return _earthCrossSectionSprite;
        }

        private static GameObject SpawnMistBand(GameObject root, string name, Vector3 pos, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateMistSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = _unlitSpriteMat;
            return go;
        }

        private static GameObject SpawnSunbeam(GameObject root, string name, Vector3 pos, Vector2 size, float rotZ, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSunbeamSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = _unlitSpriteMat;
            return go;
        }

        [MenuItem("Hoa va Mau/Concept & Layout/Generate 2D Visual Layout Boards From Real Assets", false, 1)]
        public static void GenerateAllBoards()
        {
            if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            _mistSprite = null;
            _sunbeamSprite = null;
            _earthCrossSectionSprite = null;

            Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                     ?? Shader.Find("Sprites/Default");
            _unlitSpriteMat = new Material(sh);

            Debug.Log("<color=#38bdf8>[LayoutMockup]</color> Đang tạm ẩn các GameObject trong scene hiện tại để cách ly 100% sân khấu bố cục 2D...");

            // Cách ly scene: Tạm ẩn tất cả root GameObject trong scene hiện tại
            var activeScene = EditorSceneManager.GetActiveScene();
            var rootGOs = activeScene.GetRootGameObjects();
            var previousStates = new Dictionary<GameObject, bool>();
            foreach (var go in rootGOs)
            {
                if (go != null)
                {
                    previousStates[go] = go.activeSelf;
                    go.SetActive(false);
                }
            }

            try
            {
                // 1. Tạo 4 bản vẽ bố cục từng phân khu (1920x1080)
                GenerateBoard1_DenseForest();
                GenerateBoard2_RuralGardenTrellis();
                GenerateBoard3_CanalBridgeSampan();
                GenerateBoard4_VastRiceSea();

                // 2. Tạo bản vẽ toàn cảnh 60m Panorama nối liền 4 phân khu (3840x1080)
                GenerateMasterPanorama60m();

                Debug.Log("<color=#4ade80>[LayoutMockup]</color> <b>HOÀN TẤT TẠO TOÀN BỘ BẢN VẼ BỐ CỤC 2D!</b> Đã lưu vào " + OutDir);
            }
            finally
            {
                // Khôi phục trạng thái các vật thể trong scene
                foreach (var kvp in previousStates)
                {
                    if (kvp.Key != null) kvp.Key.SetActive(kvp.Value);
                }
                Debug.Log("<color=#38bdf8>[LayoutMockup]</color> Đã khôi phục trạng thái các GameObject trong scene.");
            }
        }

        // =========================================================================================
        // HELPER: TẠO SPRITE TỪ TEXTURE THỰC TẾ & KHỐI SOLID MÀU
        // =========================================================================================
        private static Sprite LoadSprite(string path, Vector2 pivot)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning("[LayoutMockup] Không tìm thấy texture: " + path);
                return null;
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 100f);
        }

        private static GameObject Spawn2DSprite(GameObject root, string name, string path, Vector3 pos, Vector2 scale, int sortingOrder, Color tint, bool flipX = false, Vector2? pivot = null)
        {
            var piv = pivot ?? new Vector2(0.5f, 0f); // Mặc định neo chân ở đáy giữa (gốc cây/chân cột chạm đất)
            var sp = LoadSprite(path, piv);
            if (sp == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale.x * (flipX ? -1f : 1f), scale.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortingOrder;
            sr.color = tint;
            sr.sharedMaterial = _unlitSpriteMat;
            return go;
        }

        // Tạo mảng hình học phẳng (Solid Box) hoàn toàn bằng SpriteRenderer 2D để tránh Z-fighting và kiểm soát sortingOrder 100%
        private static GameObject SpawnSolidBox(GameObject root, string name, Vector3 pos, Vector2 size, Color color, int sortingOrder, Vector2? pivot = null)
        {
            var piv = pivot ?? new Vector2(0.5f, 0.5f);
            var sp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), piv, 4f);

            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = _unlitSpriteMat;
            return go;
        }

        // Tạo rặng đồi nhấp nhô liên tục (Continuous Terrain Ridge) không bao giờ bị đứt đoạn hay hở khe
        private static void SpawnContinuousRidge(GameObject root, string namePrefix, string moundPath1, string moundPath2, float leftX, float rightX, float yBase, float scale, int sortingOrder, Color tint)
        {
            float step = 2.4f * scale; // Bước giãn cách đảm bảo các đỉnh đồi gối đè nhau ~50%
            int count = 0;
            for (float x = leftX - 2.0f; x <= rightX + 2.0f; x += step)
            {
                string pMound = (count % 2 == 0) ? moundPath1 : moundPath2;
                bool flip = (count % 3 == 0);
                float xJitter = (count % 2 == 0 ? 0.35f : -0.25f) * scale;
                float yJitter = (count % 3 == 1 ? 0.10f : (count % 3 == 2 ? -0.06f : 0f)) * scale;

                // Căn chuẩn pivot Y theo hàng pixel có nội dung đầu tiên ở chân đồi:
                // Curved_Tree_Mound có đáy tại Y=96/402 (0.239f), Curved_Tree_V2_Mound có đáy tại Y=19/651 (0.029f)
                Vector2 piv = pMound.Contains("Curved_Tree_V2_Mound") ? new Vector2(0.5f, 19f / 651f)
                            : (pMound.Contains("Curved_Tree_Mound") ? new Vector2(0.5f, 96f / 402f) : new Vector2(0.5f, 0f));

                Spawn2DSprite(root, $"{namePrefix}_{count}", pMound, new Vector3(x + xJitter, yBase + yJitter, 0f), new Vector2(scale, scale), sortingOrder, tint, flip, pivot: piv);
                count++;
            }
        }

        // Tạo vách cắt lòng đất đi bộ có gradient bóng đổ tự nhiên (Y: ViewBottomY = -4.4m đến GroundY = -2.7m)
        private static GameObject SpawnUnderbellyCrossSection(GameObject root, string name, float leftX, float rightX, Color earthTint, int sortingOrder = 11)
        {
            float width = rightX - leftX;
            float height = GroundY - ViewBottomY; // 1.70m
            float centerX = (leftX + rightX) * 0.5f;

            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            // Pivot đặt tại đáy giữa (0.5f, 0f), neo chân tại ViewBottomY (-4.4m), đỉnh vươn lên đúng GroundY (-2.7m)
            go.transform.position = new Vector3(centerX, ViewBottomY, 0f);
            go.transform.localScale = new Vector3(width, height, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateEarthCrossSectionSprite();
            sr.color = earthTint;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = _unlitSpriteMat;
            return go;
        }

        // Tạo bóng nhân vật để người xem dễ dàng đối chiếu tỷ lệ thực tế (Nhân vật cao 1.4m = 140cm)
        private static void SpawnScaleFigure(GameObject root, Vector3 footPos)
        {
            var charRoot = new GameObject("ScaleFigure_1.4m");
            charRoot.transform.SetParent(root.transform, false);
            charRoot.transform.position = footPos;

            Color bodyColor = new Color(0.12f, 0.16f, 0.20f, 0.95f); // Silhouette xanh thẫm trang nhã
            Color hatColor  = new Color(0.92f, 0.82f, 0.45f, 1.0f);  // Nón lá màu vàng rơm nổi bật

            // Chân (cao 0.55m)
            SpawnSolidBox(charRoot, "LegLeft",  footPos + new Vector3(-0.06f, 0.27f, 0f), new Vector2(0.08f, 0.55f), bodyColor, 24);
            SpawnSolidBox(charRoot, "LegRight", footPos + new Vector3( 0.06f, 0.27f, 0f), new Vector2(0.08f, 0.55f), bodyColor, 24);

            // Thân áo bà ba (cao 0.55m, rộng 0.30m)
            SpawnSolidBox(charRoot, "Torso", footPos + new Vector3(0f, 0.82f, 0f), new Vector2(0.30f, 0.55f), bodyColor, 24);

            // Đầu (đường kính 0.20m, đỉnh ở 1.25m)
            SpawnSolidBox(charRoot, "Head", footPos + new Vector3(0f, 1.20f, 0f), new Vector2(0.20f, 0.20f), bodyColor, 24);

            // Nón lá Việt Nam (chóp nón đạt 1.40m, vành nón rộng 0.65m)
            SpawnSolidBox(charRoot, "HatBrim", footPos + new Vector3(0f, 1.28f, 0f), new Vector2(0.65f, 0.10f), hatColor, 25);
            SpawnSolidBox(charRoot, "HatCone", footPos + new Vector3(0f, 1.35f, 0f), new Vector2(0.35f, 0.14f), hatColor, 25);
        }

        private static void RenderStageToPNG(GameObject stage, Camera cam, string fileName, int width = 1920, int height = 1080)
        {
            cam.aspect = (float)width / (float)height;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var outTex = new Texture2D(width, height, TextureFormat.RGB24, false);
            outTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            outTex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = outTex.EncodeToPNG();
            Object.DestroyImmediate(outTex);

            string localPath = Path.Combine(OutDir, fileName);
            File.WriteAllBytes(localPath, bytes);

            string artPath = Path.Combine(ArtifactDir, fileName);
            File.WriteAllBytes(artPath, bytes);

            Debug.Log($"<color=#38bdf8>[LayoutMockup]</color> Đã xuất bản vẽ: {fileName} ({width}x{height})");
        }

        // =========================================================================================
        // PHÂN KHU 1: RỪNG RẬM & CỔNG VÒM CỔ THỤ (DENSE FOREST GATE) — MULTI-LAYER DEPTH OVERHAUL
        // =========================================================================================
        private static void GenerateBoard1_DenseForest()
        {
            var stage = new GameObject("__Stage_Forest__");
            var camGO = new GameObject("Cam");
            camGO.transform.SetParent(stage.transform, false);
            camGO.transform.position = new Vector3(0f, CamY, -10f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.40f, 0.62f, 0.82f);

            string pSky    = "Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png";
            string pMound1 = "Assets/Textures/Tree/Curved_Tree_Mound.png";
            string pMound2 = "Assets/Textures/Tree/Curved_Tree_V2_Mound.png";
            string pTree1  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_01_LargeCanopy.png";
            string pTree2  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_02_Medium.png";
            string pTree4  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_04_SlenderTall.png";
            string pTree5  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_05_Banana.png";
            string pTree6  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png";
            string pTree7  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png";
            string pRock1  = "Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_01_LargeCliffOutcrop.png";
            string pRock2  = "Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_02_MediumMossyDome.png";
            string pBush1  = "Assets/Textures/Environment/Lana_Concept_Assets/Bushes/T_HD_Bush_Broadleaf_01.png";
            string pBush2  = "Assets/Textures/Environment/Lana_Concept_Assets/Bushes/T_HD_Bush_Broadleaf_02.png";
            string pGrass  = "Assets/Textures/Environment/Lana_Concept_Assets/Grass/T_HD_GrassCarpet_02_TallMeadow.png";
            string pDaisy  = "Assets/Textures/Environment/Lana_Concept_Assets/Wildflowers/T_HD_Wildflower_03_WhiteDaisy.png";

            // Layer 1: Bầu trời & Mây trắng vút cao (Sorting 1)
            Spawn2DSprite(stage, "Sky", pSky, new Vector3(0f, 2.4f, 0f), new Vector2(2.40f, 2.20f), 1, Color.white, pivot: new Vector2(0.5f, 0.5f));

            // Layer 2: Rặng đồi xa & Cây cối mù sương (Far Forest Ridge - 40m, Sorting 2-3)
            Color farFog = new Color(0.48f, 0.64f, 0.72f, 0.88f);
            SpawnContinuousRidge(stage, "FarRidge", pMound1, pMound2, -16f, 16f, -2.85f, 2.6f, 2, new Color(0.40f, 0.56f, 0.65f));
            Spawn2DSprite(stage, "FarTree_Left",    pTree1, new Vector3(-6.2f, 0.5f, 0f), new Vector2(0.85f, 0.85f), 3, farFog);
            Spawn2DSprite(stage, "FarTree_Slender", pTree4, new Vector3(-2.2f, 0.4f, 0f), new Vector2(0.75f, 0.75f), 3, farFog);
            Spawn2DSprite(stage, "FarTree_Center",  pTree2, new Vector3( 0.5f, 0.45f, 0f), new Vector2(0.72f, 0.72f), 3, farFog);
            Spawn2DSprite(stage, "FarTree_Bamboo",  pTree7, new Vector3( 3.2f, 0.4f, 0f), new Vector2(0.78f, 0.78f), 3, farFog);
            Spawn2DSprite(stage, "FarTree_Right",   pTree1, new Vector3( 6.8f, 0.5f, 0f), new Vector2(0.85f, 0.85f), 3, farFog, true);

            // Layer 3: Dải sương mù thung lũng xa (Sorting 4)
            SpawnMistBand(stage, "FarMist_Band", new Vector3(0f, 0.4f, 0f), new Vector2(34.0f, 2.2f), new Color(0.70f, 0.84f, 0.92f, 0.12f), 4);

            // Layer 4: Rặng đồi trung cảnh (Mid Forest Ridge - 15m, Sorting 5-6)
            Color midHaze = new Color(0.38f, 0.56f, 0.44f, 1.0f);
            SpawnContinuousRidge(stage, "MidRidge", pMound2, pMound1, -16f, 16f, -2.80f, 1.8f, 5, new Color(0.30f, 0.46f, 0.34f));
            Spawn2DSprite(stage, "MidTree_BambooLeft", pTree7, new Vector3(-4.8f, -0.6f, 0f), new Vector2(0.95f, 0.95f), 6, midHaze);
            Spawn2DSprite(stage, "MidTree_Slender",    pTree4, new Vector3(-1.8f, -0.55f, 0f), new Vector2(0.90f, 0.90f), 6, midHaze);
            Spawn2DSprite(stage, "MidTree_Medium",     pTree2, new Vector3( 1.8f, -0.50f, 0f), new Vector2(0.92f, 0.92f), 6, midHaze);
            Spawn2DSprite(stage, "MidTree_BambooRight",pTree7, new Vector3( 5.5f, -0.55f, 0f), new Vector2(0.98f, 0.98f), 6, midHaze);

            // Layer 5: Sương lam mặt đất tầng trung (Sorting 7)
            SpawnMistBand(stage, "MidMist_Band", new Vector3(0f, -0.8f, 0f), new Vector2(34.0f, 1.8f), new Color(0.78f, 0.90f, 0.94f, 0.10f), 7);

            // Layer 6: Gò đồi đệm gần (Near Forest Ridge - 5m sau lối đi, Sorting 8-9)
            Color nearShade = new Color(0.55f, 0.72f, 0.60f, 1.0f);
            SpawnContinuousRidge(stage, "NearRidge", pMound1, pMound2, -16f, 16f, -2.75f, 1.2f, 8, new Color(0.22f, 0.36f, 0.20f));
            Spawn2DSprite(stage, "NearBanana_Left",  pTree5, new Vector3(-3.8f, -1.8f, 0f), new Vector2(0.95f, 0.95f), 9, nearShade);
            Spawn2DSprite(stage, "NearBamboo_Right", pTree7, new Vector3( 0.6f, -1.75f, 0f), new Vector2(0.98f, 0.98f), 9, nearShade);

            // Layer 7: Vệt nắng xiên qua tán lá (Volumetric Sunbeams, Sorting 10)
            SpawnSunbeam(stage, "Sunbeam_1", new Vector3( 2.2f, 1.4f, 0f), new Vector2(2.4f, 9.5f), -30f, new Color(1.0f, 0.98f, 0.75f, 0.18f), 10);
            SpawnSunbeam(stage, "Sunbeam_2", new Vector3( 0.0f, 0.8f, 0f), new Vector2(3.0f, 9.0f), -28f, new Color(1.0f, 0.96f, 0.70f, 0.24f), 10);
            SpawnSunbeam(stage, "Sunbeam_3", new Vector3(-2.4f, 0.0f, 0f), new Vector2(2.0f, 8.5f), -26f, new Color(1.0f, 0.98f, 0.75f, 0.16f), 10);

            // Layer 8: Mặt cắt lòng đất đi bộ có gradient bóng đổ tự nhiên (Sorting 11)
            SpawnUnderbellyCrossSection(stage, "Forest_Earth_CrossSection", -16f, 16f, new Color(0.70f, 0.85f, 0.65f), 11);

            // Layer 9: Dải thảm cỏ mép đất liên tục (Sorting 12)
            for (float gx = -16f; gx <= 16f; gx += 1.8f)
            {
                Spawn2DSprite(stage, "GrassEdge", pGrass, new Vector3(gx, GroundY - 0.05f, 0f), new Vector2(0.38f, 0.38f), 12, new Color(0.88f, 0.98f, 0.78f));
            }

            // Layer 10: Gameplay Hero Layer tại Y = GroundY = -2.7m (Sorting 13-18)
            Color heroTint = Color.white;
            // Cổ thụ đa mẹ bên trái (Tán vươn cao che vòm)
            Spawn2DSprite(stage, "MotherBanyan_Left", pTree1, new Vector3(-4.8f, GroundY, 0f), new Vector2(1.35f, 1.35f), 13, heroTint);
            Spawn2DSprite(stage, "CliffRock",        pRock1, new Vector3(-6.2f, GroundY, 0f), new Vector2(0.52f, 0.52f), 15, heroTint);
            Spawn2DSprite(stage, "Banana_Under",     pTree5, new Vector3(-2.6f, GroundY, 0f), new Vector2(0.96f, 0.96f), 16, heroTint);

            // Cụm bên phải: Cây đại thụ & Bụi tre lớn
            Spawn2DSprite(stage, "HeroTree_Right",   pTree1, new Vector3( 7.6f, GroundY, 0f), new Vector2(1.30f, 1.30f), 13, heroTint, true);
            Spawn2DSprite(stage, "BambooGrove_Right", pTree7, new Vector3( 5.5f, GroundY, 0f), new Vector2(1.15f, 1.15f), 14, heroTint);
            Spawn2DSprite(stage, "MossyDomeRock",     pRock2, new Vector3( 4.0f, GroundY, 0f), new Vector2(0.48f, 0.48f), 16, heroTint);

            // Hoa dại mép đường
            Spawn2DSprite(stage, "Daisy_1", pDaisy, new Vector3(-0.6f, GroundY, 0f), new Vector2(0.28f, 0.28f), 17, Color.white);
            Spawn2DSprite(stage, "Daisy_2", pDaisy, new Vector3( 2.8f, GroundY, 0f), new Vector2(0.28f, 0.28f), 17, Color.white);

            // Scale Figure 1.4m đứng đón nắng
            SpawnScaleFigure(stage, new Vector3(-0.2f, GroundY, 0f));

            // Layer 11: Tiền cảnh vòm lá & dương xỉ buông rủ (Foreground Silhouette, Sorting 22-25)
            Color fgDark = new Color(0.04f, 0.07f, 0.05f, 0.98f);
            Spawn2DSprite(stage, "FG_Canopy_TopLeft",  pBush1, new Vector3(-7.2f, 4.6f, 0f), new Vector2(1.25f, 1.25f), 22, fgDark);
            Spawn2DSprite(stage, "FG_Canopy_TopRight", pBush2, new Vector3( 7.2f, 4.6f, 0f), new Vector2(1.25f, 1.25f), 22, fgDark, true);
            Spawn2DSprite(stage, "FG_Fern_BottomLeft", pBush1, new Vector3(-8.2f, -3.8f, 0f), new Vector2(1.15f, 1.15f), 23, fgDark);
            Spawn2DSprite(stage, "FG_Fern_BottomRight",pBush2, new Vector3( 8.2f, -3.8f, 0f), new Vector2(1.15f, 1.15f), 23, fgDark, true);

            RenderStageToPNG(stage, cam, "LayoutMockup_01_DenseForest_Gate.png");
            Object.DestroyImmediate(stage);
        }

        // =========================================================================================
        // PHÂN KHU 2: VƯỜN QUÊ & GIÀN BẦU BÍ 2.5D (RURAL GARDEN & GOURD TRELLIS) — DEPTH OVERHAUL
        // =========================================================================================
        private static void GenerateBoard2_RuralGardenTrellis()
        {
            var stage = new GameObject("__Stage_Garden__");
            var camGO = new GameObject("Cam");
            camGO.transform.SetParent(stage.transform, false);
            camGO.transform.position = new Vector3(0f, CamY, -10f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.42f, 0.65f, 0.85f);

            string pSky       = "Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png";
            string pMound1    = "Assets/Textures/Tree/Curved_Tree_Mound.png";
            string pMound2    = "Assets/Textures/Tree/Curved_Tree_V2_Mound.png";
            string pTree5     = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_05_Banana.png";
            string pTree6     = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png";
            string pTree7     = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png";
            string pHouse     = "Assets/Textures/Canh1_Sprites/BG_Prop_04.png";

            string pTrellis   = "Assets/Textures/Environment/Lana_Concept_Assets/GourdTrellis/Trellises/T_HD_GourdTrellis_01_Main.png";
            string pGourdLong = "Assets/Textures/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_01_LongBottle.png";
            string pGourdPear = "Assets/Textures/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_02_PearCalabash.png";
            string pPumpkin   = "Assets/Textures/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_05_GreenPumpkin.png";
            string pGourdHalf = "Assets/Textures/Environment/Lana_Concept_Assets/GourdTrellis/Gourds/T_HD_Gourd_04_HalfCut.png";
            string pFlower    = "Assets/Textures/Environment/Lana_Concept_Assets/GourdTrellis/Flowers/T_HD_GourdFlower_01_Bloom.png";
            string pRockSlab  = "Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_04_FlatSlab.png";
            string pBushFlw   = "Assets/Textures/Environment/Lana_Concept_Assets/Bushes/T_HD_Bush_Low_02_Flower.png";
            string pBush1     = "Assets/Textures/Environment/Lana_Concept_Assets/Bushes/T_HD_Bush_Broadleaf_01.png";
            string pBush2     = "Assets/Textures/Environment/Lana_Concept_Assets/Bushes/T_HD_Bush_Broadleaf_02.png";
            string pGrass     = "Assets/Textures/Environment/Lana_Concept_Assets/Grass/T_HD_GrassCarpet_02_TallMeadow.png";

            // Layer 1: Bầu trời trong lành
            Spawn2DSprite(stage, "Sky", pSky, new Vector3(0f, 2.4f, 0f), new Vector2(2.40f, 2.20f), 1, Color.white, pivot: new Vector2(0.5f, 0.5f));

            // Layer 2: Rặng vườn xa & Rặng dừa mờ khói lam (Sorting 2-3)
            Color farGardenHaze = new Color(0.55f, 0.72f, 0.80f, 0.88f);
            SpawnContinuousRidge(stage, "FarGardenRidge", pMound1, pMound2, -16f, 16f, -2.85f, 2.6f, 2, new Color(0.42f, 0.58f, 0.65f));
            Spawn2DSprite(stage, "FarCoconut_1",    pTree6, new Vector3(-5.2f, 0.4f, 0f), new Vector2(0.80f, 0.80f), 3, farGardenHaze);
            Spawn2DSprite(stage, "FarCoconut_2",    pTree6, new Vector3( 4.5f, 0.4f, 0f), new Vector2(0.75f, 0.75f), 3, farGardenHaze, true);
            Spawn2DSprite(stage, "FarBamboo_Garden",pTree7, new Vector3( 1.5f, 0.35f, 0f), new Vector2(0.70f, 0.70f), 3, farGardenHaze);

            // Layer 3: Dải sương mù vườn sớm (Sorting 4)
            SpawnMistBand(stage, "Garden_FarMist", new Vector3(0f, 0.3f, 0f), new Vector2(34.0f, 2.0f), new Color(0.72f, 0.85f, 0.90f, 0.12f), 4);

            // Layer 4: NGÔI NHÀ LÁ MIỀN TÂY THẤP THOÁNG TRONG VƯỜN (Sorting 5-6)
            SpawnContinuousRidge(stage, "MidGardenRidge", pMound2, pMound1, -16f, 16f, -2.80f, 1.8f, 5, new Color(0.32f, 0.46f, 0.32f));
            Color houseMistTint = new Color(0.72f, 0.82f, 0.82f, 0.95f);
            Spawn2DSprite(stage, "Rural_ThatchHouse", pHouse, new Vector3(-3.2f, -1.80f, 0f), new Vector2(0.70f, 0.70f), 6, houseMistTint);
            Spawn2DSprite(stage, "House_Banana",     pTree5, new Vector3(-5.2f, -1.50f, 0f), new Vector2(0.90f, 0.90f), 6, houseMistTint);
            Spawn2DSprite(stage, "House_Bamboo",     pTree7, new Vector3(-1.2f, -1.55f, 0f), new Vector2(0.92f, 0.92f), 6, houseMistTint);

            // Layer 5: Sương lam mặt đất sau giàn bầu (Sorting 7)
            SpawnMistBand(stage, "Garden_MidMist", new Vector3(0f, -0.9f, 0f), new Vector2(34.0f, 1.6f), new Color(0.80f, 0.92f, 0.92f, 0.10f), 7);

            // Layer 6: Rặng đồi đệm gần (Sorting 8-9)
            Color nearGardenTint = new Color(0.65f, 0.80f, 0.68f, 1.0f);
            SpawnContinuousRidge(stage, "NearGardenRidge", pMound1, pMound2, -16f, 16f, -2.75f, 1.2f, 8, new Color(0.24f, 0.38f, 0.22f));
            Spawn2DSprite(stage, "NearBanana_Right",pTree5,  new Vector3( 4.8f, -1.80f, 0f), new Vector2(0.95f, 0.95f), 9, nearGardenTint, true);

            // Layer 7: Vệt nắng vàng rọi vào giàn bầu (Sorting 10)
            SpawnSunbeam(stage, "Sunbeam_Garden1", new Vector3( 1.5f, 1.4f, 0f), new Vector2(2.4f, 9.5f), -28f, new Color(1.0f, 0.96f, 0.70f, 0.20f), 10);
            SpawnSunbeam(stage, "Sunbeam_Garden2", new Vector3(-0.8f, 0.7f, 0f), new Vector2(2.6f, 9.0f), -26f, new Color(1.0f, 0.96f, 0.70f, 0.24f), 10);

            // Layer 8: Mặt cắt đất vườn quê (Sorting 11)
            SpawnUnderbellyCrossSection(stage, "Garden_Earth_CrossSection", -16f, 16f, new Color(0.95f, 0.85f, 0.70f), 11);

            // Layer 9: Dải thảm cỏ mép vườn liên tục (Sorting 12)
            for (float gx = -16f; gx <= 16f; gx += 1.8f)
            {
                Spawn2DSprite(stage, "GrassEdge", pGrass, new Vector3(gx, GroundY - 0.05f, 0f), new Vector2(0.36f, 0.36f), 12, new Color(0.92f, 0.98f, 0.82f));
            }

            // Layer 10: Gameplay Hero Layer tại Y = GroundY = -2.7m (Sorting 13-18)
            // Khóm chuối & tre ranh giới trái
            Spawn2DSprite(stage, "Border_Bamboo_Left", pTree7, new Vector3(-7.2f, GroundY, 0f), new Vector2(1.10f, 1.10f), 13, Color.white);
            Spawn2DSprite(stage, "Border_Banana_Left", pTree5, new Vector3(-5.4f, GroundY, 0f), new Vector2(0.98f, 0.98f), 14, Color.white);

            // Cây dừa nghiêng ranh giới phải
            Spawn2DSprite(stage, "Border_Coconut_Right", pTree6, new Vector3( 7.2f, GroundY, 0f), new Vector2(1.20f, 1.20f), 13, Color.white);
            Spawn2DSprite(stage, "Border_Banana_Right",  pTree5, new Vector3( 5.2f, GroundY, 0f), new Vector2(0.92f, 0.92f), 14, Color.white, true);

            // GIÀN BẦU BÍ CHÍNH (X = 0m, Chân cột chạm đất ở GroundY = -2.7m)
            Spawn2DSprite(stage, "Trellis_MainFrame", pTrellis, new Vector3(0f, GroundY, 0f), new Vector2(1.05f, 1.05f), 15, Color.white);

            // Quả bầu buông rủ
            Spawn2DSprite(stage, "Gourd_Long_1", pGourdLong, new Vector3(-0.85f, -0.15f, 0f), new Vector2(0.35f, 0.35f), 17, Color.white);
            Spawn2DSprite(stage, "Gourd_Pear_2", pGourdPear, new Vector3( 0.20f, -0.05f, 0f), new Vector2(0.38f, 0.38f), 17, Color.white);
            Spawn2DSprite(stage, "Gourd_Long_3", pGourdLong, new Vector3( 1.10f, -0.10f, 0f), new Vector2(0.32f, 0.32f), 17, Color.white, true);

            // Hoa bầu vàng tươi
            Spawn2DSprite(stage, "Flower_1", pFlower, new Vector3(-0.45f, 0.45f, 0f), new Vector2(0.35f, 0.35f), 18, Color.white);
            Spawn2DSprite(stage, "Flower_2", pFlower, new Vector3( 0.65f, 0.48f, 0f), new Vector2(0.32f, 0.32f), 18, Color.white, true);

            // Bí ngô, phiến đá, hoa bụi
            Spawn2DSprite(stage, "Garden_Slab",       pRockSlab,  new Vector3( 1.85f, GroundY, 0f), new Vector2(0.45f, 0.45f), 16, Color.white);
            Spawn2DSprite(stage, "Garden_Pumpkin",    pPumpkin,   new Vector3( 1.75f, GroundY, 0f), new Vector2(0.42f, 0.42f), 17, Color.white);
            Spawn2DSprite(stage, "Garden_HalfGourd",  pGourdHalf,  new Vector3( 2.30f, GroundY, 0f), new Vector2(0.36f, 0.36f), 18, Color.white);
            Spawn2DSprite(stage, "Garden_FlowerBush", pBushFlw,    new Vector3(-2.40f, GroundY, 0f), new Vector2(0.42f, 0.42f), 16, Color.white);

            // Scale Figure 1.4m đứng dưới giàn bầu
            SpawnScaleFigure(stage, new Vector3(-0.1f, GroundY, 0f));

            // Layer 11: Tiền cảnh lá chuối & cành lá sum suê (Sorting 22-24)
            Color fgDark = new Color(0.04f, 0.07f, 0.05f, 0.98f);
            Spawn2DSprite(stage, "FG_Leaves_TopLeft",  pBush1, new Vector3(-7.5f, 4.6f, 0f), new Vector2(1.20f, 1.20f), 22, fgDark);
            Spawn2DSprite(stage, "FG_Leaves_TopRight", pBush2, new Vector3( 7.5f, 4.6f, 0f), new Vector2(1.20f, 1.20f), 22, fgDark, true);

            RenderStageToPNG(stage, cam, "LayoutMockup_02_RuralGarden_GourdTrellis.png");
            Object.DestroyImmediate(stage);
        }

        // =========================================================================================
        // PHÂN KHU 3: CẦU GỖ KÊNH RẠCH & XUỒNG BA LÁ (CANAL BRIDGE & SAMPAN) — DEPTH OVERHAUL
        // =========================================================================================
        private static void GenerateBoard3_CanalBridgeSampan()
        {
            var stage = new GameObject("__Stage_Canal__");
            var camGO = new GameObject("Cam");
            camGO.transform.SetParent(stage.transform, false);
            camGO.transform.position = new Vector3(0f, CamY, -10f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.40f, 0.65f, 0.85f);

            string pSky       = "Assets/Textures/Backgrounds_Lana/BG_Lana_CoastalBay_Day.jpg";
            string pMound1    = "Assets/Textures/Tree/Curved_Tree_Mound.png";
            string pMound2    = "Assets/Textures/Tree/Curved_Tree_V2_Mound.png";
            string pTree5     = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_05_Banana.png";
            string pTree6     = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png";
            string pTree7     = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png";
            string pReeds     = "Assets/Textures/Environment/Lana_Concept_Assets/Wildflowers/T_HD_Wildflower_02_Reeds.png";
            string pBridge    = "Assets/Textures/Canh1_Sprites/BG_Prop_02.png";
            string pSampan    = "Assets/Textures/Canh1_Sprites/BG_Prop_03.png";
            string pMangrove  = "Assets/Textures/Canh1_Sprites/FG_Mangrove_07.png";
            string pRoots     = "Assets/Textures/Canh1_Sprites/FG_Mangrove_01.png";
            string pBush1     = "Assets/Textures/Environment/Lana_Concept_Assets/Bushes/T_HD_Bush_Broadleaf_01.png";
            string pGrass     = "Assets/Textures/Environment/Lana_Concept_Assets/Grass/T_HD_GrassCarpet_02_TallMeadow.png";

            // Layer 1: Bầu trời & Mây trắng rộng mở (Sorting 1)
            Spawn2DSprite(stage, "Sky", pSky, new Vector3(0f, 2.2f, 0f), new Vector2(2.40f, 2.20f), 1, Color.white, pivot: new Vector2(0.5f, 0.5f));

            // Layer 2: Rặng dừa & Rừng ngập mặn xa xôi bên kia sông (Sorting 2-3)
            Color farRiverHaze = new Color(0.55f, 0.72f, 0.78f, 0.88f);
            SpawnContinuousRidge(stage, "FarRiverRidge", pMound1, pMound2, -16f, 16f, -3.15f, 2.6f, 2, new Color(0.40f, 0.56f, 0.62f));
            Spawn2DSprite(stage, "FarBank_Coconut1",pTree6,   new Vector3(-5.2f, 0.2f, 0f), new Vector2(0.80f, 0.80f), 3, farRiverHaze);
            Spawn2DSprite(stage, "FarBank_Bamboo",  pTree7,   new Vector3( 0.2f, 0.15f, 0f), new Vector2(0.75f, 0.75f), 3, farRiverHaze);
            Spawn2DSprite(stage, "FarBank_Coconut2",pTree6,   new Vector3( 5.5f, 0.2f, 0f), new Vector2(0.78f, 0.78f), 3, farRiverHaze, true);

            // Layer 3: Sương sớm ven sông tầng xa (Sorting 4)
            SpawnMistBand(stage, "River_FarMist", new Vector3(0f, 0.1f, 0f), new Vector2(34.0f, 2.2f), new Color(0.72f, 0.86f, 0.92f, 0.12f), 4);

            // Layer 4: Bờ kênh trung cảnh với bụi đước & lau sậy (Sorting 5-6)
            Color midBankHaze = new Color(0.42f, 0.62f, 0.56f, 1.0f);
            SpawnContinuousRidge(stage, "MidRiverRidge", pMound2, pMound1, -16f, 16f, -3.05f, 1.8f, 5, new Color(0.30f, 0.46f, 0.34f));
            Spawn2DSprite(stage, "MidBank_Mangrove", pMangrove,  new Vector3(-2.2f, -0.80f, 0f), new Vector2(0.90f, 0.90f), 6, midBankHaze);
            Spawn2DSprite(stage, "MidBank_Reeds",    pReeds,     new Vector3( 2.4f, -0.85f, 0f), new Vector2(0.55f, 0.55f), 6, midBankHaze);

            // Layer 5: Mặt nước sông Cửu Long xanh ngọc phản quang (Sorting 7-8)
            float waterTop = GroundY - 0.25f; // -2.95m
            float waterHeight = waterTop - ViewBottomY; // 1.45m
            SpawnSolidBox(stage, "Canal_Water_Surface", new Vector3(0f, ViewBottomY + waterHeight * 0.5f, 0f), new Vector2(34.0f, waterHeight), new Color(0.12f, 0.42f, 0.58f), 7);
            SpawnSolidBox(stage, "Water_Shimmer", new Vector3(0f, waterTop - 0.04f, 0f), new Vector2(34.0f, 0.10f), new Color(0.42f, 0.76f, 0.90f, 0.75f), 8);

            // CHIẾC XUỒNG BA LÁ NÓN LÁ TRUYỀN THỐNG (Bồng bềnh trên mặt nước, Sorting 9)
            Spawn2DSprite(stage, "Hero_Sampan", pSampan, new Vector3(-1.2f, waterTop - 0.25f, 0f), new Vector2(1.30f, 1.30f), 9, Color.white);

            // Layer 6: Sương mù là đà trên mặt nước kênh (Sorting 10)
            SpawnMistBand(stage, "Water_Hover_Mist", new Vector3(0f, waterTop + 0.12f, 0f), new Vector2(34.0f, 0.8f), new Color(0.85f, 0.95f, 1.0f, 0.16f), 10);

            // Layer 7: Bờ đất hai bên mố cầu (West bank: -16m đến -2.6m, East bank: +2.6m đến +16m, Sorting 11)
            Color earthBankColor = new Color(0.85f, 0.75f, 0.65f);
            SpawnUnderbellyCrossSection(stage, "West_Bank_Earth", -16f, -2.6f, earthBankColor, 11);
            SpawnUnderbellyCrossSection(stage, "East_Bank_Earth",   2.6f, 16f, earthBankColor, 11);

            // Thảm cỏ mép đất hai bờ (Sorting 12)
            for (float gx = -16f; gx <= -2.6f; gx += 1.8f)
            {
                Spawn2DSprite(stage, "West_Grass", pGrass, new Vector3(gx, GroundY - 0.05f, 0f), new Vector2(0.36f, 0.36f), 12, new Color(0.88f, 0.98f, 0.80f));
            }
            for (float gx = 2.6f; gx <= 16f; gx += 1.8f)
            {
                Spawn2DSprite(stage, "East_Grass", pGrass, new Vector3(gx, GroundY - 0.05f, 0f), new Vector2(0.36f, 0.36f), 12, new Color(0.88f, 0.98f, 0.80f));
            }

            // Bụi đước Cà Mau có rễ cọc bám bờ kênh (Sorting 13)
            Spawn2DSprite(stage, "West_Roots",    pRoots,    new Vector3(-2.85f, GroundY - 0.15f, 0f), new Vector2(0.70f, 0.70f), 13, new Color(0.85f, 0.85f, 0.85f));
            Spawn2DSprite(stage, "West_Mangrove", pMangrove, new Vector3(-4.8f, GroundY, 0f), new Vector2(1.05f, 1.05f), 13, Color.white);
            Spawn2DSprite(stage, "West_Bamboo",   pTree7,    new Vector3(-6.8f, GroundY, 0f), new Vector2(1.15f, 1.15f), 13, Color.white);
            Spawn2DSprite(stage, "West_Reeds",    pReeds,    new Vector3(-3.2f, GroundY, 0f), new Vector2(0.48f, 0.48f), 13, Color.white);

            Spawn2DSprite(stage, "East_Coconut",  pTree6,    new Vector3( 6.8f, GroundY, 0f), new Vector2(1.20f, 1.20f), 13, Color.white);
            Spawn2DSprite(stage, "East_Mangrove", pMangrove, new Vector3( 4.5f, GroundY, 0f), new Vector2(0.95f, 0.95f), 13, Color.white, true);
            Spawn2DSprite(stage, "East_Reeds",    pReeds,    new Vector3( 3.2f, GroundY, 0f), new Vector2(0.48f, 0.48f), 13, Color.white, true);

            // Layer 8: CẦU VÁN GỖ MỘC MẠC CHUẨN TỶ LỆ (Sử dụng chính sprite cầu gỗ BG_Prop_02, Sorting 14)
            // Neo chân ván tại GroundY = -2.7m (pivot y = 0.78f)
            Spawn2DSprite(stage, "Hero_Wood_Bridge", pBridge, new Vector3(0f, GroundY, 0f), new Vector2(2.15f, 1.45f), 14, Color.white, pivot: new Vector2(0.5f, 0.78f));

            // Scale Figure: Nhân vật cao 1.4m dạo bước trên cầu
            SpawnScaleFigure(stage, new Vector3(0.2f, GroundY, 0f));

            // Layer 9: Tiền cảnh cành lá rủ & rễ đước buông xuống (Sorting 22-24)
            Color fgDark = new Color(0.04f, 0.07f, 0.05f, 0.98f);
            Spawn2DSprite(stage, "FG_Canopy_TopLeft", pBush1, new Vector3(-7.5f, 4.6f, 0f), new Vector2(1.20f, 1.20f), 22, fgDark);
            Spawn2DSprite(stage, "FG_Canopy_TopRight",pBush1, new Vector3( 7.5f, 4.6f, 0f), new Vector2(1.20f, 1.20f), 22, fgDark, true);

            RenderStageToPNG(stage, cam, "LayoutMockup_03_CanalBridge_Sampan.png");
            Object.DestroyImmediate(stage);
        }

        // =========================================================================================
        // PHÂN KHU 4: BIỂN LÚA VÀNG MÊNH MÔNG (VAST RICE PADDY FIELD SEA) — DEPTH OVERHAUL
        // =========================================================================================
        private static void GenerateBoard4_VastRiceSea()
        {
            var stage = new GameObject("__Stage_Rice__");
            var camGO = new GameObject("Cam");
            camGO.transform.SetParent(stage.transform, false);
            camGO.transform.position = new Vector3(0f, CamY, -10f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.88f, 0.68f, 0.42f);

            string pSky    = "Assets/Textures/Backgrounds_Lana/BG_Lana_HillsFields_Sunset.jpg";
            string pMound1 = "Assets/Textures/Tree/Curved_Tree_Mound.png";
            string pMound2 = "Assets/Textures/Tree/Curved_Tree_V2_Mound.png";
            string pTree1  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_01_LargeCanopy.png";
            string pTree6  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png";
            string pTree7  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png";
            string pWind   = "Assets/Textures/Environment/Lana_Concept_Assets/Grass/T_HD_GrassCarpet_03_Windblown.png";
            string pReeds  = "Assets/Textures/Environment/Lana_Concept_Assets/Wildflowers/T_HD_Wildflower_02_Reeds.png";

            // Layer 1: Bầu trời hoàng hôn vàng ấm (Sorting 1)
            Spawn2DSprite(stage, "Sky", pSky, new Vector3(0f, 2.2f, 0f), new Vector2(2.40f, 2.20f), 1, new Color(1.0f, 0.98f, 0.90f), pivot: new Vector2(0.5f, 0.5f));

            // Layer 2: Rặng đồi & Rặng tre dừa xa xăm chân trời (Sorting 2-3)
            Color farHorizonGold = new Color(0.88f, 0.82f, 0.58f, 1.0f);
            SpawnContinuousRidge(stage, "FarRiceRidge", pMound1, pMound2, -16f, 16f, -2.85f, 2.6f, 2, new Color(0.65f, 0.58f, 0.40f));
            Spawn2DSprite(stage, "Horizon_Coconut_1", pTree6,  new Vector3(-6.2f, 0.2f, 0f), new Vector2(0.65f, 0.65f), 3, farHorizonGold);
            Spawn2DSprite(stage, "Horizon_Bamboo_2",  pTree7,  new Vector3(-2.2f, 0.15f, 0f), new Vector2(0.60f, 0.60f), 3, farHorizonGold);
            Spawn2DSprite(stage, "Horizon_Coconut_3", pTree6,  new Vector3( 2.5f, 0.2f, 0f), new Vector2(0.62f, 0.62f), 3, farHorizonGold);

            // Layer 3: Dải sương chiều màu vàng cam (Sorting 4)
            SpawnMistBand(stage, "Sunset_Mist", new Vector3(0f, -0.3f, 0f), new Vector2(34.0f, 2.0f), new Color(0.95f, 0.88f, 0.68f, 0.12f), 4);

            // Layer 4: Mặt nước ruộng ngập trũng phản chiếu trời vàng (Sorting 5)
            SpawnSolidBox(stage, "Rice_Water_Mirror", new Vector3(0f, -1.90f, 0f), new Vector2(34.0f, 1.4f), new Color(0.88f, 0.74f, 0.48f, 0.70f), 5);

            // BIỂN LÚA VÀNG 4 TẦNG TRẢI DÀI MÊNH MÔNG (Sorting 6-9)
            Color riceFar  = new Color(0.85f, 0.80f, 0.48f, 1.0f);
            Color riceMid  = new Color(0.92f, 0.84f, 0.42f, 1.0f);
            Color riceNear = new Color(0.98f, 0.88f, 0.38f, 1.0f);

            // Tier 4: Lúa xa
            for (float rx = -16f; rx <= 5.5f; rx += 1.5f)
            {
                Spawn2DSprite(stage, "Rice_Tier4", pWind, new Vector3(rx, -1.60f, 0f), new Vector2(0.50f, 0.50f), 6, riceFar, (rx % 2 == 0));
            }

            // Tier 3: Lúa trung cảnh 1
            for (float rx = -16f; rx <= 5.5f; rx += 1.6f)
            {
                Spawn2DSprite(stage, "Rice_Tier3", pWind, new Vector3(rx + 0.6f, -1.95f, 0f), new Vector2(0.55f, 0.55f), 7, riceMid, (rx % 2 != 0));
            }

            // Tier 2: Lúa trung cảnh 2
            for (float rx = -16f; rx <= 5.5f; rx += 1.7f)
            {
                Spawn2DSprite(stage, "Rice_Tier2", pWind, new Vector3(rx, -2.30f, 0f), new Vector2(0.60f, 0.60f), 8, riceNear);
            }

            // Layer 5: Vệt nắng chiều hoàng hôn trải dài trên đồng (Sorting 10)
            SpawnSunbeam(stage, "Sunset_Sunbeam1", new Vector3(-2.0f, 1.0f, 0f), new Vector2(3.2f, 10.0f), 35f, new Color(1.0f, 0.92f, 0.65f, 0.20f), 10);
            SpawnSunbeam(stage, "Sunset_Sunbeam2", new Vector3( 1.5f, 1.4f, 0f), new Vector2(2.8f, 10.0f), 32f, new Color(1.0f, 0.90f, 0.60f, 0.16f), 10);

            // Layer 6: CÂY ĐA ĐẠI THỤ ĐÍCH ĐẾN (GOAL LANDMARK) Ở GÓC PHẢI (Sorting 11-13)
            Spawn2DSprite(stage, "Goal_AncientBanyan", pTree1, new Vector3(6.8f, GroundY, 0f), new Vector2(1.40f, 1.40f), 11, new Color(1.0f, 0.96f, 0.88f));
            Spawn2DSprite(stage, "Goal_Reeds",        pReeds, new Vector3(4.8f, GroundY, 0f), new Vector2(0.48f, 0.48f), 12, riceNear);

            // Layer 7: BỜ ĐÊ ĐẤT DẪN LỐI TẠI Y = -2.70m (Sorting 11)
            SpawnUnderbellyCrossSection(stage, "Dike_Earth_CrossSection", -16f, 16f, new Color(0.95f, 0.80f, 0.55f), 11);

            // Viền lúa sát mép đê (Tier 1, Sorting 12)
            for (float rx = -16f; rx <= 5.0f; rx += 1.8f)
            {
                Spawn2DSprite(stage, "Rice_DikeEdge", pWind, new Vector3(rx, GroundY - 0.05f, 0f), new Vector2(0.65f, 0.65f), 12, riceNear);
            }

            // Scale Figure: Nhân vật cao 1.4m dạo bước trên bờ đê lúa
            SpawnScaleFigure(stage, new Vector3(-1.5f, GroundY, 0f));

            RenderStageToPNG(stage, cam, "LayoutMockup_04_VastRiceSea.png");
            Object.DestroyImmediate(stage);
        }

        // =========================================================================================
        // BẢN VẼ TOÀN CẢNH 60M MASTER PANORAMA (3840x1080 ULTRA-WIDE 32:9) — DEPTH OVERHAUL
        // =========================================================================================
        private static void GenerateMasterPanorama60m()
        {
            var stage = new GameObject("__Stage_Panorama__");
            var camGO = new GameObject("Cam");
            camGO.transform.SetParent(stage.transform, false);
            camGO.transform.position = new Vector3(0f, CamY, -10f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.42f, 0.65f, 0.85f);

            // Toàn cảnh 60m: 3840x1080 (tỷ lệ 32:9 -> W = 38.4m, X in [-19.2, +19.2])
            // Phân khu 1 (Rừng rậm): X = -19.2m đến -9.6m (Tâm = -14.4m)
            // Phân khu 2 (Vườn quê): X = -9.6m đến 0.0m   (Tâm = -4.8m)
            // Phân khu 3 (Cầu gỗ):   X = 0.0m đến +9.6m   (Tâm = +4.8m)
            // Phân khu 4 (Đồng lúa): X = +9.6m đến +19.2m (Tâm = +14.4m)

            string pSkyDay   = "Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png";
            string pSkyCoast = "Assets/Textures/Backgrounds_Lana/BG_Lana_CoastalBay_Day.jpg";
            string pSkySunset= "Assets/Textures/Backgrounds_Lana/BG_Lana_HillsFields_Sunset.jpg";
            string pMound1 = "Assets/Textures/Tree/Curved_Tree_Mound.png";
            string pMound2 = "Assets/Textures/Tree/Curved_Tree_V2_Mound.png";
            string pTree1  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_01_LargeCanopy.png";
            string pTree4  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_04_SlenderTall.png";
            string pTree5  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_05_Banana.png";
            string pTree6  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png";
            string pTree7  = "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png";
            string pHouse  = "Assets/Textures/Canh1_Sprites/BG_Prop_04.png";
            string pTrellis= "Assets/Textures/Environment/Lana_Concept_Assets/GourdTrellis/Trellises/T_HD_GourdTrellis_01_Main.png";
            string pBridge = "Assets/Textures/Canh1_Sprites/BG_Prop_02.png";
            string pSampan = "Assets/Textures/Canh1_Sprites/BG_Prop_03.png";
            string pMangrove= "Assets/Textures/Canh1_Sprites/FG_Mangrove_07.png";
            string pWind   = "Assets/Textures/Environment/Lana_Concept_Assets/Grass/T_HD_GrassCarpet_03_Windblown.png";
            string pRock1  = "Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_01_LargeCliffOutcrop.png";
            string pReeds  = "Assets/Textures/Environment/Lana_Concept_Assets/Wildflowers/T_HD_Wildflower_02_Reeds.png";
            string pGrass  = "Assets/Textures/Environment/Lana_Concept_Assets/Grass/T_HD_GrassCarpet_02_TallMeadow.png";

            // Layer 1: Bầu trời Panorama chuyển tiếp 4 phân khu (Rừng -> Vườn -> Bờ Sông -> Đồng Lúa Hoàng Hôn)
            Spawn2DSprite(stage, "Sky_1", pSkyDay,    new Vector3(-16.0f, 2.2f, 0f), new Vector2(2.45f, 2.10f), 1, Color.white, pivot: new Vector2(0.5f, 0.5f));
            Spawn2DSprite(stage, "Sky_2", pSkyDay,    new Vector3( -5.0f, 2.2f, 0f), new Vector2(2.45f, 2.10f), 1, Color.white, pivot: new Vector2(0.5f, 0.5f));
            Spawn2DSprite(stage, "Sky_3", pSkyCoast,  new Vector3(  5.0f, 2.2f, 0f), new Vector2(2.45f, 2.10f), 1, Color.white, pivot: new Vector2(0.5f, 0.5f));
            Spawn2DSprite(stage, "Sky_4", pSkySunset, new Vector3( 16.0f, 2.2f, 0f), new Vector2(2.45f, 2.10f), 1, Color.white, pivot: new Vector2(0.5f, 0.5f));

            // Layer 2: Rặng gò đồi & Cây cối mù sương chạy suốt 60m (Sorting 2-3)
            Color farHaze = new Color(0.55f, 0.72f, 0.80f, 0.88f);
            SpawnContinuousRidge(stage, "Pano_FarRidge", pMound1, pMound2, -30f, 30f, -2.90f, 2.6f, 2, new Color(0.42f, 0.58f, 0.65f));
            Spawn2DSprite(stage, "Pano_FarTree1", pTree1, new Vector3(-17.0f, 0.4f, 0f), new Vector2(0.75f, 0.75f), 3, farHaze);
            Spawn2DSprite(stage, "Pano_FarTree2", pTree7, new Vector3(-12.5f, 0.35f, 0f), new Vector2(0.70f, 0.70f), 3, farHaze);
            Spawn2DSprite(stage, "Pano_FarTree3", pTree6, new Vector3( -6.5f, 0.35f, 0f), new Vector2(0.75f, 0.75f), 3, farHaze);
            Spawn2DSprite(stage, "Pano_FarTree4", pTree6, new Vector3(  3.5f, 0.35f, 0f), new Vector2(0.75f, 0.75f), 3, farHaze);
            Spawn2DSprite(stage, "Pano_FarTree5", pTree7, new Vector3( 12.5f, 0.35f, 0f), new Vector2(0.65f, 0.65f), 3, new Color(0.85f, 0.82f, 0.65f));

            // Layer 3: Dải sương mù thung lũng chạy dài (Sorting 4)
            SpawnMistBand(stage, "Pano_Mist_Far", new Vector3(0f, 0.2f, 0f), new Vector2(64.0f, 2.0f), new Color(0.74f, 0.86f, 0.92f, 0.10f), 4);

            // Layer 4: Rặng gò đồi trung cảnh & Nhà lá vườn quê (Sorting 5-6)
            SpawnContinuousRidge(stage, "Pano_MidRidge", pMound2, pMound1, -30f, 30f, -2.80f, 1.8f, 5, new Color(0.32f, 0.46f, 0.34f));
            Spawn2DSprite(stage, "Pano_ThatchHouse", pHouse, new Vector3(-7.2f, -1.80f, 0f), new Vector2(0.68f, 0.68f), 6, new Color(0.70f, 0.80f, 0.82f, 0.92f));
            Spawn2DSprite(stage, "Pano_MidBamboo1",  pTree7, new Vector3(-14.2f, -0.60f, 0f), new Vector2(0.88f, 0.88f), 6, new Color(0.48f, 0.68f, 0.58f));
            Spawn2DSprite(stage, "Pano_MidMangrove", pMangrove, new Vector3(2.5f, -0.65f, 0f), new Vector2(0.85f, 0.85f), 6, new Color(0.48f, 0.68f, 0.62f));

            // Sương mù tầng trung (Sorting 7)
            SpawnMistBand(stage, "Pano_Mist_Mid", new Vector3(0f, -0.9f, 0f), new Vector2(64.0f, 1.6f), new Color(0.80f, 0.90f, 0.94f, 0.08f), 7);

            // Dải đất nền chạy suốt toàn cảnh (ngắt quãng bởi dòng sông ở X: +2.0m đến +7.4m)
            Color earthColor = new Color(0.90f, 0.80f, 0.68f);
            SpawnUnderbellyCrossSection(stage, "Earth_West", -30f, 2.0f, earthColor, 10);
            SpawnUnderbellyCrossSection(stage, "Earth_East",  7.4f, 30f,  earthColor, 10);

            // Dòng sông Phân khu 3 (X = 2.0m đến 7.4m)
            float waterTop = GroundY - 0.25f;
            float waterH = waterTop - ViewBottomY;
            SpawnSolidBox(stage, "Pano_Water", new Vector3(4.7f, ViewBottomY + waterH * 0.5f, 0f), new Vector2(6.0f, waterH), new Color(0.12f, 0.42f, 0.58f), 8);
            SpawnSolidBox(stage, "Pano_Water_Shimmer", new Vector3(4.7f, waterTop - 0.04f, 0f), new Vector2(6.0f, 0.10f), new Color(0.42f, 0.76f, 0.90f, 0.75f), 9);
            SpawnMistBand(stage, "Pano_Water_Mist", new Vector3(4.7f, waterTop + 0.12f, 0f), new Vector2(7.0f, 0.8f), new Color(0.85f, 0.95f, 1.0f, 0.30f), 10);

            // Thảm cỏ mép đất
            for (float gx = -30.0f; gx <= 1.8f; gx += 1.8f)
            {
                Spawn2DSprite(stage, "GrassEdge", pGrass, new Vector3(gx, GroundY - 0.05f, 0f), new Vector2(0.36f, 0.36f), 12, new Color(0.90f, 0.96f, 0.80f));
            }
            for (float gx = 7.6f; gx <= 30.0f; gx += 1.8f)
            {
                Spawn2DSprite(stage, "GrassEdge", pGrass, new Vector3(gx, GroundY - 0.05f, 0f), new Vector2(0.36f, 0.36f), 12, new Color(0.90f, 0.96f, 0.80f));
            }

            // Vệt nắng chiếu rọi các phân khu (Sorting 11)
            SpawnSunbeam(stage, "Pano_Sunbeam1", new Vector3(-13.0f, 1.4f, 0f), new Vector2(2.5f, 9.5f), -30f, new Color(1.0f, 0.96f, 0.70f, 0.18f), 11);
            SpawnSunbeam(stage, "Pano_Sunbeam2", new Vector3( -3.5f, 1.4f, 0f), new Vector2(2.5f, 9.5f), -28f, new Color(1.0f, 0.96f, 0.70f, 0.18f), 11);
            SpawnSunbeam(stage, "Pano_Sunbeam3", new Vector3( 13.5f, 1.4f, 0f), new Vector2(2.8f, 9.5f),  32f, new Color(1.0f, 0.92f, 0.65f, 0.18f), 11);

            // ĐẶT CÁC HERO LANDMARK TRÊN 4 PHÂN KHU (Sorting 12-16):

            // 1. Phân khu 1: Rừng rậm Cổ thụ Đa Mẹ + Vách đá rêu
            Spawn2DSprite(stage, "Pano_ForestBanyan", pTree1, new Vector3(-16.0f, GroundY, 0f), new Vector2(1.25f, 1.25f), 12, Color.white);
            Spawn2DSprite(stage, "Pano_ForestRock",   pRock1, new Vector3(-17.5f, GroundY, 0f), new Vector2(0.48f, 0.48f), 13, Color.white);
            Spawn2DSprite(stage, "Pano_ForestBamboo", pTree7, new Vector3(-11.5f, GroundY, 0f), new Vector2(1.05f, 1.05f), 12, Color.white);

            // 2. Phân khu 2: Vườn quê Giàn bầu bí trung tâm
            Spawn2DSprite(stage, "Pano_GourdTrellis", pTrellis, new Vector3(-4.8f, GroundY, 0f), new Vector2(0.95f, 0.95f), 13, Color.white);
            Spawn2DSprite(stage, "Pano_GardenBanana", pTree5,   new Vector3(-1.8f, GroundY, 0f), new Vector2(0.90f, 0.90f), 12, Color.white);

            // 3. Phân khu 3: Cầu gỗ bắc qua kênh & Xuồng ba lá
            Spawn2DSprite(stage, "Pano_Hero_Bridge",  pBridge,  new Vector3(4.7f, GroundY, 0f), new Vector2(1.85f, 1.35f), 14, Color.white, pivot: new Vector2(0.5f, 0.78f));
            Spawn2DSprite(stage, "Pano_Sampan",       pSampan,  new Vector3(3.6f, waterTop - 0.25f, 0f), new Vector2(1.15f, 1.15f), 11, Color.white);
            Spawn2DSprite(stage, "Pano_CanalCoconut", pTree6,   new Vector3(8.5f, GroundY, 0f), new Vector2(1.15f, 1.15f), 12, Color.white);
            Spawn2DSprite(stage, "Pano_CanalReeds",   pReeds,   new Vector3(7.4f, GroundY, 0f), new Vector2(0.45f, 0.45f), 13, Color.white);

            // 4. Phân khu 4: Đồng lúa vàng 3 tầng & Cây đa đại thụ đích đến
            for (float rx = 8.5f; rx <= 16.5f; rx += 1.6f)
            {
                Spawn2DSprite(stage, "Pano_Rice_Mid", pWind, new Vector3(rx, -2.20f, 0f), new Vector2(0.55f, 0.55f), 11, new Color(0.92f, 0.84f, 0.42f));
                Spawn2DSprite(stage, "Pano_Rice_Fore",pWind, new Vector3(rx + 0.5f, GroundY - 0.05f, 0f), new Vector2(0.62f, 0.62f), 15, new Color(0.98f, 0.88f, 0.38f));
            }
            Spawn2DSprite(stage, "Pano_AncientGoal", pTree1, new Vector3(17.2f, GroundY, 0f), new Vector2(1.35f, 1.35f), 12, new Color(1.0f, 0.96f, 0.88f));

            // Scale Figures đặt tại các mốc phân khu để so sánh tỷ lệ
            SpawnScaleFigure(stage, new Vector3(-14.0f, GroundY, 0f));
            SpawnScaleFigure(stage, new Vector3( -4.8f, GroundY, 0f));
            SpawnScaleFigure(stage, new Vector3(  4.7f, GroundY, 0f));
            SpawnScaleFigure(stage, new Vector3( 12.0f, GroundY, 0f));

            RenderStageToPNG(stage, cam, "LayoutMockup_Master_Panorama_60m.png", width: 3840, height: 1080);
            Object.DestroyImmediate(stage);
        }
    }
}
#endif
