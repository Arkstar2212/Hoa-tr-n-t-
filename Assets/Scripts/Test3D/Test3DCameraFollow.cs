using UnityEngine;

namespace HoaVaMau.Test3D
{
    public class Test3DCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Offset Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 4.0f, -6.5f);
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private float lookAtHeightOffset = 1.2f;

        private Vector3 currentVelocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                // Auto-find Player if target is missing
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) target = p.transform;
                else return;
            }

            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

            Vector3 lookTarget = target.position + Vector3.up * lookAtHeightOffset;
            transform.LookAt(lookTarget);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
