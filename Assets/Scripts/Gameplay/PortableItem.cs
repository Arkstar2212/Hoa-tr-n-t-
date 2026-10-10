using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Pickable and throwable object in Planet of Lana style.
    /// Can be picked up with 'E', carried in front of the player,
    /// dropped gently, or aimed and launched with variable distance.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class PortableItem : MonoBehaviour
    {
        [Header("Item Properties")]
        [SerializeField] private string itemName = "Ancient Power Core";
        [SerializeField] private float pickupRadius = 1.35f;
        [SerializeField] private float itemMass = 2.5f;

        [Header("Respawn")]
        [SerializeField] private float respawnBelowY = -15f;

        private Rigidbody rb;
        private Collider col;
        private Vector3 initialSpawnPos;
        private bool isCarried = false;
        private bool wasThrown = false;
        private PlayerController carrier;

        public bool IsCarried => isCarried;
        public string ItemName => itemName;
        public float PickupRadius => pickupRadius;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            initialSpawnPos = transform.position;

            ConfigurePhysics();
        }

        private void ConfigurePhysics()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            rb.mass = itemMass;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Lock Z plane for 2.5D gameplay
            rb.constraints = RigidbodyConstraints.FreezePositionZ |
                             RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationY;
        }

        private void Update()
        {
            if (isCarried && carrier != null)
            {
                // Lock Z to 0 during carry
                Vector3 p = transform.position;
                p.z = 0f;
                transform.position = p;
                return;
            }

            // Fall-off-world safety respawn
            if (transform.position.y < respawnBelowY)
            {
                Respawn();
            }
        }

        /// <summary>
        /// Checks if player is within pickup range.
        /// </summary>
        public bool CanPickup(Vector3 playerPos)
        {
            if (isCarried) return false;
            float dist = Vector3.Distance(transform.position, playerPos);
            return dist <= pickupRadius;
        }

        /// <summary>
        /// Attaches item to player hands.
        /// </summary>
        public void OnPickedUp(PlayerController player, Transform carrySocket)
        {
            isCarried = true;
            carrier = player;

            rb.isKinematic = true;
            rb.useGravity = false;
            col.enabled = false;

            transform.SetParent(carrySocket);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Gently places item down in front of player.
        /// </summary>
        public void OnDropped(Vector3 dropPosition, Vector3 gentleNudge)
        {
            isCarried = false;
            carrier = null;

            transform.SetParent(null);
            transform.position = new Vector3(dropPosition.x, dropPosition.y, 0f);

            col.enabled = true;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = gentleNudge;
            wasThrown = false;
        }

        /// <summary>
        /// Launches item along specified velocity arc.
        /// </summary>
        public void OnThrown(Vector3 launchPosition, Vector3 throwVelocity)
        {
            isCarried = false;
            carrier = null;

            transform.SetParent(null);
            transform.position = new Vector3(launchPosition.x, launchPosition.y, 0f);

            col.enabled = true;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = throwVelocity;
            rb.angularVelocity = new Vector3(0f, 0f, -Mathf.Sign(throwVelocity.x) * 12f);
            wasThrown = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (wasThrown || collision.relativeVelocity.magnitude > 2.8f)
            {
                var destructible = collision.gameObject.GetComponentInParent<DestructibleCrate>();
                if (destructible != null && !destructible.IsDestroyed)
                {
                    Vector3 contactPt = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                    destructible.TakeDamage(1, contactPt, collision.relativeVelocity);
                }
            }
            wasThrown = false;
        }

        public void Respawn()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = initialSpawnPos;
            wasThrown = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
