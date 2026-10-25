using System.Collections;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Controls An sitting on the rocky platform, chilling, breathing, and looking around naturally.
    /// Provides subtle procedural head tracking and micro-movements for an atmospheric idle menu experience.
    /// </summary>
    public class AnChillIdleController : MonoBehaviour
    {
        [Header("Animator & Bones")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform neckBone;

        [Header("Look Around Tuning")]
        [SerializeField] private float minLookDuration = 3.5f;
        [SerializeField] private float maxLookDuration = 7.0f;
        [SerializeField] private float lookTurnSpeed = 2.2f;

        [Header("Breathing Tuning")]
        [SerializeField] private float breathSpeed = 1.6f;
        [SerializeField] private float breathAmplitude = 0.012f;

        private Vector3 initialScale;
        private Quaternion currentHeadOffset = Quaternion.identity;
        private Quaternion targetHeadOffset = Quaternion.identity;
        private float nextLookChangeTime = 0f;

        // Variety of relaxed gaze directions relative to character forward
        private readonly Quaternion[] gazeDirections = new Quaternion[]
        {
            Quaternion.Euler(0f, 0f, 0f),       // Straight ahead towards the horizon
            Quaternion.Euler(-4f, 22f, 0f),     // Looking right towards the shimmering sun on the sea
            Quaternion.Euler(-8f, 35f, 0f),     // Looking further right towards the distant sailing boats
            Quaternion.Euler(6f, -18f, 0f),     // Looking slightly left towards the water below
            Quaternion.Euler(-14f, 10f, 0f),    // Gazing upwards at the towering clouds
            Quaternion.Euler(2f, -32f, 0f)      // Glancing back/left towards the companion NPC
        };

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            initialScale = transform.localScale;

            // Auto-detect head and neck bones if not assigned
            if (headBone == null)
            {
                Transform[] allBones = GetComponentsInChildren<Transform>();
                foreach (var b in allBones)
                {
                    string n = b.name.ToLower();
                    if (headBone == null && (n.Contains("head") || n.Contains("c_head") || n.Contains("cc_base_head")))
                    {
                        headBone = b;
                    }
                    else if (neckBone == null && (n.Contains("neck") || n.Contains("c_neck") || n.Contains("cc_base_neck")))
                    {
                        neckBone = b;
                    }
                }
            }
        }

        private void Start()
        {
            ScheduleNextLook();
        }

        private void Update()
        {
            // 1. Chill looking around selection
            if (Time.time >= nextLookChangeTime)
            {
                ScheduleNextLook();
            }

            // Smoothly interpolate current head gaze
            currentHeadOffset = Quaternion.Slerp(currentHeadOffset, targetHeadOffset, Time.deltaTime * lookTurnSpeed);

            // 2. Natural subtle breathing scale cycle
            float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmplitude;
            transform.localScale = new Vector3(
                initialScale.x * (1f - breath * 0.3f),
                initialScale.y * (1f + breath),
                initialScale.z * (1f + breath * 0.4f)
            );
        }

        private void LateUpdate()
        {
            // Apply head offset after animation evaluation
            if (headBone != null)
            {
                headBone.localRotation = headBone.localRotation * currentHeadOffset;
            }
            if (neckBone != null)
            {
                // Soft split: 30% on neck, 70% on head
                Quaternion neckSplit = Quaternion.Slerp(Quaternion.identity, currentHeadOffset, 0.35f);
                neckBone.localRotation = neckBone.localRotation * neckSplit;
            }
        }

        private void ScheduleNextLook()
        {
            int randIndex = Random.Range(0, gazeDirections.Length);
            targetHeadOffset = gazeDirections[randIndex];
            nextLookChangeTime = Time.time + Random.Range(minLookDuration, maxLookDuration);
        }
    }
}
