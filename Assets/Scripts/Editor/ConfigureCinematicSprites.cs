using UnityEditor;
using UnityEngine;

namespace HoaVaMau.Editor
{
    public static class ConfigureCinematicSprites
    {
        [MenuItem("Tools/Hoa Va Mau/Import & Configure Cinematic Sprites")]
        public static void ConfigureSprites()
        {
            string[] paths = new string[]
            {
                "Assets/Textures/Cinematics/HistorySlides/history_1.jpg",
                "Assets/Textures/Cinematics/HistorySlides/history_2.jpg",
                "Assets/Textures/Cinematics/HistorySlides/history_3.jpg",
                "Assets/Textures/Cinematics/HistorySlides/history_4.jpg",
                "Assets/Textures/UI/T_Icon_Headphone_BW.png",
                "Assets/Textures/UI/T_Film_Vignette.png",
                "Assets/Textures/UI/T_Film_Scratches.png"
            };

            foreach (var path in paths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = path.EndsWith(".png");
                    importer.sRGBTexture = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    Debug.Log($"[ConfigureCinematicSprites] Configured sprite at {path}");
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
