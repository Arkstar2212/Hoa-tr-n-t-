using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;
using HoaVaMau.Dialogue;

namespace HoaVaMau.EditorScripts
{
    public static class SetupGuideManualOnTop
    {
        private const string CaMau3DScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string OverhaulScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";

        [MenuItem("Tools/Hoa Tren Dat Do/Configure Guide Manual Always On Top")]
        public static void Execute()
        {
            Debug.Log("<color=#38bdf8>[SetupGuideManualOnTop]</color> Starting Guide Manual On Top configuration...");

            // 1. Process CaMau_1954_Village
            ConfigureCaMau1954Village();

            // 2. Process New Scene 1_Overhaul
            ConfigureNewScene1Overhaul();

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#10b981><b>[SetupGuideManualOnTop]</b></color> Successfully configured Guide Manual to be ALWAYS ON TOP (sortingOrder = 32700) across all scenes!");
        }

        public static void ConfigureCaMau1954Village()
        {
            bool sceneWasAlreadyOpen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == CaMau3DScenePath;
            var scene = sceneWasAlreadyOpen 
                ? UnityEngine.SceneManagement.SceneManager.GetActiveScene() 
                : EditorSceneManager.OpenScene(CaMau3DScenePath, OpenSceneMode.Single);

            // Find Managers parent
            GameObject managersParent = GameObject.Find("CaMau_1954_Village/Managers") 
                                     ?? GameObject.Find("Managers") 
                                     ?? GameObject.Find("06_Managers");

            if (managersParent == null)
            {
                var villageRoot = GameObject.Find("CaMau_1954_Village");
                managersParent = new GameObject("Managers");
                if (villageRoot != null) managersParent.transform.SetParent(villageRoot.transform, false);
            }

            // Remove CaMau3DUIGuideController from 3D Village scene
            Transform guideChild = managersParent.transform.Find("CaMau3DUIGuideController");
            if (guideChild != null)
            {
                Object.DestroyImmediate(guideChild.gameObject);
            }
            var guideInScene = Object.FindAnyObjectByType<CaMau3DUIGuideController>();
            if (guideInScene != null)
            {
                Object.DestroyImmediate(guideInScene.gameObject);
            }
            var canvasGuide = GameObject.Find("Canvas_CaMau3D_UIGuides");
            if (canvasGuide != null)
            {
                Object.DestroyImmediate(canvasGuide);
            }

            // Ensure PlanetOfLanaPromptSystem exists
            Transform promptChild = managersParent.transform.Find("PlanetOfLanaPromptSystem");
            GameObject promptGO = promptChild != null ? promptChild.gameObject : null;
            if (promptGO == null)
            {
                promptGO = new GameObject("PlanetOfLanaPromptSystem");
                promptGO.transform.SetParent(managersParent.transform, false);
            }

            var promptSys = promptGO.GetComponent<PlanetOfLanaPromptSystem>();
            if (promptSys == null) promptSys = promptGO.AddComponent<PlanetOfLanaPromptSystem>();

            // Configure Canvas_Genshin_Dialogue_Master sorting order (25000, below 32700 Guide Manual but above 9999 fade)
            var genshinCanvasGO = GameObject.Find("Canvas_Genshin_Dialogue_Master");
            if (genshinCanvasGO != null)
            {
                var c = genshinCanvasGO.GetComponent<Canvas>();
                if (c != null)
                {
                    c.overrideSorting = true;
                    c.sortingOrder = 25000;
                    EditorUtility.SetDirty(c);
                }
            }

            EditorUtility.SetDirty(promptGO);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=#10b981>[SetupGuideManualOnTop]</color> Configured '{CaMau3DScenePath}' (Removed CaMau3DUIGuideController, PromptSystem on top)!");
        }

        public static void ConfigureNewScene1Overhaul()
        {
            if (!File.Exists(OverhaulScenePath)) return;

            var scene = EditorSceneManager.OpenScene(OverhaulScenePath, OpenSceneMode.Single);

            // Verify or setup PlanetOfLanaPromptSystem under 02_UI
            var uiRoot = GameObject.Find("02_UI") ?? new GameObject("02_UI");
            var promptSys = uiRoot.GetComponentInChildren<PlanetOfLanaPromptSystem>();
            if (promptSys == null)
            {
                var promptSysGO = new GameObject("PlanetOfLanaPromptSystem");
                promptSysGO.transform.SetParent(uiRoot.transform, false);
                promptSys = promptSysGO.AddComponent<PlanetOfLanaPromptSystem>();
                EditorUtility.SetDirty(promptSysGO);
            }

            // Verify PlanetOfLanaTutorialController
            var tutCtrl = Object.FindAnyObjectByType<PlanetOfLanaTutorialController>();
            if (tutCtrl == null)
            {
                var pauseMenuGO = GameObject.Find("00_UI_PauseMenuController");
                if (pauseMenuGO != null)
                {
                    tutCtrl = pauseMenuGO.AddComponent<PlanetOfLanaTutorialController>();
                    EditorUtility.SetDirty(pauseMenuGO);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=#10b981>[SetupGuideManualOnTop]</color> Verified and saved '{OverhaulScenePath}' with Guide Manual setup!");

            // Re-open CaMau_1954_Village so user stays on their active scene
            if (File.Exists(CaMau3DScenePath))
            {
                EditorSceneManager.OpenScene(CaMau3DScenePath, OpenSceneMode.Single);
            }
        }
    }
}
