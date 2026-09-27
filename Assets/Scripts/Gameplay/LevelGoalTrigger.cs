using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Level Goal Trigger attached to the Goal Marker at the end of Chapter 0.
    /// Handles victory condition, disables hazards/enemy aggro, triggers atmospheric celebration VFX,
    /// and displays a cinematic non-verbal chapter completion banner.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LevelGoalTrigger : MonoBehaviour
    {
        public static LevelGoalTrigger Instance { get; private set; }

        [Header("Status")]
        public bool IsGoalReached { get; private set; } = false;

        [Header("Celebration Particle")]
        [SerializeField] private ParticleSystem celebrationVFX;

        private float goalReachedTime = 0f;

        private void Awake()
        {
            Instance = this;
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsGoalReached) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                TriggerVictory();
            }
        }

        public void TriggerVictory()
        {
            if (IsGoalReached) return;
            IsGoalReached = true;
            goalReachedTime = Time.time;

            Debug.Log("<color=#50e3c2><b>[Hoa và Máu]</b></color> 🎉 CHƯƠNG 0: LÊN ĐƯỜNG - HOÀN THÀNH XUẤT SẮC!");

            // Trigger celebration VFX
            if (celebrationVFX != null)
            {
                celebrationVFX.Play();
            }
            else
            {
                CreateCelebrationParticles();
            }

            // Pacify all enemies
            var enemies = Object.FindObjectsByType<EnemyController>();
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.ResetToDefaultState();
                    enemy.enabled = false; // Gracefully pause enemy patrol
                }
            }
        }

        private void CreateCelebrationParticles()
        {
            GameObject vfxGO = new GameObject("Celebration_Pollen_Burst");
            vfxGO.transform.position = transform.position + Vector3.up * 1.5f;

            ParticleSystem ps = vfxGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.startLifetime = 3.5f;
            main.startSpeed = 3.5f;
            main.startSize = 0.14f;
            main.maxParticles = 120;
            main.startColor = new Color(0.98f, 0.92f, 0.45f, 0.95f); // Golden flower pollen

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 90) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            vel.x = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);

            ParticleSystemRenderer rend = vfxGO.GetComponent<ParticleSystemRenderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader != null)
            {
                Material m = new Material(unlitShader) { name = "Mat_CelebrationGold" };
                m.color = new Color(0.98f, 0.92f, 0.45f, 0.95f);
                rend.sharedMaterial = m;
            }

            celebrationVFX = ps;
            ps.Play();
        }

        private void Update()
        {
            if (IsGoalReached && Time.time - goalReachedTime > 1.5f)
            {
                // Press Space or R to restart from beginning
                bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame)) restartPressed = true;
#else
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R)) restartPressed = true;
#endif
                if (restartPressed)
                {
                    RestartFromStart();
                }
            }
        }

        public void RestartFromStart()
        {
            IsGoalReached = false;
            var enemies = Object.FindObjectsByType<EnemyController>();
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.enabled = true;
                    enemy.ResetToDefaultState();
                }
            }

            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RestartFromBeginning();
            }
        }

        private void OnGUI()
        {
            if (!IsGoalReached) return;

            float elapsed = Time.time - goalReachedTime;
            float alpha = Mathf.Clamp01(elapsed * 1.5f);

            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            int boxWidth = 480;
            int boxHeight = 160;
            Rect centerRect = new Rect((Screen.width - boxWidth) * 0.5f, (Screen.height - boxHeight) * 0.35f, boxWidth, boxHeight);

            GUI.backgroundColor = new Color(0.08f, 0.14f, 0.12f, 0.92f);
            GUILayout.BeginArea(centerRect, GUI.skin.box);

            GUILayout.Space(12);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label("<color=#50e3c2>HOA VÀ MÁU</color>", titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Normal
            };
            GUILayout.Label("<color=#f5f5f5>Chương 0: Lên Đường — <b>HOÀN THÀNH XUẤT SẮC!</b></color>", subStyle);

            GUILayout.Space(8);
            GUIStyle quoteStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Italic
            };
            GUILayout.Label("<color=#a8d5c8>\"Chuyến tàu rời bến Sông Đốc, mang theo những ước mơ về một ngày mai hòa bình...\"</color>", quoteStyle);

            GUILayout.Space(12);
            GUIStyle tipStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            GUILayout.Label("<color=#f5a623>[ Nhấn phím SPACE hoặc R để chơi lại từ đầu ]</color>", tipStyle);

            GUILayout.EndArea();
            GUI.color = prevColor;
        }
    }
}
