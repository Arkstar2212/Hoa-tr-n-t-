using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace HoaVaMau.EditorTools
{
    public static class SetupSurroundingSkyPanorama
    {
        private const string ScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string MaterialsDir = "Assets/Materials/Environment/Backdrop_Sky";
        private const string MasterSkyTexPath = "Assets/Textures/Sky/Sky_Cloud_Krita_5120x2160.png";

        [MenuItem("Tools/Hoa Tren Dat Do/Setup Surrounding Sky")]
        public static void Execute()
        {
            // 1. Ensure target scene is open
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // 2. Ensure Master Sky Texture import settings (Repeat wrap mode)
            var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MasterSkyTexPath);
            var importer = AssetImporter.GetAtPath(MasterSkyTexPath) as TextureImporter;
            if (importer != null && importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
                skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MasterSkyTexPath);
            }

            // 3. Create or Update Panoramic Skybox Material
            if (!AssetDatabase.IsValidFolder(MaterialsDir))
            {
                AssetDatabase.CreateFolder("Assets/Materials/Environment", "Backdrop_Sky");
            }

            string skyboxMatPath = $"{MaterialsDir}/M_Skybox_CaMau_Panorama.mat";
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(skyboxMatPath);
            Shader panoSkyboxShader = Shader.Find("Skybox/Panoramic");
            if (skyboxMat == null)
            {
                skyboxMat = new Material(panoSkyboxShader);
                AssetDatabase.CreateAsset(skyboxMat, skyboxMatPath);
            }
            else
            {
                skyboxMat.shader = panoSkyboxShader;
            }

            skyboxMat.mainTexture = skyTex;
            skyboxMat.SetFloat("_Exposure", 1.05f);
            skyboxMat.SetColor("_Tint", new Color(1.0f, 0.98f, 0.94f, 1.0f));
            skyboxMat.SetFloat("_Mapping", 0f); // 0 = Latitude Longitude
            skyboxMat.SetFloat("_ImageType", 0f); // 0 = 360 Degrees
            skyboxMat.SetFloat("_MirrorOnBack", 0f);
            EditorUtility.SetDirty(skyboxMat);

            // Assign to RenderSettings
            RenderSettings.skybox = skyboxMat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.96f, 0.88f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.78f, 0.68f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.32f, 0.25f);

            // 4. Create or Update 360 Surrounding Cylinder Material
            string cylinderMatPath = $"{MaterialsDir}/M_Sky_Backdrop_Surround.mat";
            Material cylinderMat = AssetDatabase.LoadAssetAtPath<Material>(cylinderMatPath);
            Shader panoShader = Shader.Find("HoaVaMau/PanoramaSky");
            if (panoShader == null) panoShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (cylinderMat == null)
            {
                cylinderMat = new Material(panoShader);
                AssetDatabase.CreateAsset(cylinderMat, cylinderMatPath);
            }
            else
            {
                cylinderMat.shader = panoShader;
            }

            cylinderMat.mainTexture = skyTex;
            if (cylinderMat.HasProperty("_Exposure")) cylinderMat.SetFloat("_Exposure", 1.10f);
            if (cylinderMat.HasProperty("_Tint")) cylinderMat.SetColor("_Tint", Color.white);
            if (cylinderMat.HasProperty("_Vignette")) cylinderMat.SetFloat("_Vignette", 0.10f);
            cylinderMat.renderQueue = 1000; // Background queue
            EditorUtility.SetDirty(cylinderMat);

            // 5. Fix any 100x Blender FBX child scale anomalies on houses, boulders, crates
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t.name == "Mesh_0" && t.localScale.x > 10f)
                {
                    t.localScale = Vector3.one;
                    EditorUtility.SetDirty(t);
                }
            }

            // 6. Ensure inanimate objects (houses, huts, fences, steps) stand upright (X=0, Z=0)
            var villageAreas = GameObject.Find("CaMau_1954_Village/Village_Areas");
            if (villageAreas != null)
            {
                var renderers = villageAreas.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                {
                    Transform t = r.transform;
                    if (t.name == "Mesh_0" && t.parent != null) t = t.parent;

                    Vector3 angles = t.rotation.eulerAngles;
                    if (Mathf.Abs(Mathf.DeltaAngle(angles.x, 0f)) > 1f || Mathf.Abs(Mathf.DeltaAngle(angles.z, 0f)) > 1f)
                    {
                        t.rotation = Quaternion.Euler(0f, angles.y, 0f);
                        EditorUtility.SetDirty(t);
                    }
                }
            }

            // 7. Setup Hierarchy under Background_Atmosphere
            GameObject bgGroup = GameObject.Find("CaMau_1954_Village/Environment/Background_Atmosphere");
            if (bgGroup == null) bgGroup = GameObject.Find("Background_Atmosphere");
            if (bgGroup == null)
            {
                GameObject env = GameObject.Find("CaMau_1954_Village/Environment");
                if (env == null) env = new GameObject("Environment");
                bgGroup = new GameObject("Background_Atmosphere");
                bgGroup.transform.SetParent(env.transform, false);
            }

            // Remove old flat Backdrop_SkySunset quad
            Transform oldBackdrop = bgGroup.transform.Find("Backdrop_SkySunset");
            if (oldBackdrop != null)
            {
                Object.DestroyImmediate(oldBackdrop.gameObject);
            }

            // 8. Build 360-degree Sky Surround Cylinder (Radius 135m, Height 110m)
            Transform cylTrans = bgGroup.transform.Find("Sky_Surround_Cylinder_360");
            GameObject cylObj = (cylTrans != null) ? cylTrans.gameObject : new GameObject("Sky_Surround_Cylinder_360");
            cylObj.transform.SetParent(bgGroup.transform, false);
            cylObj.transform.position = new Vector3(0f, 18f, 0f);
            cylObj.transform.rotation = Quaternion.identity;
            cylObj.transform.localScale = Vector3.one;

            var mf = cylObj.GetComponent<MeshFilter>();
            if (mf == null) mf = cylObj.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateInwardCylinderMesh(135f, 110f, 18f, 64, 3);

            var mr = cylObj.GetComponent<MeshRenderer>();
            if (mr == null) mr = cylObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = cylinderMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            var col = cylObj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            // 9. Configure Cardinal Vista Materials with soft edge feathering
            Material matVistaNorth = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Vista_Official_01_ForestSky.mat");
            if (matVistaNorth != null)
            {
                if (matVistaNorth.HasProperty("_TopFeather")) matVistaNorth.SetFloat("_TopFeather", 0.35f);
                if (matVistaNorth.HasProperty("_LeftFeather")) matVistaNorth.SetFloat("_LeftFeather", 0.25f);
                if (matVistaNorth.HasProperty("_RightFeather")) matVistaNorth.SetFloat("_RightFeather", 0.25f);
                EditorUtility.SetDirty(matVistaNorth);
            }
            else matVistaNorth = cylinderMat;

            Material matVistaSouth = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Vista_04_SeaHorizon_Sunset.mat");
            if (matVistaSouth != null)
            {
                if (matVistaSouth.HasProperty("_TopFeather")) matVistaSouth.SetFloat("_TopFeather", 0.38f);
                if (matVistaSouth.HasProperty("_LeftFeather")) matVistaSouth.SetFloat("_LeftFeather", 0.25f);
                if (matVistaSouth.HasProperty("_RightFeather")) matVistaSouth.SetFloat("_RightFeather", 0.25f);
                EditorUtility.SetDirty(matVistaSouth);
            }
            else matVistaSouth = cylinderMat;

            Material matVistaEast = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Vista_03_HillsFields_Sunset.mat");
            if (matVistaEast != null)
            {
                if (matVistaEast.HasProperty("_TopFeather")) matVistaEast.SetFloat("_TopFeather", 0.38f);
                if (matVistaEast.HasProperty("_LeftFeather")) matVistaEast.SetFloat("_LeftFeather", 0.25f);
                if (matVistaEast.HasProperty("_RightFeather")) matVistaEast.SetFloat("_RightFeather", 0.25f);
                EditorUtility.SetDirty(matVistaEast);
            }
            else matVistaEast = cylinderMat;

            Material matVistaWest = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Vista_Official_01_ForestSky.mat");
            if (matVistaWest == null) matVistaWest = cylinderMat;

            // Cardinal Vistas placed at distance 105m with width 230m, height 62m
            SetupCardinalVista(bgGroup.transform, "Backdrop_Vista_North", new Vector3(0f, 18f, 105f), Quaternion.Euler(0f, 180f, 0f), new Vector3(230f, 62f, 1f), matVistaNorth);
            SetupCardinalVista(bgGroup.transform, "Backdrop_Vista_South", new Vector3(0f, 13f, -105f), Quaternion.Euler(0f, 0f, 0f), new Vector3(230f, 62f, 1f), matVistaSouth);
            SetupCardinalVista(bgGroup.transform, "Backdrop_Vista_East", new Vector3(105f, 15f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(230f, 62f, 1f), matVistaEast);
            SetupCardinalVista(bgGroup.transform, "Backdrop_Vista_West", new Vector3(-105f, 15f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(230f, 62f, 1f), matVistaWest);

            // 10. Update Camera Settings for generous draw distance
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cameras)
            {
                if (c.name.Contains("Main Camera") || c.CompareTag("MainCamera"))
                {
                    c.clearFlags = CameraClearFlags.Skybox;
                    if (c.farClipPlane < 400f)
                    {
                        c.farClipPlane = 400f;
                        EditorUtility.SetDirty(c);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[SetupSurroundingSkyPanorama] Successfully configured 360-degree surrounding watercolor sky with soft edge feathering!");
        }

        private static void SetupCardinalVista(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            Transform existing = parent.Find(name);
            GameObject obj = (existing != null) ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            obj.transform.localScale = scale;

            var rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            var col = obj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        private static Mesh CreateInwardCylinderMesh(float radius, float height, float yCenter, int segments, int uRepeats)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Inward_Sky_Cylinder_Mesh";

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            float yBottom = yCenter - height * 0.35f;
            float yTop = yCenter + height * 0.65f;

            for (int i = 0; i <= segments; i++)
            {
                float ratio = (float)i / segments;
                float angle = ratio * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;

                int idxBottom = i * 2;
                int idxTop = i * 2 + 1;

                vertices[idxBottom] = new Vector3(x, yBottom, z);
                vertices[idxTop] = new Vector3(x, yTop, z);

                // Inward normal
                Vector3 normal = new Vector3(-Mathf.Sin(angle), 0f, -Mathf.Cos(angle));
                normals[idxBottom] = normal;
                normals[idxTop] = normal;

                uvs[idxBottom] = new Vector2(ratio * uRepeats, 0f);
                uvs[idxTop] = new Vector2(ratio * uRepeats, 1f);

                if (i < segments)
                {
                    int triIdx = i * 6;
                    // Clockwise when looking from inside
                    triangles[triIdx + 0] = idxBottom;
                    triangles[triIdx + 1] = idxTop;
                    triangles[triIdx + 2] = idxBottom + 2;

                    triangles[triIdx + 3] = idxTop;
                    triangles[triIdx + 4] = idxTop + 2;
                    triangles[triIdx + 5] = idxBottom + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
