using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Tạo hoạt ảnh cử động sống động cho chú chó làng quê Nam Bộ:
    /// - Vẫy đuôi nhẹ nhàng (Tail wagging)
    /// - Nhịp thở phập phồng (Breathing idle)
    /// - Xoay đầu ngước nhìn (Head look around / looking towards player)
    /// </summary>
    [ExecuteAlways]
    public class VillageDogIdle : MonoBehaviour
    {
        [Header("--- Tail Wagging ---")]
        [SerializeField] private float tailWagSpeed = 4.5f;
        [SerializeField] private float tailWagAngle = 20.0f;

        [Header("--- Breathing Idle ---")]
        [SerializeField] private float breathSpeed = 1.8f;
        [SerializeField] private float breathAngle = 2.0f;

        [Header("--- Head Subtle Motion ---")]
        [SerializeField] private float headMotionSpeed = 0.8f;
        [SerializeField] private float headTurnAngle = 8.0f;

        private Transform tailBone;
        private Transform spineBone;
        private Transform headBone;

        private Quaternion initialTailRot;
        private Quaternion initialSpineRot;
        private Quaternion initialHeadRot;

        private bool isInitialized = false;

        private void OnEnable()
        {
            InitializeBones();
        }

        private void Start()
        {
            InitializeBones();
        }

        private void InitializeBones()
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.bones != null)
            {
                foreach (var b in smr.bones)
                {
                    if (b == null) continue;
                    string n = b.name.ToLower();
                    if (n.Contains("tail") && tailBone == null)
                    {
                        tailBone = b;
                        initialTailRot = b.localRotation;
                    }
                    else if ((n.Contains("head_0") || n.Contains("head")) && headBone == null)
                    {
                        headBone = b;
                        initialHeadRot = b.localRotation;
                    }
                    else if (n.Contains("spine_0") && spineBone == null)
                    {
                        spineBone = b;
                        initialSpineRot = b.localRotation;
                    }
                }
            }

            isInitialized = (tailBone != null || headBone != null);
        }

        private void Update()
        {
            if (!isInitialized)
            {
                InitializeBones();
                if (!isInitialized) return;
            }

            float t;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                t = (float)UnityEditor.EditorApplication.timeSinceStartup;
            }
            else
            {
                t = Time.time;
            }
#else
            t = Time.time;
#endif

            // Vẫy đuôi nhịp nhàng
            if (tailBone != null)
            {
                float wag = Mathf.Sin(t * tailWagSpeed) * tailWagAngle;
                tailBone.localRotation = initialTailRot * Quaternion.Euler(0f, wag, 0f);
            }

            // Nhịp thở
            if (spineBone != null)
            {
                float breath = Mathf.Sin(t * breathSpeed) * breathAngle;
                spineBone.localRotation = initialSpineRot * Quaternion.Euler(breath, 0f, 0f);
            }

            // Đầu ngó nghiêng tò mò
            if (headBone != null)
            {
                float headTurn = Mathf.Sin(t * headMotionSpeed) * headTurnAngle;
                float headTilt = Mathf.Cos(t * (headMotionSpeed * 0.7f)) * (headTurnAngle * 0.4f);
                headBone.localRotation = initialHeadRot * Quaternion.Euler(headTilt, headTurn, 0f);
            }
        }
    }
}
