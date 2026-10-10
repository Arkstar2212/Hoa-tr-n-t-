using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class Inspect
{
    public static string Controller()
    {
        var sb = new StringBuilder();
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Controllers/AC_Player_An.controller");
        if (c == null) return "no controller";
        foreach (var l in c.layers)
            foreach (var s in l.stateMachine.states)
            {
                var m = s.state.motion;
                sb.AppendLine($"{s.state.name} -> {(m == null ? "NULL MOTION" : m.name + " @ " + AssetDatabase.GetAssetPath(m))}");
            }
        return sb.ToString();
    }

    public static string Fbx(string path)
    {
        var sb = new StringBuilder();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is AnimationClip ac && !ac.name.StartsWith("__preview"))
                sb.AppendLine($"clip {ac.name} len={ac.length:F2} loop={ac.isLooping} humanMotion={ac.humanMotion} frameRate={ac.frameRate}");
            else if (o is Avatar av)
                sb.AppendLine($"avatar {av.name} valid={av.isValid} human={av.isHuman}");
        }
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp != null) sb.AppendLine($"animType={imp.animationType} avatarSetup={imp.avatarSetup}");
        return sb.ToString();
    }

    public static string Prefab(string path)
    {
        var sb = new StringBuilder();
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) return "no prefab";
        Walk(go.transform, 0, sb, 3);
        return sb.ToString();
    }

    static void Walk(Transform t, int d, StringBuilder sb, int maxD)
    {
        var comps = t.GetComponents<Component>();
        var names = new StringBuilder();
        foreach (var c in comps) if (c != null && !(c is Transform)) names.Append(c.GetType().Name + ",");
        sb.AppendLine($"{new string(' ', d * 2)}{t.name} [{names}] pos={t.localPosition} scale={t.localScale}");
        if (d >= maxD) return;
        foreach (Transform ch in t) Walk(ch, d + 1, sb, maxD);
    }

    public static string AllClips()
    {
        var sb = new StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.StartsWith("Packages")) continue;
            sb.AppendLine(p);
        }
        return sb.ToString();
    }
    public static string Run2()
    {
        var sb = new StringBuilder();
        string[] paths = {
            "Assets/Models/Characters/Character_An.fbx",
            "Assets/Models/Characters/Character_Rigged_Animations.fbx",
            "Assets/Animations/Hero/Loco/Hero_Loco_Walk_Fwd_IP.fbx",
            "Assets/Animations/Hero/Loco/Hero_Loco_Run_Fwd_IP.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Jump_Start.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Push_Loop_IP.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Throw_Release.fbx",
            "Assets/Animations/Hero/Y Bot.fbx",
        };
        foreach (var p in paths) { sb.AppendLine("== " + p); sb.Append(Fbx(p)); }
        sb.AppendLine("== PF_Player_An");
        sb.Append(Prefab("Assets/Prefabs/Characters/PF_Player_An.prefab"));
        return sb.ToString();
    }
    public static string HeroImport()
    {
        var sb = new StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Animations/Hero" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            sb.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(p),-48} {imp.animationType} {imp.avatarSetup} srcAvatar={(imp.sourceAvatar != null ? imp.sourceAvatar.name : "-")}");
        }
        return sb.ToString();
    }

    public static string Hier()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var sc = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            sb.AppendLine("## " + sc.path + " dirty=" + sc.isDirty);
            foreach (var r in sc.GetRootGameObjects()) Walk(r.transform, 0, sb, 2);
        }
        return sb.ToString();
    }
    public static string Scenes()
    {
        var sb = new StringBuilder();
        var tree = UnityEngine.SceneManagement.SceneManager.GetSceneAt(0);
        if (tree.isDirty)
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(tree, System.IO.Path.GetFullPath("Temp/Scene_Tree_Test_unsaved_backup.unity"), true);
            sb.AppendLine("backed up dirty tree scene to Temp/");
        }
        foreach (var path in new[] { "Assets/Scenes/Playground_Mechanics.unity", "Assets/Scenes/Scene_Platform_Test.unity" })
        {
            var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            sb.AppendLine("## " + path);
            foreach (var r in sc.GetRootGameObjects()) Walk(r.transform, 0, sb, 1);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(sc, true);
        }
        return sb.ToString();
    }
    public static string Prefabs()
    {
        var sb = new StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var b = new Bounds(); bool has = false; int tris = 0;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
                var sm = r as SkinnedMeshRenderer;
                if (sm != null && sm.sharedMesh != null) tris += sm.sharedMesh.triangles.Length / 3;
            }
            var comps = new System.Collections.Generic.HashSet<string>();
            foreach (var c in go.GetComponentsInChildren<Component>()) if (c != null && !(c is Transform)) comps.Add(c.GetType().Name);
            sb.AppendLine($"{p.Replace("Assets/Prefabs/", "")} size={b.size:F2} center={b.center:F2} tris={tris} [{string.Join(",", comps)}]");
        }
        return sb.ToString();
    }
    public static string Fbxs()
    {
        var sb = new StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models/Environment", "Assets/Models/Props" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var b = new Bounds(); bool has = false; int tris = 0; int subs = 0;
            var mats = new System.Collections.Generic.HashSet<string>();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
                foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m.name);
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) { tris += mf.sharedMesh.triangles.Length / 3; subs += mf.sharedMesh.subMeshCount; }
            }
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            sb.AppendLine($"{System.IO.Path.GetFileName(p)} size={b.size:F2} ctr={b.center:F2} tris={tris} scale={imp.globalScale} readable={imp.isReadable} mats=[{string.Join(",", mats)}]");
        }
        return sb.ToString();
    }
    public static string Env()
    {
        var sb = new StringBuilder();
        var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Playground_Mechanics.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(sc);
        foreach (var r in sc.GetRootGameObjects())
        {
            var cam = r.GetComponent<Camera>();
            if (cam != null) sb.AppendLine($"CAM {r.name} fov={cam.fieldOfView} near={cam.nearClipPlane} far={cam.farClipPlane} ortho={cam.orthographic} clear={cam.clearFlags} bg={cam.backgroundColor} pos={r.transform.position} rot={r.transform.eulerAngles}");
            var l = r.GetComponent<Light>();
            if (l != null) sb.AppendLine($"LIGHT {r.name} type={l.type} color={l.color} int={l.intensity} shadows={l.shadows} rot={r.transform.eulerAngles}");
            var v = r.GetComponent<UnityEngine.Rendering.Volume>();
            if (v != null) sb.AppendLine($"VOLUME {r.name} profile={(v.sharedProfile != null ? AssetDatabase.GetAssetPath(v.sharedProfile) : "none")} global={v.isGlobal}");
            var cc = r.GetComponent<HoaVaMau.Gameplay.Cinematic2DCamera>();
            if (cc != null) { var so = new SerializedObject(cc); var it = so.GetIterator(); it.NextVisible(true); while (it.NextVisible(false)) sb.AppendLine($"  cam.{it.propertyPath}={(it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString() : it.propertyType == SerializedPropertyType.Boolean ? it.boolValue.ToString() : it.propertyType == SerializedPropertyType.Vector2 ? it.vector2Value.ToString() : it.propertyType.ToString())}"); }
        }
        sb.AppendLine($"RS ambientMode={RenderSettings.ambientMode} ambient={RenderSettings.ambientLight} sky={RenderSettings.ambientSkyColor} fog={RenderSettings.fog} skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name : "none")} sun={(RenderSettings.sun != null ? RenderSettings.sun.name : "none")}");
        var pl = GameObject.Find("Player_An");
        if (pl != null)
        {
            var pc = pl.GetComponent<HoaVaMau.Gameplay.PlayerController>(); var so = new SerializedObject(pc); var it = so.GetIterator(); it.NextVisible(true);
            while (it.NextVisible(false)) sb.AppendLine($"  pc.{it.propertyPath}={(it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString() : it.propertyType == SerializedPropertyType.Integer ? it.intValue.ToString() : it.propertyType == SerializedPropertyType.Boolean ? it.boolValue.ToString() : it.propertyType.ToString())}");
            var cc = pl.GetComponent<CharacterController>(); sb.AppendLine($"CC h={cc.height} r={cc.radius} center={cc.center} step={cc.stepOffset} slope={cc.slopeLimit} layer={pl.layer}");
            var an = pl.GetComponent<Animator>(); sb.AppendLine($"Animator ctrl={an.runtimeAnimatorController?.name} avatar={an.avatar?.name} rootMotion={an.applyRootMotion}");
            var ledge = pl.GetComponent<HoaVaMau.Gameplay.LedgeGrabDetector>(); var so2 = new SerializedObject(ledge); var it2 = so2.GetIterator(); it2.NextVisible(true);
            while (it2.NextVisible(false)) sb.AppendLine($"  ledge.{it2.propertyPath}={(it2.propertyType == SerializedPropertyType.Float ? it2.floatValue.ToString() : it2.propertyType == SerializedPropertyType.Boolean ? it2.boolValue.ToString() : it2.propertyType.ToString())}");
            sb.AppendLine("playerScale=" + pl.transform.localScale + " visualChild=" + (pl.transform.Find("Mesh_0") != null));
        }
        foreach (var t in GameObject.FindObjectsByType<HoaVaMau.Gameplay.RopeController>()) { var so = new SerializedObject(t); var it = so.GetIterator(); it.NextVisible(true); sb.Append("ROPE " + t.name + ": "); while (it.NextVisible(false)) if (it.propertyType == SerializedPropertyType.Float || it.propertyType == SerializedPropertyType.Integer) sb.Append(it.propertyPath + "=" + (it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString() : it.intValue.ToString()) + " "); sb.AppendLine(); }
        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(sc, true);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.SceneManager.GetSceneAt(0));
        return sb.ToString();
    }
    public static string CheckHumanoid()
    {
        var sb = new StringBuilder();
        var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animations/Controllers/AC_Player_An.controller");
        foreach (var l in ctrl.layers)
        {
            foreach (var s in l.stateMachine.states)
            {
                var clip = s.state.motion as AnimationClip;
                if (clip != null)
                {
                    sb.AppendLine($"State {s.state.name}: clip={clip.name} isHumanMotion={clip.humanMotion} isLooping={clip.isLooping} length={clip.length:F2}");
                }
                else
                {
                    sb.AppendLine($"State {s.state.name}: motion is NULL or not clip");
                }
            }
        }
        return sb.ToString();
    }
    public static string TestMakeHumanoid()
    {
        var sb = new StringBuilder();
        string[] testFiles = {
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Jump_Start.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Fall_Loop.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Push_Loop_IP.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Throw_Release.fbx"
        };
        var anAvatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Models/Characters/Character_An.fbx");
        if (anAvatar == null)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Characters/Character_An.fbx"))
                if (a is Avatar av) { anAvatar = av; break; }
        }
        sb.AppendLine("anAvatar: " + (anAvatar != null ? anAvatar.name : "null"));

        foreach (var p in testFiles)
        {
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            sb.AppendLine($"Before: {p} -> animType={imp.animationType} avatarSetup={imp.avatarSetup}");
            
            // Try setting CopyFromOther with anAvatar or CreateFromThisModel
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = anAvatar;
            imp.SaveAndReimport();

            var newImp = AssetImporter.GetAtPath(p) as ModelImporter;
            sb.AppendLine($"After CopyFromOther: animType={newImp.animationType} avatarSetup={newImp.avatarSetup} sourceAvatar={(newImp.sourceAvatar != null ? newImp.sourceAvatar.name : "null")}");

            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (o is AnimationClip ac)
                {
                    sb.AppendLine($"  clip {ac.name} isHumanMotion={ac.humanMotion} length={ac.length:F2}");
                }
            }
        }
        return sb.ToString();
    }
    public static string CheckClips()
    {
        var sb = new StringBuilder();
        string[] testFiles = {
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Jump_Start.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Fall_Loop.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Push_Loop_IP.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Throw_Release.fbx"
        };
        foreach (var p in testFiles)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (o is AnimationClip ac)
                {
                    sb.AppendLine($"{p} -> clip {ac.name} isHumanMotion={ac.humanMotion} length={ac.length:F2}");
                }
            }
        }
        return sb.ToString();
    }
    public static string InspectAllObjects(string p)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Objects in " + p + ":");
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
        {
            sb.AppendLine($"  {o.GetType().Name} : {o.name}");
        }
        return sb.ToString();
    }
    public static string CheckObj()
    {
        return InspectAllObjects("Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx");
    }
    public static string TestCreateFromThisModel()
    {
        var sb = new StringBuilder();
        string p = "Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx";
        var imp = AssetImporter.GetAtPath(p) as ModelImporter;
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.sourceAvatar = null;
        imp.SaveAndReimport();

        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
        {
            if (o is AnimationClip ac)
                sb.AppendLine($"  clip {ac.name} isHumanMotion={ac.humanMotion} len={ac.length:F2}");
            else if (o is Avatar av)
                sb.AppendLine($"  avatar {av.name} valid={av.isValid} isHuman={av.isHuman}");
        }
        return sb.ToString();
    }
    public static string ConvertAllHeroAnims()
    {
        var sb = new StringBuilder();
        int converted = 0;
        foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Animations/Hero" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith("Y Bot.fbx")) continue;
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            if (imp.animationType != ModelImporterAnimationType.Human)
            {
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.sourceAvatar = null;
                // If the clip name contains Loop, ensure loopTime is true
                var clips = imp.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    foreach (var c in clips)
                    {
                        if (p.Contains("Loop")) c.loopTime = true;
                    }
                    imp.clipAnimations = clips;
                }
                imp.SaveAndReimport();
                converted++;
                sb.AppendLine($"Converted: {p}");
            }
        }
        sb.AppendLine($"Total converted to Humanoid: {converted}");
        return sb.ToString();
    }
    public static string ListAllHeroClips()
    {
        var sb = new StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Animations/Hero" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith("Y Bot.fbx")) continue;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (o is AnimationClip ac && !ac.name.StartsWith("__preview"))
                {
                    sb.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(p),-45} clip={ac.name,-38} human={ac.humanMotion} loop={ac.isLooping} len={ac.length:F2}");
                }
            }
        }
        return sb.ToString();
    }
    public static string InspectThrowAndPush()
    {
        var sb = new StringBuilder();
        string[] files = {
            "Assets/Animations/Hero/Interact/Hero_Interact_Throw_Release.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Push_Loop_IP.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Jump_Start.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Fall_Loop.fbx"
        };
        foreach (var p in files)
        {
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            sb.AppendLine($"{p} animType={imp.animationType} avatarSetup={imp.avatarSetup}");
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (o is AnimationClip ac && !ac.name.StartsWith("__preview"))
                    sb.AppendLine($"  clip {ac.name} human={ac.humanMotion} len={ac.length:F2}");
            }
        }
        return sb.ToString();
    }
    public static string FixCopyFromOther()
    {
        var sb = new StringBuilder();
        string[] files = {
            "Assets/Animations/Hero/Interact/Hero_Interact_Throw_Release.fbx",
            "Assets/Animations/Hero/Interact/Hero_Interact_Push_Loop_IP.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Jump_Start.fbx",
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Fall_Loop.fbx"
        };
        foreach (var p in files)
        {
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.sourceAvatar = null;
            var clips = imp.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                foreach (var c in clips) if (p.Contains("Loop")) c.loopTime = true;
                imp.clipAnimations = clips;
            }
            imp.SaveAndReimport();
            sb.AppendLine($"Fixed {p}");
        }
        return sb.ToString();
    }
    public static string SetupPlayerAnController()
    {
        var sb = new StringBuilder();
        var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animations/Controllers/AC_Player_An.controller");
        var sm = ctrl.layers[0].stateMachine;

        // Helper to find or add state
        UnityEditor.Animations.AnimatorState GetOrAddState(string name, Vector3 pos)
        {
            foreach (var cs in sm.states)
            {
                if (cs.state.name == name) return cs.state;
            }
            var s = sm.AddState(name, pos);
            return s;
        }

        AnimationClip LoadClip(string fbxPath, string clipName = null)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (o is AnimationClip ac && !ac.name.StartsWith("__preview"))
                {
                    if (clipName == null || ac.name == clipName) return ac;
                }
            }
            return null;
        }

        var idleClip = LoadClip("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Loop_IP.fbx");
        var walkClip = LoadClip("Assets/Animations/Hero/Loco/Hero_Loco_Walk_Fwd_IP.fbx");
        var runClip = LoadClip("Assets/Animations/Hero/Loco/Hero_Loco_Run_Fwd_IP.fbx");
        var crouchClip = LoadClip("Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Sneak_Left_IP.fbx");
        var crawlClip = LoadClip("Assets/Animations/Hero/Stealth/Hero_Stealth_Crawl_Fwd_IP.fbx");
        var climbClip = LoadClip("Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx");
        var jumpClip = LoadClip("Assets/Animations/Hero/Traverse/Hero_Traverse_Jump_Start.fbx");
        var fallClip = LoadClip("Assets/Animations/Hero/Traverse/Hero_Traverse_Fall_Loop.fbx");
        var landClip = LoadClip("Assets/Animations/Hero/Traverse/Hero_Traverse_Land_Soft.fbx");
        var pushClip = LoadClip("Assets/Animations/Hero/Interact/Hero_Interact_Push_Loop_IP.fbx");
        var throwClip = LoadClip("Assets/Animations/Hero/Interact/Hero_Interact_Throw_Release.fbx");
        var carryClip = LoadClip("Assets/Animations/Hero/Interact/Hero_Interact_Carry_Heavy_Walk_Fwd_IP.fbx");

        var sIdle = GetOrAddState("Idle", new Vector3(250, 50, 0)); sIdle.motion = idleClip;
        var sWalk = GetOrAddState("Walk", new Vector3(250, 150, 0)); sWalk.motion = walkClip;
        var sRun = GetOrAddState("Run", new Vector3(250, 250, 0)); sRun.motion = runClip;
        var sCrouch = GetOrAddState("Crouch", new Vector3(500, 50, 0)); sCrouch.motion = crouchClip;
        var sCrawl = GetOrAddState("Crawl", new Vector3(500, 150, 0)); sCrawl.motion = crawlClip;
        var sClimb = GetOrAddState("Climb", new Vector3(500, 250, 0)); sClimb.motion = climbClip;
        var sJump = GetOrAddState("Jump", new Vector3(250, 350, 0)); sJump.motion = jumpClip;
        var sFall = GetOrAddState("Fall", new Vector3(500, 350, 0)); sFall.motion = fallClip;
        var sLand = GetOrAddState("Land", new Vector3(250, 450, 0)); sLand.motion = landClip;
        var sPush = GetOrAddState("PushPull", new Vector3(500, 450, 0)); sPush.motion = pushClip;
        var sThrow = GetOrAddState("Throw", new Vector3(250, 550, 0)); sThrow.motion = throwClip;
        var sCarry = GetOrAddState("Carry", new Vector3(500, 550, 0)); sCarry.motion = carryClip;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        sb.AppendLine("Updated AC_Player_An with all states:");
        foreach (var cs in sm.states)
        {
            sb.AppendLine($"  {cs.state.name} -> {cs.state.motion?.name} (human={((cs.state.motion as AnimationClip)?.humanMotion)})");
        }
        return sb.ToString();
    }
    public static string CheckSkyTexture()
    {
        var sb = new StringBuilder();
        string path = "Assets/Textures/Sky/Sky_Cloud_Krita_5120x2160.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.maxTextureSize = 4096;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            sb.AppendLine("Configured Sky texture as Sprite, max 4096.");
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            sb.AppendLine($"Loaded Texture: {tex.width}x{tex.height}");
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            sb.AppendLine($"Loaded Sprite: rect={sprite.rect} pixelsPerUnit={sprite.pixelsPerUnit}");
        }
        return sb.ToString();
    }
    public static string CheckCratePrefab()
    {
        var sb = new StringBuilder();
        string path = "Assets/Prefabs/Props/PF_WoodenCrate.prefab";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go != null)
        {
            sb.AppendLine("PF_WoodenCrate hierarchy:");
            Walk(go.transform, 0, sb, 3);
        }
        else sb.AppendLine("PF_WoodenCrate is null");
        return sb.ToString();
    }
    public static string CheckCrateModel()
    {
        var sb = new StringBuilder();
        string path = "Assets/Models/Props/SM_WoodenCrate.fbx";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go != null)
        {
            sb.AppendLine("SM_WoodenCrate hierarchy:");
            Walk(go.transform, 0, sb, 3);
        }
        else sb.AppendLine("SM_WoodenCrate is null");
        return sb.ToString();
    }
    public static string CheckSlatCube()
    {
        var sb = new StringBuilder();
        string path = "Assets/Models/Props/SM_WoodenSlatCube.fbx";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go != null) Walk(go.transform, 0, sb, 3);
        return sb.ToString();
    }
    public static string BuildScene()
    {
        HoaVaMau.Editor.BuildJungleTrialScene.Build();
        return "Build finished!";
    }
    public static string InspectTrialScene()
    {
        var sb = new StringBuilder();
        var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene_Tutorial_JungleTrial.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        sb.AppendLine($"Scene: {sc.name} path={sc.path} rootCount={sc.rootCount}");
        foreach (var r in sc.GetRootGameObjects())
        {
            Walk(r.transform, 0, sb, 2);
        }
        return sb.ToString();
    }
}
