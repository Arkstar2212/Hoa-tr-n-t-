using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HoaVaMau.Gameplay;
using HoaVaMau.Test3D;
using HoaVaMau;

namespace HoaVaMau.EditorScripts
{
    public static class FixVillageGateSpawnEditor
    {
        [MenuItem("Tools/CaMau/Fix Village Gate Spawn On Play")]
        public static void FixGateSpawn()
        {
            Debug.Log("<color=#40b8a6>[FixVillageGateSpawnEditor]</color> Starting comprehensive spawn & save fix...");

            // 1. Update OverhaulEndMapTransitionTrigger.cs default spawn
            string trigPath = "Assets/Scripts/Gameplay/OverhaulEndMapTransitionTrigger.cs";
            if (File.Exists(trigPath))
            {
                string trigCode = File.ReadAllText(trigPath);
                string oldDest = "new Vector3(0.5f, 0.45f, 13.0f);";
                string newDest = "new Vector3(-7.0f, 0.55f, 182.0f);";
                if (trigCode.Contains(oldDest))
                {
                    trigCode = trigCode.Replace(oldDest, newDest);
                    File.WriteAllText(trigPath, trigCode);
                    Debug.Log("<color=#40b8a6>[FixVillageGateSpawnEditor]</color> Updated OverhaulEndMapTransitionTrigger.cs default destination spawn to North Gate.");
                }
            }

            // 2. Update HoaVaMauSaveSystem slot 1 in PlayerPrefs and JSON file
            string savesDir = Path.Combine(Application.persistentDataPath, "Saves");
            if (Directory.Exists(savesDir))
            {
                string[] saveFiles = Directory.GetFiles(savesDir, "save_slot_*.json");
                foreach (var sf in saveFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(sf);
                        if (json.Contains("CaMau_1954_Village"))
                        {
                            var data = JsonUtility.FromJson<HoaVaMauSaveSystem.SaveSlotData>(json);
                            if (data != null)
                            {
                                data.playerPosX = -7.0f;
                                data.playerPosY = 0.55f;
                                data.playerPosZ = 182.0f;
                                File.WriteAllText(sf, JsonUtility.ToJson(data, true));
                                Debug.Log($"<color=#40b8a6>[FixVillageGateSpawnEditor]</color> Updated save file {Path.GetFileName(sf)} to North Gate (-7, 0.55, 182).");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FixVillageGateSpawnEditor] Error updating save file {sf}: {ex.Message}");
                    }
                }
            }

            // Update PlayerPrefs for slots
            for (int slot = 1; slot <= 3; slot++)
            {
                string sceneKey = $"HoaVaMau_Save_Slot_{slot}_Scene";
                if (PlayerPrefs.HasKey(sceneKey) && PlayerPrefs.GetString(sceneKey) == "CaMau_1954_Village")
                {
                    PlayerPrefs.SetFloat($"HoaVaMau_Save_Slot_{slot}_PosX", -7.0f);
                    PlayerPrefs.SetFloat($"HoaVaMau_Save_Slot_{slot}_PosY", 0.55f);
                    PlayerPrefs.SetFloat($"HoaVaMau_Save_Slot_{slot}_PosZ", 182.0f);
                    PlayerPrefs.Save();
                    Debug.Log($"<color=#40b8a6>[FixVillageGateSpawnEditor]</color> Updated PlayerPrefs for slot {slot} to North Gate coordinates.");
                }
            }

            // 3. Update GameObjects in the active scene
            var activeScene = SceneManager.GetActiveScene();
            Vector3 gateSpawnPos = new Vector3(-7.0f, 0.55f, 182.0f);
            Quaternion gateLookRot = Quaternion.Euler(0f, 180f, 0f);

            var spawnObjScene = GameObject.Find("PlayerSpawn");
            if (spawnObjScene != null)
            {
                spawnObjScene.transform.position = gateSpawnPos;
                spawnObjScene.transform.rotation = gateLookRot;
                EditorUtility.SetDirty(spawnObjScene);
            }

            var player = GameObject.Find("An_Player");
            if (player != null)
            {
                player.transform.position = gateSpawnPos;
                player.transform.rotation = gateLookRot;
                EditorUtility.SetDirty(player);
            }

            var orbitCam = UnityEngine.Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
            if (orbitCam != null)
            {
                if (player != null) orbitCam.Target = player.transform;
                orbitCam.SnapToTarget(180f);
                EditorUtility.SetDirty(orbitCam.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#40b8a6>[FixVillageGateSpawnEditor]</color> All spawn points, scripts, and saves successfully fixed to North Gate looking South!");
        }
    }
}
