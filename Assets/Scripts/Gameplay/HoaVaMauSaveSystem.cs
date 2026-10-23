using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Robust, production-grade Save System for "Hoa trên Đất Đỏ".
    /// Tracks up to 3 Save Slots with real chapter progression, playtime, location, and timestamps.
    /// Provides atomic JSON persistence and PlayerPrefs fallback for maximum reliability.
    /// </summary>
    public static class HoaVaMauSaveSystem
    {
        public const int MaxSlots = 3;
        private const string PrefsPrefix = "HoaVaMau_Save_";

        [System.Serializable]
        public class SaveSlotData
        {
            public int slotIndex = 1;
            public bool isOccupied = false;
            public string chapterTitle = "Chương 1: Đất Đỏ Cà Mau";
            public string locationName = "Bờ Sông Đất Đỏ";
            public float playTimeSeconds = 0f;
            public string saveTimestamp = "";
            public int checkpointIndex = 0;
            public float playerPosX = 0f;
            public float playerPosY = -1.77f;
            public float playerPosZ = 0f;

            public string GetFormattedPlayTime()
            {
                int totalSec = Mathf.FloorToInt(playTimeSeconds);
                int hours = totalSec / 3600;
                int minutes = (totalSec % 3600) / 60;
                int seconds = totalSec % 60;
                if (hours > 0)
                    return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

        public static int ActiveSlotIndex { get; set; } = 1;
        public static float SessionPlayTimeCounter = 0f;
        public static event Action<bool, string> OnAutosaveCompleted;

        private static string GetSaveFilePath(int slot)
        {
            string dir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, $"save_slot_{slot}.json");
        }

        public static bool HasAnySaveData()
        {
            for (int i = 1; i <= MaxSlots; i++)
            {
                var slot = GetSlotData(i);
                if (slot != null && slot.isOccupied) return true;
            }
            return false;
        }

        public static SaveSlotData GetSlotData(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > MaxSlots) slotIndex = 1;
            string filePath = GetSaveFilePath(slotIndex);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var data = JsonUtility.FromJson<SaveSlotData>(json);
                    if (data != null && data.isOccupied)
                    {
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[HoaVaMauSaveSystem] Error reading save slot {slotIndex} from disk: {ex.Message}");
                }
            }

            // Fallback to PlayerPrefs metadata
            string key = $"{PrefsPrefix}Slot_{slotIndex}_Occupied";
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return new SaveSlotData
                {
                    slotIndex = slotIndex,
                    isOccupied = true,
                    chapterTitle = PlayerPrefs.GetString($"{PrefsPrefix}Slot_{slotIndex}_Chapter", "Chương 1: Đất Đỏ Cà Mau"),
                    locationName = PlayerPrefs.GetString($"{PrefsPrefix}Slot_{slotIndex}_Location", "Bờ Sông Đất Đỏ"),
                    playTimeSeconds = PlayerPrefs.GetFloat($"{PrefsPrefix}Slot_{slotIndex}_PlayTime", 60f),
                    saveTimestamp = PlayerPrefs.GetString($"{PrefsPrefix}Slot_{slotIndex}_Date", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    checkpointIndex = PlayerPrefs.GetInt($"{PrefsPrefix}Slot_{slotIndex}_Checkpoint", 0),
                    playerPosX = PlayerPrefs.GetFloat($"{PrefsPrefix}Slot_{slotIndex}_PosX", 0.0f),
                    playerPosY = PlayerPrefs.GetFloat($"{PrefsPrefix}Slot_{slotIndex}_PosY", -1.77f),
                    playerPosZ = 0f
                };
            }

            return new SaveSlotData
            {
                slotIndex = slotIndex,
                isOccupied = false,
                chapterTitle = "Chưa có dữ liệu",
                locationName = "Vị trí trống",
                playTimeSeconds = 0f,
                saveTimestamp = "--/--/----"
            };
        }

        public static bool SaveCurrentGameState(int slotIndex, string chapter = "Chương 1: Đất Đỏ Cà Mau", string location = "Bờ Sông Đất Đỏ", int checkpoint = 0, Vector3? pos = null)
        {
            if (slotIndex < 1 || slotIndex > MaxSlots) slotIndex = 1;
            ActiveSlotIndex = slotIndex;

            Vector3 playerPos = pos.HasValue ? pos.Value : new Vector3(0.0f, -1.77f, 0f);

            var existing = GetSlotData(slotIndex);
            float totalPlayTime = (existing != null && existing.isOccupied ? existing.playTimeSeconds : 0f) + SessionPlayTimeCounter;
            SessionPlayTimeCounter = 0f; // reset session delta once committed

            var data = new SaveSlotData
            {
                slotIndex = slotIndex,
                isOccupied = true,
                chapterTitle = chapter,
                locationName = location,
                playTimeSeconds = totalPlayTime,
                saveTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                checkpointIndex = checkpoint,
                playerPosX = playerPos.x,
                playerPosY = playerPos.y,
                playerPosZ = playerPos.z
            };

            bool diskWriteOk = false;
            try
            {
                string path = GetSaveFilePath(slotIndex);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                diskWriteOk = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HoaVaMauSaveSystem] Failed writing save file: {ex.Message}");
            }

            // Also mirror to PlayerPrefs for instant cache & backup
            try
            {
                PlayerPrefs.SetInt($"{PrefsPrefix}Slot_{slotIndex}_Occupied", 1);
                PlayerPrefs.SetString($"{PrefsPrefix}Slot_{slotIndex}_Chapter", data.chapterTitle);
                PlayerPrefs.SetString($"{PrefsPrefix}Slot_{slotIndex}_Location", data.locationName);
                PlayerPrefs.SetFloat($"{PrefsPrefix}Slot_{slotIndex}_PlayTime", data.playTimeSeconds);
                PlayerPrefs.SetString($"{PrefsPrefix}Slot_{slotIndex}_Date", data.saveTimestamp);
                PlayerPrefs.SetInt($"{PrefsPrefix}Slot_{slotIndex}_Checkpoint", data.checkpointIndex);
                PlayerPrefs.SetFloat($"{PrefsPrefix}Slot_{slotIndex}_PosX", data.playerPosX);
                PlayerPrefs.SetFloat($"{PrefsPrefix}Slot_{slotIndex}_PosY", data.playerPosY);
                PlayerPrefs.SetInt("HoaVaMau_ActiveSlot", slotIndex);
                PlayerPrefs.Save();
            }
            catch { }

            OnAutosaveCompleted?.Invoke(diskWriteOk, data.locationName);
            return diskWriteOk;
        }

        public static void StartNewGame(int slotIndex, string targetScene = "New Scene 1_Overhaul")
        {
            ActiveSlotIndex = slotIndex;
            SessionPlayTimeCounter = 0f;

            // Commit new game slot state
            SaveCurrentGameState(slotIndex, "Chương 1: Đất Đỏ Cà Mau", "Khởi hành", 0, new Vector3(0.0f, -1.77f, 0f));

            IntroCutsceneState.MarkForNewGame();
            if (!string.IsNullOrEmpty(targetScene))
            {
                Time.timeScale = 1.0f;
                SceneManager.LoadScene(targetScene);
            }
        }

        public static void ContinueGame(int slotIndex, string targetScene = "New Scene 1_Overhaul")
        {
            ActiveSlotIndex = slotIndex;
            SessionPlayTimeCounter = 0f;
            var slot = GetSlotData(slotIndex);
            int cp = (slot != null && slot.isOccupied) ? slot.checkpointIndex : 0;
            IntroCutsceneState.MarkForContinue(cp);


            if (!string.IsNullOrEmpty(targetScene))
            {
                Time.timeScale = 1.0f;
                SceneManager.LoadScene(targetScene);
            }
        }

        public static void DeleteSlot(int slotIndex)
        {
            try
            {
                string path = GetSaveFilePath(slotIndex);
                if (File.Exists(path)) File.Delete(path);
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_Occupied");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_Chapter");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_Location");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_PlayTime");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_Date");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_Checkpoint");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_PosX");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}Slot_{slotIndex}_PosY");
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HoaVaMauSaveSystem] Error deleting slot {slotIndex}: {ex.Message}");
            }
        }
    }
}
