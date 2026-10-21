#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Tự động đồng bộ hóa, chuẩn hóa tỷ lệ (~1.5m nhân vật), Pivot, Material, hiệu ứng gió đung đưa (PlanetOfLanaFoliageSway)
    /// và đóng gói toàn bộ 54 Assets từ 2 bảng Concept Art "Cây Cối" & "Dàn Bầu Bí" (Phong cách Planet of Lana Việt Nam)
    /// thành Prefab thống nhất sẵn sàng kéo thả vào Scene.
    /// </summary>
    [InitializeOnLoad]
    public static class LanaVNAssetPackBuilder
    {
        public const string TexturesRoot = "Assets/Textures/Environment/Lana_VN_Pack";
        public const string PrefabsRoot  = "Assets/Prefabs/Environment/Lana_VN_Pack";
        public const string MaterialsDir = "Assets/Materials/LanaOverhaul";

        private struct AssetConfig
        {
            public string Subfolder;
            public string TexName;
            public float PixelsPerUnit;
            public Vector2 Pivot;
            public int SortingOrder;
            public bool AddWindSway;
            public float SwayMaxAngle;
            public float SwaySpeed;

            public AssetConfig(string sub, string texName, float ppu, Vector2 pivot, int sortOrder, bool sway, float angle = 1.8f, float speed = 1.5f)
            {
                Subfolder = sub;
                TexName = texName;
                PixelsPerUnit = ppu;
                Pivot = pivot;
                SortingOrder = sortOrder;
                AddWindSway = sway;
                SwayMaxAngle = angle;
                SwaySpeed = speed;
            }
        }

        // Chuẩn hóa tỷ lệ thế giới thực theo thước đo nhân vật ~1.5m trên bản vẽ:
        // - Sheet 1 (upscaled 2x): Nhân vật 1.5m = 106px -> PPU = 70.6 (làm tròn 70 px/m).
        // - Sheet 2 (upscaled 2x):
        //   + Biến thể dàn bầu bí (1:1 với thước đo nhân vật 152px = 1.5m): PPU = 100 px/m (~2.2m cao).
        //   + Dàn bầu bí chính (vẽ phóng đại chi tiết 2x = 488px cao): PPU = 200 px/m (~2.44m cao).
        //   + Quả bầu, lá bầu, hoa bầu tách lẻ (vẽ phóng đại chi tiết ~5x): PPU = 500 px/m (Quả bầu lớn ~0.55m, Lá bầu ~0.28m, Hoa bầu ~0.24m).
        private static readonly AssetConfig[] AllConfigs = new AssetConfig[]
        {
            // 1. TREES (7) - PPU = 70, Pivot Bottom-Center (0.5, 0.0)
            new AssetConfig("Trees", "T_LanaVN_Tree_01_LargeCanopy",    70f, new Vector2(0.5f, 0.02f), 10, true, 0.9f, 1.1f),
            new AssetConfig("Trees", "T_LanaVN_Tree_02_Medium",         70f, new Vector2(0.5f, 0.02f), 10, true, 1.1f, 1.2f),
            new AssetConfig("Trees", "T_LanaVN_Tree_03_SmallBushTree",  70f, new Vector2(0.5f, 0.02f), 10, true, 1.3f, 1.3f),
            new AssetConfig("Trees", "T_LanaVN_Tree_04_SlenderTall",    70f, new Vector2(0.5f, 0.02f), 10, true, 1.2f, 1.2f),
            new AssetConfig("Trees", "T_LanaVN_Tree_05_Banana",         70f, new Vector2(0.5f, 0.02f), 11, true, 1.6f, 1.4f),
            new AssetConfig("Trees", "T_LanaVN_Tree_06_Coconut",        70f, new Vector2(0.45f,0.02f), 10, true, 1.4f, 1.2f),
            new AssetConfig("Trees", "T_LanaVN_Tree_07_BambooGrove",    70f, new Vector2(0.5f, 0.02f), 10, true, 1.5f, 1.4f),

            // 2. BUSHES (6) - PPU = 75, Pivot Bottom-Center (0.5, 0.04)
            new AssetConfig("Bushes", "T_LanaVN_Bush_Low_01",           75f, new Vector2(0.5f, 0.04f), 12, true, 1.8f, 1.7f),
            new AssetConfig("Bushes", "T_LanaVN_Bush_Low_02_Flower",    75f, new Vector2(0.5f, 0.04f), 12, true, 1.8f, 1.7f),
            new AssetConfig("Bushes", "T_LanaVN_Bush_Low_03",           75f, new Vector2(0.5f, 0.04f), 12, true, 1.8f, 1.7f),
            new AssetConfig("Bushes", "T_LanaVN_Bush_Low_04",           75f, new Vector2(0.5f, 0.04f), 12, true, 1.8f, 1.7f),
            new AssetConfig("Bushes", "T_LanaVN_Bush_Broadleaf_01",     75f, new Vector2(0.5f, 0.04f), 12, true, 1.6f, 1.5f),
            new AssetConfig("Bushes", "T_LanaVN_Bush_Broadleaf_02",     75f, new Vector2(0.5f, 0.04f), 12, true, 1.6f, 1.5f),

            // 3. WILDFLOWERS (6) - PPU = 95, Pivot Bottom-Center (0.5, 0.04)
            new AssetConfig("Wildflowers", "T_LanaVN_Wildflower_01_Reeds",         95f, new Vector2(0.5f, 0.04f), 14, true, 2.8f, 2.0f),
            new AssetConfig("Wildflowers", "T_LanaVN_Wildflower_02_TallGrass",     95f, new Vector2(0.5f, 0.04f), 14, true, 2.8f, 2.0f),
            new AssetConfig("Wildflowers", "T_LanaVN_Wildflower_03_WhiteBlossom",  95f, new Vector2(0.5f, 0.04f), 14, true, 2.5f, 1.9f),
            new AssetConfig("Wildflowers", "T_LanaVN_Wildflower_04_DaisySprig",    95f, new Vector2(0.5f, 0.04f), 14, true, 2.6f, 2.0f),
            new AssetConfig("Wildflowers", "T_LanaVN_Wildflower_05_YellowBlossom", 95f, new Vector2(0.5f, 0.04f), 14, true, 2.5f, 1.9f),
            new AssetConfig("Wildflowers", "T_LanaVN_Wildflower_06_SmallSprig",    95f, new Vector2(0.5f, 0.04f), 14, true, 2.6f, 2.1f),

            // 4. GRASS CARPETS & VINES (10) - PPU = 85
            new AssetConfig("Grass_Vines", "T_LanaVN_GrassCarpet_01",          85f, new Vector2(0.5f, 0.05f), 15, true, 2.4f, 2.0f),
            new AssetConfig("Grass_Vines", "T_LanaVN_GrassCarpet_02_Tall",     85f, new Vector2(0.5f, 0.05f), 15, true, 2.6f, 2.1f),
            new AssetConfig("Grass_Vines", "T_LanaVN_GrassCarpet_03",          85f, new Vector2(0.5f, 0.05f), 15, true, 2.8f, 2.2f),
            new AssetConfig("Grass_Vines", "T_LanaVN_GrassCarpet_04",          85f, new Vector2(0.5f, 0.05f), 15, true, 2.6f, 2.1f),
            new AssetConfig("Grass_Vines", "T_LanaVN_GrassCarpet_05_Dense",    85f, new Vector2(0.5f, 0.05f), 15, true, 2.0f, 1.8f),
            new AssetConfig("Grass_Vines", "T_LanaVN_GrassCarpet_06_Shrubby",  85f, new Vector2(0.5f, 0.05f), 15, true, 2.0f, 1.8f),
            new AssetConfig("Grass_Vines", "T_LanaVN_VineLeaf_01_Trio",       110f, new Vector2(0.5f, 0.10f), 15, true, 2.2f, 1.8f),
            new AssetConfig("Grass_Vines", "T_LanaVN_VineLeaf_02_Cluster",    100f, new Vector2(0.5f, 0.50f), 15, true, 2.0f, 1.7f),
            new AssetConfig("Grass_Vines", "T_LanaVN_VineLeaf_03_Sprout",     110f, new Vector2(0.5f, 0.10f), 15, true, 2.2f, 1.8f),
            new AssetConfig("Grass_Vines", "T_LanaVN_VineLeaf_04_CreepingVine",95f, new Vector2(0.5f, 0.75f), 15, true, 1.8f, 1.5f),

            // 5. MOSSY ROCKS (7) - PPU = 70, Pivot Bottom-Center, No Wind Sway
            new AssetConfig("Rocks", "T_LanaVN_Rock_01_LargeOutcrop",    70f, new Vector2(0.5f, 0.04f), 11, false),
            new AssetConfig("Rocks", "T_LanaVN_Rock_02_MossyMediumTop",  70f, new Vector2(0.5f, 0.05f), 11, false),
            new AssetConfig("Rocks", "T_LanaVN_Rock_03_MossyMediumBot",  70f, new Vector2(0.5f, 0.05f), 11, false),
            new AssetConfig("Rocks", "T_LanaVN_Rock_04_FlatSlab",        70f, new Vector2(0.5f, 0.06f), 11, false),
            new AssetConfig("Rocks", "T_LanaVN_Rock_05_SmallMossy",      70f, new Vector2(0.5f, 0.06f), 13, false),
            new AssetConfig("Rocks", "T_LanaVN_Rock_06_PebbleGroupTop",  70f, new Vector2(0.5f, 0.06f), 13, false),
            new AssetConfig("Rocks", "T_LanaVN_Rock_07_PebbleGroupBot",  70f, new Vector2(0.5f, 0.06f), 13, false),

            // 6. GOURD TRELLISES & PARTS (18)
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdTrellis_Main",            200f, new Vector2(0.5f, 0.02f), 11, false),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdTrellis_Var01_Long",      100f, new Vector2(0.5f, 0.02f), 11, false),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdTrellis_Var02_LeanTo",    100f, new Vector2(0.5f, 0.02f), 11, false),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdTrellis_Var03_LowGround", 100f, new Vector2(0.5f, 0.04f), 12, false),

            // Hanging Gourds - Pivot at Top-Center stem (0.5, 0.95) so they swing naturally like pendulums!
            new AssetConfig("GourdTrellis", "T_LanaVN_Gourd_Bottle_Large",   480f, new Vector2(0.52f, 0.96f), 13, true, 2.2f, 1.4f),
            new AssetConfig("GourdTrellis", "T_LanaVN_Gourd_Bottle_Medium",  380f, new Vector2(0.45f, 0.96f), 13, true, 2.5f, 1.6f),
            new AssetConfig("GourdTrellis", "T_LanaVN_Gourd_Bottle_Small",   400f, new Vector2(0.50f, 0.96f), 13, true, 2.8f, 1.8f),
            new AssetConfig("GourdTrellis", "T_LanaVN_Gourd_Bottle_Half",    380f, new Vector2(0.50f, 0.10f), 14, false),
            new AssetConfig("GourdTrellis", "T_LanaVN_Gourd_Pumpkin_Green",  340f, new Vector2(0.50f, 0.08f), 14, false),

            // Gourd Leaves, Tendrils & Flowers
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdLeaf_01_Large",   450f, new Vector2(0.48f, 0.12f), 14, true, 2.4f, 1.8f),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdLeaf_02_Medium",  450f, new Vector2(0.55f, 0.10f), 14, true, 2.4f, 1.8f),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdLeaf_03_Angled",  450f, new Vector2(0.48f, 0.10f), 14, true, 2.4f, 1.8f),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdTendril_01",      420f, new Vector2(0.50f, 0.92f), 14, true, 3.2f, 2.1f),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdTendril_02",      420f, new Vector2(0.50f, 0.92f), 14, true, 3.2f, 2.1f),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdVine_Branch",     360f, new Vector2(0.35f, 0.50f), 14, true, 2.0f, 1.6f),

            new AssetConfig("GourdTrellis", "T_LanaVN_GourdFlower_01_Bloom",    480f, new Vector2(0.50f, 0.08f), 15, true, 2.5f, 1.9f),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdFlower_02_BudSmall", 480f, new Vector2(0.50f, 0.08f), 15, true, 2.5f, 1.9f),
            new AssetConfig("GourdTrellis", "T_LanaVN_GourdFlower_03_BudTall",  480f, new Vector2(0.50f, 0.08f), 15, true, 2.5f, 1.9f)
        };

        static LanaVNAssetPackBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfMissing;
        }

        private static void AutoBuildIfMissing()
        {
            string markerPrefab = $"{PrefabsRoot}/GourdTrellis/PF_LanaVN_GourdTrellis_25D_Composite.prefab";
            if (Directory.Exists(TexturesRoot) && AssetDatabase.LoadAssetAtPath<GameObject>(markerPrefab) == null)
            {
                BuildAllAssetsAndPrefabs();
            }
        }

        [MenuItem("Hoa va Mau/Lana VN Asset Pack/1. Dong Bo & Tao Toan Bo 54 Prefabs (Chuẩn Tỷ Lệ ~1.5m)", false, 1)]
        public static void BuildAllAssetsAndPrefabs()
        {
            Debug.Log("<color=#38bdf8>[LanaVNAssetPack]</color> Bắt đầu đồng bộ hóa 54 Assets (Cây Cối & Dàn Bầu Bí Việt Nam)...");

            EnsureDirectory(PrefabsRoot);
            EnsureDirectory(MaterialsDir);

            Material spriteMat = GetOrCreateUnifiedSpriteMaterial();
            int importedCount = 0;
            int prefabCount = 0;

            foreach (var cfg in AllConfigs)
            {
                string texPath = $"{TexturesRoot}/{cfg.Subfolder}/{cfg.TexName}.png";
                if (!File.Exists(texPath)) continue;

                Sprite spr = ConfigureAndLoadSprite(texPath, cfg.PixelsPerUnit, cfg.Pivot);
                if (spr == null) continue;
                importedCount++;

                string prefabSubDir = $"{PrefabsRoot}/{cfg.Subfolder}";
                EnsureDirectory(prefabSubDir);

                string prefabName = cfg.TexName.Replace("T_LanaVN_", "PF_LanaVN_");
                string prefabPath = $"{prefabSubDir}/{prefabName}.prefab";

                GameObject go = new GameObject(prefabName);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                if (spriteMat != null) sr.sharedMaterial = spriteMat;
                sr.sortingOrder = cfg.SortingOrder;

                if (cfg.AddWindSway)
                {
                    AttachFoliageSway(go, cfg.SwayMaxAngle, cfg.SwaySpeed, 0f);
                }

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                UnityEngine.Object.DestroyImmediate(go);
                prefabCount++;
            }

            // Tạo Prefab Tổ Hợp 2.5D Dàn Bầu Bí (có chiều sâu Z + quả bầu đung đưa độc lập + hoa bầu + dây leo)
            BuildComposite25DGourdTrellisPrefab(spriteMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#4ade80>[LanaVNAssetPack]</color> <b>HOÀN TẤT ĐỒNG BỘ HÓA:</b> Đã chuẩn hóa {importedCount} Sprites và đóng gói {prefabCount + 1} Prefabs tại <color=#facc15>{PrefabsRoot}</color>!");
        }

        [MenuItem("Hoa va Mau/Lana VN Asset Pack/2. Đặt Bộ Sưu Tập 54 Assets Vào Scene Để Xem Trước (Showcase)", false, 2)]
        public static void SpawnAssetShowcaseInScene()
        {
            BuildAllAssetsAndPrefabs();

            const string ShowcaseName = "[LANA_VN_ASSET_PACK_SHOWCASE]";
            GameObject existing = GameObject.Find(ShowcaseName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            Vector3 basePos = Vector3.zero;
            GameObject player = GameObject.Find("Player_Minh") ?? GameObject.Find("Player_An");
            if (player != null)
            {
                basePos = player.transform.position + new Vector3(2.5f, 0f, 0f);
            }

            GameObject root = new GameObject(ShowcaseName);
            Undo.RegisterCreatedObjectUndo(root, "Spawn Lana VN Asset Pack Showcase");
            root.transform.position = basePos;

            // Đặt Dàn Bầu Bí 2.5D Composite ngay đầu tiên cạnh nhân vật để so sánh tỷ lệ 1.5m
            GameObject compPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsRoot}/GourdTrellis/PF_LanaVN_GourdTrellis_25D_Composite.prefab");
            if (compPrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(compPrefab, root.transform);
                inst.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            }

            float curX = 3.5f;
            string currentSub = "";
            float rowZ = 0f;

            foreach (var cfg in AllConfigs)
            {
                if (cfg.Subfolder != currentSub)
                {
                    currentSub = cfg.Subfolder;
                    curX = 3.5f;
                    rowZ += 2.2f;
                }

                string prefabName = cfg.TexName.Replace("T_LanaVN_", "PF_LanaVN_");
                string prefabPath = $"{PrefabsRoot}/{cfg.Subfolder}/{prefabName}.prefab";
                GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (pf == null) continue;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
                inst.transform.localPosition = new Vector3(curX, 0f, rowZ);

                var sr = inst.GetComponentInChildren<SpriteRenderer>();
                float width = (sr != null && sr.sprite != null) ? sr.sprite.bounds.size.x : 1.5f;
                curX += Mathf.Max(0.8f, width + 0.6f);
            }

            Selection.activeGameObject = root;
            Debug.Log($"<color=#4ade80>[LanaVNAssetPack]</color> Đã đặt bộ sưu tập 54 Assets + Dàn Bầu Bí 2.5D tại {basePos}!");
        }

        private static void BuildComposite25DGourdTrellisPrefab(Material spriteMat)
        {
            string subDir = $"{PrefabsRoot}/GourdTrellis";
            EnsureDirectory(subDir);

            Sprite sprTrellis = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_GourdTrellis_Main.png");
            Sprite sprGourdL  = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_Gourd_Bottle_Large.png");
            Sprite sprGourdM  = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_Gourd_Bottle_Medium.png");
            Sprite sprGourdS  = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_Gourd_Bottle_Small.png");
            Sprite sprPumpkin = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_Gourd_Pumpkin_Green.png");
            Sprite sprHalf    = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_Gourd_Bottle_Half.png");
            Sprite sprVine    = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_GourdVine_Branch.png");
            Sprite sprTendril1= AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_GourdTendril_01.png");
            Sprite sprFlower1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_GourdFlower_01_Bloom.png");
            Sprite sprFlower3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{TexturesRoot}/GourdTrellis/T_LanaVN_GourdFlower_03_BudTall.png");

            if (sprTrellis == null) return;

            GameObject root = new GameObject("PF_LanaVN_GourdTrellis_25D_Composite");

            // 1. Khung giàn chính (Main Frame)
            GameObject mainFrame = new GameObject("Trellis_MainFrame");
            mainFrame.transform.SetParent(root.transform, false);
            var srMain = mainFrame.AddComponent<SpriteRenderer>();
            srMain.sprite = sprTrellis;
            if (spriteMat != null) srMain.sharedMaterial = spriteMat;
            srMain.sortingOrder = 11;

            void AddPart(string name, Sprite spr, Vector3 localPos, int order, float swayAngle, float speed, float phase)
            {
                if (spr == null) return;
                GameObject part = new GameObject(name);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = localPos;
                var sr = part.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                if (spriteMat != null) sr.sharedMaterial = spriteMat;
                sr.sortingOrder = order;
                if (swayAngle > 0.01f)
                {
                    AttachFoliageSway(part, swayAngle, speed, phase);
                }
            }

            // 2. Các quả bầu treo lủng lẳng ở các lớp chiều sâu Z khác nhau (-0.08m đến +0.08m) để tạo Parallax 2.5D
            AddPart("Hanging_Gourd_Large_FG",  sprGourdL,   new Vector3(-0.32f, 1.82f, -0.06f), 13, 2.4f, 1.35f, 0.3f);
            AddPart("Hanging_Gourd_Med_BG",    sprGourdM,   new Vector3( 0.25f, 1.86f,  0.06f), 10, 2.8f, 1.55f, 1.4f);
            AddPart("Hanging_Gourd_Small_FG",  sprGourdS,   new Vector3(-0.78f, 1.78f, -0.04f), 13, 3.0f, 1.70f, 2.1f);
            AddPart("Ground_Pumpkin_Base",     sprPumpkin,  new Vector3( 0.58f, 0.02f, -0.08f), 14, 0f,   0f,    0f);
            AddPart("Ground_GourdHalf_Base",   sprHalf,     new Vector3( 0.82f, 0.01f, -0.10f), 15, 0f,   0f,    0f);

            // 3. Nhánh dây leo & Hoa bầu vàng điểm xuyết trên mái giàn
            AddPart("Canopy_VineBranch_Left",  sprVine,     new Vector3(-1.15f, 1.90f, -0.05f), 14, 2.2f, 1.60f, 0.8f);
            AddPart("Canopy_Tendril_Center",   sprTendril1, new Vector3(-0.05f, 1.72f, -0.07f), 14, 3.5f, 2.10f, 1.7f);
            AddPart("Canopy_Flower_Bloom_1",   sprFlower1,  new Vector3(-0.55f, 2.02f, -0.08f), 15, 2.5f, 1.80f, 0.5f);
            AddPart("Canopy_Flower_Bloom_2",   sprFlower1,  new Vector3( 0.48f, 2.12f, -0.08f), 15, 2.5f, 1.80f, 1.9f);
            AddPart("Canopy_Flower_Bud_Right", sprFlower3,  new Vector3( 0.92f, 1.95f, -0.06f), 15, 2.2f, 1.90f, 2.6f);

            PrefabUtility.SaveAsPrefabAsset(root, $"{subDir}/PF_LanaVN_GourdTrellis_25D_Composite.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Sprite ConfigureAndLoadSprite(string path, float ppu, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer != null)
            {
                bool needsReimport = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; needsReimport = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; needsReimport = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; needsReimport = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; needsReimport = true; }
                if (Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.1f) { importer.spritePixelsPerUnit = ppu; needsReimport = true; }

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.Custom || Vector2.Distance(settings.spritePivot, pivot) > 0.005f)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = pivot;
                    importer.SetTextureSettings(settings);
                    needsReimport = true;
                }

                if (needsReimport)
                {
                    importer.SaveAndReimport();
                }
            }

            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Sprite spr) return spr;
            }
            return null;
        }

        private static void AttachFoliageSway(GameObject go, float maxAngle, float speed, float phase)
        {
            var sway = go.GetComponent<PlanetOfLanaFoliageSway>();
            if (sway == null) sway = go.AddComponent<PlanetOfLanaFoliageSway>();

            var so = new SerializedObject(sway);
            so.FindProperty("maxRotationAngle").floatValue = maxAngle;
            so.FindProperty("swaySpeed").floatValue = speed;
            so.FindProperty("useWorldPositionOffset").boolValue = true;
            so.FindProperty("manualPhaseOffset").floatValue = phase;
            so.FindProperty("harmonicNoise").floatValue = 0.35f;
            so.FindProperty("scaleBreathing").floatValue = 0.010f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetOrCreateUnifiedSpriteMaterial()
        {
            string matPath = $"{MaterialsDir}/M_Lana_SpriteDefault.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) return mat;

            Material builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (builtin != null) return builtin;

            Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (sh != null)
            {
                mat = new Material(sh) { name = "M_Lana_SpriteDefault" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
#endif
