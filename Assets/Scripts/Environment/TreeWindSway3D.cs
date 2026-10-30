using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Hệ thống đung đưa cây trong gió 3D cực mượt (Cinematic Smooth Sway).
    /// - Sử dụng sóng gió SmoothStep kết hợp bộ lọc nội suy Quaternion.Slerp thời gian thực.
    /// - Loại bỏ hoàn toàn hiện tượng co giật (jitter/stutter) của các chuỗi xương và tán lá.
    /// - Mô phỏng nhịp gió nhẹ nhàng, thư thái của sông nước Cà Mau 1954.
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    public class TreeWindSway3D : MonoBehaviour
    {
        [Header("--- Wind Sway Configuration ---")]
        [Tooltip("Góc lắc tối đa của thân cây (độ)")]
        [Range(0.5f, 10f)]
        [SerializeField] private float trunkSwayAngle = 2.2f;

        [Tooltip("Góc dập dềnh của tàu lá/tán lá (độ)")]
        [Range(0.5f, 15f)]
        [SerializeField] private float frondFlutterAngle = 3.6f;

        [Tooltip("Tốc độ đung đưa nhịp nhàng (chu kỳ/giây)")]
        [Range(0.2f, 3f)]
        [SerializeField] private float swaySpeed = 0.85f;

        [Tooltip("Độ giật/bất định của gió Cà Mau")]
        [Range(0f, 1f)]
        [SerializeField] private float windTurbulence = 0.20f;

        [Header("--- Wind Direction ---")]
        [Tooltip("Hướng gió thổi mặc định")]
        [SerializeField] private Vector3 defaultWindDir = new Vector3(1f, 0f, 0.3f);

        [Tooltip("Sử dụng tọa độ thế giới (X, Z) để tạo độ lệch pha tự nhiên giữa các cây")]
        [SerializeField] private bool useSpatialPhase = true;

        [Tooltip("Độ lệch pha thủ công")]
        [SerializeField] private float manualPhaseOffset = 0f;

        [Header("--- Advanced / Hierarchy ---")]
        [Tooltip("Tự động tìm kiếm xương nếu có SkinnedMeshRenderer")]
        [SerializeField] private bool autoDetectBones = true;

        [Serializable]
        private struct BoneEntry
        {
            public Transform boneTransform;
            public Quaternion initialLocalRotation;
            public bool isTrunk;
            public float weight;
            public float phase;
        }

        [SerializeField] private List<BoneEntry> cachedBones = new List<BoneEntry>();
        [SerializeField] private List<Transform> cachedCanopies = new List<Transform>();
        [SerializeField] private List<Quaternion> cachedCanopyRotations = new List<Quaternion>();

        private Quaternion initialTransformRotation;
        private bool isInitialized = false;
        private float spatialPhase = 0f;

        public float TrunkSwayAngle { get => trunkSwayAngle; set => trunkSwayAngle = value; }
        public float FrondFlutterAngle { get => frondFlutterAngle; set => frondFlutterAngle = value; }
        public float SwaySpeed { get => swaySpeed; set => swaySpeed = value; }
        public float WindTurbulence { get => windTurbulence; set => windTurbulence = value; }

        private void OnEnable()
        {
            InitializeSway();
        }

        private void Start()
        {
            InitializeSway();
        }

        public void InitializeSway()
        {
            initialTransformRotation = transform.localRotation;
            UpdateSpatialPhase();

            cachedBones.Clear();
            cachedCanopies.Clear();
            cachedCanopyRotations.Clear();

            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (autoDetectBones && smr != null && smr.bones != null && smr.bones.Length > 0)
            {
                AnalyzeSkeletalHierarchy(smr);
            }
            else
            {
                AnalyzeTransformHierarchy();
            }

            isInitialized = true;
        }

        private void UpdateSpatialPhase()
        {
            if (useSpatialPhase)
            {
                Vector3 pos = transform.position;
                spatialPhase = (pos.x * 0.35f) + (pos.z * 0.25f) + manualPhaseOffset;
            }
            else
            {
                spatialPhase = manualPhaseOffset;
            }
        }

        private void AnalyzeSkeletalHierarchy(SkinnedMeshRenderer smr)
        {
            Transform root = smr.rootBone;
            if (root == null)
            {
                var armature = transform.Find("Armature");
                if (armature != null && armature.childCount > 0) root = armature.GetChild(0);
            }

            if (root == null) return;

            List<Transform> trunkChain = new List<Transform>();
            Transform current = root;

            if (current.childCount == 1)
            {
                current = current.GetChild(0);
            }

            while (current != null && current.childCount == 1)
            {
                trunkChain.Add(current);
                current = current.GetChild(0);
            }

            if (current != null && current.childCount > 1)
            {
                trunkChain.Add(current);

                for (int i = 0; i < trunkChain.Count; i++)
                {
                    float factor = (float)(i + 1) / trunkChain.Count;
                    cachedBones.Add(new BoneEntry
                    {
                        boneTransform = trunkChain[i],
                        initialLocalRotation = trunkChain[i].localRotation,
                        isTrunk = true,
                        weight = factor * factor,
                        phase = factor * 0.8f
                    });
                }

                for (int c = 0; c < current.childCount; c++)
                {
                    CollectFrondChain(current.GetChild(c), c * 0.7f, 0);
                }
            }
            else
            {
                for (int c = 0; c < root.childCount; c++)
                {
                    CollectFrondChain(root.GetChild(c), c * 0.65f, 0);
                }
            }
        }

        private void CollectFrondChain(Transform branchRoot, float branchPhase, int depth)
        {
            if (branchRoot == null) return;

            float depthWeight = Mathf.Clamp01(0.35f + depth * 0.25f);
            cachedBones.Add(new BoneEntry
            {
                boneTransform = branchRoot,
                initialLocalRotation = branchRoot.localRotation,
                isTrunk = false,
                weight = depthWeight,
                phase = branchPhase + depth * 0.35f
            });

            for (int i = 0; i < branchRoot.childCount; i++)
            {
                CollectFrondChain(branchRoot.GetChild(i), branchPhase, depth + 1);
            }
        }

        private void AnalyzeTransformHierarchy()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                string childName = child.name.ToLower();
                if (childName.Contains("canopy") || childName.Contains("foliage") || 
                    childName.Contains("leaf") || childName.Contains("leaves") || 
                    childName.Contains("branch") || childName.Contains("top"))
                {
                    cachedCanopies.Add(child);
                    cachedCanopyRotations.Add(child.localRotation);
                }
            }
        }

        private void Update()
        {
            if (!isInitialized)
            {
                InitializeSway();
            }

            float dt = Application.isPlaying ? Time.deltaTime : 0.02f;
            if (dt <= 0.0001f) dt = 0.02f;

            float timeVal;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                timeVal = (float)UnityEditor.EditorApplication.timeSinceStartup;
            }
            else
            {
                timeVal = Time.time;
            }
#else
            timeVal = Time.time;
#endif

            float globalWindFactor = 1.0f;
            Vector3 activeWindDir = defaultWindDir.normalized;

            try
            {
                if (HoaVaMau.Gameplay.WindManager.Instance != null)
                {
                    globalWindFactor = 0.7f + Mathf.Abs(HoaVaMau.Gameplay.WindManager.WindWave) * 0.5f;
                    Vector3 gWind = HoaVaMau.Gameplay.WindManager.WindVector;
                    if (gWind.sqrMagnitude > 0.01f)
                    {
                        activeWindDir = gWind.normalized;
                    }
                }
            }
            catch
            {
            }

            float t = timeVal * swaySpeed + spatialPhase;

            // Sóng gió chính mượt mà qua hàm SmoothStep loại bỏ đỉnh nhọn gắt
            float rawSin = Mathf.Sin(t);
            float smoothMainWave = Mathf.SmoothStep(-1f, 1f, (rawSin + 1f) * 0.5f) * 2f - 1f;
            float subWave = Mathf.Sin(t * 1.37f + 0.5f) * 0.20f;
            float gustWave = Mathf.Sin(t * 0.41f) * (windTurbulence * 0.5f);
            float totalWindWave = (smoothMainWave + subWave + gustWave) * globalWindFactor;

            Vector3 bendAxis = Vector3.Cross(Vector3.up, activeWindDir).normalized;
            if (bendAxis.sqrMagnitude < 0.01f) bendAxis = Vector3.forward;

            if (cachedBones.Count > 0)
            {
                // Cây có xương (Coconut 45 xương, NipaPalm 39 xương)
                for (int i = 0; i < cachedBones.Count; i++)
                {
                    var entry = cachedBones[i];
                    if (entry.boneTransform == null) continue;

                    if (entry.isTrunk)
                    {
                        // Thân uốn lượn hình học mượt mà
                        float boneAngle = totalWindWave * (trunkSwayAngle * 0.12f) * entry.weight;
                        Quaternion targetTrunkRot = entry.initialLocalRotation * Quaternion.AngleAxis(boneAngle, bendAxis);
                        entry.boneTransform.localRotation = Quaternion.Slerp(entry.boneTransform.localRotation, targetTrunkRot, 8.0f * dt);
                    }
                    else
                    {
                        // Tàu lá dập dềnh êm ái theo nhịp sóng lan truyền
                        float leafTime = timeVal * (swaySpeed * 1.1f) + spatialPhase + entry.phase;
                        float rawLeaf = Mathf.Sin(leafTime);
                        float smoothLeaf = Mathf.SmoothStep(-1f, 1f, (rawLeaf + 1f) * 0.5f) * 2f - 1f;
                        float flutter = smoothLeaf * frondFlutterAngle * entry.weight * globalWindFactor;

                        Quaternion targetLeafRot = entry.initialLocalRotation * Quaternion.Euler(flutter, flutter * 0.15f, flutter * 0.08f);
                        entry.boneTransform.localRotation = Quaternion.Slerp(entry.boneTransform.localRotation, targetLeafRot, 9.0f * dt);
                    }
                }
            }
            else
            {
                // Cây Mesh tĩnh (Chuối 3D, Cây bụi, v.v.)
                float treeTiltAngle = totalWindWave * (trunkSwayAngle * 0.8f);
                Quaternion targetTreeRot = initialTransformRotation * Quaternion.AngleAxis(treeTiltAngle, bendAxis);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetTreeRot, 7.0f * dt);

                for (int i = 0; i < cachedCanopies.Count; i++)
                {
                    Transform canopy = cachedCanopies[i];
                    if (canopy == null) continue;

                    float canopyTime = timeVal * (swaySpeed * 1.2f) + spatialPhase + (i * 0.6f);
                    float canopyWave = Mathf.Sin(canopyTime) * (frondFlutterAngle * 0.4f);
                    Quaternion targetCanopyRot = cachedCanopyRotations[i] * Quaternion.Euler(canopyWave * 0.4f, 0f, canopyWave * 0.7f);
                    canopy.localRotation = Quaternion.Slerp(canopy.localRotation, targetCanopyRot, 8.0f * dt);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateSpatialPhase();
        }
#endif
    }
}
