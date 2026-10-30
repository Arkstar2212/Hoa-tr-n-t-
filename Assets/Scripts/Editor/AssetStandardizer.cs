using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoaVaMau.Editor
{
    public static class AssetStandardizer
    {
        private static readonly Dictionary<string, string> MoveMap = new Dictionary<string, string>
        {
            // ========================================================
            // 1. MODELS -> Categorized (Characters, Environment, Props, Vehicles)
            // ========================================================
            { "Assets/Models/nhan vat chinh 3d.fbx", "Assets/Models/Characters/Character_An.fbx" },
            { "Assets/Models/nhan vat 3d test.fbx", "Assets/Models/Characters/Character_Test.fbx" },
            { "Assets/Models/cha_rigged_animations.fbx", "Assets/Models/Characters/Character_Rigged_Animations.fbx" },

            { "Assets/Models/Nhà Sàn Lá.fbx", "Assets/Models/Environment/SM_StiltedHouse_Thatched.fbx" },
            { "Assets/Models/Meshy_AI_Stilted_Wooden_House_0929042822_texture.fbx", "Assets/Models/Environment/SM_StiltedHouse_Wood.fbx" },
            { "Assets/Models/Đá Tảng Rêu 1.fbx", "Assets/Models/Environment/SM_MossyBoulder_01.fbx" },
            { "Assets/Models/Meshy_AI_Lone_Boulder_0928161951_texture.fbx", "Assets/Models/Environment/SM_LoneBoulder.fbx" },
            { "Assets/Models/Rock001.fbx", "Assets/Models/Environment/SM_Rock_Small_01.fbx" },
            { "Assets/Models/Rock0022.fbx", "Assets/Models/Environment/SM_Rock_Small_02.fbx" },
            { "Assets/Models/Viên Đá 1.fbx", "Assets/Models/Environment/SM_Rock_Medium_01.fbx" },
            { "Assets/Models/Viên Đá 2.fbx", "Assets/Models/Environment/SM_Rock_Medium_02.fbx" },
            { "Assets/Models/Viên Đá 3.fbx", "Assets/Models/Environment/SM_Rock_Large_01.fbx" },

            { "Assets/Models/Thùng gỗ.fbx", "Assets/Models/Props/SM_WoodenCrate.fbx" },
            { "Assets/Models/Meshy_AI_Wooden_Slat_Cube_0927180014_texture.fbx", "Assets/Models/Props/SM_WoodenSlatCube.fbx" },

            { "Assets/Models/Stylized_Ship.fbx", "Assets/Models/Vehicles/SM_StylizedShip.fbx" },
            { "Assets/Models/shipmain.blend", "Assets/Models/Vehicles/SM_StylizedShip_Source.blend" },
            { "Assets/Models/Meshy_AI_Forest_Patrol_Jeep_0927160125_texture.fbx", "Assets/Models/Vehicles/SM_PatrolJeep.fbx" },

            // Controllers previously in Models



            { "Assets/Animations/Hero/An_Hero_AnimatorController.controller", "Assets/Animations/Controllers/AC_Player_An.controller" },

            // ========================================================
            // 2. PREFABS -> Categorized (Characters, Environment, Foliage, Props, Vehicles)
            // ========================================================
            { "Assets/Prefabs/Player_An.prefab", "Assets/Prefabs/Characters/PF_Player_An.prefab" },


            { "Assets/Prefabs/NhaSanGo.prefab", "Assets/Prefabs/Environment/PF_StiltedHouse_Wood.prefab" },
            { "Assets/Prefabs/Nhà Sàn Lá.prefab", "Assets/Prefabs/Environment/PF_StiltedHouse_Thatched.prefab" },
            { "Assets/Prefabs/Meshy_AI_Lone_Boulder_0928161951_texture.prefab", "Assets/Prefabs/Environment/PF_LoneBoulder.prefab" },
            { "Assets/Prefabs/Viên Đá 3.prefab", "Assets/Prefabs/Environment/PF_Rock_Large_01.prefab" },
            { "Assets/Prefabs/Đá Tảng Rêu.prefab", "Assets/Prefabs/Environment/PF_MossyBoulder.prefab" },
            { "Assets/Prefabs/Branch_Cluster_Tree.prefab", "Assets/Prefabs/Environment/Trees/PF_Branch_Cluster_Tree.prefab" },
            { "Assets/Prefabs/Hero_Layered_Tree.prefab", "Assets/Prefabs/Environment/Trees/PF_Hero_Layered_Tree.prefab" },
            { "Assets/Prefabs/Lana_Artistic_Hero_Tree.prefab", "Assets/Prefabs/Environment/Trees/PF_Lana_Artistic_Hero_Tree.prefab" },
            { "Assets/Prefabs/Tree_GocCay2_Artistic.prefab", "Assets/Prefabs/Environment/Trees/PF_Tree_GocCay2_Artistic.prefab" },

            { "Assets/Prefabs/PF_StylizedGrassTuft_3D.prefab", "Assets/Prefabs/Foliage/PF_StylizedGrassTuft_3D.prefab" },
            { "Assets/Prefabs/PF_StylizedGrassTuft_MeadowCluster.prefab", "Assets/Prefabs/Foliage/PF_StylizedGrassTuft_MeadowCluster.prefab" },

            { "Assets/Prefabs/Thùng gỗ.prefab", "Assets/Prefabs/Props/PF_WoodenCrate.prefab" },
            { "Assets/Prefabs/Meshy_AI_Wooden_Slat_Cube_0927180014_texture.prefab", "Assets/Prefabs/Props/PF_WoodenSlatCube.prefab" },
            { "Assets/Prefabs/Cylinder.prefab", "Assets/Prefabs/Props/PF_Cylinder_Marker.prefab" },

            { "Assets/Prefabs/PF_StylizedBoat.prefab", "Assets/Prefabs/Vehicles/PF_StylizedBoat.prefab" },
            { "Assets/Prefabs/Xe jeep.prefab", "Assets/Prefabs/Vehicles/PF_PatrolJeep.prefab" },

            // ========================================================
            // 3. MATERIALS -> Categorized (Characters, Environment, Foliage, Props, Vehicles, VFX, Unused)
            // ========================================================
            // Characters
            { "Assets/Materials/M_Character_Silhouette.mat", "Assets/Materials/Characters/M_Character_Silhouette.mat" },
            { "Assets/Materials/M_Playground_PlayerTorso.mat", "Assets/Materials/Characters/M_Playground_PlayerTorso.mat" },
            { "Assets/Materials/M_Playground_Skin.mat", "Assets/Materials/Characters/M_Playground_Skin.mat" },
            { "Assets/Materials/M_Playground_Headband.mat", "Assets/Materials/Characters/M_Playground_Headband.mat" },

            // Foliage
            { "Assets/Materials/Mat_Grass_Back.mat", "Assets/Materials/Foliage/Mat_Grass_Back.mat" },
            { "Assets/Materials/Mat_Grass_Front.mat", "Assets/Materials/Foliage/Mat_Grass_Front.mat" },
            { "Assets/Materials/Mat_Grass_Middle.mat", "Assets/Materials/Foliage/Mat_Grass_Middle.mat" },
            { "Assets/Materials/Mat_Grass_StealthReeds.mat", "Assets/Materials/Foliage/Mat_Grass_StealthReeds.mat" },
            { "Assets/Materials/Mat_PainterlyGrass_Tuft_Leaning.mat", "Assets/Materials/Foliage/Mat_PainterlyGrass_Tuft_Leaning.mat" },
            { "Assets/Materials/Mat_PainterlyGrass_Tuft_Upright.mat", "Assets/Materials/Foliage/Mat_PainterlyGrass_Tuft_Upright.mat" },
            { "Assets/Materials/M_PlanetOfLana_Tree.mat", "Assets/Materials/Foliage/M_PlanetOfLana_Tree.mat" },
            { "Assets/Materials/M_Test3D_Foliage.mat", "Assets/Materials/Foliage/M_Test3D_Foliage.mat" },
            { "Assets/Materials/M_Test3D_Grass.mat", "Assets/Materials/Foliage/M_Test3D_Grass.mat" },
            { "Assets/Materials/la 1.mat", "Assets/Materials/Foliage/M_Leaves_Layer1.mat" },
            { "Assets/Materials/la 2.mat", "Assets/Materials/Foliage/M_Leaves_Layer2.mat" },
            { "Assets/Materials/cay xoa phong.mat", "Assets/Materials/Foliage/M_Tree_Defoliated.mat" },
            { "Assets/Materials/goc cay.mat", "Assets/Materials/Foliage/M_TreeTrunk.mat" },
            { "Assets/Materials/M_Framing_Branch_TL.mat", "Assets/Materials/Foliage/M_Framing_Branch_TL.mat" },
            { "Assets/Materials/M_Framing_Foliage_Bottom.mat", "Assets/Materials/Foliage/M_Framing_Foliage_Bottom.mat" },
            { "Assets/Materials/M_Framing_Palm_TR.mat", "Assets/Materials/Foliage/M_Framing_Palm_TR.mat" },
            { "Assets/Materials/M_Framing_Sprite.mat", "Assets/Materials/Foliage/M_Framing_Sprite.mat" },

            // Environment / Rocks
            { "Assets/Materials/ĐaTangReu.mat", "Assets/Materials/Environment/Rocks/M_Rock_MossyBoulder.mat" },
            { "Assets/Materials/Mat_VienDa2.mat", "Assets/Materials/Environment/Rocks/M_Rock_Medium_02.mat" },
            { "Assets/Materials/VienDa2_Platfom.mat", "Assets/Materials/Environment/Rocks/M_Rock_Medium_02_Platform.mat" },
            { "Assets/Materials/Viên Đá 1.mat", "Assets/Materials/Environment/Rocks/M_Rock_Medium_01.mat" },
            { "Assets/Materials/Vien Da 4.mat", "Assets/Materials/Environment/Rocks/M_Rock_Large_01.mat" },
            { "Assets/Materials/M_StylizedRock_Silhouette.mat", "Assets/Materials/Environment/Rocks/M_StylizedRock_Silhouette.mat" },
            { "Assets/Materials/M_Playground_Cliff.mat", "Assets/Materials/Environment/Rocks/M_Playground_Cliff.mat" },
            { "Assets/Materials/M_Playground_Altar.mat", "Assets/Materials/Environment/Rocks/M_Playground_Altar.mat" },
            { "Assets/Materials/M_Test3D_Relic.mat", "Assets/Materials/Environment/Rocks/M_Test3D_Relic.mat" },

            // Environment / Architecture
            { "Assets/Materials/NhaSanLa.mat", "Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat" },

            // Environment / Ground
            { "Assets/Materials/M_Playground_Ground.mat", "Assets/Materials/Environment/Ground/M_Playground_Ground.mat" },
            { "Assets/Materials/M_Foreground_Ground.mat", "Assets/Materials/Environment/Ground/M_Foreground_Ground.mat" },
            { "Assets/Materials/M_Foreground_Silhouette.mat", "Assets/Materials/Environment/Ground/M_Foreground_Silhouette.mat" },
            { "Assets/Materials/M_Test3D_Dirt.mat", "Assets/Materials/Environment/Ground/M_Test3D_Dirt.mat" },

            // Environment / Backdrop & Sky
            { "Assets/Materials/Mat_Sky_Background.mat", "Assets/Materials/Environment/Backdrop_Sky/M_Sky_Background.mat" },
            { "Assets/Materials/M_BG_Far.mat", "Assets/Materials/Environment/Backdrop_Sky/M_BG_Far.mat" },
            { "Assets/Materials/M_BG_Mid.mat", "Assets/Materials/Environment/Backdrop_Sky/M_BG_Mid.mat" },
            { "Assets/Materials/M_DistantClouds.mat", "Assets/Materials/Environment/Backdrop_Sky/M_DistantClouds.mat" },
            { "Assets/Materials/M_DistantMountains.mat", "Assets/Materials/Environment/Backdrop_Sky/M_DistantMountains.mat" },
            { "Assets/Materials/M_StylizedSky_Sunset.mat", "Assets/Materials/Environment/Backdrop_Sky/M_StylizedSky_Sunset.mat" },
            { "Assets/Materials/M_StylizedSky_Twilight.mat", "Assets/Materials/Environment/Backdrop_Sky/M_StylizedSky_Twilight.mat" },
            { "Assets/Materials/M_StylizedSun_Sunset.mat", "Assets/Materials/Environment/Backdrop_Sky/M_StylizedSun_Sunset.mat" },
            { "Assets/Materials/M_StylizedSun_Twilight.mat", "Assets/Materials/Environment/Backdrop_Sky/M_StylizedSun_Twilight.mat" },
            { "Assets/Materials/sau 1.mat", "Assets/Materials/Environment/Backdrop_Sky/M_Layer_Back_01.mat" },
            { "Assets/Materials/trung 1.mat", "Assets/Materials/Environment/Backdrop_Sky/M_Layer_Mid_01.mat" },
            { "Assets/Materials/truoc 1.mat", "Assets/Materials/Environment/Backdrop_Sky/M_Layer_Fore_01.mat" },

            // Environment / Water
            { "Assets/Materials/M_StylizedWater_Sunset.mat", "Assets/Materials/Environment/Water/M_StylizedWater_Sunset.mat" },
            { "Assets/Materials/M_StylizedWater_Twilight.mat", "Assets/Materials/Environment/Water/M_StylizedWater_Twilight.mat" },
            { "Assets/Materials/M_Test3D_Water.mat", "Assets/Materials/Environment/Water/M_Test3D_Water.mat" },

            // Environment / Ca Mau
            { "Assets/Materials/M_CaMau_Bamboo.mat", "Assets/Materials/Environment/CaMau/M_CaMau_Bamboo.mat" },
            { "Assets/Materials/M_CaMau_BoatHull.mat", "Assets/Materials/Environment/CaMau/M_CaMau_BoatHull.mat" },
            { "Assets/Materials/M_CaMau_BoatSail.mat", "Assets/Materials/Environment/CaMau/M_CaMau_BoatSail.mat" },
            { "Assets/Materials/M_CaMau_ColonialShutter.mat", "Assets/Materials/Environment/CaMau/M_CaMau_ColonialShutter.mat" },
            { "Assets/Materials/M_CaMau_ColonialWall.mat", "Assets/Materials/Environment/CaMau/M_CaMau_ColonialWall.mat" },
            { "Assets/Materials/M_CaMau_DarkSilhouette.mat", "Assets/Materials/Environment/CaMau/M_CaMau_DarkSilhouette.mat" },
            { "Assets/Materials/M_CaMau_ForegroundSil.mat", "Assets/Materials/Environment/CaMau/M_CaMau_ForegroundSil.mat" },
            { "Assets/Materials/M_CaMau_MangroveBark.mat", "Assets/Materials/Environment/CaMau/M_CaMau_MangroveBark.mat" },
            { "Assets/Materials/M_CaMau_MangroveLeaves.mat", "Assets/Materials/Environment/CaMau/M_CaMau_MangroveLeaves.mat" },
            { "Assets/Materials/M_CaMau_OldTileRoof.mat", "Assets/Materials/Environment/CaMau/M_CaMau_OldTileRoof.mat" },
            { "Assets/Materials/M_CaMau_PalmLeaves.mat", "Assets/Materials/Environment/CaMau/M_CaMau_PalmLeaves.mat" },
            { "Assets/Materials/M_CaMau_RiverMud.mat", "Assets/Materials/Environment/CaMau/M_CaMau_RiverMud.mat" },
            { "Assets/Materials/M_CaMau_Rope.mat", "Assets/Materials/Environment/CaMau/M_CaMau_Rope.mat" },
            { "Assets/Materials/M_CaMau_ThatchRoof.mat", "Assets/Materials/Environment/CaMau/M_CaMau_ThatchRoof.mat" },
            { "Assets/Materials/M_CaMau_TimberDark.mat", "Assets/Materials/Environment/CaMau/M_CaMau_TimberDark.mat" },
            { "Assets/Materials/M_CaMau_TimberLight.mat", "Assets/Materials/Environment/CaMau/M_CaMau_TimberLight.mat" },
            { "Assets/Materials/M_CaMau_WetPiling.mat", "Assets/Materials/Environment/CaMau/M_CaMau_WetPiling.mat" },

            // Props
            { "Assets/Materials/MatThungGo.mat", "Assets/Materials/Props/M_WoodenCrate_01.mat" },
            { "Assets/Materials/MatThungGo 1.mat", "Assets/Materials/Props/M_WoodenCrate_02.mat" },
            { "Assets/Materials/Thung Go Vuong.mat", "Assets/Materials/Props/M_WoodenCrate_Square.mat" },
            { "Assets/Materials/M_Playground_Crate.mat", "Assets/Materials/Props/M_Playground_Crate.mat" },
            { "Assets/Materials/M_Playground_Timber.mat", "Assets/Materials/Props/M_Playground_Timber.mat" },
            { "Assets/Materials/M_Playground_Iron.mat", "Assets/Materials/Props/M_Playground_Iron.mat" },
            { "Assets/Materials/M_Playground_Orb.mat", "Assets/Materials/Props/M_Playground_Orb.mat" },

            // Vehicles
            { "Assets/Materials/M_Boat_Wood.mat", "Assets/Materials/Vehicles/M_Boat_Wood.mat" },
            { "Assets/Materials/M_Boat_Sail.mat", "Assets/Materials/Vehicles/M_Boat_Sail.mat" },
            { "Assets/Materials/M_Boat_Lantern.mat", "Assets/Materials/Vehicles/M_Boat_Lantern.mat" },
            { "Assets/Materials/Xe jeep.mat", "Assets/Materials/Vehicles/M_PatrolJeep.mat" },

            // VFX
            { "Assets/Materials/M_GoldenMotes.mat", "Assets/Materials/VFX/M_GoldenMotes.mat" },
            { "Assets/Materials/M_StylizedGodRays.mat", "Assets/Materials/VFX/M_StylizedGodRays.mat" },
            { "Assets/Materials/M_Framing_GodRays.mat", "Assets/Materials/VFX/M_Framing_GodRays.mat" },

            // Unused / Temp
            { "Assets/Materials/5843d38f-ab5e-4526-af2d-cd3566eaa361-removebg-preview.mat", "Assets/Materials/Unused/M_Temp_Cutout_TreeCanopy.mat" },
            { "Assets/Materials/mau.mat", "Assets/Materials/Unused/M_Temp_Color_Palette.mat" },
            { "Assets/Materials/New Material (Shaders).mat", "Assets/Materials/Unused/M_Temp_ShaderTest.mat" },
            { "Assets/Materials/New Material.mat", "Assets/Materials/Unused/M_Temp_Default.mat" },

            // ========================================================
            // 4. SCENES with Diacritics -> Standardized English
            // ========================================================
            { "Assets/Scenes/canh 2d.unity", "Assets/Scenes/Scene_2D_Sunset_Art.unity" },
            { "Assets/Scenes/man hinh chinh.unity", "Assets/Scenes/Scene_MainMenu.unity" },
            { "Assets/Scenes/platform test.unity", "Assets/Scenes/Scene_Platform_Test.unity" },
            { "Assets/Scenes/test cay coi.unity", "Assets/Scenes/Scene_Tree_Test.unity" },

            // ========================================================
            // 5. TEXTURES in unsure/Cropped_And_Mock -> Standardized Names
            // ========================================================
            { "Assets/Textures/unsure/Cropped_And_Mock/cay xoa phong.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Tree_Defoliated_Alpha.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/crop_target_cloud.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cloud_Target_Crop.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/Firefly_RemoveBackground.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Firefly_Alpha.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/1265d5a6-473c-479a-b6b2-b1c02e4a2c4c-removebg-preview.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_TreeCanopy_01.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/2606cce3-f047-4c8f-b68a-508cad9fe8b8-removebg-preview.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_TreeCanopy_02.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/5843d38f-ab5e-4526-af2d-cd3566eaa361-removebg-preview.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_TreeCanopy_03.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/2aOboR0QAstNNXTNmLooaPtw1Yf9bp95WmjybyXQ-removebg-preview.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_FramingBranch_01.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/2aOboR0QAt2s4NL7abL2rzBHQnySH91RO5HSM9dw-removebg-preview.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_FramingBranch_02.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/2aOboR1po0Bt4gCKniI4o7ujfKzZFzlveIvLJwCO-removebg-preview.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_Pier_01.png" },
            { "Assets/Textures/unsure/Cropped_And_Mock/2aOboR05aywC5SWuYR2KiqY2iY4Av1cLTLv6r2m0.png", "Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_Pier_02.png" }
        };

        [MenuItem("Hoa va Mau/Standardize & Organize All Assets")]
        public static void Run()
        {
            Debug.Log("<color=#38bdf8>[AssetStandardizer]</color> Starting Asset Standardization & Reorganization...");

            // 1. Pre-pass: Ensure all destination directories exist in AssetDatabase
            var uniqueDirs = new HashSet<string>();
            foreach (var kvp in MoveMap)
            {
                string destDir = Path.GetDirectoryName(kvp.Value).Replace('\\', '/');
                if (!string.IsNullOrEmpty(destDir))
                {
                    uniqueDirs.Add(destDir);
                }
            }

            foreach (var dir in uniqueDirs)
            {
                EnsureFolderExists(dir);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            int moveCount = 0;
            int skipCount = 0;
            int errorCount = 0;

            foreach (var kvp in MoveMap)
            {
                string oldPath = ResolveExistingPath(kvp.Key);
                string newPath = kvp.Value.Replace('\\', '/');

                if (!File.Exists(oldPath) && !Directory.Exists(oldPath))
                {
                    skipCount++;
                    continue;
                }

                if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    skipCount++;
                    continue;
                }

                if (File.Exists(newPath))
                {
                    Debug.LogWarning($"<color=#f59e0b>[Already Exists]</color> Target {newPath} already exists, skipping move from {oldPath}");
                    skipCount++;
                    continue;
                }

                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (string.IsNullOrEmpty(error))
                {
                    moveCount++;
                    Debug.Log($"<color=#4ade80>[Moved]</color> {oldPath} -> {newPath}");
                }
                else
                {
                    errorCount++;
                    Debug.LogError($"<color=#ef4444>[Move Error]</color> {oldPath} -> {newPath}: {error}");
                }
            }

            Debug.Log($"<color=#38bdf8>[AssetStandardizer]</color> Moves completed: {moveCount} moved, {skipCount} skipped, {errorCount} errors.");

            // Update C# Script References
            UpdateScriptReferences();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("<color=#40b8a6>[AssetStandardizer]</color> Asset Standardization & Reorganization fully finished!");
        }

        private static void EnsureFolderExists(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folderPath) || folderPath == "Assets") return;
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            int lastSlash = folderPath.LastIndexOf('/');
            if (lastSlash > 0)
            {
                string parent = folderPath.Substring(0, lastSlash);
                string leaf = folderPath.Substring(lastSlash + 1);
                EnsureFolderExists(parent);
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    if (!Directory.Exists(folderPath))
                    {
                        AssetDatabase.CreateFolder(parent, leaf);
                    }
                    else
                    {
                        AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport);
                    }
                }
            }
        }

        private static string ResolveExistingPath(string path)
        {
            path = path.Replace('\\', '/');
            if (File.Exists(path) || Directory.Exists(path)) return path;

            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return path;

            string targetName = Path.GetFileName(path).Normalize(System.Text.NormalizationForm.FormC);
            foreach (var file in Directory.GetFiles(dir))
            {
                string fn = Path.GetFileName(file).Normalize(System.Text.NormalizationForm.FormC);
                if (string.Equals(fn, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return file.Replace('\\', '/');
                }
            }
            return path;
        }

        private static void UpdateScriptReferences()
        {
            string scriptsDir = "Assets/Scripts";
            if (!Directory.Exists(scriptsDir)) return;

            string[] csFiles = Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories);
            int updatedFileCount = 0;

            foreach (string file in csFiles)
            {
                // Skip the standardizer tool itself
                if (file.EndsWith("AssetStandardizer.cs", StringComparison.OrdinalIgnoreCase)) continue;

                string content = File.ReadAllText(file);
                bool modified = false;

                foreach (var kvp in MoveMap)
                {
                    string oldPath = kvp.Key;
                    string newPath = kvp.Value;

                    if (content.Contains(oldPath))
                    {
                        content = content.Replace(oldPath, newPath);
                        modified = true;
                    }
                }

                if (modified)
                {
                    File.WriteAllText(file, content);
                    updatedFileCount++;
                    Debug.Log($"<color=#facc15>[Script Updated]</color> {file}");
                }
            }

            Debug.Log($"<color=#38bdf8>[AssetStandardizer]</color> Updated {updatedFileCount} scripts with new asset paths.");
        }
    }
}