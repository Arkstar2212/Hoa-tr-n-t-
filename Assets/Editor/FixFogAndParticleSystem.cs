#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HoaDo.EditorTools
{
    public static class FixFogAndParticleSystem
    {
        [MenuItem("Tools/Hoa trên đất đỏ/Remove Fog and Fix Particles", priority = 12)]
        public static void ExecuteFix()
        {
            Undo.SetCurrentGroupName("Remove Fog and Fix Particles");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. BỎ HOÀN TOÀN FOG / GODRAYS (Xóa các dải sương mù che mờ màn hình)
            var godrayRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int removedFogCount = 0;
            foreach (var r in godrayRenderers)
            {
                if (r == null) continue;
                string objName = r.gameObject.name;
                string matName = r.sharedMaterial != null ? r.sharedMaterial.name : "";
                string shaderName = r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "";

                bool isFogOrBeam = objName.Contains("Sunbeam") ||
                                   objName.Contains("GodRay") ||
                                   objName.Contains("Haze") ||
                                   matName.Contains("Sunbeam") ||
                                   matName.Contains("GodRay") ||
                                   matName.Contains("Haze") ||
                                   shaderName.Contains("StylizedGodRays") ||
                                   shaderName.Contains("AtmosphericHaze");

                if (isFogOrBeam)
                {
                    Undo.RecordObject(r.gameObject, "Disable Fog/Sunbeam");
                    r.gameObject.SetActive(false);
                    removedFogCount++;
                }
            }

            // Tắt hoàn toàn RenderSettings fog nếu đang bật
            RenderSettings.fog = false;

            // 2. LÀM LẠI MATERIAL PARTICLE CHUẨN URP (Không còn bị ô vuông tím / pink square)
            string matPath = "Assets/Materials/VFX/M_GoldenMotes.mat";
            Material motesMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (motesMat == null)
            {
                motesMat = new Material(particleShader) { name = "M_GoldenMotes" };
                AssetDatabase.CreateAsset(motesMat, matPath);
            }
            else
            {
                motesMat.shader = particleShader;
            }

            Texture2D softTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sky/soft_particle.png");
            if (softTex != null)
            {
                motesMat.SetTexture("_BaseMap", softTex);
            }

            motesMat.SetColor("_BaseColor", new Color(1.0f, 0.95f, 0.72f, 0.35f));
            // Cấu hình Transparent Blend cho URP Particles Unlit
            motesMat.SetFloat("_Surface", 1.0f); // Transparent
            motesMat.SetFloat("_Blend", 0.0f);   // Alpha
            motesMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            motesMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            motesMat.SetInt("_ZWrite", 0);
            motesMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(motesMat);

            // 3. TẮT HOẶC CẬP NHẬT TOÀN BỘ PARTICLE TRONG SCENE
            var allPS = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            int fixedParticleCount = 0;
            foreach (var ps in allPS)
            {
                if (ps == null) continue;
                var psr = ps.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    Undo.RecordObject(psr, "Update Particle Material");
                    psr.sharedMaterial = motesMat;
                }

                // Tắt các hạt bụi gây rối mắt ở tiền cảnh (bỏ particle thừa theo yêu cầu)
                Undo.RecordObject(ps.gameObject, "Toggle Particle Active");
                ps.gameObject.SetActive(false);
                fixedParticleCount++;
            }

            // Đồng thời tắt các nhóm cha VFX nếu có
            var vfxParent = GameObject.Find("09_VFX");
            if (vfxParent != null)
            {
                Undo.RecordObject(vfxParent, "Disable 09_VFX Root");
                vfxParent.SetActive(false);
            }

            var atmoVfx = GameObject.Find("Atmospheric_VFX");
            if (atmoVfx != null)
            {
                Undo.RecordObject(atmoVfx, "Disable Atmospheric_VFX");
                atmoVfx.SetActive(false);
            }

            var sunbeamGroup = GameObject.Find("Lana_CloudRadiance_And_Sunbeams");
            if (sunbeamGroup != null)
            {
                Undo.RecordObject(sunbeamGroup, "Disable Lana_CloudRadiance_And_Sunbeams");
                sunbeamGroup.SetActive(false);
            }

            Undo.CollapseUndoOperations(undoGroup);

            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#40b8a6>[Hoa trên đất đỏ]</color> Đã dọn dẹp triệt để: Tắt {removedFogCount} dải fog/godray, sửa material chuẩn URP & tắt {fixedParticleCount} hệ thống particle gây lỗi ô vuông tím!");
        }
    }
}
#endif
