#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Batch Editor utility that fixes NPC and ground-level sinking in New Scene 1_Overhaul.unity:
    /// 1. Repairs the broken 'Wave' state motion in AC_Lana_LivingVillager.controller.
    /// 2. Aligns the 3D terrain ledges (Ledge_01..Ledge_05) and PhysicsWalkway colliders so the visual
    ///    ground surface and physics surface match across the entire level (forest, cliff, slope, rice field, village).
    /// 3. Bakes each rigged 3D NPC's actual posed SkinnedMeshRenderer vertices and places all 9 NPCs cleanly
    ///    on top of the village walkway surface in front of background rice clutter so their feet are never buried.
    /// 4. Grounds village boulders, baskets, barrels, rice sacks, bushes, and foreground rice lips cleanly on the surface.
    /// 5. Restores PF_Player_Minh's SkinnedMeshRenderer 3D depth (localScale = 1,1,1) and cleans up stray Edit-Mode TrajectoryPredictor objects.
    /// </summary>
    public static class FixScene1OverhaulGroundSinking
    {
        private const string OverhaulScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string VillagerControllerPath = "Assets/Animations/AC_Lana_LivingVillager.controller";
        private const string WaveClipFbxPath = "Assets/Animations/Hero/Reaction/Hero_Social_VN_Farewell_Wave_Loop_IP.fbx";

        [MenuItem("Tools/Hoa Tren Dat Do/Fix Scene 1 Overhaul NPC And Ground Sinking")]
        public static void ExecuteFix()
        {
            if (SceneManager.GetActiveScene().path != OverhaulScenePath)
            {
                EditorSceneManager.OpenScene(OverhaulScenePath, OpenSceneMode.Single);
            }

            // 1. Repair AC_Lana_LivingVillager.controller 'Wave' state motion
            FixVillagerAnimatorController();

            // 2. Align Ledge_05 3D visual top surface with the -1.85m / SampleTerrainSurfaceY(x) walkway surface
            //    and align Col_01..Col_09 physics colliders with the visual terrain surface
            AlignTerrainMeshesAndPhysicsColliders();

            // 3. Fix PF_Player_Minh 3D mesh scale and grounding
            FixPlayerCharacter();

            // 4. Fix all 9 rigged 3D Village NPCs under 02_Gameplay/Living_Rigged_3D_NPCs
            FixAllVillageRiggedNPCs();

            // 5. Fix village props, boulders, bushes, and rice field foreground lip sprites so nothing is sunk or burying NPC feet
            FixVillagePropsAndFoliageGrounding();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Debug.Log("<color=#10b981><b>[FixScene1OverhaulGroundSinking]</b></color> Successfully fixed NPC & terrain ground sinking in New Scene 1_Overhaul.unity!");
        }

        private static void AdjustLedge05UVsFast(Mesh mesh)
        {
            if (mesh == null) return;
            Vector3[] verts = mesh.vertices;
            if (verts == null || verts.Length == 0) return;

            var topYByX = new Dictionary<int, float>();
            for (int i = 0; i < verts.Length; i++)
            {
                int key = Mathf.RoundToInt(verts[i].x * 4f);
                if (!topYByX.TryGetValue(key, out float cur) || verts[i].y > cur)
                    topYByX[key] = verts[i].y;
            }

            Vector2[] newUVs = new Vector2[verts.Length];
            const float cliffBottomY = RoadDualSurfaceTool.CLIFF_BOTTOM_Y;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 pos = verts[i];
                int key = Mathf.RoundToInt(pos.x * 4f);
                float topY = topYByX.TryGetValue(key, out float ty) ? ty : -1.85f;
                float u = pos.x * 0.25f;
                float v = Mathf.Clamp01((pos.y - cliffBottomY) / Mathf.Max(0.1f, topY - cliffBottomY));
                newUVs[i] = new Vector2(u, v);
            }

            mesh.uv = newUVs;
            EditorUtility.SetDirty(mesh);
        }

        public static float SampleTerrainSurfaceY(float x)
        {
            if (x <= -4.5f)
            {
                float t = Mathf.InverseLerp(-12.0f, -4.5f, x);
                return Mathf.Lerp(-0.85f, -1.95f, Mathf.SmoothStep(0f, 1f, t));
            }
            if (x <= 5.2f) return -1.95f;
            if (x <= 14.6f) return -0.55f;
            if (x < 17.6f) return -3.80f;
            if (x <= 20.6f) return -0.55f;
            if (x <= 22.0f)
            {
                float t = Mathf.InverseLerp(20.6f, 22.0f, x);
                return Mathf.Lerp(-0.55f, -1.10f, t);
            }
            if (x <= 35.5f) return -1.10f;
            if (x <= 43.5f) return 2.15f;
            if (x <= 62.0f)
            {
                float t = Mathf.InverseLerp(43.5f, 62.0f, x);
                float smooth = t * t * (3f - 2f * t);
                return Mathf.Lerp(2.15f, -1.85f, smooth);
            }
            return -1.85f;
        }

        private static void FixVillagerAnimatorController()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(VillagerControllerPath);
            if (ctrl == null) return;

            AnimationClip LoadFbxClip(string fbxPath)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        var settings = AnimationUtility.GetAnimationClipSettings(clip);
                        if (!settings.loopTime)
                        {
                            settings.loopTime = true;
                            AnimationUtility.SetAnimationClipSettings(clip, settings);
                        }
                        return clip;
                    }
                }
                return null;
            }

            AnimationClip waveClip = LoadFbxClip(WaveClipFbxPath);
            AnimationClip idleClip = LoadFbxClip("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Casual_Loop_IP.fbx");
            AnimationClip idle2Clip = LoadFbxClip("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Variation_02_Loop_IP.fbx") ?? idleClip;
            AnimationClip talkClip = LoadFbxClip("Assets/Animations/Hero/Reaction/Hero_Social_Talk_Gesture_01.fbx") ?? waveClip;
            AnimationClip sitClip = LoadFbxClip("Assets/Animations/Hero/Stealth/Hero_Post_Sit_Idle_Loop.fbx") ?? idleClip;
            AnimationClip walkClip = LoadFbxClip("Assets/Animations/Hero/Loco/Hero_Loco_Walk_Casual_Fwd_IP.fbx") ?? idleClip;

            bool dirty = false;
            var states = ctrl.layers[0].stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                var st = states[i].state;
                if (st.motion == null)
                {
                    if (st.name == "Wave" && waveClip != null) { st.motion = waveClip; dirty = true; }
                    else if (st.name == "Idle" && idleClip != null) { st.motion = idleClip; dirty = true; }
                    else if (st.name == "Idle2" && idle2Clip != null) { st.motion = idle2Clip; dirty = true; }
                    else if (st.name == "Talk" && talkClip != null) { st.motion = talkClip; dirty = true; }
                    else if (st.name == "Sit" && sitClip != null) { st.motion = sitClip; dirty = true; }
                    else if (st.name == "Walk" && walkClip != null) { st.motion = walkClip; dirty = true; }
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(ctrl);
            }
        }

        private static void AlignTerrainMeshesAndPhysicsColliders()
        {
            // 1. Ensure Ledge_05_HighOverlook_Hill_Rice_And_Village_RockUnderbelly top surface is flush with SampleTerrainSurfaceY(x)
            //    (Previously BuildChiseledRockMesh recessed its top surface to surfY - 0.18m = -2.03m while its GrassTurfCap was deleted,
            //     leaving a -0.18m sunken trough from X = 35.5m to 188.0m!)
            var ledge05RockGO = GameObject.Find("Ledge_05_HighOverlook_Hill_Rice_And_Village_RockUnderbelly");
            if (ledge05RockGO != null)
            {
                var mf = ledge05RockGO.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Mesh mesh = mf.sharedMesh;
                    Vector3[] verts = mesh.vertices;
                    bool modified = false;

                    // First check the current top Y at X = 80m
                    float maxAt80 = -999f;
                    for (int i = 0; i < verts.Length; i++)
                    {
                        if (Mathf.Abs(verts[i].x - 80f) < 0.35f && verts[i].y > maxAt80)
                            maxAt80 = verts[i].y;
                    }

                    // If top surface at X=80 is recessed around -2.03m (below -1.88m), raise the top plateau/slope vertices to match SampleTerrainSurfaceY(x)
                    if (maxAt80 < -1.88f)
                    {
                        // Group topY by X column
                        var colMaxY = new Dictionary<int, float>();
                        for (int i = 0; i < verts.Length; i++)
                        {
                            int key = Mathf.RoundToInt(verts[i].x * 10f);
                            if (!colMaxY.TryGetValue(key, out float curMax) || verts[i].y > curMax)
                                colMaxY[key] = verts[i].y;
                        }

                        for (int i = 0; i < verts.Length; i++)
                        {
                            int key = Mathf.RoundToInt(verts[i].x * 10f);
                            float curTop = colMaxY[key];
                            float targetTop = SampleTerrainSurfaceY(verts[i].x);
                            float diffToTop = curTop - verts[i].y;

                            // Top cap & upper lip vertices (within 0.45m of the column top) smoothly shift up to targetTop
                            if (diffToTop <= 0.08f)
                            {
                                verts[i].y = targetTop - diffToTop;
                                modified = true;
                            }
                            else if (diffToTop <= 1.20f)
                            {
                                float blend = 1f - Mathf.InverseLerp(0.08f, 1.20f, diffToTop);
                                verts[i].y += (targetTop - curTop) * blend;
                                modified = true;
                            }
                        }
                    }

                    if (modified)
                    {
                        mesh.vertices = verts;
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();
                        AdjustLedge05UVsFast(mesh);
                        EditorUtility.SetDirty(mesh);
                    }
                }
            }

            // 2. Align PhysicsWalkway_And_Cliffs_145m colliders so the character's feet rest on top of the visual grass caps
            //    Note: CharacterController has skinWidth = 0.08f (or 0.02f), and Ledge_01/02/04 have GrassTurfCap top at surfY + 0.04m.
            var col01 = GameObject.Find("Col_01_ForestStart")?.GetComponent<BoxCollider>();
            if (col01 != null)
            {
                // Ledge_01 GrassTurfCap top is around -1.78m..-1.85m; keep Col_01 top at -1.82f so Player at X=0 stands flush on top of the grass cap
                float topY = -1.82f;
                float botY = -8.00f;
                col01.transform.position = new Vector3(-2.65f, (topY + botY) * 0.5f, 0f);
                col01.size = new Vector3(15.7f, topY - botY, 4.0f);
                EditorUtility.SetDirty(col01);
            }

            var col09 = GameObject.Find("Col_09_FlatRiceAndVillagePlatform")?.GetComponent<BoxCollider>();
            if (col09 != null)
            {
                // Flat rice and village platform top at -1.85f
                float topY = -1.85f;
                float botY = -8.00f;
                col09.transform.position = new Vector3((61.9f + 188.0f) * 0.5f, (topY + botY) * 0.5f, 0f);
                col09.size = new Vector3(188.0f - 61.9f, topY - botY, 4.0f);
                EditorUtility.SetDirty(col09);
            }
        }

        private static void FixPlayerCharacter()
        {
            var player = GameObject.Find("PF_Player_Minh");
            if (player == null) return;

            // 1. Remove any stray Edit-Mode TrajectoryPredictor child
            var strayTraj = player.transform.Find("TrajectoryPredictor");
            if (strayTraj != null)
            {
                Object.DestroyImmediate(strayTraj.gameObject);
            }

            // 2. Fix flattened localScale.z = 0 override on tripo_node_518b8c94
            foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (Mathf.Abs(smr.transform.localScale.z) < 0.01f)
                {
                    smr.transform.localScale = Vector3.one;
                    EditorUtility.SetDirty(smr.transform);
                }
            }

            // 3. Ensure CharacterController skinWidth is tight (0.02f) and place PF_Player_Minh cleanly on Col_01 at X=0
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.skinWidth = 0.02f;
                EditorUtility.SetDirty(cc);
            }

            player.transform.position = new Vector3(0.0f, -1.81f, 0.0f);
            EditorUtility.SetDirty(player.transform);
        }

        private static void FixAllVillageRiggedNPCs()
        {
            var npcsRoot = GameObject.Find("Living_Rigged_3D_NPCs");
            if (npcsRoot == null) return;

            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(VillagerControllerPath);
            FieldInfo fDefaultState = typeof(VillageNPCInteractable).GetField("defaultAnimState", BindingFlags.NonPublic | BindingFlags.Instance);

            // Walkable road surface in the village (X = 148..182) is Y = -1.85m (with up to +0.035m wave displacement -> -1.815m).
            // Place NPCs at Z = 0.34m..0.44m (cleanly on the 3D village road surface in front of the Z = 0.85m+ background rice & props)
            // and align each NPC's actual posed lowest foot vertex to Y = -1.810m so shader waves never clip their sandals.
            const float targetGroundFootY = -1.810f;

            var targetYByIndex = new Dictionary<int, float>();
            AnimationMode.StartAnimationMode();
            try
            {
                for (int i = 0; i < npcsRoot.transform.childCount; i++)
                {
                    GameObject npc = npcsRoot.transform.GetChild(i).gameObject;
                    var anim = npc.GetComponentInChildren<Animator>();
                    var vi = npc.GetComponent<VillageNPCInteractable>();
                    var smr = npc.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr == null) continue;

                    string stateName = "Idle";
                    if (vi != null && fDefaultState != null)
                    {
                        stateName = (string)fDefaultState.GetValue(vi);
                    }

                    if (anim != null)
                    {
                        if (ctrl != null && anim.runtimeAnimatorController == null)
                            anim.runtimeAnimatorController = ctrl;
                        anim.applyRootMotion = false;
                        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        EditorUtility.SetDirty(anim);
                    }

                    // Sample the NPC's default animation clip so we measure the true posed foot height
                    AnimationClip clipToSample = null;
                    if (ctrl != null)
                    {
                        foreach (var st in ctrl.layers[0].stateMachine.states)
                        {
                            if (st.state.name == stateName && st.state.motion is AnimationClip c)
                            {
                                clipToSample = c;
                                break;
                            }
                        }
                    }

                    if (clipToSample != null && anim != null)
                    {
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(anim.gameObject, clipToSample, 0.25f);
                        AnimationMode.EndSampling();
                    }

                    Mesh baked = new Mesh();
                    smr.BakeMesh(baked);
                    Vector3[] verts = baked.vertices;
                    float minWorldFootY = float.MaxValue;
                    Transform smrTr = smr.transform;
                    for (int v = 0; v < verts.Length; v++)
                    {
                        float wy = smrTr.TransformPoint(verts[v]).y;
                        if (wy < minWorldFootY) minWorldFootY = wy;
                    }
                    Object.DestroyImmediate(baked);

                    if (minWorldFootY < float.MaxValue)
                    {
                        float footToRootOffset = npc.transform.position.y - minWorldFootY;
                        targetYByIndex[i] = targetGroundFootY + footToRootOffset;
                    }
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            // Apply the computed positions AFTER StopAnimationMode so AnimationMode does not revert transform changes!
            for (int i = 0; i < npcsRoot.transform.childCount; i++)
            {
                GameObject npc = npcsRoot.transform.GetChild(i).gameObject;
                var smr = npc.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr == null) continue;

                Vector3 pos = npc.transform.position;
                if (targetYByIndex.TryGetValue(i, out float newY))
                {
                    pos.y = newY;
                }
                pos.z = Mathf.Clamp(pos.z, 0.34f, 0.44f);
                npc.transform.position = pos;

                // Also verify bind pose so neither bind pose nor animated pose sinks below targetGroundFootY
                Mesh baked = new Mesh();
                smr.BakeMesh(baked);
                Vector3[] verts = baked.vertices;
                float minWorldFootY = float.MaxValue;
                Transform smrTr = smr.transform;
                for (int v = 0; v < verts.Length; v++)
                {
                    float wy = smrTr.TransformPoint(verts[v]).y;
                    if (wy < minWorldFootY) minWorldFootY = wy;
                }
                Object.DestroyImmediate(baked);

                if (minWorldFootY < targetGroundFootY)
                {
                    float lift = targetGroundFootY - minWorldFootY;
                    npc.transform.position += new Vector3(0f, lift, 0f);
                }
                EditorUtility.SetDirty(npc.transform);
            }
        }

        private static void FixVillagePropsAndFoliageGrounding()
        {
            // 1. In the Village zone (X = 146m .. 185m), Rice_FrontLip_*_VillageExt sprites were placed at Z = -0.33..+0.23
            //    with top Y = -1.36m (50cm above the village road!), burying the lower legs of all 9 village NPCs!
            //    Move VillageExt rice strips behind the NPC walkway (Z >= 0.92m) and ground their bottoms cleanly at -1.90m.
            var riceGroup = GameObject.Find("Golden_RiceField_2D");
            if (riceGroup != null)
            {
                foreach (Transform child in riceGroup.transform)
                {
                    var sr = child.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;

                    Vector3 p = child.position;
                    if (child.name.Contains("Rice_FrontLip"))
                    {
                        if (p.x >= 146.0f)
                        {
                            // In the village courtyard (X >= 146m), push FrontLip rice behind the walking path (Z = 0.92m)
                            // and lower slightly so the village road and NPC feet are 100% unobstructed.
                            p.z = 0.92f;
                            p.y = -2.12f;
                            child.position = p;
                            sr.sortingOrder = 5;
                            EditorUtility.SetDirty(child);
                            EditorUtility.SetDirty(sr);
                        }
                        else
                        {
                            // Along the main rice field (X = 61.5 .. 146m), keep Rice_FrontLip as a low border lip
                            // that hugs the front edge of the road (top around -1.72m instead of -1.36m) so it doesn't bury the player to the waist
                            float curMaxY = sr.bounds.max.y;
                            float targetMaxY = SampleTerrainSurfaceY(p.x) + 0.12f;
                            if (curMaxY > targetMaxY)
                            {
                                p.y -= (curMaxY - targetMaxY);
                                child.position = p;
                                EditorUtility.SetDirty(child);
                            }
                        }
                    }
                    else if (child.name.Contains("Rice_MidPaddy") && p.x >= 146.0f)
                    {
                        // Push village MidPaddy slightly back to Z = 1.05m so NPCs at Z = 0.35..0.46m stand cleanly in front
                        if (p.z < 1.02f)
                        {
                            p.z = 1.05f;
                            child.position = p;
                            EditorUtility.SetDirty(child);
                        }
                    }
                }
            }

            // 2. Fix Zone4_LivingVillage_RachGoc props (boulders, baskets, barrels, rice sacks, bushes) that were sunk below -1.85m
            var villageRoot = GameObject.Find("Zone4_LivingVillage_RachGoc");
            if (villageRoot != null)
            {
                foreach (Transform child in villageRoot.transform)
                {
                    var rends = child.GetComponentsInChildren<Renderer>(true);
                    if (rends == null || rends.Length == 0) continue;

                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

                    float groundY = SampleTerrainSurfaceY(child.position.x);

                    if (child.name.StartsWith("Village_Boulder"))
                    {
                        // Boulders were sunk 0.50m underground (bounds.min.y = -2.35m); embed base just 0.08m into ground
                        float targetMinY = groundY - 0.08f;
                        float deltaY = targetMinY - b.min.y;
                        if (Mathf.Abs(deltaY) > 0.01f)
                        {
                            child.position += new Vector3(0f, deltaY, 0f);
                            EditorUtility.SetDirty(child);
                        }
                    }
                    else if (child.name.StartsWith("Village_RiceSacks") ||
                             child.name.StartsWith("Village_BambooBasket") ||
                             child.name.StartsWith("Village_SaltBarrel"))
                    {
                        // Ground props flush on the -1.85m surface (bounds.min.y = groundY - 0.02f)
                        float targetMinY = groundY - 0.02f;
                        float deltaY = targetMinY - b.min.y;
                        if (Mathf.Abs(deltaY) > 0.005f)
                        {
                            child.position += new Vector3(0f, deltaY, 0f);
                            EditorUtility.SetDirty(child);
                        }
                    }
                    else if (child.name.StartsWith("Village_Bush"))
                    {
                        // Bushes were sunk at bounds.min.y = -2.28m; ground them at groundY - 0.04f
                        float targetMinY = groundY - 0.04f;
                        float deltaY = targetMinY - b.min.y;
                        if (Mathf.Abs(deltaY) > 0.01f)
                        {
                            child.position += new Vector3(0f, deltaY, 0f);
                            EditorUtility.SetDirty(child);
                        }
                    }
                }
            }

            // 3. Check [Official_Lana_Foliage_Meadow_Additive] along the slope (X = 43.5..62m) and flat plain (X = 62..145m)
            //    so every grass tuft/flower rests cleanly on SampleTerrainSurfaceY(x)
            var meadowRoot = GameObject.Find("[Official_Lana_Foliage_Meadow_Additive]");
            if (meadowRoot != null)
            {
                foreach (Transform zone in meadowRoot.transform)
                {
                    foreach (Transform item in zone)
                    {
                        var sr = item.GetComponentInChildren<SpriteRenderer>();
                        if (sr == null) continue;
                        float gx = item.position.x;
                        float surfY = SampleTerrainSurfaceY(gx);
                        float curMinY = sr.bounds.min.y;
                        float targetMinY = surfY - 0.03f;
                        float diff = targetMinY - curMinY;
                        if (Mathf.Abs(diff) > 0.02f && Mathf.Abs(diff) < 1.5f)
                        {
                            item.position += new Vector3(0f, diff, 0f);
                            EditorUtility.SetDirty(item);
                        }
                    }
                }
            }
        }
    }
}
#endif
