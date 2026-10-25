using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace HoaVaMau.EditorTools
{
    public static class ReplaceShipWith3D
    {
        [MenuItem("Tools/Replace Ship With 3D")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // 1. Load the 3D model
            string modelPath = "Assets/Models/Battleship/tripo_convert_a0f3e7e5-4dc9-4da6-8e6a-f58a7fe2a979.fbx";
            var shipAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (shipAsset == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:Model tripo_convert");
                if (guids.Length > 0)
                {
                    modelPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    shipAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                }
            }

            if (shipAsset == null)
            {
                Debug.LogError("[ReplaceShipWith3D] Could not find battleship FBX at " + modelPath);
                return;
            }

            // 2. Setup Material with authentic textures
            string matPath = "Assets/Materials/M_Battleship_3D.mat";
            var shipMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (shipMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                shipMat = new Material(shader);
                AssetDatabase.CreateAsset(shipMat, matPath);
            }

            string fbmDir = "Assets/Models/Battleship/tripo_convert_a0f3e7e5-4dc9-4da6-8e6a-f58a7fe2a979.fbm";
            string baseColorPath = Path.Combine(fbmDir, "tripo_node_01e4edec-41ad-402e-a67f-a395c55a5e3d_BaseColor.png").Replace("\\", "/");
            string normalPath = Path.Combine(fbmDir, "tripo_node_01e4edec-41ad-402e-a67f-a395c55a5e3d_Normal_Bake.png").Replace("\\", "/");

            var texBase = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
            if (texBase != null)
            {
                shipMat.SetTexture("_BaseMap", texBase);
                shipMat.SetColor("_BaseColor", Color.white);
            }

            var texNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (texNormal != null)
            {
                // Ensure normal map texture importer setting
                var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
                shipMat.SetTexture("_BumpMap", texNormal);
                shipMat.EnableKeyword("_NORMALMAP");
            }

            shipMat.SetFloat("_Smoothness", 0.35f);
            shipMat.SetFloat("_Metallic", 0.65f);
            EditorUtility.SetDirty(shipMat);
            AssetDatabase.SaveAssets();

            // 3. Hide old 2D ship
            var oldShip = GameObject.Find("TransportShip_1954_Horizon");
            if (oldShip != null) oldShip.SetActive(false);

            // Clean up existing 3D ship
            var existing3DShip = GameObject.Find("Battleship_3D_Horizon");
            if (existing3DShip != null) Object.DestroyImmediate(existing3DShip);

            // 4. Instantiate 3D Battleship
            var shipInstance = (GameObject)PrefabUtility.InstantiatePrefab(shipAsset);
            shipInstance.name = "Battleship_3D_Horizon";

            var expGo = GameObject.Find("Scene1_Expansion_115m");
            if (expGo != null)
            {
                var z4 = expGo.transform.Find("Zone4_PierAndHorizonShip");
                if (z4 != null) shipInstance.transform.SetParent(z4, true);
            }

            // Assign material
            var renderers = shipInstance.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds();
            bool hasBounds = false;
            foreach (var r in renderers)
            {
                r.sharedMaterial = shipMat;
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            // Scale ship so it looks appropriately massive on the horizon
            float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float targetLength = 25.0f;
            float scaleFactor = targetLength / Mathf.Max(maxDim, 0.001f);
            shipInstance.transform.localScale = Vector3.one * scaleFactor;

            // Rotation: broadside with slight 12 deg tilt for 2.5D perspective
            if (bounds.size.z >= bounds.size.x)
            {
                shipInstance.transform.rotation = Quaternion.Euler(0f, 78.0f, 0f);
            }
            else
            {
                shipInstance.transform.rotation = Quaternion.Euler(0f, -12.0f, 0f);
            }

            // Position at horizon: X = 110m, Y = -2.35m (waterline sits naturally in river water), Z = 30m
            shipInstance.transform.position = new Vector3(110.0f, -2.35f, 30.0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // 5. RENDER PREVIEWS (Rules: In-Game max 1280x720, Panorama max 1920x600, JPG 80)
            string outDir = "C:/Users/Thanh Tài/.gemini/antigravity/brain/91d03331-ffbc-4dc5-be7e-61bc484b62a5";

            bool originalFog = RenderSettings.fog;
            RenderSettings.fog = false;

            // In-Game Dock View (1280 x 720)
            int wGame = 1280, hGame = 720;
            var rtGame = new RenderTexture(wGame, hGame, 24, RenderTextureFormat.ARGB32);
            var texGame = new Texture2D(wGame, hGame, TextureFormat.RGB24, false);
            var camGo = new GameObject("ShipPreview_Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.72f, 0.82f, 0.86f);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 500f;
            cam.targetTexture = rtGame;
            camGo.transform.position = new Vector3(101.5f, 0.8f, -10.8f);
            camGo.transform.rotation = Quaternion.Euler(5.0f, 0.0f, 0.0f);
            cam.Render();
            RenderTexture.active = rtGame;
            texGame.ReadPixels(new Rect(0, 0, wGame, hGame), 0, 0);
            texGame.Apply();
            File.WriteAllBytes(Path.Combine(outDir, "expansion_zone4_dock_3d_battleship.jpg"), texGame.EncodeToJPG(80));

            // Panorama Overview (1920 x 600)
            int wPan = 1920, hPan = 600;
            var rtPan = new RenderTexture(wPan, hPan, 24, RenderTextureFormat.ARGB32);
            var texPan = new Texture2D(wPan, hPan, TextureFormat.RGB24, false);
            cam.targetTexture = rtPan;
            cam.fieldOfView = 34f;
            camGo.transform.position = new Vector3(58.0f, 13.0f, -58.0f);
            camGo.transform.rotation = Quaternion.Euler(11.0f, 0.0f, 0.0f);
            cam.Render();
            RenderTexture.active = rtPan;
            texPan.ReadPixels(new Rect(0, 0, wPan, hPan), 0, 0);
            texPan.Apply();
            File.WriteAllBytes(Path.Combine(outDir, "scene_full_level_panoramic_3d_battleship.jpg"), texPan.EncodeToJPG(80));

            RenderSettings.fog = originalFog;
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rtGame);
            Object.DestroyImmediate(texGame);
            Object.DestroyImmediate(rtPan);
            Object.DestroyImmediate(texPan);
            Object.DestroyImmediate(camGo);

            Debug.Log("[ReplaceShipWith3D] Completed 3D battleship texturing and rendered JPG previews!");
        }
    }
}
