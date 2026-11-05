using UnityEngine;
using UnityEditor;

public static class ApplyGhibliAnimeRockColor
{
    private const string TargetMatPath = "Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat";
    private const string ShaderName = "HoaVaMau/GhibliAnimeCliffRock";
    private const string StoneTexPath = "Assets/Textures/Meshy_AI_Moss_Crowned_Stone_1007092109_texture.png";

    [MenuItem("Tools/Hoa Do/Apply Ghibli Anime Rock Color")]
    public static void Execute()
    {
        Debug.Log("[ApplyGhibliAnimeRockColor] Starting Ghibli rock tuning...");

        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[ApplyGhibliAnimeRockColor] Could not find shader {ShaderName}!");
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(TargetMatPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, TargetMatPath);
        }
        else
        {
            mat.shader = shader;
        }

        // Texture
        var stoneTex = AssetDatabase.LoadAssetAtPath<Texture2D>(StoneTexPath);
        if (stoneTex != null)
        {
            mat.SetTexture("_MainTex", stoneTex);
        }

        // Color palette sampled directly from anime reference image
        // Highlights: warm pale limestone grey with soft sage-yellow warmth
        mat.SetColor("_BaseColor", new Color(0.64f, 0.67f, 0.58f, 1.0f));
        // Midtones: muted slate stone body
        mat.SetColor("_MidtoneColor", new Color(0.42f, 0.49f, 0.45f, 1.0f));
        // Shadows: deep cool teal-slate umber crevices
        mat.SetColor("_ShadowColor", new Color(0.12f, 0.22f, 0.24f, 1.0f));

        // Chiseled Anime Facets
        mat.SetFloat("_FacetStrength", 0.72f);

        // Top Shelf Lichen Ledge (only near-horizontal ledges catch lichen, soft weathered sage)
        mat.SetFloat("_UseTopOverlay", 1.0f);
        mat.SetColor("_TopColor", new Color(0.52f, 0.58f, 0.44f, 1.0f));
        mat.SetColor("_TopShadowColor", new Color(0.24f, 0.32f, 0.25f, 1.0f));
        mat.SetFloat("_TopNormalThreshold", 0.68f);
        mat.SetFloat("_TopBlendSmoothness", 0.12f);

        // Triplanar Stone Detail
        mat.SetFloat("_DetailScale", 0.22f);
        mat.SetFloat("_DetailStrength", 0.55f);

        // Vertical Cliff Striations (anime watercolor weathering)
        mat.SetFloat("_VerticalBandingFreq", 0.45f);
        mat.SetFloat("_VerticalBandingStrength", 0.22f);

        // Crevice Cracks
        mat.SetFloat("_CrackScale", 0.35f);
        mat.SetFloat("_CrackStrength", 0.28f);

        // Cel Shading & Soft Sunlight Rim
        mat.SetFloat("_CelRampThreshold", 0.36f);
        mat.SetFloat("_CelRampSmoothness", 0.18f);
        mat.SetColor("_RimColor", new Color(0.85f, 0.88f, 0.75f, 1.0f));
        mat.SetFloat("_RimPower", 4.0f);
        mat.SetFloat("_RimStrength", 0.24f);

        // Painterly Noise
        mat.SetFloat("_NoiseScale", 2.8f);
        mat.SetFloat("_NoiseStrength", 0.10f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // Ensure all scene rocks use this material
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int rockCount = 0;
        foreach (var r in renderers)
        {
            if (r.name.Contains("Rock") || r.name.Contains("Mtn") || r.name.Contains("Mountain") || r.name.Contains("Cliff"))
            {
                r.sharedMaterial = mat;
                EditorUtility.SetDirty(r);
                rockCount++;
            }
        }

        Debug.Log($"[ApplyGhibliAnimeRockColor] Successfully tuned M_Mountain_Crag_Rock and assigned to {rockCount} scene rocks!");
    }
}
