#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HoaVaMau.Gameplay;

namespace HoaDo.EditorTools
{
    public static class FixGrassSystemPivotRotation
    {
        [MenuItem("Tools/Hoa trên đất đỏ/Apply Planet of Lana Pivot Grass System", priority = 10)]
        public static void ApplyPivotGrassSystem()
        {
            Undo.SetCurrentGroupName("Apply Planet of Lana Pivot Grass System");
            int undoGroup = Undo.GetCurrentGroup();

            Shader unlit2dShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                                ?? Shader.Find("Sprites/Default");

            Material standardSpriteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Lana_SpriteDefault.mat");
            if (standardSpriteMat == null && unlit2dShader != null)
            {
                standardSpriteMat = new Material(unlit2dShader) { name = "M_Lana_SpriteDefault" };
                AssetDatabase.CreateAsset(standardSpriteMat, "Assets/Materials/M_Lana_SpriteDefault.mat");
            }

            // 1. Cấu hình toàn bộ thảm thực vật trên sàn đá / lối đi [Official_Lana_Foliage]
            GameObject foliageRoot = GameObject.Find("[Official_Lana_Foliage]");
            int foliageCount = 0;

            if (foliageRoot != null)
            {
                var allRenderers = foliageRoot.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in allRenderers)
                {
                    string name = sr.gameObject.name;
                    Undo.RecordObject(sr, "Update Sprite Material");
                    sr.sharedMaterial = standardSpriteMat;

                    bool isGrass = name.StartsWith("GrassTuft");
                    bool isFlower = name.StartsWith("Wildflower");
                    bool isWeed = name.StartsWith("BroadleafWeed");
                    bool isStaticGround = name.StartsWith("PebbleCluster") || name.StartsWith("LipMoss");

                    var sway = sr.GetComponent<PlanetOfLanaFoliageSway>();

                    if (isStaticGround)
                    {
                        // Sỏi đá và rêu áp sát mặt đất không cần lắc
                        if (sway != null)
                        {
                            Undo.DestroyObjectImmediate(sway);
                        }
                        continue;
                    }

                    if (isGrass || isFlower || isWeed)
                    {
                        if (sway == null)
                        {
                            sway = Undo.AddComponent<PlanetOfLanaFoliageSway>(sr.gameObject);
                        }
                        else
                        {
                            Undo.RecordObject(sway, "Configure Pivot Sway");
                        }

                        sway.EnableCharacterInteraction = true;
                        sway.MaxRotationAngle = isGrass ? 3.0f : (isFlower ? 2.5f : 2.0f);
                        sway.MaxPushAngle = isGrass ? 14.0f : 11.0f;
                        sway.InteractionRadius = 0.85f;

                        EditorUtility.SetDirty(sway);
                        foliageCount++;
                    }
                }
            }

            // 2. Bảo đảm cánh đồng lúa Golden_RiceField_2D không bị co giãn
            var riceRoot = GameObject.Find("Golden_RiceField_2D");
            int riceCount = 0;
            if (riceRoot != null)
            {
                var riceSways = riceRoot.GetComponentsInChildren<PlanetOfLanaFoliageSway>(true);
                foreach (var sway in riceSways)
                {
                    Undo.RecordObject(sway, "Configure Rice Sway");
                    sway.EnableCharacterInteraction = false; // Lúa xa chỉ đung đưa theo gió
                    EditorUtility.SetDirty(sway);
                    riceCount++;
                }
            }

            // 3. Đảm bảo PlayerShaderPositionTracker không can thiệp méo đỉnh
            var tracker = GameObject.FindAnyObjectByType<HoaDo.Environment.PlayerShaderPositionTracker>();
            if (tracker != null)
            {
                // Reset global shader vectors về trạng thái an toàn
                Shader.SetGlobalVector("_PlayerPos", Vector4.zero);
                Shader.SetGlobalVector("_PlayerPosition", Vector4.zero);
                Shader.SetGlobalVector("_PlayerMovement", Vector4.zero);
            }

            Undo.CollapseUndoOperations(undoGroup);

            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#40b8a6>[Hoa trên đất đỏ]</color> Đã áp dụng thành công cơ chế cỏ Pivot Rotation cho {foliageCount} bụi cỏ/hoa và bảo toàn {riceCount} nhánh lúa! Không co giãn hình học, 100% giữ nguyên nét vẽ tranh.");
        }
    }
}
#endif
