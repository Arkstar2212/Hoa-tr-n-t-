using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Interactive heavy crate that can be pushed and pulled by the player
    /// in classic Planet of Lana / Limbo / Inside style.
    /// - Player approaches either side, holds Left Mouse Button (or E / Gamepad RT) to grab.
    /// - Moving A/D pushes or pulls the crate with heavy weighted biomechanics.
    /// - The top of the crate is a solid walkable surface so the player can push it next
    ///   to a high wall, jump onto it, and grab the ledge above!
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class InteractableCrate : MonoBehaviour
    {
        [Header("Dimensions & Physics")]
        [SerializeField] private Vector3 boxSize = new Vector3(0.92f, 0.92f, 1.0f);
        [SerializeField] private float mass = 60f;
        [SerializeField] private float moveSpeed = 2.1f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Interaction Radius")]
        [SerializeField] private float grabRange = 1.35f;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip cratePushSfx;
        private AudioSource audioSource;

        // Runtime state
        private Rigidbody rb;
        private BoxCollider boxCollider;
        private PlayerController attachedPlayer;
        private int playerSide = 0; // -1 = player is to the left, +1 = player is to the right
        private bool isGrabbed = false;

        public bool IsGrabbed => isGrabbed;
        public int PlayerSide => playerSide;
        public Vector3 BoxSize => boxSize;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();

            if (GetComponent<DestructibleCrate>() == null)
            {
                gameObject.AddComponent<DestructibleCrate>();
            }

            ConfigurePhysics();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = true;
                audioSource.spatialBlend = 0.4f;
            }
#if UNITY_EDITOR
            if (cratePushSfx == null)
            {
                cratePushSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Crate_Push.mp3");
            }
#endif
        }

        private void Reset()
        {
            ConfigurePhysics();
        }

        private void ConfigurePhysics()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

            rb.mass = mass;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Constrain movement strictly to 2D plane (X/Y) and prevent tilting/spinning
            rb.constraints = RigidbodyConstraints.FreezePositionZ |
                             RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationY |
                             RigidbodyConstraints.FreezeRotationZ;

            boxCollider.size = boxSize;
            boxCollider.center = new Vector3(0f, boxSize.y * 0.5f, 0f);

            AlignChildMeshToBase();
        }

        /// <summary>
        /// Đồng bộ chính xác đáy của MeshRenderer với đáy của BoxCollider tại Y = 0 (tránh lún đất).
        /// </summary>
        public void AlignChildMeshToBase()
        {
            var meshFilter = GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var child = meshFilter.transform;
                float rawMeshHeight = meshFilter.sharedMesh.bounds.size.y;
                if (rawMeshHeight > 0.0001f)
                {
                    float targetScale = boxSize.y / rawMeshHeight;
                    child.localScale = new Vector3(targetScale, targetScale, targetScale);
                }
                float meshCenterOffsetY = meshFilter.sharedMesh.bounds.center.y * child.localScale.y;
                child.localPosition = new Vector3(0f, (boxSize.y * 0.5f) - meshCenterOffsetY, 0f);
            }
        }

        /// <summary>
        /// Checks if a player is close enough to grab this crate and returns which side they are on.
        /// </summary>
        public bool CanPlayerGrab(Vector3 playerPos, out int side)
        {
            side = 0;
            if (isGrabbed) return false;

            var destructible = GetComponent<DestructibleCrate>();
            if (destructible != null && destructible.IsDestroyed) return false;

            Vector3 crateCenter = transform.position + new Vector3(0f, boxSize.y * 0.5f, 0f);
            float deltaX = playerPos.x - crateCenter.x;
            float deltaY = Mathf.Abs(playerPos.y - transform.position.y);

            // Must be roughly at same ground elevation
            if (deltaY > 1.2f) return false;

            float halfWidth = boxSize.x * 0.5f;
            float distFromLeft = Mathf.Abs(playerPos.x - (crateCenter.x - halfWidth));
            float distFromRight = Mathf.Abs(playerPos.x - (crateCenter.x + halfWidth));

            if (deltaX < 0 && distFromLeft <= grabRange)
            {
                side = -1; // Player is on left side
                return true;
            }
            if (deltaX > 0 && distFromRight <= grabRange)
            {
                side = 1; // Player is on right side
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the ideal player attachment position along the X axis.
        /// </summary>
        public Vector3 GetPlayerAnchorPosition(int side)
        {
            float halfWidth = boxSize.x * 0.5f;
            float playerOffset = 0.32f; // Calibrated for Bé An's arm reach (0.24m - 0.28m) so hands rest solidly on crate face
            float targetX = transform.position.x + side * (halfWidth + playerOffset);
            return new Vector3(targetX, transform.position.y, 0f);
        }

        /// <summary>
        /// Attaches player to this crate for pushing/pulling.
        /// </summary>
        public void StartGrab(PlayerController player, int side)
        {
            attachedPlayer = player;
            playerSide = side;
            isGrabbed = true;

            // Tạm thời vô hiệu hóa va chạm giữa CharacterController của người chơi và BoxCollider thùng gỗ
            // để triệt tiêu 100% hiện tượng PhysX depenetration đẩy capsule nhân vật bay lên không trung
            var charCtrl = player.GetComponent<CharacterController>();
            if (charCtrl != null)
            {
                foreach (var col in GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(charCtrl, col, true);
                }
            }
        }

        /// <summary>
        /// Detaches player from this crate.
        /// </summary>
        public void ReleaseGrab()
        {
            if (attachedPlayer != null)
            {
                var charCtrl = attachedPlayer.GetComponent<CharacterController>();
                if (charCtrl != null)
                {
                    foreach (var col in GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(charCtrl, col, false);
                    }
                }
            }

            attachedPlayer = null;
            playerSide = 0;
            isGrabbed = false;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Moves the crate based on horizontal input from the attached player.
        /// Performs raycast obstacle check in push/pull direction to prevent tunneling into walls.
        /// </summary>
        public void MoveWithPlayer(float horizontalInput, float dt)
        {
            if (!isGrabbed || attachedPlayer == null) return;

            // Lock Z to 0
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;

            if (Mathf.Abs(horizontalInput) < 0.05f)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
                return;
            }

            float moveDir = Mathf.Sign(horizontalInput);
            float targetVelX = moveDir * moveSpeed;

            // Check if crate is blocked in moving direction
            Vector3 crateCenter = transform.position + new Vector3(0f, boxSize.y * 0.5f, 0f);
            float rayDist = (boxSize.x * 0.5f) + 0.12f;
            Ray rayLow = new Ray(crateCenter + Vector3.down * (boxSize.y * 0.3f), Vector3.right * moveDir);
            Ray rayMid = new Ray(crateCenter, Vector3.right * moveDir);

            int layerMaskWithoutPlayer = obstacleMask & ~(1 << attachedPlayer.gameObject.layer);

            if (Physics.Raycast(rayLow, out RaycastHit hitLow, rayDist, layerMaskWithoutPlayer, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(rayMid, out RaycastHit hitMid, rayDist, layerMaskWithoutPlayer, QueryTriggerInteraction.Ignore))
            {
                // Obstructed by wall, stop crate horizontal movement
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return;
            }

            rb.linearVelocity = new Vector3(targetVelX, rb.linearVelocity.y, 0f);

            if (cratePushSfx != null && audioSource != null)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = cratePushSfx;
                    audioSource.volume = 0.85f;
                    audioSource.Play();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + new Vector3(0f, boxSize.y * 0.5f, 0f);
            Gizmos.DrawWireCube(center, boxSize);

            // Draw grab zones
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center + Vector3.left * (boxSize.x * 0.5f + 0.5f), 0.3f);
            Gizmos.DrawWireSphere(center + Vector3.right * (boxSize.x * 0.5f + 0.5f), 0.3f);
        }
    }
}
