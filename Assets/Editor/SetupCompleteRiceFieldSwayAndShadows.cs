using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;

public static class SetupCompleteRiceFieldSwayAndShadows
{
    private const string ShadowSpritePath = "Assets/Textures/Environment/T_Foliage_ContactShadow_Soft.png";

    [MenuItem("Tools/HoaDo/Setup Complete Rice Field Sway and Shadows")]
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        Sprite shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
        if (shadowSprite == null)
        {
            Debug.LogError($"[SetupSway] Shadow sprite not found at {ShadowSpritePath}!");
            return;
        }

        int backdropSwayCount = 0;
        int backdropShadowCount = 0;
        int foregroundSwayCount = 0;
        int foregroundShadowCount = 0;

        // 1. Process Active Scene Objects
        var activeScene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[SetupSway] Processing Active Scene: {activeScene.name}");

        // 1A. Backdrop Complete Vista
        var backdropRoot = GameObject.Find("[Additive_Backdrop_RiceField_Complete_Vista]");
        if (backdropRoot != null)
        {
            ProcessHierarchy(backdropRoot.transform, shadowSprite, isForeground: false, ref backdropSwayCount, ref backdropShadowCount);
            EditorUtility.SetDirty(backdropRoot);
            Debug.Log($"[SetupSway] Backdrop Scene Processed: {backdropSwayCount} sway components, {backdropShadowCount} contact shadows.");
        }
        else
        {
            Debug.LogWarning("[SetupSway] [Additive_Backdrop_RiceField_Complete_Vista] not found in scene!");
        }

        // 1B. Foreground Overhaul
        var foregroundRoot = GameObject.Find("[Additive_Foreground_RiceField_Overhaul]");
        if (foregroundRoot != null)
        {
            ProcessHierarchy(foregroundRoot.transform, shadowSprite, isForeground: true, ref foregroundSwayCount, ref foregroundShadowCount);
            EditorUtility.SetDirty(foregroundRoot);
            Debug.Log($"[SetupSway] Foreground Scene Processed: {foregroundSwayCount} sway components, {foregroundShadowCount} contact shadows.");
        }
        else
        {
            Debug.LogWarning("[SetupSway] [Additive_Foreground_RiceField_Overhaul] not found in scene!");
        }

        // Mark and Save Active Scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        // 2. Process all RiceField prefabs on disk
        int prefabsProcessed = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/RiceField" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            try
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root != null)
                {
                    bool isForeground = path.ToLower().Contains("foreground");
                    int sCount = 0, shCount = 0;
                    ProcessHierarchy(root.transform, shadowSprite, isForeground, ref sCount, ref shCount);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    prefabsProcessed++;
                    Debug.Log($"[SetupSway] Updated Prefab: {Path.GetFileName(path)} (Sway: {sCount}, Shadow: {shCount})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SetupSway] Failed updating prefab at {path}: {ex.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. Render Visual Verifications (720p close-up and 1920x600 panorama)
        RenderVerificationScreenshots();

        Debug.Log($"<color=#10b981>[SetupSway] SUCCESS! Backdrop Sway: {backdropSwayCount}, Foreground Sway: {foregroundSwayCount}, Prefabs: {prefabsProcessed}</color>");
    }

    private static void ProcessHierarchy(Transform parent, Sprite shadowSprite, bool isForeground, ref int swayCount, ref int shadowCount)
    {
        var renderers = parent.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            if (sr == null || sr.gameObject == null) continue;
            string objName = sr.gameObject.name;

            // Skip existing contact shadows or debug markers
            if (objName.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (sr.sprite == null) continue;

            // A. Configure PlanetOfLanaFoliageSway
            var sway = sr.gameObject.GetComponent<PlanetOfLanaFoliageSway>();
            if (sway == null)
            {
                sway = sr.gameObject.AddComponent<PlanetOfLanaFoliageSway>();
            }

            var so = new SerializedObject(sway);
            so.Update();

            var propMaxAngle = so.FindProperty("maxRotationAngle");
            var propSwaySpeed = so.FindProperty("swaySpeed");
            var propWorldOffset = so.FindProperty("useWorldPositionOffset");
            var propHarmonic = so.FindProperty("harmonicNoise");
            var propAnchor = so.FindProperty("anchorBottomPivot");
            var propInteract = so.FindProperty("enableCharacterInteraction");
            var propRadius = so.FindProperty("interactionRadius");
            var propPushAngle = so.FindProperty("maxPushAngle");

            if (propMaxAngle != null) propMaxAngle.floatValue = isForeground ? 3.5f : 4.0f;
            if (propSwaySpeed != null) propSwaySpeed.floatValue = 1.6f;
            if (propWorldOffset != null) propWorldOffset.boolValue = true;
            if (propHarmonic != null) propHarmonic.floatValue = 0.35f;
            if (propAnchor != null) propAnchor.boolValue = true;
            if (propInteract != null) propInteract.boolValue = isForeground;
            if (propRadius != null) propRadius.floatValue = 0.85f;
            if (propPushAngle != null) propPushAngle.floatValue = 12f;

            so.ApplyModifiedPropertiesWithoutUndo();
            swayCount++;

            // B. Configure Contact Shadow
            Transform existingShadow = sr.transform.Find("Contact_Shadow");
            GameObject shadowObj = existingShadow != null ? existingShadow.gameObject : null;

            if (shadowObj == null)
            {
                shadowObj = new GameObject("Contact_Shadow");
                shadowObj.transform.SetParent(sr.transform, false);
            }

            var shadowSr = shadowObj.GetComponent<SpriteRenderer>();
            if (shadowSr == null)
            {
                shadowSr = shadowObj.AddComponent<SpriteRenderer>();
            }

            shadowSr.sprite = shadowSprite;
            shadowSr.color = isForeground 
                ? new Color(0.04f, 0.08f, 0.04f, 0.65f) 
                : new Color(0.12f, 0.16f, 0.10f, 0.45f);

            shadowSr.sortingLayerID = sr.sortingLayerID;
            shadowSr.sortingOrder = Mathf.Max(0, sr.sortingOrder - 1);

            // Compute bounds-based placement
            Bounds b = sr.bounds;
            // Local base anchor
            float halfH = b.extents.y;
            float width = Mathf.Max(0.5f, b.size.x);

            // Position at bottom base of the foliage sprite
            // Note: Since local scales may vary, calculate local offset
            Vector3 worldBase = sr.transform.position - new Vector3(0f, halfH * 0.95f, -0.02f);
            shadowObj.transform.position = worldBase;
            shadowObj.transform.rotation = Quaternion.identity;

            // Scale shadow ellipse proportional to clump width
            float shadowW = width * 0.85f;
            float shadowH = width * 0.26f;
            shadowObj.transform.localScale = new Vector3(shadowW, shadowH, 1f);

            shadowCount++;
            EditorUtility.SetDirty(sr.gameObject);
        }
    }

    private static void RenderVerificationScreenshots()
    {
        string artifactDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\1c44fbae-764e-472a-aec6-c9c375d88a28";
        if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

        var tempCamObj = new GameObject("Temp_Verification_Camera");
        var cam = tempCamObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0.35f, 0.45f, 0.55f, 1f);
        cam.orthographic = true;

        try
        {
            // 1. Close-up at Tree (X = 93, Y = 0.8, Size = 4.2)
            tempCamObj.transform.position = new Vector3(93.0f, 0.8f, -12.0f);
            cam.orthographicSize = 4.2f;

            var rtClose = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rtClose;
            cam.Render();

            RenderTexture.active = rtClose;
            var texClose = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texClose.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texClose.Apply();
            File.WriteAllBytes(Path.Combine(artifactDir, "verified_backdrop_sway_and_shadow_closeup.jpg"), texClose.EncodeToJPG(80));

            cam.targetTexture = null;
            RenderTexture.active = null;
            rtClose.Release();
            UnityEngine.Object.DestroyImmediate(texClose);

            // 2. Wide Vista Panorama (X = 95, Y = 1.8, Size = 8.5)
            tempCamObj.transform.position = new Vector3(95.0f, 1.8f, -15.0f);
            cam.orthographicSize = 8.5f;

            var rtPano = new RenderTexture(1920, 600, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rtPano;
            cam.Render();

            RenderTexture.active = rtPano;
            var texPano = new Texture2D(1920, 600, TextureFormat.RGB24, false);
            texPano.ReadPixels(new Rect(0, 0, 1920, 600), 0, 0);
            texPano.Apply();
            File.WriteAllBytes(Path.Combine(artifactDir, "verified_backdrop_sway_and_shadow_panorama.jpg"), texPano.EncodeToJPG(80));

            cam.targetTexture = null;
            RenderTexture.active = null;
            rtPano.Release();
            UnityEngine.Object.DestroyImmediate(texPano);

            Debug.Log("[SetupSway] Verification screenshots captured successfully to artifact directory!");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tempCamObj);
        }
    }
}
