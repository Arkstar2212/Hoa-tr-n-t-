using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using HoaVaMau.Gameplay;

namespace HoaVaMau.EditorScripts
{
    public static class FixCaMauGroundRiceRocksAndSpawn
    {
        [MenuItem("Tools/Fix CaMau Ground Rice Rocks And Spawn")]
        public static void ExecuteFix()
        {
            Debug.Log(">>> [FixCaMauGroundRiceRocksAndSpawn] Starting fix...");

            // ---------------------------------------------------------
            // 1. DELETE ALL BLUE / CYAN ROCKS
            // ---------------------------------------------------------
            int rocksDeleted = 0;
            var toDestroy = new List<GameObject>();

            // Find all renderers with M_Rock_Platform_01 or Material.001
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r == null || r.gameObject == null) continue;
                string goName = r.gameObject.name;
                string matName = r.sharedMaterial != null ? r.sharedMaterial.name : "";

                // Preserve village campfire stone ring
                if (r.transform.parent != null && r.transform.parent.name == "Tree_Central_StoneRing")
                {
                    // Reassign campfire stones to M_Mountain_Crag_Rock so they are natural stone, not cyan
                    var naturalStoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/M_Mountain_Crag_Rock.mat");
                    if (naturalStoneMat != null)
                    {
                        r.sharedMaterial = naturalStoneMat;
                    }
                    continue;
                }

                // Check for blue rocks
                bool isBlueRock = false;
                if (matName == "M_Rock_Platform_01" || matName == "Material.001" || matName == "Material_0.001")
                {
                    isBlueRock = true;
                }

                // Also check by object name patterns for old pass/boundary rocks
                if (goName.StartsWith("Pass_Crag_") ||
                    goName.StartsWith("North_Gate_Pass_") ||
                    goName.StartsWith("West_Crag_") ||
                    goName.StartsWith("East_Crag_") ||
                    goName.StartsWith("West_Plat_") ||
                    goName.StartsWith("East_Plat_") ||
                    goName.StartsWith("West_Platform_") ||
                    goName.StartsWith("East_Platform_") ||
                    goName.StartsWith("West_Boulder_") ||
                    goName.StartsWith("East_Boulder_"))
                {
                    isBlueRock = true;
                }

                // Never delete the new Planet of Lana mountains!
                if (goName.StartsWith("WestMtn_") || goName.StartsWith("EastMtn_") || goName.StartsWith("NorthMtn_"))
                {
                    isBlueRock = false;
                }

                if (isBlueRock && !toDestroy.Contains(r.gameObject))
                {
                    toDestroy.Add(r.gameObject);
                }
            }

            foreach (var go in toDestroy)
            {
                if (go != null)
                {
                    Undo.DestroyObjectImmediate(go);
                    rocksDeleted++;
                }
            }
            Debug.Log($"[FixCaMauGroundRiceRocksAndSpawn] Deleted {rocksDeleted} blue rocks.");

            // ---------------------------------------------------------
            // 2. FIX GROUND COLOR & TEXTURE
            // ---------------------------------------------------------
            // Load authentic village soil grass material
            var villageSoilMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/M_Ground_Grass_VillageSoil.mat");
            if (villageSoilMat == null)
            {
                villageSoilMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Ground_Grass_VillageSoil.mat");
            }

            int hillsUpdated = 0;
            var allHills = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mr in allHills)
            {
                if (mr == null) continue;
                if (mr.name.StartsWith("Hill_"))
                {
                    if (villageSoilMat != null)
                    {
                        Undo.RecordObject(mr, "Update Hill Material");
                        mr.sharedMaterial = villageSoilMat;
                    }

                    // Recalculate smooth normals on hill mesh so there are no faceted triangles
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Mesh m = mf.sharedMesh;
                        Undo.RecordObject(m, "Recalculate Hill Normals");
                        m.RecalculateNormals();
                        m.RecalculateTangents();
                    }
                    hillsUpdated++;
                }
            }
            Debug.Log($"[FixCaMauGroundRiceRocksAndSpawn] Updated {hillsUpdated} hill ground meshes with smooth normals and uniform grass texture.");

            // Also ensure M_Ground_Grass_HillsideMeadow itself is harmonized if referenced anywhere
            var hillsideMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/M_Ground_Grass_HillsideMeadow.mat");
            if (hillsideMat != null && villageSoilMat != null)
            {
                Undo.RecordObject(hillsideMat, "Harmonize Hillside Meadow Mat");
                if (villageSoilMat.HasProperty("_BaseMap") && villageSoilMat.GetTexture("_BaseMap") != null)
                {
                    hillsideMat.SetTexture("_BaseMap", villageSoilMat.GetTexture("_BaseMap"));
                    hillsideMat.SetVector("_BaseMap_ST", new Vector4(24f, 24f, 0f, 0f));
                }
                if (villageSoilMat.HasProperty("_BaseColor"))
                {
                    hillsideMat.SetColor("_BaseColor", villageSoilMat.GetColor("_BaseColor"));
                }
            }

            // ---------------------------------------------------------
            // 3. FIX RICE: REMOVE ROW SWAY & ANCHOR FIRMLY TO GROUND
            // ---------------------------------------------------------
            int swayeysRemoved = 0;
            var allSwayComponents = Object.FindObjectsByType<PlanetOfLanaFoliageSway>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sway in allSwayComponents)
            {
                if (sway == null) continue;
                // Check if this sway component is on a rice row or paddy object
                string goName = sway.gameObject.name;
                if (goName.StartsWith("Paddy_") || goName.Contains("_Plot_") || goName.Contains("Rice"))
                {
                    // Reset transform to identity and cleanly grounded
                    Transform t = sway.transform;
                    Undo.RecordObject(t, "Reset Rice Row Transform");
                    t.localRotation = Quaternion.identity;

                    Undo.DestroyObjectImmediate(sway);
                    swayeysRemoved++;
                }
            }
            Debug.Log($"[FixCaMauGroundRiceRocksAndSpawn] Removed {swayeysRemoved} FoliageSway components from rice rows.");

            // On Mat_Rice_Meshy: turn OFF player push reaction so rice never bends/dances when approached
            var riceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Foliage/Mat_Rice_Meshy.mat");
            if (riceMat == null)
            {
                var allMats = Resources.FindObjectsOfTypeAll<Material>();
                foreach (var m in allMats)
                {
                    if (m.name == "Mat_Rice_Meshy") { riceMat = m; break; }
                }
            }
            if (riceMat != null)
            {
                Undo.RecordObject(riceMat, "Disable Rice Player Push");
                if (riceMat.HasProperty("_EnablePlayerPush")) riceMat.SetFloat("_EnablePlayerPush", 0.0f);
                if (riceMat.HasProperty("_PushStrength")) riceMat.SetFloat("_PushStrength", 0.0f);
                if (riceMat.HasProperty("_WindSwayStrength")) riceMat.SetFloat("_WindSwayStrength", 0.04f); // Very gentle breeze
                Debug.Log("[FixCaMauGroundRiceRocksAndSpawn] Mat_Rice_Meshy player push disabled, anchored firmly to ground.");
            }

            // ---------------------------------------------------------
            // 4. SET CHARACTER AN SPAWN AT VILLAGE GATE LOOKING SOUTH
            // ---------------------------------------------------------
            // Village Gate is at X = -7.0, Y = 0.5, Z = 184.0
            // Spawn An at X = -7.0, Y = 0.55, Z = 182.0 facing South (Y rotation = 180 degrees)
            Vector3 spawnPos = new Vector3(-7.0f, 0.55f, 182.0f);
            Quaternion spawnRot = Quaternion.Euler(0f, 180f, 0f);

            var playerAn = GameObject.Find("An_Player");
            if (playerAn != null)
            {
                Undo.RecordObject(playerAn.transform, "Move An to Gate Spawn");
                playerAn.transform.position = spawnPos;
                playerAn.transform.rotation = spawnRot;
                playerAn.tag = "Player";
                Debug.Log($"[FixCaMauGroundRiceRocksAndSpawn] Moved An_Player to gate spawn: {spawnPos}, rot: (0, 180, 0)");
            }

            var playerSpawnObj = GameObject.Find("PlayerSpawn");
            if (playerSpawnObj != null)
            {
                Undo.RecordObject(playerSpawnObj.transform, "Update PlayerSpawn Marker");
                playerSpawnObj.transform.position = spawnPos;
                playerSpawnObj.transform.rotation = spawnRot;
            }

            // Fix NPC Minh tag if it was set to "Player"
            var friendMinh = GameObject.Find("NPC_Friend_Minh");
            if (friendMinh != null && friendMinh.CompareTag("Player"))
            {
                Undo.RecordObject(friendMinh, "Fix Friend Minh Tag");
                friendMinh.tag = "Untagged";
                Debug.Log("[FixCaMauGroundRiceRocksAndSpawn] Re-tagged NPC_Friend_Minh to Untagged.");
            }

            // Align ThirdPersonOrbitCamera behind An looking South
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                var orbit = mainCam.GetComponent<ThirdPersonOrbitCamera>();
                if (orbit != null)
                {
                    Undo.RecordObject(orbit, "Init Camera Yaw/Pitch");
                    var type = typeof(ThirdPersonOrbitCamera);
                    var yawFi = type.GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var curYawFi = type.GetField("currentYaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var pitchFi = type.GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var curPitchFi = type.GetField("currentPitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (yawFi != null) yawFi.SetValue(orbit, 180f);
                    if (curYawFi != null) curYawFi.SetValue(orbit, 180f);
                    if (pitchFi != null) pitchFi.SetValue(orbit, 14f);
                    if (curPitchFi != null) curPitchFi.SetValue(orbit, 14f);
                }

                Undo.RecordObject(mainCam.transform, "Set Camera Gate Start Transform");
                mainCam.transform.position = new Vector3(-6.65f, 2.5f, 186.8f);
                mainCam.transform.rotation = Quaternion.Euler(14f, 180f, 0f);
                Debug.Log("[FixCaMauGroundRiceRocksAndSpawn] Positioned Main Camera behind An at North Gate looking South.");
            }

            Debug.Log(">>> [FixCaMauGroundRiceRocksAndSpawn] Execution complete!");
        }
    }
}
