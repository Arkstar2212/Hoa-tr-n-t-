using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Component quản lý hướng xoay tự động của cây/cỏ/hoa lá 2.5D trong không gian 3D:
    /// - YAxisBillboard: Xoay quanh trục đứng Y hướng về Camera (giữ tư thế đứng thẳng, không bị nghiêng đổ).
    /// - CrossQuads: Cấu trúc 2 hoặc 3 mặt cắt vuông góc (Chữ Thập / Hình Sao) giữ thể tích 3D tĩnh cố định khi xoay camera.
    /// - RaycastGrounding: Tự động bắn tia Raycast xuống các mặt phẳng Collider 3D (Terrain/Plane/Platform) để đặt chân gốc bám chính xác vào mặt đất.
    /// </summary>
    [ExecuteAlways]
    public class Billboard3DFoliage : MonoBehaviour
    {
        public enum FoliageFacingMode
        {
            YAxisBillboard, // Quay theo trục Y hướng về Camera (không bị lật ngang hay dẹp lép)
            CameraAligned,  // Quay toàn diện theo Camera
            CrossPlanes3D   // Giữ nguyên góc xoay chéo lập thể 3D (0° và 90°)
        }

        [Header("3D Foliage Rule Settings")]
        [SerializeField] private FoliageFacingMode facingMode = FoliageFacingMode.YAxisBillboard;
        [SerializeField] private bool snapToGroundSurface = true;
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Camera mainCamera;
        private Transform camTransform;

        public FoliageFacingMode Mode
        {
            get => facingMode;
            set => facingMode = value;
        }

        public bool SnapToGround
        {
            get => snapToGroundSurface;
            set => snapToGroundSurface = value;
        }

        private void OnEnable()
        {
            FindCamera();
            if (snapToGroundSurface)
            {
                SnapDownToGround();
            }
        }

        private void Start()
        {
            FindCamera();
            if (snapToGroundSurface)
            {
                SnapDownToGround();
            }
        }

        private void FindCamera()
        {
            if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
            {
                mainCamera = Camera.main;
            }
            if (mainCamera != null)
            {
                camTransform = mainCamera.transform;
            }
        }

        private void LateUpdate()
        {
            if (facingMode == FoliageFacingMode.CrossPlanes3D) return;

            if (camTransform == null)
            {
                FindCamera();
                if (camTransform == null) return;
            }

            if (facingMode == FoliageFacingMode.YAxisBillboard)
            {
                Vector3 toCam = camTransform.position - transform.position;
                toCam.y = 0f; // Khóa trục Y để cây cỏ đứng thẳng tắp trên mặt đất
                if (toCam.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(toCam);
                }
            }
            else if (facingMode == FoliageFacingMode.CameraAligned)
            {
                transform.rotation = camTransform.rotation;
            }
        }

        /// <summary>
        /// Bắn tia xuống mặt phẳng địa hình 3D (Terrain/Plane/Platform) và đặt chân gốc cây cỏ bám chính xác vào bề mặt.
        /// </summary>
        [ContextMenu("Snap To Ground")]
        public void SnapDownToGround()
        {
            Vector3 origin = transform.position + Vector3.up * 3.0f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 15.0f, groundLayers, QueryTriggerInteraction.Ignore))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
            }
        }
    }
}
