using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Perspective Layer Scaler (Giải pháp Bẫy kỹ thuật 2.5D #3):
    /// Tự động bù tỷ lệ (Perspective Scale Compensation) khi kéo thả Layer Card trên trục Z.
    /// Giữ nguyên 100% kích thước hiển thị trên Camera (Screen Footprint) theo đúng bản vẽ Concept/Styleframe,
    /// đồng thời tận dụng triệt để Parallax vật lý 3D tự nhiên khi Camera di chuyển.
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu("Hoa va Mau/Environment/Perspective Layer Scaler")]
    public class PerspectiveLayerScaler : MonoBehaviour
    {
        [Header("Camera & Plane Tham Chiếu")]
        [Tooltip("Camera chính dùng để đo khoảng cách phối cảnh (bỏ trống để tự lấy Camera.main).")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Mặt phẳng tham chiếu nơi vẽ concept ban đầu (Thường là Playable Ground Z = 0).")]
        [SerializeField] private float referenceZ = 0.0f;

        [Tooltip("Kích thước Local Scale chuẩn khi vật thể nằm tại Reference Z.")]
        [SerializeField] private Vector3 referenceScale = Vector3.one;

        [Header("Cơ Chế Bù Tỷ Lệ (Perspective Scaling)")]
        [Tooltip("Bật chế độ tự động tính lại Scale mỗi khi thay đổi tọa độ Z trong Editor.")]
        [SerializeField] private bool lockScreenSize = true;

        [Tooltip("Giữ tỷ lệ đồng nhất (Uniform Scale: X = Y).")]
        [SerializeField] private bool uniformScale = true;

        [Header("Quản Lý Sorting & Depth (Bẫy #1)")]
        [Tooltip("Tự động gán Sorting Order hoặc cấu hình Sorting Group dựa theo độ sâu Z.")]
        [SerializeField] private bool autoManageSorting = true;

        [SerializeField] private int baseSortingOrder = 0;

        // Lưu lại Z trước đó để phát hiện thay đổi trong Editor
        private float lastKnownZ = float.NaN;
        private Vector3 lastAssignedScale = Vector3.zero;

        public float ReferenceZ
        {
            get => referenceZ;
            set { referenceZ = value; UpdateScaleForCurrentZ(); }
        }

        public Vector3 ReferenceScale
        {
            get => referenceScale;
            set { referenceScale = value; UpdateScaleForCurrentZ(); }
        }

        public bool LockScreenSize
        {
            get => lockScreenSize;
            set => lockScreenSize = value;
        }

        private void OnEnable()
        {
            CacheCamera();
            if (referenceScale == Vector3.zero)
            {
                referenceScale = transform.localScale;
            }
            lastKnownZ = transform.position.z;
            UpdateScaleForCurrentZ();
        }

        private void Update()
        {
            // Trong Play Mode nếu object tĩnh thì không cần tính liên tục mỗi frame
            if (Application.isPlaying && !transform.hasChanged) return;

            if (lockScreenSize)
            {
                float currentZ = transform.position.z;
                if (!Mathf.Approximately(currentZ, lastKnownZ) || transform.hasChanged)
                {
                    UpdateScaleForCurrentZ();
                    transform.hasChanged = false;
                }
            }
        }

        private void OnValidate()
        {
            CacheCamera();
            UpdateScaleForCurrentZ();
        }

        private void CacheCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        /// <summary>
        /// Tính toán tỷ lệ phóng đại quang học:
        /// Ratio = (Z_current - Z_cam) / (Z_ref - Z_cam)
        /// </summary>
        public void UpdateScaleForCurrentZ()
        {
            CacheCamera();
            if (targetCamera == null) return;

            float camZ = targetCamera.transform.position.z;
            float distRef = Mathf.Abs(referenceZ - camZ);
            float distCurrent = Mathf.Abs(transform.position.z - camZ);

            if (distRef < 0.001f) return;

            float scaleFactor = distCurrent / distRef;

            Vector3 newScale;
            if (uniformScale)
            {
                newScale = new Vector3(
                    referenceScale.x * scaleFactor,
                    referenceScale.y * scaleFactor,
                    referenceScale.z
                );
            }
            else
            {
                newScale = new Vector3(
                    referenceScale.x * scaleFactor,
                    referenceScale.y * scaleFactor,
                    referenceScale.z * scaleFactor
                );
            }

            if (transform.localScale != newScale)
            {
                transform.localScale = newScale;
                lastAssignedScale = newScale;
                lastKnownZ = transform.position.z;
            }

            if (autoManageSorting)
            {
                ApplySortingOrder();
            }
        }

        /// <summary>
        /// Bẫy kỹ thuật #1: Tự động điều chỉnh Sorting Order theo độ sâu Z
        /// Càng xa camera (Z lớn) -> Order càng nhỏ.
        /// </summary>
        private void ApplySortingOrder()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Quy ước: cứ 1m chiều sâu lùi về sau thì giảm 10 order
                int calculatedOrder = baseSortingOrder - Mathf.RoundToInt(transform.position.z * 10f);
                if (sr.sortingOrder != calculatedOrder)
                {
                    sr.sortingOrder = calculatedOrder;
                }
            }

            var sg = GetComponent<SortingGroup>();
            if (sg != null)
            {
                int calculatedOrder = baseSortingOrder - Mathf.RoundToInt(transform.position.z * 10f);
                if (sg.sortingOrder != calculatedOrder)
                {
                    sg.sortingOrder = calculatedOrder;
                }
            }
        }

        /// <summary>
        /// Lưu trạng thái hiện tại (Z và Scale hiện tại) làm mốc tham chiếu Concept
        /// </summary>
        [ContextMenu("Khóa Vị Trí Hiện Tại Làm Tham Chiếu (Set As Reference)")]
        public void CaptureCurrentAsReference()
        {
            referenceZ = transform.position.z;
            referenceScale = transform.localScale;
            lastKnownZ = referenceZ;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Debug.Log($"<color=#40b8a6>[PerspectiveLayerScaler]</color> Đã khóa mốc tham chiếu cho '{name}': RefZ = {referenceZ:F2}, RefScale = {referenceScale}");
#endif
        }

        /// <summary>
        /// Đưa vật thể về Playable Ground (Z = 0) và khôi phục Base Scale
        /// </summary>
        [ContextMenu("Đưa Về Playable Ground (Z = 0)")]
        public void SnapToPlayableGround()
        {
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;
            UpdateScaleForCurrentZ();
        }

        /// <summary>
        /// Đẩy nhanh vào các tầng kiến trúc tiêu chuẩn (Z Preset)
        /// </summary>
        public void PushToLayerPreset(float targetZ)
        {
#if UNITY_EDITOR
            Undo.RecordObject(transform, "Push Layer Z");
#endif
            Vector3 pos = transform.position;
            pos.z = targetZ;
            transform.position = pos;
            UpdateScaleForCurrentZ();
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ đường nối từ Camera tới vật thể để trực quan hóa tia phối cảnh
            if (targetCamera != null)
            {
                Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.45f);
                Gizmos.DrawLine(targetCamera.transform.position, transform.position);

                // Vẽ khung tham chiếu tại Z = referenceZ
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
                Vector3 refPos = new Vector3(transform.position.x, transform.position.y, referenceZ);
                Gizmos.DrawWireCube(refPos, referenceScale);
            }
        }
    }
}
