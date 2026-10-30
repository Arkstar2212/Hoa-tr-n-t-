using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoaDo.EditorTools
{
    public static class CinematicAtmosphereAndFoliageBuilder
    {
        private const string LanaArtBasePath = "Assets/Art/PlanetOfLana_Official";

        [MenuItem("Tools/Planet of Lana/3. Apply Cinematic Atmosphere & Interactive Grass", priority = 30)]
        public static void BuildCinematicAtmosphereAndFoliage()
        {
            // 1. Ensure PlayerShaderPositionTracker is in the scene
            EnsurePlayerPositionTracker();

            // 2. Configure Interactive Grass Material
            Material interactiveGrassMat = GetOrCreateInteractiveGrassMaterial();

            // 3. Update Platform Road Material with Red Basalt Dirt (Đất đỏ bazan)
            UpdatePlatformMaterialRedBasalt();

            // 4. Build or update [Cinematic_Atmosphere_Overhaul]
            BuildAtmosphericLightingAndHaze();

            // 5. Restage Platform Foliage with Continuous Ribbon & Interactive Material
            RestagePlatformWithInteractiveFoliage(interactiveGrassMat);

            // 6. Build Top Canopy Vignette & Framing
            BuildTopCanopyFraming();

            // Save Scene
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log("[CinematicAtmosphereAndFoliageBuilder] Successfully applied Planet of Lana cinematic atmosphere, God rays, atmospheric haze, red basalt soil, and interactive grass!");
        }

        private static void EnsurePlayerPositionTracker()
        {
            var tracker = GameObject.FindAnyObjectByType<HoaDo.Environment.PlayerShaderPositionTracker>();
            if (tracker == null)
            {
                var envSystems = GameObject.Find("10_EnvironmentSystems");
                if (envSystems != null)
                {
                    tracker = envSystems.AddComponent<HoaDo.Environment.PlayerShaderPositionTracker>();
                    Undo.RegisterCreatedObjectUndo(tracker, "Add PlayerShaderPositionTracker");
                }
                else
                {
                    var go = new GameObject("PlayerShaderPositionTracker");
                    tracker = go.AddComponent<HoaDo.Environment.PlayerShaderPositionTracker>();
                    Undo.RegisterCreatedObjectUndo(go, "Create PlayerShaderPositionTracker");
                }
            }
        }

        private static Material GetOrCreateInteractiveGrassMaterial()
        {
            string matPath = "Assets/Materials/M_Lana_SpriteDefault.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");

            if (mat == null)
            {
                mat = new Material(shader) { name = "M_Lana_SpriteDefault" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static void UpdatePlatformMaterialRedBasalt()
        {
            string matPath = "Assets/Materials/M_Lana_Road3D_DualSurface.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                // Rich red basalt earth (Đất đỏ bazan Nam Bộ)
                Color redBasalt = new Color(0.55f, 0.27f, 0.14f, 1.0f);
                mat.SetColor("_WornTrailColor", redBasalt);
                mat.SetFloat("_TrailBlend", 0.52f);
                mat.SetFloat("_UseMeshUV", 1.0f);
                mat.SetVector("_SoilTexTiling", new Vector4(0.35f, 1.0f, 0f, 0f));
                EditorUtility.SetDirty(mat);
            }
        }

        private static void BuildAtmosphericLightingAndHaze()
        {
            // Parent container under 08_Lighting or root
            GameObject atmoRoot = GameObject.Find("[Cinematic_Atmosphere_Overhaul]");
            if (atmoRoot != null)
            {
                Undo.DestroyObjectImmediate(atmoRoot);
            }

            atmoRoot = new GameObject("[Cinematic_Atmosphere_Overhaul]");
            Undo.RegisterCreatedObjectUndo(atmoRoot, "Create [Cinematic_Atmosphere_Overhaul]");
            atmoRoot.transform.position = Vector3.zero;

            // 1. GOD RAYS (Volumetric sunbeam angled at 35 degrees shining directly on Minh)
            // Minh stands at X: 0.0, Y: -1.9, Z: 0.0
            GameObject godRayObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(godRayObj, "Create God Rays Quad");
            godRayObj.name = "GodRays_HeroSpotlight";
            godRayObj.transform.SetParent(atmoRoot.transform, true);

            godRayObj.transform.position = new Vector3(-0.4f, 0.6f, -0.2f);
            godRayObj.transform.localScale = new Vector3(4.2f, 8.8f, 1f);
            godRayObj.transform.rotation = Quaternion.Euler(0, 0, -34f); // Angled 34 deg

            var col = godRayObj.GetComponent<Collider>();
            if (col != null) Undo.DestroyObjectImmediate(col);

            Shader godRayShader = Shader.Find("HoaVaMau/StylizedGodRays");
            string rayMatPath = "Assets/Materials/M_Lana_HeroGodRays.mat";
            Material rayMat = AssetDatabase.LoadAssetAtPath<Material>(rayMatPath);
            if (rayMat == null && godRayShader != null)
            {
                rayMat = new Material(godRayShader);
                AssetDatabase.CreateAsset(rayMat, rayMatPath);
            }
            else if (rayMat != null && godRayShader != null)
            {
                rayMat.shader = godRayShader;
            }

            if (rayMat != null)
            {
                rayMat.SetColor("_RayColor", new Color(1.0f, 0.92f, 0.72f, 0.42f));
                rayMat.SetFloat("_RayIntensity", 1.45f);
                rayMat.SetFloat("_ShimmerSpeed", 0.5f);
                rayMat.SetFloat("_FalloffPower", 1.4f);
                rayMat.SetFloat("_BeamCount", 3.2f);
                EditorUtility.SetDirty(rayMat);

                var r = godRayObj.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = rayMat;
                    r.sortingLayerName = "Environment";
                    r.sortingOrder = 35; // Bathes player and ground in light
                }
            }

            // 2. ATMOSPHERIC HAZE FOG PLANE (Transparent mist softening the background hills)
            GameObject hazeObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(hazeObj, "Create Atmospheric Haze Quad");
            hazeObj.name = "Atmospheric_Haze_Midground";
            hazeObj.transform.SetParent(atmoRoot.transform, true);

            hazeObj.transform.position = new Vector3(0.0f, 0.8f, 3.5f);
            hazeObj.transform.localScale = new Vector3(36.0f, 14.0f, 1f);
            hazeObj.transform.rotation = Quaternion.identity;

            var hazeCol = hazeObj.GetComponent<Collider>();
            if (hazeCol != null) Undo.DestroyObjectImmediate(hazeCol);

            string hazeMatPath = "Assets/Materials/M_Lana_AtmosphericHazePlane.mat";
            Material hazeMat = AssetDatabase.LoadAssetAtPath<Material>(hazeMatPath);
            Shader hazeShader = Shader.Find("HoaVaMau/LanaAtmosphericHaze");
            if (hazeMat == null && hazeShader != null)
            {
                hazeMat = new Material(hazeShader);
                AssetDatabase.CreateAsset(hazeMat, hazeMatPath);
            }
            else if (hazeMat != null && hazeShader != null)
            {
                hazeMat.shader = hazeShader;
            }

            if (hazeMat != null)
            {
                // Soft pale sky-cyan atmospheric haze (transparent, letting background hills breathe)
                Color hazeColor = new Color(0.68f, 0.84f, 0.90f, 0.32f);
                hazeMat.SetColor("_HazeColor", hazeColor);
                hazeMat.SetFloat("_BottomFade", 0.12f);
                hazeMat.SetFloat("_TopFade", 0.88f);
                EditorUtility.SetDirty(hazeMat);

                var r = hazeObj.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = hazeMat;
                    r.sortingLayerName = "Environment";
                    r.sortingOrder = 3; // In front of background, behind midground trees
                }
            }

            // 3. FLOATING DUST MOTES (Particles in the sunbeam around Minh)
            GameObject dustGo = new GameObject("DustMotes_HeroParticles");
            dustGo.transform.SetParent(atmoRoot.transform, true);
            dustGo.transform.position = new Vector3(0.0f, -0.8f, -0.1f);

            var ps = dustGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 40;
            main.startLifetime = 6.0f;
            main.startSpeed = 0.08f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new Color(1.0f, 0.95f, 0.80f, 0.65f); // Warm golden motes
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 6.0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(4.0f, 3.5f, 1.5f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.04f);
            vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.92f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.96f, 0.85f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = grad;

            var psr = dustGo.GetComponent<ParticleSystemRenderer>();
            psr.sortingLayerName = "Environment";
            psr.sortingOrder = 30;
        }

        private static void RestagePlatformWithInteractiveFoliage(Material interactiveMat)
        {
            GameObject ledge01 = GameObject.Find("Ledge_01_ForestStart");
            if (ledge01 == null) return;

            Transform foliageParent = ledge01.transform.Find("[Official_Lana_Foliage]");
            if (foliageParent != null)
            {
                Undo.DestroyObjectImmediate(foliageParent.gameObject);
            }

            GameObject foliageGO = new GameObject("[Official_Lana_Foliage]");
            Undo.RegisterCreatedObjectUndo(foliageGO, "Create [Official_Lana_Foliage]");
            foliageGO.transform.SetParent(ledge01.transform, true);
            foliageGO.transform.position = Vector3.zero;

            // Load official sprites
            Sprite yellowButtercup = LoadSpriteByPattern("RockFlowerYellow01") ?? LoadSpriteByPattern("RockFlowerYellow04");
            Sprite yellowButtercupAlt = LoadSpriteByPattern("RockFlowerYellow07") ?? LoadSpriteByPattern("RockFlowerYellow05");
            Sprite whiteDaisy = LoadSpriteByPattern("RockFlowerWhitePetal01") ?? LoadSpriteByPattern("RockFlowerWhite01");
            Sprite whiteSmallSprig = LoadSpriteByPattern("Std_Pg_WhiteSmallFlower02") ?? LoadSpriteByPattern("RockFlowerWhite02");
            Sprite broadleafWeed = LoadSpriteByPattern("Std_RockPlantWeed01") ?? LoadSpriteByPattern("Std_RockPlantWeed02");
            Sprite rockWeedAlt = LoadSpriteByPattern("Std_RockPlantWeed03") ?? LoadSpriteByPattern("Std_RockPlantWeed04");
            Sprite forestGrassLight = LoadSpriteByPattern("Std_Pg_NHL_GrassForest_001") ?? LoadSpriteByPattern("Std_Pg_NHL_GrassForest_002");
            Sprite forestGrassMed = LoadSpriteByPattern("Std_Pg_NHL_GrassForest_006") ?? LoadSpriteByPattern("Std_Pg_NHL_GrassForest_012");
            Sprite forestGrassDark = LoadSpriteByPattern("Std_Pg_NHL_GrassForest_013_dark") ?? LoadSpriteByPattern("Std_Pg_NHL_GrassForest_011_dark");
            Sprite groundMoss = LoadSpriteByPattern("GroundMoss_004") ?? LoadSpriteByPattern("GroundMossLight_008");
            Sprite pebbleCluster = LoadSpriteByPattern("T_LanaVN_Rock_06_PebbleCluster") ?? LoadSpriteByPattern("Std_Pg_HDL_Rock_017");

            // Continuous walking band: from X: -6.5 (hill slope) to X: +4.2 (rock shelf edge)
            // Smaller step (0.30f) to form continuous lush ribbons of grass & flowers
            float startX = -6.5f;
            float endX = 4.2f;
            float step = 0.32f;

            int index = 0;
            for (float x = startX; x <= endX; x += step)
            {
                index++;
                float jitterX = (Mathf.Sin(x * 15.3f) * 0.06f);
                float posX = x + jitterX;
                float baseY = PlanetOfLanaAssetPipeline.GetPlatformSurfaceY(posX);

                // 1. Continuous Grass Ribbon with Interactive Sway Shader
                Sprite grassSprite = (index % 3 == 0) ? forestGrassDark : ((index % 3 == 1) ? forestGrassMed : forestGrassLight);
                if (grassSprite != null)
                {
                    GameObject grassObj = new GameObject($"GrassTuft_{index:D2}");
                    grassObj.transform.SetParent(foliageGO.transform, true);
                    float scale = 0.33f + (Mathf.Sin(x * 4.3f) * 0.05f);
                    float groundYOffset = (grassSprite.bounds.size.y * 0.5f * scale) - 0.015f;
                    grassObj.transform.position = new Vector3(posX, baseY + groundYOffset, -0.08f);
                    grassObj.transform.localScale = new Vector3(scale * ((index % 2 == 0) ? 1f : -1f), scale, 1f);

                    SpriteRenderer sr = grassObj.AddComponent<SpriteRenderer>();
                    sr.sprite = grassSprite;
                    sr.sharedMaterial = interactiveMat;
                    sr.sortingLayerName = "Environment";
                    sr.sortingOrder = 18;

                    var sway = grassObj.AddComponent<HoaVaMau.Gameplay.PlanetOfLanaFoliageSway>();
                    sway.EnableCharacterInteraction = true;
                    sway.MaxRotationAngle = 3.0f;
                    sway.MaxPushAngle = 14.0f;
                    sway.InteractionRadius = 0.85f;
                }

                // 2. Natural Clusters of Wildflowers (Yellow buttercups & White daisies)
                // Form bands along the grass edge
                if (index % 2 == 0)
                {
                    bool isYellow = (index % 4 == 0);
                    Sprite flowerSprite = isYellow ? ((index % 8 == 0) ? yellowButtercupAlt : yellowButtercup)
                                                   : ((index % 6 == 0) ? whiteDaisy : whiteSmallSprig);
                    if (flowerSprite != null)
                    {
                        GameObject flowerObj = new GameObject($"Wildflower_{(isYellow ? "Yellow" : "White")}_{index:D2}");
                        flowerObj.transform.SetParent(foliageGO.transform, true);
                        float offsetX = (isYellow ? 0.05f : -0.05f);
                        float flScale = 0.28f + (Mathf.Sin(x * 7.7f) * 0.06f);
                        float groundYOffset = (flowerSprite.bounds.size.y * 0.5f * flScale) - 0.015f;
                        flowerObj.transform.position = new Vector3(posX + offsetX, baseY + groundYOffset, -0.14f);
                        flowerObj.transform.localScale = new Vector3(flScale * ((index % 3 == 0) ? -1f : 1f), flScale, 1f);
                        flowerObj.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(x * 6.3f) * 7f);

                        SpriteRenderer sr = flowerObj.AddComponent<SpriteRenderer>();
                        sr.sprite = flowerSprite;
                        sr.sharedMaterial = interactiveMat;
                        sr.sortingLayerName = "Environment";
                        sr.sortingOrder = 24;

                        var sway = flowerObj.AddComponent<HoaVaMau.Gameplay.PlanetOfLanaFoliageSway>();
                        sway.EnableCharacterInteraction = true;
                        sway.MaxRotationAngle = 2.5f;
                        sway.MaxPushAngle = 11.0f;
                        sway.InteractionRadius = 0.85f;
                    }
                }

                // 3. Broadleaf Weeds (Plantain weed matching reference media_1793066371338_007bfdfb.png)
                if (index % 5 == 2)
                {
                    Sprite weedSprite = (index % 10 == 2) ? broadleafWeed : rockWeedAlt;
                    if (weedSprite != null)
                    {
                        GameObject weedObj = new GameObject($"BroadleafWeed_{index:D2}");
                        weedObj.transform.SetParent(foliageGO.transform, true);
                        float weedScale = 0.12f;
                        float groundYOffset = (weedSprite.bounds.size.y * 0.5f * weedScale) - 0.015f;
                        weedObj.transform.position = new Vector3(posX - 0.08f, baseY + groundYOffset, -0.10f);
                        weedObj.transform.localScale = new Vector3(weedScale * ((index % 2 == 0) ? 1f : -1f), weedScale, 1f);

                        SpriteRenderer sr = weedObj.AddComponent<SpriteRenderer>();
                        sr.sprite = weedSprite;
                        sr.sharedMaterial = interactiveMat;
                        sr.sortingLayerName = "Environment";
                        sr.sortingOrder = 21;

                        var sway = weedObj.AddComponent<HoaVaMau.Gameplay.PlanetOfLanaFoliageSway>();
                        sway.EnableCharacterInteraction = true;
                        sway.MaxRotationAngle = 2.0f;
                        sway.MaxPushAngle = 9.0f;
                        sway.InteractionRadius = 0.85f;
                    }
                }

                // 4. Red Dirt Pebbles & Moss Patches breaking up ground monotony
                if (index % 6 == 1 && pebbleCluster != null)
                {
                    GameObject pebObj = new GameObject($"PebbleCluster_{index:D2}");
                    pebObj.transform.SetParent(foliageGO.transform, true);
                    pebObj.transform.position = new Vector3(posX + 0.12f, baseY - 0.02f, -0.04f);
                    pebObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

                    SpriteRenderer sr = pebObj.AddComponent<SpriteRenderer>();
                    sr.sprite = pebbleCluster;
                    sr.sortingLayerName = "Environment";
                    sr.sortingOrder = 14;
                    sr.color = new Color(0.75f, 0.65f, 0.58f, 0.9f);
                }

                if (index % 4 == 1 && groundMoss != null)
                {
                    GameObject mossObj = new GameObject($"LipMoss_{index:D2}");
                    mossObj.transform.SetParent(foliageGO.transform, true);
                    mossObj.transform.position = new Vector3(posX, baseY - 0.05f, -0.06f);
                    mossObj.transform.localScale = new Vector3(0.75f, 0.65f, 1f);

                    SpriteRenderer sr = mossObj.AddComponent<SpriteRenderer>();
                    sr.sprite = groundMoss;
                    sr.sortingLayerName = "Environment";
                    sr.sortingOrder = 15;
                }
            }
        }

        private static void BuildTopCanopyFraming()
        {
            // Lens vignette hanging foliage across top screen
            GameObject fgParent = GameObject.Find("[Official_Lana_Foreground_Framing]");
            if (fgParent == null)
            {
                fgParent = new GameObject("[Official_Lana_Foreground_Framing]");
            }

            Transform oldTop = fgParent.transform.Find("FG_Top_Canopy_Vignette");
            if (oldTop != null)
            {
                Undo.DestroyObjectImmediate(oldTop.gameObject);
            }

            GameObject topCanopy = new GameObject("FG_Top_Canopy_Vignette");
            Undo.RegisterCreatedObjectUndo(topCanopy, "Create FG_Top_Canopy_Vignette");
            topCanopy.transform.SetParent(fgParent.transform, true);
            topCanopy.transform.position = Vector3.zero;

            Sprite branchSprite = LoadSpriteByPattern("Std_Pg_HDL_Tree_020_branch_001") ?? LoadSpriteByPattern("Std_Mg_HDL_TreeLeaves_02");
            Sprite leavesSprite = LoadSpriteByPattern("Std_Fg_HDL_Tree_002_Leaves02_black") ?? LoadSpriteByPattern("Std_Mg_HDL_TreeTop_006");

            // Upper-left hanging branch
            if (branchSprite != null)
            {
                GameObject upperLeft = new GameObject("TopLeft_Branch_Vignette");
                upperLeft.transform.SetParent(topCanopy.transform, true);
                upperLeft.transform.position = new Vector3(-4.0f, 4.4f, -3.5f);
                upperLeft.transform.localScale = new Vector3(2.0f, -1.6f, 1f);
                upperLeft.transform.rotation = Quaternion.Euler(0, 0, -12f);

                SpriteRenderer sr = upperLeft.AddComponent<SpriteRenderer>();
                sr.sprite = branchSprite;
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = 55;
                sr.color = new Color(0.04f, 0.08f, 0.06f, 0.88f); // Deep dark silhouette
            }

            // Upper-right canopy framing
            if (leavesSprite != null)
            {
                GameObject upperRight = new GameObject("TopRight_Canopy_Vignette");
                upperRight.transform.SetParent(topCanopy.transform, true);
                upperRight.transform.position = new Vector3(5.2f, 4.5f, -3.5f);
                upperRight.transform.localScale = new Vector3(2.2f, -1.8f, 1f);
                upperRight.transform.rotation = Quaternion.Euler(0, 0, 8f);

                SpriteRenderer sr = upperRight.AddComponent<SpriteRenderer>();
                sr.sprite = leavesSprite;
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = 55;
                sr.color = new Color(0.04f, 0.08f, 0.06f, 0.88f);
            }
        }

        private static Sprite LoadSpriteByPattern(string pattern)
        {
            string[] guids = AssetDatabase.FindAssets($"{pattern} t:Sprite");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }
    }
}
