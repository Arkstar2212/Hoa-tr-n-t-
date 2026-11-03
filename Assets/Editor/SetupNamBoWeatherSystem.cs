using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using HoaTrenDatDo.Environment;

namespace HoaTrenDatDo.EditorTools
{
    public static class SetupNamBoWeatherSystem
    {
        [MenuItem("Tools/Hoa Tren Dat Do/Setup Nam Bo Weather System")]
        public static void SetupWeather()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[WeatherSetup] Setting up Nam Bo Weather System in scene: {activeScene.name}");

            // 1. Tìm hoặc tạo VolumeProfile cho Tropical Rain
            string profilePath = "Assets/Settings/VolumeProfile_TropicalRain.asset";
            VolumeProfile rainProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (rainProfile == null)
            {
                rainProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                
                // Thêm Color Adjustments
                var colorAdj = rainProfile.Add<ColorAdjustments>(true);
                colorAdj.colorFilter.overrideState = true;
                colorAdj.colorFilter.value = new Color(0.48f, 0.58f, 0.66f, 1f); // Xám xanh mờ lạnh
                colorAdj.postExposure.overrideState = true;
                colorAdj.postExposure.value = -0.35f;
                colorAdj.saturation.overrideState = true;
                colorAdj.saturation.value = -18f;

                // Thêm White Balance (làm lạnh màu nước)
                var wb = rainProfile.Add<WhiteBalance>(true);
                wb.temperature.overrideState = true;
                wb.temperature.value = -20f;

                // Thêm Vignette
                var vignette = rainProfile.Add<Vignette>(true);
                vignette.color.overrideState = true;
                vignette.color.value = new Color(0.1f, 0.15f, 0.2f, 1f);
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.28f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.45f;

                AssetDatabase.CreateAsset(rainProfile, profilePath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[WeatherSetup] Created Rain VolumeProfile at {profilePath}");
            }

            // 2. Kiểm tra hoặc tạo Root GameObject [WEATHER_SYSTEM] (Additive Only)
            GameObject weatherRoot = GameObject.Find("[WEATHER_SYSTEM]");
            if (weatherRoot == null)
            {
                weatherRoot = new GameObject("[WEATHER_SYSTEM]");
                Undo.RegisterCreatedObjectUndo(weatherRoot, "Create [WEATHER_SYSTEM]");
            }

            var weatherController = weatherRoot.GetComponent<NamBoWeatherController>();
            if (weatherController == null)
            {
                weatherController = Undo.AddComponent<NamBoWeatherController>(weatherRoot);
            }

            // 3. Tạo Rain Volume (Child)
            Transform rainVolTrans = weatherRoot.transform.Find("Rain_PostProcess_Volume");
            GameObject rainVolObj;
            if (rainVolTrans == null)
            {
                rainVolObj = new GameObject("Rain_PostProcess_Volume");
                rainVolObj.transform.SetParent(weatherRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(rainVolObj, "Create Rain Volume");
            }
            else
            {
                rainVolObj = rainVolTrans.gameObject;
            }

            var rainVol = rainVolObj.GetComponent<Volume>();
            if (rainVol == null)
            {
                rainVol = rainVolObj.AddComponent<Volume>();
            }
            rainVol.isGlobal = true;
            rainVol.priority = 10;
            rainVol.weight = 0f; // Bắt đầu ở Nắng Trưa nên weight = 0
            rainVol.sharedProfile = rainProfile;

            // 4. Tạo Particle System mưa (Child)
            Transform rainVfxTrans = weatherRoot.transform.Find("Rain_VFX_Particles");
            GameObject rainVfxObj;
            if (rainVfxTrans == null)
            {
                rainVfxObj = new GameObject("Rain_VFX_Particles");
                rainVfxObj.transform.SetParent(weatherRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(rainVfxObj, "Create Rain Particles");
            }
            else
            {
                rainVfxObj = rainVfxTrans.gameObject;
            }

            var ps = rainVfxObj.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = rainVfxObj.AddComponent<ParticleSystem>();
            }

            // Cấu hình Particle System hạt mưa
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.0f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 24f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startColor = new Color(0.85f, 0.92f, 1.0f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;

            var emission = ps.emission;
            emission.rateOverTime = 600f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(32f, 0.1f, 16f);
            shape.rotation = new Vector3(85f, 0f, -10f); // Nghiêng nhẹ theo gió sông

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-3.5f); // Gió thổi xiên nhẹ

            var renderer = rainVfxObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.8f;
            renderer.velocityScale = 0.04f;

            // 5. Tạo Audio Source cho mưa (Child)
            Transform audioTrans = weatherRoot.transform.Find("Weather_Audio_Rain");
            GameObject audioObj;
            if (audioTrans == null)
            {
                audioObj = new GameObject("Weather_Audio_Rain");
                audioObj.transform.SetParent(weatherRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(audioObj, "Create Rain Audio");
            }
            else
            {
                audioObj = audioTrans.gameObject;
            }

            var audioSource = audioObj.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = audioObj.AddComponent<AudioSource>();
            }
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;

            // 6. Gán tham chiếu vào NamBoWeatherController thông qua SerializedObject
            SerializedObject so = new SerializedObject(weatherController);

            // Tìm Directional Light
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    so.FindProperty("mainDirectionalLight").objectReferenceValue = l;
                    break;
                }
            }

            // Tìm Noon Volume
            var noonVolObj = GameObject.Find("Global_Volume_Canh1");
            if (noonVolObj != null)
            {
                so.FindProperty("noonVolume").objectReferenceValue = noonVolObj.GetComponent<Volume>();
            }

            so.FindProperty("rainVolume").objectReferenceValue = rainVol;
            so.FindProperty("rainParticles").objectReferenceValue = ps;
            so.FindProperty("rainAudioSource").objectReferenceValue = audioSource;

            if (Camera.main != null)
            {
                so.FindProperty("targetCamera").objectReferenceValue = Camera.main.transform;
            }

            so.ApplyModifiedProperties();

            // Đánh dấu Scene Dirty và lưu lại
            EditorUtility.SetDirty(weatherRoot);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[WeatherSetup] Nam Bo Weather System successfully installed in Scene!");
        }
    }
}
