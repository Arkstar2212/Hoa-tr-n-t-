#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HoaVaMau.Water;

namespace HoaVaMau.Editor
{
    public static class BuildDredgeScene
    {
        [MenuItem("Tools/Build Dredge Sunset Scene")]
        public static void BuildScene()
        {
            // 1. Sky & Ambient Settings
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.20f, 0.30f);

            // 2. Sky Gradient Material
            Shader skyShader = Shader.Find("HoaVaMau/Dredge_SkyGradient");
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_SkyGradient.mat");
            if (skyMat == null)
            {
                skyMat = new Material(skyShader);
                AssetDatabase.CreateAsset(skyMat, "Assets/Materials/M_Dredge_SkyGradient.mat");
            }
            else
            {
                skyMat.shader = skyShader;
            }

            skyMat.SetColor("_ZenithColor", new Color(0.16f, 0.10f, 0.24f, 1.0f));   // Twilight Indigo
            skyMat.SetColor("_UpperMidColor", new Color(0.48f, 0.20f, 0.34f, 1.0f)); // Dusk Plum
            skyMat.SetColor("_LowerMidColor", new Color(0.92f, 0.38f, 0.18f, 1.0f)); // Sunset Orange
            skyMat.SetColor("_HorizonColor", new Color(1.00f, 0.68f, 0.28f, 1.0f));  // Sunset Gold
            skyMat.SetFloat("_HorizonCutoff", 0.12f);
            skyMat.SetFloat("_MidStop", 0.45f);
            skyMat.SetFloat("_CurvePower", 1.05f);
            EditorUtility.SetDirty(skyMat);

            // 3. Materials
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_Rock.mat");
            if (rockMat != null)
            {
                rockMat.color = new Color(0.42f, 0.40f, 0.38f);
                EditorUtility.SetDirty(rockMat);
            }

            Material pineMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_Pine.mat");
            if (pineMat != null)
            {
                pineMat.color = new Color(0.12f, 0.18f, 0.14f);
                EditorUtility.SetDirty(pineMat);
            }

            Material cloudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_Cloud.mat");
            if (cloudMat != null)
            {
                cloudMat.color = new Color(0.85f, 0.52f, 0.46f);
                EditorUtility.SetDirty(cloudMat);
            }

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_HouseWood.mat");
            Material redMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Lighthouse_Red.mat");
            Material whiteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Lighthouse_White.mat");
            Material lanternMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Lighthouse_Lantern.mat");
            Material windowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_WindowGlow.mat");
            Material boatHullMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_BoatHull.mat");
            Material boatCabinMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_BoatCabin.mat");
            Material boatRoofMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_BoatRoof.mat");
            Material tireMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_TireRubber.mat");
            Material wakeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_BoatWake.mat");
            Material flagMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_BoatFlag.mat");
            if (flagMat != null)
            {
                flagMat.color = new Color(0.08f, 0.62f, 0.72f);
                EditorUtility.SetDirty(flagMat);
            }

            // Ocean Material
            Material oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Dredge_FacetedOcean.mat");
            if (oceanMat != null)
            {
                oceanMat.SetColor("_DeepColor", new Color(0.043f, 0.090f, 0.180f, 1.0f));
                oceanMat.SetColor("_ShallowColor", new Color(0.114f, 0.290f, 0.368f, 0.96f));
                oceanMat.SetFloat("_WaveSpeed", 0.80f);
                oceanMat.SetFloat("_WaveHeight", 0.38f);
                oceanMat.SetFloat("_CrestFoamThreshold", 0.28f);
                oceanMat.SetFloat("_CrestFoamWidth", 0.20f);
                oceanMat.SetFloat("_CrestFoamNoiseCutoff", 0.52f);
                oceanMat.SetFloat("_CrestFoamNoiseScale", 3.8f);
                oceanMat.SetFloat("_ContactFoamDistance", 0.85f);
                oceanMat.SetColor("_SpecularColor", new Color(1.0f, 0.82f, 0.44f, 1.0f));
                oceanMat.SetFloat("_SpecularIntensity", 5.6f);
                oceanMat.SetFloat("_SpecularPower", 32.0f);
                oceanMat.SetFloat("_SpecularStep", 0.20f);
                oceanMat.SetFloat("_FlatShadingStrength", 1.0f);
                EditorUtility.SetDirty(oceanMat);
            }

            // 4. Create Scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // A. Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camObj.transform.position = new Vector3(-0.85f, 2.50f, 2.2f);
            camObj.transform.rotation = Quaternion.Euler(10.2f, 7.5f, 0.0f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 350f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.10f, 0.24f, 1.0f);

            // B. Sky Backdrop Quad
            GameObject skyObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyObj.name = "Sunset_Sky_Backdrop";
            skyObj.transform.position = new Vector3(0f, 30f, 130f);
            skyObj.transform.localScale = new Vector3(320f, 110f, 1f);
            skyObj.GetComponent<MeshRenderer>().sharedMaterial = skyMat;
            Object.DestroyImmediate(skyObj.GetComponent<Collider>());

            // C. Setting Sun Disc & Directional Sun Light
            Vector3 sunPos = new Vector3(-24f, 3.8f, 92f);
            GameObject sunSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunSphere.name = "Sunset_Sun_Disc";
            sunSphere.transform.position = sunPos;
            sunSphere.transform.localScale = new Vector3(13f, 13f, 1f);
            Material sunDiscMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sunDiscMat.color = new Color(1.8f, 1.4f, 0.9f);
            sunSphere.GetComponent<MeshRenderer>().sharedMaterial = sunDiscMat;
            Object.DestroyImmediate(sunSphere.GetComponent<Collider>());

            GameObject sunObj = new GameObject("Sunset Sun Light");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.76f, 0.50f);
            sun.intensity = 2.1f;
            Vector3 lightDir = (new Vector3(0f, 1.5f, 5.0f) - sunPos).normalized;
            sunObj.transform.rotation = Quaternion.LookRotation(lightDir);

            // D. Stepped Low-Poly Sunset Clouds
            GameObject cloudGroup = new GameObject("LowPoly_Sunset_Clouds");
            Vector3[] cPos = new Vector3[] {
                new Vector3(-35f, 28f, 115f),
                new Vector3(-6f, 33f, 120f),
                new Vector3(26f, 27f, 112f),
                new Vector3(-16f, 21f, 105f),
                new Vector3(16f, 19f, 100f),
                new Vector3(-30f, 16f, 95f)
            };
            for (int c = 0; c < cPos.Length; c++)
            {
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloud.name = $"Cloud_{c + 1}";
                cloud.transform.SetParent(cloudGroup.transform);
                cloud.transform.position = cPos[c];
                cloud.transform.localScale = new Vector3(22f + (c % 2) * 8f, 3.2f, 10f);
                cloud.transform.rotation = Quaternion.Euler(0f, 12f * c, 0f);
                cloud.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;
                Object.DestroyImmediate(cloud.GetComponent<Collider>());
            }

            // E. Ocean Water Surface
            GameObject waterObj = new GameObject("Dredge_Ocean_Surface");
            waterObj.transform.position = Vector3.zero;
            WaterMeshGenerator meshGen = waterObj.AddComponent<WaterMeshGenerator>();
            meshGen.width = 180f;
            meshGen.length = 220f;
            meshGen.segmentsX = 90;
            meshGen.segmentsZ = 90;
            meshGen.GenerateMesh();
            waterObj.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;
            GerstnerWaterManager waterMgr = waterObj.AddComponent<GerstnerWaterManager>();
            waterMgr.waterMaterial = oceanMat;
            waterMgr.waterBaseHeight = 0f;

            // F. Coastal Island Cliffs & Rocky Outcrops
            GameObject islandGroup = new GameObject("Dredge_Coastal_Islands");

            // Main Lighthouse Bluff
            GameObject cliffBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffBase.name = "Lighthouse_Cliff_Base";
            cliffBase.transform.SetParent(islandGroup.transform);
            cliffBase.transform.position = new Vector3(19f, 1.2f, 50f);
            cliffBase.transform.localScale = new Vector3(22f, 4.2f, 24f);
            cliffBase.transform.rotation = Quaternion.Euler(0f, 16f, 0f);
            cliffBase.GetComponent<MeshRenderer>().sharedMaterial = rockMat;

            GameObject cliffSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffSlope.name = "Cliff_Slanted_Facet";
            cliffSlope.transform.SetParent(islandGroup.transform);
            cliffSlope.transform.position = new Vector3(13f, 0.8f, 45f);
            cliffSlope.transform.localScale = new Vector3(14f, 3.6f, 12f);
            cliffSlope.transform.rotation = Quaternion.Euler(8f, 28f, -6f);
            cliffSlope.GetComponent<MeshRenderer>().sharedMaterial = rockMat;

            Vector3[] rockPos = new Vector3[] {
                new Vector3(29f, 1.0f, 48f),
                new Vector3(11f, 0.5f, 58f),
                new Vector3(-32f, 0.8f, 75f),
                new Vector3(-42f, 1.2f, 85f),
                new Vector3(-13f, 0.3f, 52f),
                new Vector3(3.5f, 0.3f, 42f)
            };
            Vector3[] rockScale = new Vector3[] {
                new Vector3(10f, 3.4f, 10f),
                new Vector3(9f, 3.0f, 9f),
                new Vector3(20f, 3.5f, 20f),
                new Vector3(22f, 4.0f, 22f),
                new Vector3(3.2f, 2.0f, 3.2f),
                new Vector3(2.5f, 1.8f, 2.5f)
            };
            for (int r = 0; r < rockPos.Length; r++)
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = $"Sea_Rock_{r + 1}";
                rock.transform.SetParent(islandGroup.transform);
                rock.transform.position = rockPos[r];
                rock.transform.localScale = rockScale[r];
                rock.transform.rotation = Quaternion.Euler(4f * r, 22f * r, -3f * r);
                rock.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
            }

            // G. Lighthouse
            GameObject lighthouse = new GameObject("Lighthouse_Tower");
            lighthouse.transform.position = new Vector3(18f, 3.3f, 48f);
            float segH = 1.35f;
            for (int s = 0; s < 6; s++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = $"Tower_Ring_{s + 1}";
                ring.transform.SetParent(lighthouse.transform, false);
                float rad = Mathf.Lerp(1.7f, 1.2f, (float)s / 6f);
                ring.transform.localPosition = new Vector3(0f, s * segH + segH * 0.5f, 0f);
                ring.transform.localScale = new Vector3(rad * 2f, segH * 0.5f, rad * 2f);
                ring.GetComponent<MeshRenderer>().sharedMaterial = (s % 2 == 0) ? redMat : whiteMat;
            }

            float lanternY = 6 * segH;
            GameObject balcony = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            balcony.name = "Lantern_Balcony";
            balcony.transform.SetParent(lighthouse.transform, false);
            balcony.transform.localPosition = new Vector3(0f, lanternY + 0.2f, 0f);
            balcony.transform.localScale = new Vector3(3.0f, 0.15f, 3.0f);
            balcony.GetComponent<MeshRenderer>().sharedMaterial = boatRoofMat;

            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lantern.name = "Lantern_Glass";
            lantern.transform.SetParent(lighthouse.transform, false);
            lantern.transform.localPosition = new Vector3(0f, lanternY + 0.85f, 0f);
            lantern.transform.localScale = new Vector3(1.8f, 0.55f, 1.8f);
            lantern.GetComponent<MeshRenderer>().sharedMaterial = lanternMat;

            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dome.name = "Lantern_Dome";
            dome.transform.SetParent(lighthouse.transform, false);
            dome.transform.localPosition = new Vector3(0f, lanternY + 1.55f, 0f);
            dome.transform.localScale = new Vector3(2.0f, 0.35f, 2.0f);
            dome.GetComponent<MeshRenderer>().sharedMaterial = redMat;

            GameObject lgt = new GameObject("Lantern_PointLight");
            lgt.transform.SetParent(lighthouse.transform, false);
            lgt.transform.localPosition = new Vector3(0f, lanternY + 0.85f, 0f);
            Light beacon = lgt.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.color = new Color(1.0f, 0.90f, 0.60f);
            beacon.range = 45f;
            beacon.intensity = 6.0f;

            // H. Pine Trees on Bluff
            GameObject pineGroup = new GameObject("Pine_Trees_Ridge");
            Vector3[] pineSpots = new Vector3[] {
                new Vector3(14f, 3.3f, 52f),
                new Vector3(16f, 3.3f, 56f),
                new Vector3(23f, 3.3f, 50f),
                new Vector3(25f, 3.3f, 54f),
                new Vector3(21f, 3.3f, 58f),
                new Vector3(27f, 3.3f, 52f),
                new Vector3(-28f, 2.4f, 78f),
                new Vector3(-32f, 2.4f, 82f),
                new Vector3(-36f, 2.4f, 85f)
            };
            for (int p = 0; p < pineSpots.Length; p++)
            {
                GameObject pine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pine.name = $"Pine_{p + 1}";
                pine.transform.SetParent(pineGroup.transform);
                pine.transform.position = pineSpots[p];
                float r = 1.6f + (p % 3) * 0.3f;
                float h = 4.5f + (p % 2) * 1.2f;
                pine.transform.localScale = new Vector3(r, h * 0.5f, r);
                pine.GetComponent<MeshRenderer>().sharedMaterial = pineMat;
                Object.DestroyImmediate(pine.GetComponent<Collider>());
            }

            // I. Distant Village in Harbor
            GameObject villageGroup = new GameObject("Dredge_Coastal_Village");
            Vector3[] houseSpots = new Vector3[] {
                new Vector3(-4f, 0.8f, 72f),
                new Vector3(0f, 1.0f, 75f),
                new Vector3(4f, 1.1f, 78f),
                new Vector3(8f, 1.3f, 80f),
                new Vector3(-8f, 1.0f, 76f)
            };
            for (int h = 0; h < houseSpots.Length; h++)
            {
                GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
                house.name = $"Village_House_{h + 1}";
                house.transform.SetParent(villageGroup.transform);
                house.transform.position = houseSpots[h];
                house.transform.localScale = new Vector3(2.8f, 2.0f, 2.6f);
                house.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                Object.DestroyImmediate(house.GetComponent<Collider>());

                GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "Window_Glow";
                win.transform.SetParent(house.transform, false);
                win.transform.localPosition = new Vector3(0f, 0.1f, -0.52f);
                win.transform.localScale = new Vector3(0.5f, 0.4f, 0.05f);
                win.GetComponent<MeshRenderer>().sharedMaterial = windowMat;
                Object.DestroyImmediate(win.GetComponent<Collider>());
            }

            GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "Village_Pier";
            pier.transform.SetParent(villageGroup.transform);
            pier.transform.position = new Vector3(0f, 0.4f, 66f);
            pier.transform.localScale = new Vector3(2.0f, 0.3f, 8.0f);
            pier.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(pier.GetComponent<Collider>());

            // J. The DREDGE Fishing Trawler Boat
            GameObject boat = new GameObject("Dredge_Fishing_Trawler");
            boat.transform.position = new Vector3(0f, 0.28f, 8.5f);
            boat.transform.rotation = Quaternion.Euler(0f, -6f, 0f);

            GameObject hullLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hullLower.name = "Hull_Lower";
            hullLower.transform.SetParent(boat.transform, false);
            hullLower.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            hullLower.transform.localScale = new Vector3(2.2f, 0.7f, 5.0f);
            hullLower.GetComponent<MeshRenderer>().sharedMaterial = boatHullMat;

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin_Wheelhouse";
            cabin.transform.SetParent(boat.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 0.7f, -0.6f);
            cabin.transform.localScale = new Vector3(1.8f, 1.1f, 1.8f);
            cabin.GetComponent<MeshRenderer>().sharedMaterial = boatCabinMat;

            GameObject cabinWin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinWin.name = "Cabin_Window_Rear";
            cabinWin.transform.SetParent(cabin.transform, false);
            cabinWin.transform.localPosition = new Vector3(0f, 0.12f, -0.52f);
            cabinWin.transform.localScale = new Vector3(0.6f, 0.35f, 0.05f);
            cabinWin.GetComponent<MeshRenderer>().sharedMaterial = windowMat;
            Object.DestroyImmediate(cabinWin.GetComponent<Collider>());

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Cabin_Roof";
            roof.transform.SetParent(boat.transform, false);
            roof.transform.localPosition = new Vector3(0f, 1.3f, -0.6f);
            roof.transform.localScale = new Vector3(2.0f, 0.15f, 2.0f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = boatRoofMat;

            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chimney.name = "Exhaust_Chimney";
            chimney.transform.SetParent(boat.transform, false);
            chimney.transform.localPosition = new Vector3(-0.5f, 1.65f, -1.2f);
            chimney.transform.localScale = new Vector3(0.18f, 0.45f, 0.18f);
            chimney.GetComponent<MeshRenderer>().sharedMaterial = boatRoofMat;

            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Forward_Mast";
            mast.transform.SetParent(boat.transform, false);
            mast.transform.localPosition = new Vector3(0f, 1.4f, 1.5f);
            mast.transform.localScale = new Vector3(0.12f, 1.4f, 0.12f);
            mast.GetComponent<MeshRenderer>().sharedMaterial = boatRoofMat;

            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Boat_Pennant_Flag";
            flag.transform.SetParent(boat.transform, false);
            flag.transform.localPosition = new Vector3(0.35f, 2.3f, 1.5f);
            flag.transform.localScale = new Vector3(0.65f, 0.32f, 0.04f);
            flag.GetComponent<MeshRenderer>().sharedMaterial = flagMat;
            Object.DestroyImmediate(flag.GetComponent<Collider>());

            float[] tZ = new float[] { -1.5f, 0.0f, 1.5f };
            for (int t = 0; t < tZ.Length; t++)
            {
                GameObject tL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tL.name = $"Tire_L_{t + 1}";
                tL.transform.SetParent(boat.transform, false);
                tL.transform.localPosition = new Vector3(-1.18f, 0f, tZ[t]);
                tL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tL.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
                tL.GetComponent<MeshRenderer>().sharedMaterial = tireMat;

                GameObject tR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tR.name = $"Tire_R_{t + 1}";
                tR.transform.SetParent(boat.transform, false);
                tR.transform.localPosition = new Vector3(1.18f, 0f, tZ[t]);
                tR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tR.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
                tR.GetComponent<MeshRenderer>().sharedMaterial = tireMat;
            }

            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Deck_Fish_Crate";
            crate.transform.SetParent(boat.transform, false);
            crate.transform.localPosition = new Vector3(0.4f, 0.45f, 0.9f);
            crate.transform.localScale = new Vector3(0.6f, 0.45f, 0.75f);
            crate.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Buoyancy
            Rigidbody boatRb = boat.AddComponent<Rigidbody>();
            boatRb.mass = 350f;
            boatRb.linearDamping = 0.8f;
            boatRb.angularDamping = 1.0f;
            BoxCollider boatCol = boat.AddComponent<BoxCollider>();
            boatCol.size = new Vector3(2.4f, 1.5f, 5.2f);
            WaterFloater boatFloat = boat.AddComponent<WaterFloater>();
            boatFloat.buoyancyPower = 28f;
            boatFloat.waterDrag = 3.2f;
            boatFloat.waterAngularDrag = 3.0f;
            boatFloat.normalAlignmentSpeed = 4.5f;

            // K. Wake
            GameObject wake = new GameObject("Boat_Wake_Trail");
            wake.transform.position = boat.transform.position;
            wake.transform.rotation = boat.transform.rotation;

            GameObject wakeL = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wakeL.name = "Wake_L";
            wakeL.transform.SetParent(wake.transform, false);
            wakeL.transform.localPosition = new Vector3(-0.85f, 0.02f, -2.5f);
            wakeL.transform.localRotation = Quaternion.Euler(90f, 0f, 12f);
            wakeL.transform.localScale = new Vector3(0.35f, 4.5f, 1f);
            wakeL.GetComponent<MeshRenderer>().sharedMaterial = wakeMat;
            Object.DestroyImmediate(wakeL.GetComponent<Collider>());

            GameObject wakeR = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wakeR.name = "Wake_R";
            wakeR.transform.SetParent(wake.transform, false);
            wakeR.transform.localPosition = new Vector3(0.85f, 0.02f, -2.5f);
            wakeR.transform.localRotation = Quaternion.Euler(90f, 0f, -12f);
            wakeR.transform.localScale = new Vector3(0.35f, 4.5f, 1f);
            wakeR.GetComponent<MeshRenderer>().sharedMaterial = wakeMat;
            Object.DestroyImmediate(wakeR.GetComponent<Collider>());

            GameObject wakeChurn = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wakeChurn.name = "Wake_Churn";
            wakeChurn.transform.SetParent(wake.transform, false);
            wakeChurn.transform.localPosition = new Vector3(0f, 0.02f, -0.6f);
            wakeChurn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wakeChurn.transform.localScale = new Vector3(1.2f, 1.5f, 1f);
            wakeChurn.GetComponent<MeshRenderer>().sharedMaterial = wakeMat;
            Object.DestroyImmediate(wakeChurn.GetComponent<Collider>());

            // Sync SceneView camera
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.pivot = camObj.transform.position + camObj.transform.forward * 16f;
                SceneView.lastActiveSceneView.rotation = camObj.transform.rotation;
                SceneView.lastActiveSceneView.size = 14f;
                SceneView.lastActiveSceneView.Repaint();
            }

            string scenePath = "Assets/Scenes/Dredge_Sunset_Scene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[BuildDredgeScene] Scene built successfully and saved to: " + scenePath);
        }
    }
}
#endif
