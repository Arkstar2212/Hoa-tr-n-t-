using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;

public static class FixRiceFieldSwayAndShadowsComplete
{
    private const string ShadowSpritePath = "Assets/Textures/Environment/T_Foliage_ContactShadow_Soft.png";

    [MenuItem("Tools/HoaDo/Fix Rice Field Sway And Shadows Complete")]
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        Debug.Log("<color=#3b82f6>[FixSwayComplete] --- STARTING SWAY & SHADOW MASTER FIX ---</color>");

        Sprite shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
        if (shadowSprite == null)
        {
            Debug.LogError($"[FixSwayComplete] Contact shadow sprite not found at {ShadowSpritePath}!");
            return;
        }

        var activeScene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[FixSwayComplete] Active Scene: {activeScene.name}");

        // Step 1: Global Scene Cleanup - Deduplicate components & shadows
        int duplicatesRemoved = DeduplicateSceneSwaysAndShadows();
        Debug.Log($"[FixSwayComplete] Removed {duplicatesRemoved} duplicate components/shadows from scene.");

        // Step 2: Configure Midground Rice (02_Midground_Rice_Depth)
        int mgCount = ProcessGroup("02_Midground_Rice_Depth", shadowSprite, isForeground: false, isMidground: true, maxAngle: 4.2f);
        Debug.Log($"[FixSwayComplete] Midground Rice configured: {mgCount} sprites.");

        // Step 3: Configure Backdrop Rice Groups
        string[] backdropGroups = new string[] {
            "[Additive_Backdrop_RiceClusters_TreeLayer]",
            "[Additive_Backdrop_RiceClusters_TreeLayer] (1)",
            "[Additive_Backdrop_RiceClusters_TreeLayer] (2)",
            "[Additive_Backdrop_RiceClusters_TreeLayer] (3)",
            "[Additive_Backdrop_RiceClusters_TreeLayer] (4)",
            "[Additive_Backdrop_RiceClusters_TreeLayer] (5)",
            "[Additive_Backdrop_RiceField_Complete_Vista]"
        };

        int backdropCount = 0;
        foreach (var bg in backdropGroups)
        {
            backdropCount += ProcessGroup(bg, shadowSprite, isForeground: false, isMidground: false, maxAngle: 5.2f);
        }
        Debug.Log($"[FixSwayComplete] Backdrop Rice configured: {backdropCount} sprites across all backdrop groups.");

        // Step 4: Configure Foreground Rice Groups
        string[] foregroundGroups = new string[] {
            "[Additive_Foreground_RiceField_Overhaul]",
            "[Additive_Foreground_RiceField_Overhaul] (1)",
            "[Additive_Foreground_RiceField_Overhaul] (2)",
            "[Additive_Foreground_RiceField_Overhaul] (3)",
            "[Additive_Foreground_RiceField_Overhaul] (4)",
            "[Additive_Foreground_RiceField_Overhaul] (5)",
            "01_Foreground_Rice_Clusters"
        };

        int fgCount = 0;
        foreach (var fg in foregroundGroups)
        {
            fgCount += ProcessGroup(fg, shadowSprite, isForeground: true, isMidground: false, maxAngle: 3.8f);
        }
        Debug.Log($"[FixSwayComplete] Foreground Rice configured: {fgCount} sprites across all foreground groups.");

        // Step 5: Configure Enhanced Atmosphere Rice
        int atmosCount = ProcessGroup("03_RiceField_Atmosphere_Enhanced", shadowSprite, isForeground: false, isMidground: false, maxAngle: 4.5f);
        Debug.Log($"[FixSwayComplete] Atmosphere Rice configured: {atmosCount} sprites.");

        // Step 6: Clean and configure all Prefabs on Disk
        int prefabsUpdated = CleanAndUpdatePrefabs(shadowSprite);
        Debug.Log($"[FixSwayComplete] Prefabs on disk cleaned & updated: {prefabsUpdated}.");

        // Save Scene and Assets
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 7: Capture visual verification screenshots
        RenderVerificationScreenshots();

        Debug.Log("<color=#10b981>[FixSwayComplete] === SWAY AND SHADOW MASTER FIX FINISHED SUCCESSFULLY ===</color>");
    }

    private static int DeduplicateSceneSwaysAndShadows()
    {
        int count = 0;
        var allRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

        foreach (var sr in allRenderers)
        {
            if (sr == null || sr.gameObject == null) continue;

            // 1. Deduplicate PlanetOfLanaFoliageSway
            var comps = sr.GetComponents<PlanetOfLanaFoliageSway>();
            if (comps != null && comps.Length > 1)
            {
                for (int i = 1; i < comps.Length; i++)
                {
                    if (comps[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(comps[i]);
                        count++;
                    }
                }
                EditorUtility.SetDirty(sr.gameObject);
            }

            // 2. Deduplicate Contact_Shadow children
            List<GameObject> shadows = new List<GameObject>();
            foreach (Transform child in sr.transform)
            {
                if (child != null && child.name == "Contact_Shadow")
                {
                    shadows.Add(child.gameObject);
                }
            }

            if (shadows.Count > 1)
            {
                for (int i = 1; i < shadows.Count; i++)
                {
                    if (shadows[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(shadows[i]);
                        count++;
                    }
                }
                EditorUtility.SetDirty(sr.gameObject);
            }
        }

        return count;
    }

    private static int ProcessGroup(string groupName, Sprite shadowSprite, bool isForeground, bool isMidground, float maxAngle)
    {
        var groupObj = GameObject.Find(groupName);
        if (groupObj == null) return 0;

        int count = 0;
        var renderers = groupObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            if (sr == null || sr.gameObject == null) continue;
            if (sr.gameObject.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (sr.sprite == null) continue;

            ConfigureSwayAndShadow(sr, shadowSprite, isForeground, isMidground, maxAngle);
            count++;
        }

        EditorUtility.SetDirty(groupObj);
        return count;
    }

    private static void ConfigureSwayAndShadow(SpriteRenderer sr, Sprite shadowSprite, bool isForeground, bool isMidground, float maxAngle)
    {
        // 1. Deduplicate Sway
        var sways = sr.GetComponents<PlanetOfLanaFoliageSway>();
        PlanetOfLanaFoliageSway sway = null;
        if (sways != null && sways.Length > 0)
        {
            sway = sways[0];
            for (int i = 1; i < sways.Length; i++)
            {
                if (sways[i] != null) UnityEngine.Object.DestroyImmediate(sways[i]);
            }
        }
        else
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

        if (propMaxAngle != null) propMaxAngle.floatValue = maxAngle;
        if (propSwaySpeed != null) propSwaySpeed.floatValue = 1.6f;
        if (propWorldOffset != null) propWorldOffset.boolValue = true;
        if (propHarmonic != null) propHarmonic.floatValue = 0.35f;
        if (propAnchor != null) propAnchor.boolValue = true;
        if (propInteract != null) propInteract.boolValue = isForeground;
        if (propRadius != null) propRadius.floatValue = 0.85f;
        if (propPushAngle != null) propPushAngle.floatValue = 12f;

        so.ApplyModifiedPropertiesWithoutUndo();

        // 2. Configure Contact Shadow
        List<GameObject> shadows = new List<GameObject>();
        foreach (Transform child in sr.transform)
        {
            if (child != null && child.name == "Contact_Shadow") shadows.Add(child.gameObject);
        }

        Transform shadowT = null;
        if (shadows.Count > 0)
        {
            shadowT = shadows[0].transform;
            for (int i = 1; i < shadows.Count; i++)
            {
                if (shadows[i] != null) UnityEngine.Object.DestroyImmediate(shadows[i]);
            }
        }
        else
        {
            var go = new GameObject("Contact_Shadow");
            go.transform.SetParent(sr.transform, false);
            shadowT = go.transform;
        }

        var shadowSr = shadowT.GetComponent<SpriteRenderer>();
        if (shadowSr == null) shadowSr = shadowT.gameObject.AddComponent<SpriteRenderer>();

        shadowSr.sprite = shadowSprite;
        shadowSr.sortingLayerID = sr.sortingLayerID;
        shadowSr.sortingOrder = Mathf.Max(0, sr.sortingOrder - 1);

        if (isForeground)
        {
            shadowSr.color = new Color(0.04f, 0.08f, 0.04f, 0.65f);
        }
        else if (isMidground)
        {
            shadowSr.color = new Color(0.08f, 0.12f, 0.08f, 0.55f);
        }
        else
        {
            shadowSr.color = new Color(0.12f, 0.16f, 0.10f, 0.45f);
        }

        Bounds b = sr.bounds;
        float halfH = b.extents.y;
        float width = Mathf.Max(0.5f, b.size.x);

        // Place at base of plant
        Vector3 worldBase = sr.transform.position - new Vector3(0f, halfH * 0.95f, -0.02f);
        shadowT.position = worldBase;
        shadowT.rotation = Quaternion.identity;

        float shadowW = width * 0.85f;
        float shadowH = width * 0.26f;
        shadowT.localScale = new Vector3(shadowW, shadowH, 1f);

        EditorUtility.SetDirty(sr.gameObject);
    }

    private static int CleanAndUpdatePrefabs(Sprite shadowSprite)
    {
        int count = 0;
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
                    float angle = isForeground ? 3.8f : 5.2f;

                    var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var sr in srs)
                    {
                        if (sr == null || sr.gameObject == null) continue;
                        if (sr.gameObject.name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (sr.sprite == null) continue;

                        ConfigureSwayAndShadow(sr, shadowSprite, isForeground, isMidground: false, angle);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    count++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FixSwayComplete] Failed updating prefab at {path}: {ex.Message}");
            }
        }
        return count;
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
            // Close-up showing Foreground, Midground and Backdrop rice along path (X = 82 to 98)
            tempCamObj.transform.position = new Vector3(90.0f, -0.2f, -12.0f);
            cam.orthographicSize = 3.6f;

            var rtClose = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rtClose;
            cam.Render();

            RenderTexture.active = rtClose;
            var texClose = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texClose.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texClose.Apply();
            File.WriteAllBytes(Path.Combine(artifactDir, "verified_midground_and_backdrop_sway.jpg"), texClose.EncodeToJPG(80));

            cam.targetTexture = null;
            RenderTexture.active = null;
            rtClose.Release();
            UnityEngine.Object.DestroyImmediate(texClose);

            // Panorama of the entire 70m landscape
            tempCamObj.transform.position = new Vector3(95.0f, 1.2f, -15.0f);
            cam.orthographicSize = 7.0f;

            var rtPano = new RenderTexture(1920, 600, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rtPano;
            cam.Render();

            RenderTexture.active = rtPano;
            var texPano = new Texture2D(1920, 600, TextureFormat.RGB24, false);
            texPano.ReadPixels(new Rect(0, 0, 1920, 600), 0, 0);
            texPano.Apply();
            File.WriteAllBytes(Path.Combine(artifactDir, "verified_full_sway_panorama.jpg"), texPano.EncodeToJPG(80));

            cam.targetTexture = null;
            RenderTexture.active = null;
            rtPano.Release();
            UnityEngine.Object.DestroyImmediate(texPano);

            Debug.Log("[FixSwayComplete] Verification screenshots captured successfully to artifact directory!");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tempCamObj);
        }
    }
}
