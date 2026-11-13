#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HoaTrenDatDo.EditorTools
{
    /// <summary>
    /// Idempotent fixes for residual visual defects found in "New Scene 1_Overhaul":
    ///  1. TrajectoryPredictor LineRenderer is serialized enabled with no material (magenta line in Edit mode;
    ///     the component disables itself in Awake at runtime).
    ///  2. Outer BackWall sprites of PF_Rock_Mountain_CavePass poke out past the flank rocks as black wedges.
    /// Never touches paddy / rice objects.
    /// </summary>
    public static class FixResidualSceneIssues
    {
        private struct Shift { public string name; public float originalX; public float dx; }

        private static readonly Shift[] CaveBackWallShifts =
        {
            new Shift { name = "BackWall_FarLeft",  originalX = 128.33f, dx = +2.0f },
            new Shift { name = "BackWall_MidLeft",  originalX = 131.53f, dx = +1.5f },
            new Shift { name = "BackWall_MidRight", originalX = 138.13f, dx = -1.5f },
            new Shift { name = "BackWall_FarRight", originalX = 140.53f, dx = -2.0f },
        };

        [MenuItem("Tools/Hoa Do/Fix Residual Scene Issues")]
        public static string Run()
        {
            var log = new System.Text.StringBuilder();
            bool changed = false;

            // 1) Pink predictor line
            var tp = GameObject.Find("TrajectoryPredictor");
            if (tp != null)
            {
                var lr = tp.GetComponent<LineRenderer>();
                if (lr != null && lr.enabled)
                {
                    Undo.RecordObject(lr, "Disable TrajectoryPredictor line");
                    lr.enabled = false;
                    EditorUtility.SetDirty(lr);
                    changed = true;
                    log.AppendLine("TrajectoryPredictor LineRenderer disabled (runtime Awake re-enables on demand).");
                }
            }

            // 2) Cave back-wall wedges
            var cave = GameObject.Find("PF_Rock_Mountain_CavePass");
            if (cave != null)
            {
                foreach (var s in CaveBackWallShifts)
                {
                    foreach (var t in cave.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name != s.name) continue;
                        if (Mathf.Abs(t.position.x - s.originalX) > 0.02f) { log.AppendLine($"{s.name}: already adjusted or moved (x={t.position.x:F2}), skipped."); continue; }
                        Undo.RecordObject(t, "Shift cave back wall");
                        t.position += new Vector3(s.dx, 0f, 0f);
                        changed = true;
                        log.AppendLine($"{s.name}: x {s.originalX:F2} -> {t.position.x:F2}");
                    }
                }
            }

            if (changed)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("Scene saved.");
            }
            else log.AppendLine("Nothing to change.");
            return log.ToString();
        }
    }
}
#endif
