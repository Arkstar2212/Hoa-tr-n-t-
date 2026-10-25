using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Controls the secondary NPC observing the scenery in the background / vantage point,
    /// providing natural idle menu behavior (shifting weight, scanning the horizon, checking on An).
    /// </summary>
    public class NPCMenuObserver : MonoBehaviour
    {
        [Header("Animator & Bones")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform headBone;

        [Header("Observation Tuning")]
        [SerializeField] private float minObserveDuration = 4.0f;
        [SerializeField] private float maxObserveDuration = 9.0f;
        [SerializeField] private float turnSpeed = 1.8f;

        [Header("Subtle Weight Shift")]
        [SerializeField] private float weightShiftSpeed = 0.8f;
        [SerializeField] private float weightShiftAmount = 0.8f; // degrees

        private Quaternion currentHeadOffset = Quaternion.identity;
        private Quaternion targetHeadOffset = Quaternion.identity;
        private float nextActionTime = 0f;
        private Quaternion baseRotation;

        // Observation gaze targets (relative to forward facing)
        private readonly Quaternion[] observeTargets = new Quaternion[]
        {
            Quaternion.Euler(0f, 0f, 0f),       // Gazing straight out over the open ocean
            Quaternion.Euler(-6f, 25f, 0f),     // Scanning right along the horizon
            Quaternion.Euler(-4f, 40f, 0f),     // Watching distant sail boats
            Quaternion.Euler(12f, -30f, 0f),    // Looking down towards An relaxing on the lower ledge
            Quaternion.Euler(-10f, -15f, 0f),   // Looking up at the massive cloud
            Quaternion.Euler(0f, 15f, 0f)       // Shifting attention back to the water
        };

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            baseRotation = transform.localRotation;

            if (headBone == null)
            {
                Transform[] bones = GetComponentsInChildren<Transform>();
                foreach (var b in bones)
                {
                    string n = b.name.ToLower();
                    if (n.Contains("head") || n.Contains("c_head") || n.Contains("cc_base_head"))
                    {
                        headBone = b;
                        break;
                    }
                }
            }
        }

        private void Start()
        {
            ScheduleNextObservation();
        }

        private void Update()
        {
            if (Time.time >= nextActionTime)
            {
                ScheduleNextObservation();
            }

            // Head rotation interpolation
            currentHeadOffset = Quaternion.Slerp(currentHeadOffset, targetHeadOffset, Time.deltaTime * turnSpeed);

            // Subtle body sway / weight shifting between feet
            float sway = Mathf.Sin(Time.time * weightShiftSpeed) * weightShiftAmount;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, sway, sway * 0.3f);
        }

        private void LateUpdate()
        {
            if (headBone != null)
            {
                headBone.localRotation = headBone.localRotation * currentHeadOffset;
            }
        }

        private void ScheduleNextObservation()
        {
            int idx = Random.Range(0, observeTargets.Length);
            targetHeadOffset = observeTargets[idx];
            nextActionTime = Time.time + Random.Range(minObserveDuration, maxObserveDuration);
        }
    }
}
