using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;

namespace HoaVaMau.EditorScripts
{
    public static class PrologueCutsceneBuilder
    {
        [MenuItem("HoaVaMau/Cutscene/Build Smooth Prologue Cutscene")]
        public static void BuildPrologueCutscene()
        {
            // 1. Tìm hoặc kiểm tra nhân vật Player trong Scene
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null)
            {
                var pCtrl = Object.FindFirstObjectByType<HoaVaMau.Gameplay.PlayerController>();
                if (pCtrl != null) playerGO = pCtrl.gameObject;
            }

            Vector3 basePos = Vector3.zero;
            if (playerGO != null)
            {
                basePos = playerGO.transform.position;
            }
            else
            {
                var spawn = GameObject.Find("PlayerSpawn");
                if (spawn != null) basePos = spawn.transform.position;
            }

            // 2. Tạo Cutscene Director GameObject
            GameObject directorGO = GameObject.Find("Prologue_Cutscene_Director");
            if (directorGO == null)
            {
                directorGO = new GameObject("Prologue_Cutscene_Director");
                Undo.RegisterCreatedObjectUndo(directorGO, "Create Prologue Cutscene Director");
            }

            var director = directorGO.GetComponent<HoaVaMau.Gameplay.PrologueCutsceneDirector>();
            if (director == null)
            {
                director = directorGO.AddComponent<HoaVaMau.Gameplay.PrologueCutsceneDirector>();
            }

            var playableDirector = directorGO.GetComponent<PlayableDirector>();
            if (playableDirector == null)
            {
                playableDirector = directorGO.AddComponent<PlayableDirector>();
            }

            // 3. Tạo Shot Camera Điện Ảnh
            Transform shotTransform = directorGO.transform.Find("Cinematic_Shot_Vantage");
            if (shotTransform == null)
            {
                GameObject shotGO = new GameObject("Cinematic_Shot_Vantage");
                shotGO.transform.SetParent(directorGO.transform);
                // Góc quay rộng, thấp hướng lên tôn dáng nhân vật và phong cảnh
                shotGO.transform.position = new Vector3(basePos.x - 2.5f, basePos.y + 1.8f, -9.5f);
                shotGO.transform.rotation = Quaternion.Euler(4.0f, 12.0f, 0.0f);
                shotTransform = shotGO.transform;
            }

            // 4. Tìm các Animation Clip chất lượng cao
            string locoDir = "Assets/Animations/Hero/Loco";
            string reactionDir = "Assets/Animations/Hero/Reaction";

            var clipWalkCasual = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(locoDir, "Hero_Loco_Walk_Casual_Fwd_IP.fbx"));
            var clipStopWalk = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(locoDir, "Hero_Loco_Stop_Walk_IP.fbx"));
            var clipIdleCasual = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(locoDir, "Hero_Loco_Idle_Casual_Loop_IP.fbx"));
            var clipObservation = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(reactionDir, "Hero_Reaction_Cutscene_Observation.fbx"));
            var clipIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(locoDir, "Hero_Loco_Idle_Loop_IP.fbx"));

            // Nếu là FBX container, trích xuất AnimationClip bên trong
            clipWalkCasual = ExtractAnimationClip(clipWalkCasual, Path.Combine(locoDir, "Hero_Loco_Walk_Casual_Fwd_IP.fbx"));
            clipStopWalk = ExtractAnimationClip(clipStopWalk, Path.Combine(locoDir, "Hero_Loco_Stop_Walk_IP.fbx"));
            clipIdleCasual = ExtractAnimationClip(clipIdleCasual, Path.Combine(locoDir, "Hero_Loco_Idle_Casual_Loop_IP.fbx"));
            clipObservation = ExtractAnimationClip(clipObservation, Path.Combine(reactionDir, "Hero_Reaction_Cutscene_Observation.fbx"));
            clipIdle = ExtractAnimationClip(clipIdle, Path.Combine(locoDir, "Hero_Loco_Idle_Loop_IP.fbx"));

            // 5. Tạo Timeline Asset
            string timelineDir = "Assets/Timelines";
            if (!Directory.Exists(timelineDir))
            {
                Directory.CreateDirectory(timelineDir);
                AssetDatabase.Refresh();
            }

            string timelinePath = Path.Combine(timelineDir, "TL_Prologue_Intro.playable");
            TimelineAsset timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (timelineAsset == null)
            {
                timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timelineAsset, timelinePath);
            }
            else
            {
                // Xóa các track cũ để tái cấu trúc sạch sẽ
                foreach (var track in timelineAsset.GetOutputTracks())
                {
                    timelineAsset.DeleteTrack(track);
                }
            }

            // Thêm Animation Track cho nhân vật An
            var animTrack = timelineAsset.CreateTrack<AnimationTrack>(null, "An_Actor_Track");

            double currentTime = 0.0;

            // Clip 1: Đi bộ thư thả tiến vào khung hình (3.2s)
            if (clipWalkCasual != null)
            {
                var timelineClip = animTrack.CreateClip(clipWalkCasual);
                timelineClip.start = currentTime;
                timelineClip.duration = 3.2;
                timelineClip.easeInDuration = 0.3;
                timelineClip.easeOutDuration = 0.3;
                currentTime += 2.9; // Gối đầu mượt mà 0.3s
            }

            // Clip 2: Giảm tốc dừng chân (0.8s)
            if (clipStopWalk != null)
            {
                var timelineClip = animTrack.CreateClip(clipStopWalk);
                timelineClip.start = currentTime;
                timelineClip.duration = 0.8;
                timelineClip.easeInDuration = 0.3;
                timelineClip.easeOutDuration = 0.25;
                currentTime += 0.55;
            }

            // Clip 3: Quan sát phong cảnh tự nhiên với nhịp thở (3.5s)
            var lookClip = clipObservation != null ? clipObservation : clipIdleCasual;
            if (lookClip != null)
            {
                var timelineClip = animTrack.CreateClip(lookClip);
                timelineClip.start = currentTime;
                timelineClip.duration = 3.5;
                timelineClip.easeInDuration = 0.35;
                timelineClip.easeOutDuration = 0.35;
                currentTime += 3.15;
            }

            // Clip 4: Sẵn sàng vào game (1.5s)
            if (clipIdle != null)
            {
                var timelineClip = animTrack.CreateClip(clipIdle);
                timelineClip.start = currentTime;
                timelineClip.duration = 1.5;
                timelineClip.easeInDuration = 0.35;
            }

            EditorUtility.SetDirty(timelineAsset);
            AssetDatabase.SaveAssets();

            // 6. Gán Timeline Asset vào PlayableDirector
            playableDirector.playableAsset = timelineAsset;

            // Gán Animator của An vào track
            if (playerGO != null)
            {
                var animator = playerGO.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    playableDirector.SetGenericBinding(animTrack, animator);
                }
            }

            // Cấu hình tham số cho PrologueCutsceneDirector qua SerializedObject
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("startPosition").vector3Value = new Vector3(basePos.x - 7.0f, basePos.y, basePos.z);
            so.FindProperty("vantagePosition").vector3Value = basePos;
            so.FindProperty("cinematicShotTransform").objectReferenceValue = shotTransform;
            so.FindProperty("playableDirector").objectReferenceValue = playableDirector;
            if (playerGO != null)
            {
                so.FindProperty("playerObject").objectReferenceValue = playerGO;
            }
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(directorGO);
            Debug.Log($"<color=#38EF7D><b>[Prologue Cutscene]</b> Đã xây dựng thành công Cutscene điện ảnh mượt mà tại '{timelinePath}'!</color>");
            Selection.activeGameObject = directorGO;
        }

        private static AnimationClip ExtractAnimationClip(AnimationClip mainAsset, string assetPath)
        {
            if (mainAsset != null) return mainAsset;
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
