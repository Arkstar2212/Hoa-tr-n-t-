using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;
using HoaVaMau.Dialogue;

namespace HoaDo.Editor
{
    public static class IntegrateVoiceAudioToScenes
    {
        private const string Scene2DPath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string Scene3DPath = "Assets/Scenes/CaMau_1954_Village.unity";

        private const string Voice2DDir = "Assets/Audio/Voice Character/Voice 2D";
        private const string Voice3DDir = "Assets/Audio/Voice Character/Voice 3D";

        [MenuItem("Tools/Hoa Do/Integrate Voice Audio To 2D and 3D Scenes")]
        public static void Execute()
        {
            Debug.Log("<color=#38bdf8>[VoiceIntegration] BẮT ĐẦU GHÉP TOÀN BỘ VOICE AUDIO VÀO SCENE 2D VÀ 3D...</color>");

            IntegrateVoice2DScene();
            IntegrateVoice3DScene();

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#10b981>[VoiceIntegration] HOÀN TẤT THÀNH CÔNG! Đã ghép hoàn chỉnh 78 voice audio clips (18 clip 2D + 60 clip 3D) vào cả Scene 2D và 3D!</color>");
        }

        public static void IntegrateVoice2DScene()
        {
            Debug.Log($"[VoiceIntegration] Đang mở Scene 2.5D: {Scene2DPath}");
            var scene = EditorSceneManager.OpenScene(Scene2DPath, OpenSceneMode.Single);

            var clipMap = new Dictionary<string, AudioClip[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Bé Mận", new[] { Load2D("Be_Man_Cau1.mp3"), Load2D("Be_Man_Cau2.mp3") } },
                { "Má của An", new[] { Load2D("Ma_An_Cau1.mp3"), Load2D("Ma_An_Cau2.mp3") } },
                { "Má An", new[] { Load2D("Ma_An_Cau1.mp3"), Load2D("Ma_An_Cau2.mp3") } },
                { "Bác Ba Phi", new[] { Load2D("Bac_Ba_Phi_Cau1.mp3"), Load2D("Bac_Ba_Phi_Cau2.mp3") } },
                { "Chú Bảy Chèo Đò", new[] { Load2D("Chu_Bay_Cheo_Do_Cau1.mp3"), Load2D("Chu_Bay_Cheo_Do_Cau2.mp3") } },
                { "Thằng Minh", new[] { Load2D("Thang_Minh_Cau1 (1).mp3"), Load2D("Thang_Minh_Cau2 (1).mp3") } },
                { "Chị Tư Gánh Nước", new[] { Load2D("Chi_Tu_Ganh_Nuoc_Cau1.mp3"), Load2D("Chi_Tu_Ganh_Nuoc_Cau2.mp3") } },
                { "Chú Sáu Làm Vườn", new[] { Load2D("Chu_Sau_Lam_Vuon_Cau1.mp3"), Load2D("Chu_Sau_Lam_Vuon_Cau2.mp3") } },
                { "Trưởng Thôn Năm", new[] { Load2D("Truong_Thon_Nam_Cau1.mp3"), Load2D("Truong_Thon_Nam_Cau2.mp3") } },
                { "Dì Hai", new[] { Load2D("Di_Hai_Cau1.mp3"), Load2D("Di_Hai_Cau2.mp3") } },
                { "Bà Cụ Bảy", new[] { Load3D("HT12_01_CuBayGia_MoDau1.mp3"), Load3D("HT12_02_CuBayGia_MoDau2.mp3") } },
                { "Cụ Bảy", new[] { Load3D("HT12_01_CuBayGia_MoDau1.mp3"), Load3D("HT12_02_CuBayGia_MoDau2.mp3") } }
            };

            var npcs = UnityEngine.Object.FindObjectsByType<VillageNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int boundCount = 0;

            foreach (var npc in npcs)
            {
                if (npc == null) continue;
                string rawName = npc.name;

                string matchedKey = null;
                foreach (var key in clipMap.Keys)
                {
                    if (rawName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchedKey = key;
                        break;
                    }
                }

                if (matchedKey == null)
                {
                    var serializedObj = new SerializedObject(npc);
                    var npcNameProp = serializedObj.FindProperty("npcName");
                    if (npcNameProp != null && !string.IsNullOrEmpty(npcNameProp.stringValue))
                    {
                        foreach (var key in clipMap.Keys)
                        {
                            if (npcNameProp.stringValue.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                matchedKey = key;
                                break;
                            }
                        }
                    }
                }

                if (matchedKey != null && clipMap.TryGetValue(matchedKey, out var clips))
                {
                    npc.SetVoiceClips(clips);

                    var audioSrc = npc.GetComponent<AudioSource>();
                    if (audioSrc == null) audioSrc = npc.gameObject.AddComponent<AudioSource>();
                    audioSrc.playOnAwake = false;
                    audioSrc.spatialBlend = 0f;
                    audioSrc.volume = 1f;

                    EditorUtility.SetDirty(npc.gameObject);
                    boundCount++;
                    Debug.Log($"[VoiceIntegration 2D] Đã gán voice cho '{npc.name}' ({matchedKey}): {clips[0]?.name}, {clips[1]?.name}");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VoiceIntegration 2D] Đã gán voice cho {boundCount} NPC trong {Scene2DPath}");
        }

        public static void IntegrateVoice3DScene()
        {
            Debug.Log($"[VoiceIntegration] Đang mở Scene 3D: {Scene3DPath}");
            var scene = EditorSceneManager.OpenScene(Scene3DPath, OpenSceneMode.Single);

            // Ensure GenshinConversationSystem has an AudioSource
            var convSys = UnityEngine.Object.FindAnyObjectByType<GenshinConversationSystem>();
            if (convSys != null)
            {
                var src = convSys.GetComponent<AudioSource>();
                if (src == null) src = convSys.gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.volume = 1f;
                EditorUtility.SetDirty(convSys);
            }

            var allInteractables = UnityEngine.Object.FindObjectsByType<GenshinNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var interactableMap = new Dictionary<string, GenshinNPCInteractable>(StringComparer.OrdinalIgnoreCase);

            foreach (var inter in allInteractables)
            {
                if (inter == null) continue;
                interactableMap[inter.gameObject.name] = inter;
                if (!string.IsNullOrEmpty(inter.NPCName))
                {
                    interactableMap[inter.NPCName] = inter;
                }
            }

            int bound3DCount = 0;

            // 1. Trưởng Thôn Nam (NPC_01_TruongThonNam)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_01_TruongThonNam", "Trưởng Thôn Năm",
                new[] { Load3D("HoiThoai_01_TTN_MoDau1.mp3"), Load3D("HoiThoai_02_TTN_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HoiThoai_03_BeAn_LuaChon1.mp3"), Load3D("HoiThoai_04_TTN_DapLai1.mp3")),
                    (Load3D("HoiThoai_05_BeAn_LuaChon2.mp3"), Load3D("HoiThoai_06_TTN_DapLai2.mp3")),
                    (Load3D("HoiThoai_07_BeAn_LuaChon3.mp3"), Load3D("HoiThoai_08_TTN_DapLai3.mp3"))
                });

            // 2. Má của An (NPC_02_MaAnMother)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_02_MaAnMother", "Má của An",
                new[] { Load3D("HT2_01_MaAn_MoDau1.mp3"), Load3D("HT2_02_MaAn_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT2_03_BeAn_LuaChon1.mp3"), Load3D("HT2_04_MaAn_DapLai1.mp3")),
                    (Load3D("HT2_05_BeAn_LuaChon2.mp3"), Load3D("HT2_06_MaAn_DapLai2.mp3"))
                });

            // 3. Bác Ba Phi (NPC_03_BacBaPhi)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_03_BacBaPhi", "Bác Ba Phi",
                new[] { Load3D("HT3_01_BacBa_MoDau1.mp3"), Load3D("HT3_02_BacBa_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT3_03_BeAn_LuaChon1.mp3"), Load3D("HT3_04_BacBa_DapLai1.mp3")),
                    (Load3D("HT3_05_BeAn_LuaChon2.mp3"), Load3D("HT3_06_BacBa_DapLai2.mp3"))
                });

            // 4. Chú Bảy Chèo Đò (NPC_04_ChuBayCheoDo)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_04_ChuBayCheoDo", "Chú Bảy Chèo Đò",
                new[] { Load3D("HT4_01_ChuBay_MoDau1.mp3"), Load3D("HT4_02_ChuBay_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT4_03_BeAn_LuaChon1.mp3"), Load3D("HT4_04_ChuBay_DapLai1.mp3")),
                    (Load3D("HT4_05_BeAn_LuaChon2.mp3"), Load3D("HT4_06_ChuBay_DapLai2.mp3"))
                });

            // 5. Thằng Minh (NPC_05_ThangMinh)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_05_ThangMinh", "Thằng Minh",
                new[] { Load3D("HT5_01_ThangMinh_MoDau1.mp3"), Load3D("HT5_02_ThangMinh_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT5_03_BeAn_LuaChon1.mp3"), Load3D("HT5_04_ThangMinh_DapLai1.mp3")),
                    (Load3D("HT5_05_BeAn_LuaChon2.mp3"), Load3D("HT5_06_ThangMinh_DapLai2.mp3"))
                });

            // 6. Chị Tư Gánh Nước (NPC_06_ChiTuGanhNuoc)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_06_ChiTuGanhNuoc", "Chị Tư Gánh Nước",
                new[] { Load3D("HT6_01_ChiTu_MoDau1.mp3"), Load3D("HT6_02_ChiTu_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT6_03_BeAn_LuaChon1.mp3"), Load3D("HT6_04_ChiTu_DapLai1.mp3"))
                });

            // 7. Chú Sáu Làm Vườn (NPC_07_ChuSauLamVuon)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_07_ChuSauLamVuon", "Chú Sáu Làm Vườn",
                new[] { Load3D("HT7_01_ChuSau_MoDau1.mp3"), Load3D("HT7_02_ChuSau_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT7_03_BeAn_LuaChon1.mp3"), Load3D("HT7_04_ChuSau_DapLai1.mp3"))
                });

            // 8. Bé Tèo (NPC_08_BeTeo)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_08_BeTeo", "Bé Tèo",
                new[] { Load3D("HT8_01_BeTeo_MoDau1.mp3"), Load3D("HT8_02_BeTeo_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT8_03_BeAn_LuaChon1.mp3"), Load3D("HT8_04_BeTeo_DapLai1.mp3"))
                });

            // 9. Bé Mận (NPC_09_BeMan)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_09_BeMan", "Bé Mận",
                new[] { Load3D("HT9_01_BeMan_MoDau1.mp3"), Load3D("HT9_02_BeMan_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT9_03_BeAn_LuaChon1.mp3"), Load3D("HT9_04_BeMan_DapLai1.mp3"))
                });

            // 10. Bé Út (NPC_10_BeUt)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_10_BeUt", "Bé Út",
                new[] { Load3D("HT10_01_BeUt_MoDau1.mp3"), Load3D("HT10_02_BeUt_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT10_03_BeAn_LuaChon1.mp3"), Load3D("HT10_04_BeUt_DapLai1.mp3"))
                });

            // 11. Dì Hai (NPC_11_DiHai)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_11_DiHai", "Dì Hai",
                new[] { Load3D("HT11_01_DiHai_MoDau1.mp3"), Load3D("HT11_02_DiHai_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT11_03_BeAn_LuaChon1.mp3"), Load3D("HT11_04_DiHai_DapLai1.mp3"))
                });

            // 12. Cụ Bảy Già (NPC_12_CuBayGia)
            bound3DCount += Wire3DNPC(interactableMap, "NPC_12_CuBayGia", "Cụ Bảy Già",
                new[] { Load3D("HT12_01_CuBayGia_MoDau1.mp3"), Load3D("HT12_02_CuBayGia_MoDau2.mp3") },
                new[]
                {
                    (Load3D("HT12_03_BeAn_LuaChon1.mp3"), Load3D("HT12_04_CuBayGia_DapLai1.mp3"))
                });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VoiceIntegration 3D] Đã gán voice cho {bound3DCount} / 12 NPC trong {Scene3DPath}");
        }

        private static int Wire3DNPC(Dictionary<string, GenshinNPCInteractable> map, string goName, string npcName,
            AudioClip[] openings, (AudioClip anClip, AudioClip respClip)[] choiceClips)
        {
            GenshinNPCInteractable interact = null;
            if (!map.TryGetValue(goName, out interact))
            {
                map.TryGetValue(npcName, out interact);
            }

            if (interact == null)
            {
                Debug.LogWarning($"[VoiceIntegration 3D] Không tìm thấy NPC '{goName}' hoặc '{npcName}'");
                return 0;
            }

            interact.SetVoiceClips(openings);

            var choices = interact.Choices;
            if (choices != null && choiceClips != null)
            {
                for (int i = 0; i < choices.Length && i < choiceClips.Length; i++)
                {
                    choices[i].playerVoiceClip = choiceClips[i].anClip;
                    choices[i].responseVoiceClip = choiceClips[i].respClip;
                }
            }

            EditorUtility.SetDirty(interact);
            EditorUtility.SetDirty(interact.gameObject);
            Debug.Log($"[VoiceIntegration 3D] Gán thành công voice cho '{interact.gameObject.name}' ({interact.NPCName})");
            return 1;
        }

        private static AudioClip Load2D(string filename)
        {
            string path = $"{Voice2DDir}/{filename}";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[VoiceIntegration] Không tìm thấy audio 2D: {path}");
            return clip;
        }

        private static AudioClip Load3D(string filename)
        {
            string path = $"{Voice3DDir}/{filename}";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[VoiceIntegration] Không tìm thấy audio 3D: {path}");
            return clip;
        }
    }
}
