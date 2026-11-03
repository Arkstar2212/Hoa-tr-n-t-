#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Additive-only Tree Shadow Setup cho scene New Scene 1_Overhaul.unity.
    /// Tuân thủ tuyệt đối nguyên tắc Bảo Toàn Scene Overhaul (AGENTS.md 4.3):
    /// - Không chỉnh sửa bất kỳ GameObject gốc nào trong scene.
    /// - Toàn bộ thành phần mới nằm gọn trong [Additive_TreeShadows_Overhaul]:
    ///   1) 01_URP_Realtime_CanopyCasters: Quads ShadowsOnly dùng silhouette lá cây thật đung đưa theo gió,
    ///      đổ bóng động URP lên nhân vật Minh, NPC, thùng gỗ, đá rêu và nhà sàn làng Rạch Gốc.
    ///   2) 02_Conformed_Dappled_GroundShadows: Lưới bóng màu nước chữ L (L-draped) ôm khít 100% mặt đường đi
    ///      VÀ rủ mềm mại qua bờ cỏ xanh phía trước (như Planet of Lana), hiển thị đúng tán lá từng loài cây.
    /// </summary>
    public static class OverhaulTreeShadowSetup
    {
        public const string TargetScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        public const string AdditiveRootName = "[Additive_TreeShadows_Overhaul]";

        private const string BaseAssetDir = "Assets/Materials/LanaOverhaul";
        private const string CasterMatDir = BaseAssetDir + "/TreeShadowCasters";
        private const string GroundMatDir = BaseAssetDir + "/TreeGroundShadowMats";
        private const string GroundMeshDir = BaseAssetDir + "/TreeGroundShadowMeshes";

        private struct TerrainColliderEntry
        {
            public MeshCollider collider;
            public float waveDisplacement;
        }

        private struct TreeCandidate
        {
            public SpriteRenderer renderer;
            public Sprite sprite;
            public Vector3 worldPos;
            public Bounds bounds;
            public bool isSapling;
            public bool flipX;
        }

        [MenuItem("Tools/Hoa Trên Đất Đỏ/Gắn Shadow Cây Scene Overhaul (Additive)")]
        public static void SetupOverhaulTreeShadowsMenu()
        {
            ExecuteSetup(saveScene: true);
        }

        public static int ExecuteSetup(bool saveScene = true)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != TargetScenePath)
            {
                scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }

            EnsureDirectory(BaseAssetDir);
            EnsureDirectory(CasterMatDir);
            EnsureDirectory(GroundMatDir);
            EnsureDirectory(GroundMeshDir);

            // Xóa nhóm additive cũ (nếu chạy lại nhiều lần) để đảm bảo idempotent 100%, không sinh rác
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (rootObj != null && rootObj.name == AdditiveRootName)
                {
                    Object.DestroyImmediate(rootObj);
                }
            }

            // 1. Lấy hướng nắng thực tế từ Directional Light trong scene (không chỉnh sửa Light gốc)
            Vector3 lightDir = new Vector3(-0.171f, -0.940f, 0.296f).normalized;
            GameObject dirLightObj = GameObject.Find("08_Lighting/Directional Light");
            if (dirLightObj != null)
            {
                lightDir = dirLightObj.transform.forward.normalized;
            }

            // 2. Thu thập danh sách cây đang hiển thị trong scene
            List<TreeCandidate> candidates = CollectTreeCandidates();

            // 3. Khởi tạo bộ lấy mẫu bề mặt 3D chính xác từng milimet trên 03_Terrain/Lana_Sculpted_Ledges
            GameObject tempColliderHolder = new GameObject("__TempTerrainRaycaster_DO_NOT_SAVE__")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            List<TerrainColliderEntry> terrainEntries = BuildTempTerrainColliders(tempColliderHolder.transform);
            Physics.SyncTransforms();

            int createdShadowCount = 0;

            try
            {
                GameObject additiveRoot = new GameObject(AdditiveRootName);
                SceneManager.MoveGameObjectToScene(additiveRoot, scene);

                GameObject castersGroup = new GameObject("01_URP_Realtime_CanopyCasters");
                castersGroup.transform.SetParent(additiveRoot.transform, false);

                GameObject groundShadowsGroup = new GameObject("02_Conformed_Dappled_GroundShadows");
                groundShadowsGroup.transform.SetParent(additiveRoot.transform, false);

                Shader casterShader = Shader.Find("HoaVaMau/LanaTreeShadowCaster") ?? Shader.Find("Universal Render Pipeline/Lit");
                Shader groundShadowShader = Shader.Find("HoaVaMau/LanaTreeCanopyGroundShadow") ?? Shader.Find("HoaVaMau/LanaDappledGroundShadow");

                var casterMatCache = new Dictionary<string, Material>();
                var groundMatCache = new Dictionary<string, Material>();
                Mesh sharedQuadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

                int heroIdx = 0;
                int saplingIdx = 0;

                for (int i = 0; i < candidates.Count; i++)
                {
                    TreeCandidate tc = candidates[i];
                    if (tc.sprite == null || tc.sprite.texture == null) continue;

                    Vector4 uvRect = GetSpriteUVRect(tc.sprite);
                    bool isWarmVillageOrRiceZone = tc.worldPos.x >= 62.0f;

                    if (!tc.isSapling)
                    {
                        float centerX = tc.bounds.center.x;
                        // Lệch nhẹ theo hướng nắng X để bóng râm đổ tự nhiên về phía nắng chiếu
                        float shadowCenterX = centerX + (lightDir.x / Mathf.Max(0.35f, -lightDir.y)) * 1.65f;

                        bool hasGroundCenter = TrySampleWalkwayTopY(terrainEntries, shadowCenterX, 0.45f, out float centerGroundY, out _);
                        if (!hasGroundCenter)
                        {
                            hasGroundCenter = TrySampleWalkwayTopY(terrainEntries, centerX, 0.45f, out centerGroundY, out _);
                        }

                        // A) URP Realtime 3D Shadow Caster (ShadowsOnly) đổ bóng động lên Minh, NPC, đá rêu, thùng gỗ, nhà sàn
                        if (hasGroundCenter && sharedQuadMesh != null && casterShader != null)
                        {
                            Material casterMat = GetOrCreateCasterMaterial(casterMatCache, casterShader, tc.sprite, uvRect, tc.flipX);

                            bool inVillage = tc.worldPos.x >= 140.0f;
                            float targetZ = inVillage ? 1.05f : 0.35f;
                            float rayDist = 4.35f;
                            Vector3 targetGroundPt = new Vector3(shadowCenterX, centerGroundY, targetZ);
                            Vector3 casterPos = targetGroundPt - lightDir * rayDist;

                            float quadW = Mathf.Clamp(tc.bounds.size.x * 0.78f, 4.8f, 11.5f);
                            float quadH = inVillage
                                ? Mathf.Clamp(tc.bounds.size.y * 0.52f, 4.5f, 7.2f)
                                : Mathf.Clamp(tc.bounds.size.y * 0.40f, 3.4f, 5.4f);

                            GameObject casterGO = new GameObject($"TreeCaster3D_{heroIdx:D2}_{SanitizeName(tc.renderer.name)}");
                            casterGO.transform.SetParent(castersGroup.transform, false);
                            casterGO.transform.position = casterPos;
                            casterGO.transform.rotation = Quaternion.LookRotation(lightDir, Vector3.forward);
                            casterGO.transform.localScale = new Vector3(quadW, quadH, 1f);

                            var mf = casterGO.AddComponent<MeshFilter>();
                            mf.sharedMesh = sharedQuadMesh;

                            var mr = casterGO.AddComponent<MeshRenderer>();
                            mr.sharedMaterial = casterMat;
                            mr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                            mr.receiveShadows = false;
                            mr.lightProbeUsage = LightProbeUsage.Off;
                            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        }

                        // B) Lưới bóng màu nước chữ L (L-Draped Conformed Shadow):
                        // Trải mượt từ mép sau đường đi (Z=1.08m) qua lối Minh bước tới mép trước (Z=0.06m)
                        // VÀ rủ tự nhiên xuống sườn cỏ xanh phía trước (~0.92m) để nhìn rõ bóng vòm cây từ góc máy 2.5D!
                        if (groundShadowShader != null)
                        {
                            float spanX = Mathf.Clamp(tc.bounds.size.x * 0.76f, 5.2f, 11.4f);
                            float absZ = Mathf.Abs(tc.worldPos.z);
                            float depthAtten = Mathf.Lerp(1.0f, 0.58f, Mathf.InverseLerp(1.4f, 8.5f, absZ));
                            float frontDrapeDepth = Mathf.Clamp(tc.bounds.size.y * 0.072f, 0.68f, 0.96f);

                            Mesh conformedMesh = BuildDrapedCanopyShadowMesh(
                                terrainEntries,
                                $"OverhaulTreeShadow_{heroIdx:D2}_{SanitizeName(tc.renderer.name)}",
                                shadowCenterX - spanX * 0.5f,
                                shadowCenterX + spanX * 0.5f,
                                segX: 30,
                                backZ: 1.08f,
                                frontLipZ: 0.065f,
                                segTopZ: 6,
                                drapeHeight: frontDrapeDepth,
                                segDrapeY: 8,
                                flipU: tc.flipX,
                                vStart: 0.22f,
                                vRim: 0.56f,
                                vEnd: 0.96f,
                                alphaMultiplier: depthAtten,
                                out int validTriCount);

                            if (validTriCount > 0)
                            {
                                string meshAssetPath = $"{GroundMeshDir}/OverhaulTreeShadow_{heroIdx:D2}.asset";
                                Mesh savedMesh = SaveOrReplaceMeshAsset(conformedMesh, meshAssetPath);

                                Material groundMat = GetOrCreateGroundShadowMaterial(
                                    groundMatCache, groundShadowShader, tc.sprite, uvRect, isWarmVillageOrRiceZone);

                                GameObject groundGO = new GameObject($"GroundShadow_{heroIdx:D2}_{SanitizeName(tc.renderer.name)}");
                                groundGO.transform.SetParent(groundShadowsGroup.transform, false);
                                groundGO.AddComponent<MeshFilter>().sharedMesh = savedMesh;

                                var gmr = groundGO.AddComponent<MeshRenderer>();
                                gmr.sharedMaterial = groundMat;
                                gmr.shadowCastingMode = ShadowCastingMode.Off;
                                gmr.receiveShadows = false;
                                gmr.lightProbeUsage = LightProbeUsage.Off;
                                gmr.reflectionProbeUsage = ReflectionProbeUsage.Off;

                                createdShadowCount++;
                            }
                            else
                            {
                                Object.DestroyImmediate(conformedMesh);
                            }
                        }

                        heroIdx++;
                    }
                    else
                    {
                        // C) Bóng tiếp đất dịu nhẹ dưới gốc các cây non (Saplings) nằm dọc bờ cỏ lối đi
                        if (groundShadowShader != null)
                        {
                            float saplingWidth = Mathf.Clamp(tc.bounds.size.x * 1.25f, 1.05f, 2.35f);
                            float saplingCenterX = tc.bounds.center.x - 0.10f;

                            Mesh saplingMesh = BuildDrapedCanopyShadowMesh(
                                terrainEntries,
                                $"OverhaulSaplingShadow_{saplingIdx:D2}",
                                saplingCenterX - saplingWidth * 0.5f,
                                saplingCenterX + saplingWidth * 0.5f,
                                segX: 10,
                                backZ: 0.52f,
                                frontLipZ: 0.065f,
                                segTopZ: 3,
                                drapeHeight: 0.28f,
                                segDrapeY: 4,
                                flipU: tc.flipX,
                                vStart: 0.20f,
                                vRim: 0.55f,
                                vEnd: 0.94f,
                                alphaMultiplier: 0.62f,
                                out int validTriCount);

                            if (validTriCount > 0)
                            {
                                string meshAssetPath = $"{GroundMeshDir}/OverhaulSaplingShadow_{saplingIdx:D2}.asset";
                                Mesh savedSapMesh = SaveOrReplaceMeshAsset(saplingMesh, meshAssetPath);

                                Material groundMat = GetOrCreateGroundShadowMaterial(
                                    groundMatCache, groundShadowShader, tc.sprite, uvRect, isWarmVillageOrRiceZone);

                                GameObject sapGO = new GameObject($"SaplingShadow_{saplingIdx:D2}_{SanitizeName(tc.renderer.transform.parent != null ? tc.renderer.transform.parent.name : tc.renderer.name)}");
                                sapGO.transform.SetParent(groundShadowsGroup.transform, false);
                                sapGO.AddComponent<MeshFilter>().sharedMesh = savedSapMesh;

                                var smr = sapGO.AddComponent<MeshRenderer>();
                                smr.sharedMaterial = groundMat;
                                smr.shadowCastingMode = ShadowCastingMode.Off;
                                smr.receiveShadows = false;
                                smr.lightProbeUsage = LightProbeUsage.Off;
                                smr.reflectionProbeUsage = ReflectionProbeUsage.Off;

                                createdShadowCount++;
                                saplingIdx++;
                            }
                            else
                            {
                                Object.DestroyImmediate(saplingMesh);
                            }
                        }
                    }
                }

                EditorUtility.SetDirty(additiveRoot);
            }
            finally
            {
                if (tempColliderHolder != null)
                {
                    Object.DestroyImmediate(tempColliderHolder);
                }
            }

            AssetDatabase.SaveAssets();
            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"<color=#4ade80>[OverhaulTreeShadowSetup]</color> Đã gắn thành công {createdShadowCount} cụm bóng đổ cây (URP 3D Realtime + L-Draped Watercolor Canopy Shadow) vào nhóm <b>{AdditiveRootName}</b>!");
            return createdShadowCount;
        }

        private static List<TreeCandidate> CollectTreeCandidates()
        {
            var results = new List<TreeCandidate>();
            var allSprites = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);

            foreach (SpriteRenderer sr in allSprites)
            {
                if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy || sr.sprite == null)
                    continue;

                string fullPath = GetTransformPath(sr.transform);
                string lowerPath = fullPath.ToLowerInvariant();
                if (lowerPath.Contains(AdditiveRootName.ToLowerInvariant()) ||
                    lowerPath.Contains("09_vfx") ||
                    lowerPath.Contains("07_background") ||
                    lowerPath.Contains("00_ui") ||
                    lowerPath.Contains("00_cinematic") ||
                    lowerPath.Contains("vignette") ||
                    lowerPath.Contains("foreground_framing") ||
                    lowerPath.Contains("seagull"))
                {
                    continue;
                }

                string sprName = sr.sprite.name.ToLowerInvariant();
                string objName = sr.name.ToLowerInvariant();
                Bounds b = sr.bounds;
                Vector3 pos = sr.transform.position;
                bool effectiveFlipX = sr.flipX ^ (sr.transform.lossyScale.x < 0f);

                // 1. Saplings / Cây non nằm sát lối đi
                if (lowerPath.Contains("03_saplings_and_youngtrees"))
                {
                    if (b.size.y >= 0.58f && pos.z >= -1.0f && pos.z <= 1.2f)
                    {
                        results.Add(new TreeCandidate
                        {
                            renderer = sr,
                            sprite = sr.sprite,
                            worldPos = pos,
                            bounds = b,
                            isSapling = true,
                            flipX = effectiveFlipX
                        });
                    }
                    continue;
                }

                // Bỏ qua cầu thân cây đổ ngang
                if (sprName.Contains("curved_tree_v2_trunk") || lowerPath.Contains("fallen_bridge_trunk"))
                    continue;

                // 2. Cây lớn (Hero Trees: đa, cổ thụ, tre, chuối, dừa, cây làng Rạch Gốc...)
                bool isHeroTree =
                    sprName.Contains("tree") || sprName.Contains("bamboo") || sprName.Contains("banana") ||
                    sprName.Contains("coconut") || sprName.Contains("palm") ||
                    objName.Contains("tree") || objName.Contains("bamboo") || objName.Contains("banana") ||
                    objName.Contains("coconut") || objName.Contains("palm") ||
                    lowerPath.Contains("handdrawn_herotrees");

                if (isHeroTree && b.size.y >= 3.5f && pos.z >= -2.5f && pos.z <= 8.8f)
                {
                    results.Add(new TreeCandidate
                    {
                        renderer = sr,
                        sprite = sr.sprite,
                        worldPos = pos,
                        bounds = b,
                        isSapling = false,
                        flipX = effectiveFlipX
                    });
                }
            }

            results.Sort((a, b) => a.worldPos.x.CompareTo(b.worldPos.x));
            return results;
        }

        private static List<TerrainColliderEntry> BuildTempTerrainColliders(Transform holder)
        {
            var list = new List<TerrainColliderEntry>();
            GameObject ledgesRoot = GameObject.Find("03_Terrain/Lana_Sculpted_Ledges");
            if (ledgesRoot == null) return list;

            foreach (MeshFilter mf in ledgesRoot.GetComponentsInChildren<MeshFilter>(false))
            {
                if (mf == null || mf.sharedMesh == null || !mf.gameObject.activeInHierarchy)
                    continue;

                Vector3 ls = mf.transform.lossyScale;
                if (Mathf.Abs(ls.x) < 0.001f || Mathf.Abs(ls.y) < 0.001f || Mathf.Abs(ls.z) < 0.001f)
                    continue;

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;

                float waveDisp = 0f;
                if (mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_WaveDisplacement"))
                {
                    waveDisp = mr.sharedMaterial.GetFloat("_WaveDisplacement");
                }

                GameObject colGO = new GameObject($"TempCol_{mf.name}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                colGO.transform.SetParent(holder, false);
                colGO.transform.position = mf.transform.position;
                colGO.transform.rotation = mf.transform.rotation;
                colGO.transform.localScale = ls;

                var mc = colGO.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;

                list.Add(new TerrainColliderEntry
                {
                    collider = mc,
                    waveDisplacement = waveDisp
                });
            }

            return list;
        }

        private static bool TrySampleWalkwayTopY(
            List<TerrainColliderEntry> entries,
            float worldX,
            float worldZ,
            out float exactY,
            out Vector3 hitNormal)
        {
            exactY = 0f;
            hitNormal = Vector3.up;

            Ray ray = new Ray(new Vector3(worldX, 25.0f, worldZ), Vector3.down);
            bool found = false;
            float highestY = float.MinValue;
            float bestWave = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                MeshCollider mc = entries[i].collider;
                if (mc == null) continue;

                if (mc.Raycast(ray, out RaycastHit hit, 45.0f))
                {
                    if (hit.point.y > highestY)
                    {
                        highestY = hit.point.y;
                        hitNormal = hit.normal;
                        bestWave = entries[i].waveDisplacement;
                        found = true;
                    }
                }
            }

            if (!found || hitNormal.y < 0.42f)
                return false;

            float isTopFace = Mathf.Clamp01(hitNormal.y * 1.5f);
            float wave = Mathf.Sin(worldX * 0.85f) * 0.035f + Mathf.Cos(worldX * 2.3f + worldZ * 1.5f) * 0.020f;
            exactY = highestY + wave * isTopFace * bestWave * 20.0f;
            return true;
        }

        private static bool TrySampleFrontEmbankmentZ(
            List<TerrainColliderEntry> entries,
            float worldX,
            float worldY,
            out float frontZ)
        {
            frontZ = 0.055f;
            Ray ray = new Ray(new Vector3(worldX, worldY, -2.5f), Vector3.forward);
            bool found = false;
            float minZ = float.MaxValue;

            for (int i = 0; i < entries.Count; i++)
            {
                MeshCollider mc = entries[i].collider;
                if (mc == null) continue;

                if (mc.Raycast(ray, out RaycastHit hit, 4.5f))
                {
                    if (hit.point.z < minZ)
                    {
                        minZ = hit.point.z;
                        found = true;
                    }
                }
            }

            if (!found) return false;
            frontZ = minZ;
            return true;
        }

        /// <summary>
        /// Tạo một lưới bóng liền mạch chữ L:
        /// - Các hàng [0..segTopZ]: đi từ mép sau đường (backZ = 1.08m) ra mép cỏ phía trước (frontLipZ = 0.065m).
        /// - Các hàng [segTopZ+1..segTopZ+segDrapeY]: rủ mềm mại xuống mặt sườn cỏ xanh phía trước (drapeHeight ~ 0.92m),
        ///   ôm khít từng milimet theo mặt trước Ledge và tan dần alpha về 0 trước khi chạm tầng đất tối bên dưới.
        /// </summary>
        private static Mesh BuildDrapedCanopyShadowMesh(
            List<TerrainColliderEntry> entries,
            string meshName,
            float startX,
            float endX,
            int segX,
            float backZ,
            float frontLipZ,
            int segTopZ,
            float drapeHeight,
            int segDrapeY,
            bool flipU,
            float vStart,
            float vRim,
            float vEnd,
            float alphaMultiplier,
            out int validTriCount)
        {
            int totalRows = segTopZ + segDrapeY;
            int cols = segX + 1;
            int rows = totalRows + 1;
            int totalVerts = cols * rows;

            var verts = new Vector3[totalVerts];
            var uvs = new Vector2[totalVerts];
            var colors = new Color[totalVerts];
            var valid = new bool[totalVerts];

            // Pre-sample rim height tại mỗi cột X
            var rimValid = new bool[cols];
            var rimY = new float[cols];
            var rimFrontZ = new float[cols];
            float lastGoodY = -1.85f;

            for (int cx = 0; cx <= segX; cx++)
            {
                float tx = (float)cx / segX;
                float worldX = Mathf.Lerp(startX, endX, tx);

                if (TrySampleWalkwayTopY(entries, worldX, frontLipZ + 0.04f, out float topY, out _))
                {
                    rimValid[cx] = true;
                    rimY[cx] = topY;
                    lastGoodY = topY;

                    if (TrySampleFrontEmbankmentZ(entries, worldX, topY - 0.08f, out float fz))
                        rimFrontZ[cx] = fz;
                    else
                        rimFrontZ[cx] = frontLipZ;
                }
                else
                {
                    rimValid[cx] = false;
                    rimY[cx] = lastGoodY;
                    rimFrontZ[cx] = frontLipZ;
                }
            }

            // Phát hiện cột sát mép vực đá đứng (|dRimY| > 0.35m)
            var columnNearCliff = new bool[cols];
            for (int cx = 0; cx <= segX; cx++)
            {
                if (!rimValid[cx]) { columnNearCliff[cx] = true; continue; }
                if (cx > 0 && (!rimValid[cx - 1] || Mathf.Abs(rimY[cx] - rimY[cx - 1]) > 0.35f)) columnNearCliff[cx] = true;
                if (cx < segX && (!rimValid[cx + 1] || Mathf.Abs(rimY[cx] - rimY[cx + 1]) > 0.35f)) columnNearCliff[cx] = true;
            }

            for (int r = 0; r <= totalRows; r++)
            {
                bool isTopSection = (r <= segTopZ);
                float tTop = isTopSection ? ((float)r / segTopZ) : 1.0f;
                float tDrape = isTopSection ? 0.0f : ((float)(r - segTopZ) / segDrapeY);

                float sampleV = isTopSection
                    ? Mathf.Lerp(vStart, vRim, tTop)
                    : Mathf.Lerp(vRim, vEnd, tDrape);

                // Alpha mượt từ mép sau, đậm rõ trên mặt đường + gờ cỏ trên, và tan dần xuống sườn cỏ dưới
                float rowAlpha;
                if (isTopSection)
                {
                    float backFade = Mathf.SmoothStep(0.15f, 1.0f, Mathf.Clamp01(tTop / 0.25f));
                    rowAlpha = backFade;
                }
                else
                {
                    rowAlpha = 1.0f - Mathf.SmoothStep(0.18f, 1.0f, tDrape);
                }

                for (int cx = 0; cx <= segX; cx++)
                {
                    int idx = r * cols + cx;
                    float tx = (float)cx / segX;
                    float worldX = Mathf.Lerp(startX, endX, tx);
                    float sampleU = flipU ? (1.0f - tx) : tx;
                    uvs[idx] = new Vector2(sampleU, sampleV);

                    if (!rimValid[cx])
                    {
                        verts[idx] = new Vector3(worldX, rimY[cx], frontLipZ);
                        valid[idx] = false;
                        colors[idx] = new Color(1f, 1f, 1f, 0f);
                        continue;
                    }

                    float cliffAtten = columnNearCliff[cx] ? 0.12f : 1.0f;

                    if (isTopSection)
                    {
                        float targetZ = Mathf.Lerp(backZ, Mathf.Max(frontLipZ, rimFrontZ[cx] + 0.01f), tTop);
                        if (TrySampleWalkwayTopY(entries, worldX, targetZ, out float topSurfY, out _))
                        {
                            verts[idx] = new Vector3(worldX, topSurfY + 0.016f, targetZ);
                            valid[idx] = true;
                            colors[idx] = new Color(1f, 1f, 1f, alphaMultiplier * rowAlpha * cliffAtten);
                        }
                        else
                        {
                            verts[idx] = new Vector3(worldX, rimY[cx] + 0.016f, targetZ);
                            valid[idx] = true;
                            colors[idx] = new Color(1f, 1f, 1f, alphaMultiplier * rowAlpha * cliffAtten * 0.5f);
                        }
                    }
                    else
                    {
                        float dropY = tDrape * drapeHeight;
                        float worldY = rimY[cx] - dropY;
                        if (TrySampleFrontEmbankmentZ(entries, worldX, worldY, out float fz))
                        {
                            verts[idx] = new Vector3(worldX, worldY, fz - 0.014f);
                            valid[idx] = true;
                            colors[idx] = new Color(1f, 1f, 1f, alphaMultiplier * rowAlpha * cliffAtten);
                        }
                        else
                        {
                            verts[idx] = new Vector3(worldX, worldY, rimFrontZ[cx] - 0.014f);
                            valid[idx] = false;
                            colors[idx] = new Color(1f, 1f, 1f, 0f);
                        }
                    }
                }
            }

            var tris = new List<int>(segX * totalRows * 6);
            for (int r = 0; r < totalRows; r++)
            {
                for (int cx = 0; cx < segX; cx++)
                {
                    int i0 = r * cols + cx;
                    int i1 = i0 + 1;
                    int i2 = i0 + cols;
                    int i3 = i2 + 1;

                    if (!valid[i0] || !valid[i1] || !valid[i2] || !valid[i3])
                        continue;

                    // Không nối tam giác vắt ngang qua bậc vách đá cao
                    if (Mathf.Abs(rimY[cx] - rimY[cx + 1]) > 0.35f)
                        continue;

                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

            validTriCount = tris.Count / 3;

            var mesh = new Mesh { name = meshName };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector4 GetSpriteUVRect(Sprite spr)
        {
            Texture2D tex = spr.texture;
            if (tex == null || tex.width <= 0 || tex.height <= 0)
                return new Vector4(0f, 0f, 1f, 1f);

            Rect r = spr.textureRect;
            return new Vector4(
                r.x / tex.width,
                r.y / tex.height,
                r.width / tex.width,
                r.height / tex.height);
        }

        private static Material GetOrCreateCasterMaterial(
            Dictionary<string, Material> cache,
            Shader shader,
            Sprite spr,
            Vector4 uvRect,
            bool flipX)
        {
            string key = $"{spr.texture.name}_{(flipX ? "Flip" : "Norm")}";
            if (cache.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            string matPath = $"{CasterMatDir}/M_Caster_{SanitizeName(key)}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = $"M_Caster_{SanitizeName(key)}" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", spr.texture);
            if (mat.HasProperty("_SpriteUVRect")) mat.SetVector("_SpriteUVRect", uvRect);
            if (mat.HasProperty("_VRange")) mat.SetVector("_VRange", new Vector4(0.20f, 1.0f, 0f, 0f));
            if (mat.HasProperty("_FlipX")) mat.SetFloat("_FlipX", flipX ? 1.0f : 0.0f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.28f);
            if (mat.HasProperty("_SwaySpeed")) mat.SetFloat("_SwaySpeed", 1.20f);
            if (mat.HasProperty("_SwayAmp")) mat.SetFloat("_SwayAmp", 0.11f);
            mat.enableInstancing = true;

            EditorUtility.SetDirty(mat);
            cache[key] = mat;
            return mat;
        }

        private static Material GetOrCreateGroundShadowMaterial(
            Dictionary<string, Material> cache,
            Shader shader,
            Sprite spr,
            Vector4 uvRect,
            bool warmZone)
        {
            string zoneTag = warmZone ? "WarmVillage" : "CoolForest";
            string key = $"{spr.texture.name}_{zoneTag}";
            if (cache.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            string matPath = $"{GroundMatDir}/M_GroundShadow_{SanitizeName(key)}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = $"M_GroundShadow_{SanitizeName(key)}" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            Color shadowTint = warmZone
                ? new Color(0.08f, 0.15f, 0.09f, 0.58f) // Bóng râm xanh ô-liu ấm vùng đồng lúa & làng Rạch Gốc
                : new Color(0.05f, 0.16f, 0.14f, 0.62f); // Bóng râm xanh ngọc màu nước vùng rừng cổ thụ

            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", spr.texture);
            if (mat.HasProperty("_SpriteUVRect")) mat.SetVector("_SpriteUVRect", uvRect);
            if (mat.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", shadowTint);
            if (mat.HasProperty("_SwaySpeed")) mat.SetFloat("_SwaySpeed", 1.15f);
            if (mat.HasProperty("_SwayAmp")) mat.SetFloat("_SwayAmp", 0.020f);
            if (mat.HasProperty("_BlurRadius")) mat.SetFloat("_BlurRadius", 0.018f);
            if (mat.HasProperty("_DappleScale")) mat.SetFloat("_DappleScale", 4.5f);

            EditorUtility.SetDirty(mat);
            cache[key] = mat;
            return mat;
        }

        private static Mesh SaveOrReplaceMeshAsset(Mesh newMesh, string assetPath)
        {
            string assetFileName = Path.GetFileNameWithoutExtension(assetPath);
            newMesh.name = assetFileName;

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(newMesh, existing);
                existing.name = assetFileName;
                Object.DestroyImmediate(newMesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(newMesh, assetPath);
            return newMesh;
        }

        private static void EnsureDirectory(string assetDir)
        {
            if (!Directory.Exists(assetDir))
            {
                Directory.CreateDirectory(assetDir);
            }
        }

        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Tree";
            char[] arr = raw.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                char c = arr[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    arr[i] = '_';
            }
            return new string(arr);
        }

        private static string GetTransformPath(Transform t)
        {
            if (t.parent == null) return t.name;
            return GetTransformPath(t.parent) + "/" + t.name;
        }
    }
}
#endif
