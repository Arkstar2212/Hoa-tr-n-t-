using UnityEngine;
using UnityEditor;

namespace HoaDatDo.EditorTools
{
    public static class SetupPlayerToonShader
    {
        private const string CharacterMatPath = "Assets/TripoModels/Boy_Character/Materials/M_Boy_Character.mat";
        private const string BaseColorTexturePath = "Assets/TripoModels/Boy_Character/Boy_Character.fbm/Boy_Character_basecolor.JPEG";
        private const string ToonShaderName = "Shader Graphs/Toon";
        private const string RampTexturePath = "Assets/Shaders/Uber Stylized Water/Third Party/URP_ShaderGraphCustomLighting-main/Examples/Ramp Textures/ToonRamp2.png";

        [MenuItem("Tools/Hoa Đất Đỏ/Apply Toon Shader to Player")]
        public static void ApplyToonShader()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(CharacterMatPath);
            if (mat == null)
            {
                Debug.LogError($"[SetupPlayerToonShader] Material not found at: {CharacterMatPath}");
                return;
            }

            Shader toonShader = Shader.Find(ToonShaderName);
            if (toonShader == null)
            {
                Debug.LogError($"[SetupPlayerToonShader] Shader not found: {ToonShaderName}");
                return;
            }

            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorTexturePath);
            Texture2D rampTex = AssetDatabase.LoadAssetAtPath<Texture2D>(RampTexturePath);

            Undo.RecordObject(mat, "Apply Toon Shader to Player");

            // Assign shader
            mat.shader = toonShader;

            // Configure Base Map & Tint
            if (baseColor != null)
            {
                mat.SetTexture("_BaseMap", baseColor);
            }
            mat.SetColor("_DiffuseColor", Color.white);

            // Configure Ramp Texture for Cel-shading
            if (rampTex != null)
            {
                mat.SetTexture("_RampTexture", rampTex);
            }

            // Disable specular gloss to remove plastic look
            mat.SetFloat("_SPECULAR", 0f);
            mat.SetColor("_SpecularColour", new Color(0f, 0f, 0f, 0f));
            mat.SetFloat("_Smoothness", 0f);
            mat.DisableKeyword("_SPECULAR_ON");

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#38ef7d><b>[SetupPlayerToonShader] Successfully applied '{ToonShaderName}' to '{mat.name}'!</b></color>");
        }

        [MenuItem("Tools/Hoa Đất Đỏ/Revert Player to URP Lit Shader")]
        public static void RevertToURPLit()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(CharacterMatPath);
            if (mat == null) return;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) return;

            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorTexturePath);

            Undo.RecordObject(mat, "Revert Player to URP Lit");
            mat.shader = litShader;
            if (baseColor != null)
            {
                mat.SetTexture("_BaseMap", baseColor);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#ffd200><b>[SetupPlayerToonShader] Reverted '{mat.name}' to 'Universal Render Pipeline/Lit'.</b></color>");
        }
    }
}
