using UnityEngine;
using HoaVaMau.Test3D;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// 3D Pick-Up / Carry / Place / Toss Village Item mechanic ported from Overhaul's PortableItem
    /// into full 3D space ("CaMau_1954_Village.unity").
    /// Allows Bé An to pick up countryside baskets, gourds, or tiffin carriers with [E],
    /// carry them across the village, set them down gently with [E], or toss forward with [Q].
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class PortableItem3D : MonoBehaviour
    {
        [Header("Item Metadata")]
        [SerializeField] private string itemTitle = "Rổ Tre Trái Cây";
        [SerializeField] private string pickupMessage = "Đã nhặt Rổ Tre — Nhấn [E] đặt xuống hoặc [Q] ném nhẹ";
        [SerializeField] private float pickupRadius = 1.85f;
        [SerializeField] private Vector3 carryLocalOffset = new Vector3(0f, 0.78f, 0.52f);
        [SerializeField] private float tossForwardSpeed = 4.8f;
        [SerializeField] private float tossUpwardSpeed = 2.4f;

        private Rigidbody rb;
        private Collider itemCollider;
        private Player3DController carrier;

        public bool IsCarried => carrier != null;
        public string ItemTitle => itemTitle;
        public string PickupMessage => pickupMessage;
        public float PickupRadius => pickupRadius;

        public void ConfigureItem(string title, string message)
        {
            if (!string.IsNullOrEmpty(title)) itemTitle = title;
            if (!string.IsNullOrEmpty(message)) pickupMessage = message;
        }

        private void Awake()
        {
            itemCollider = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1.8f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public bool CanPickUp(Transform playerTr)
        {
            if (IsCarried || playerTr == null) return false;
            return Vector3.Distance(playerTr.position, transform.position) <= pickupRadius;
        }

        public void PickUp(Player3DController player)
        {
            if (player == null || IsCarried) return;
            carrier = player;
            player.SetCarriedItem(this);

            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
            if (itemCollider != null) itemCollider.enabled = false;

            transform.SetParent(player.transform, true);
            transform.localPosition = carryLocalOffset;
            transform.localRotation = Quaternion.identity;
        }

        public void PlaceDown()
        {
            if (carrier == null) return;
            Transform pTr = carrier.transform;
            var prev = carrier;
            carrier = null;
            prev.ClearCarriedItem(this);

            transform.SetParent(null, true);
            transform.position = pTr.position + pTr.forward * 0.72f + Vector3.up * 0.25f;

            if (itemCollider != null) itemCollider.enabled = true;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }
        }

        public void TossForward()
        {
            if (carrier == null) return;
            Transform pTr = carrier.transform;
            var prev = carrier;
            carrier = null;
            prev.ClearCarriedItem(this);

            transform.SetParent(null, true);
            transform.position = pTr.position + pTr.forward * 0.68f + Vector3.up * 0.85f;

            if (itemCollider != null) itemCollider.enabled = true;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = pTr.forward * tossForwardSpeed + Vector3.up * tossUpwardSpeed;
            }
        }
    }
}
