using UnityEngine;

namespace HoaVaMau.Water
{
    [RequireComponent(typeof(Rigidbody))]
    public class WaterFloater : MonoBehaviour
    {
        [Header("Buoyancy Settings")]
        [Tooltip("Multiplies upward buoyant force.")]
        public float buoyancyPower = 18f;

        [Tooltip("Linear drag inside water to prevent endless bouncing.")]
        public float waterDrag = 2.5f;

        [Tooltip("Angular drag inside water to stabilize tilting.")]
        public float waterAngularDrag = 2.0f;

        [Header("Float Points")]
        [Tooltip("Offsets relative to transform. If empty, 4 corners are generated automatically.")]
        public Vector3[] customFloatPoints;

        [Header("Wave Alignment")]
        [Tooltip("Power to align boat up-vector to wave surface normal.")]
        public float normalAlignmentSpeed = 3.0f;

        private Rigidbody rb;
        private float originalDrag;
        private float originalAngularDrag;
        private Vector3[] activeFloatPoints;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            originalDrag = rb.linearDamping;
            originalAngularDrag = rb.angularDamping;

            SetupFloatPoints();
        }

        private void SetupFloatPoints()
        {
            if (customFloatPoints != null && customFloatPoints.Length > 0)
            {
                activeFloatPoints = customFloatPoints;
            }
            else
            {
                // Auto generate 4 corner float points based on collider or default bounds
                Collider col = GetComponent<Collider>();
                Vector3 extents = col != null ? col.bounds.extents : new Vector3(0.8f, 0.3f, 1.5f);
                float yOffset = -extents.y * 0.5f;

                activeFloatPoints = new Vector3[]
                {
                    new Vector3(-extents.x * 0.7f, yOffset,  extents.z * 0.7f), // Front-Left
                    new Vector3( extents.x * 0.7f, yOffset,  extents.z * 0.7f), // Front-Right
                    new Vector3(-extents.x * 0.7f, yOffset, -extents.z * 0.7f), // Back-Left
                    new Vector3( extents.x * 0.7f, yOffset, -extents.z * 0.7f), // Back-Right
                };
            }
        }

        private void FixedUpdate()
        {
            int submergedPointsCount = 0;
            Vector3 avgNormal = Vector3.up;

            for (int i = 0; i < activeFloatPoints.Length; i++)
            {
                Vector3 worldPt = transform.TransformPoint(activeFloatPoints[i]);
                float waterHeight = GerstnerWaterManager.GetWaterHeight(worldPt);

                if (worldPt.y < waterHeight)
                {
                    submergedPointsCount++;
                    float displacementFactor = Mathf.Clamp01((waterHeight - worldPt.y) / 1.0f);
                    
                    // Buoyant force applied at each point creates natural pitch and roll
                    Vector3 buoyantForce = Vector3.up * Mathf.Abs(waterHeight - worldPt.y) * (buoyancyPower / activeFloatPoints.Length);
                    rb.AddForceAtPosition(buoyantForce, worldPt, ForceMode.Acceleration);

                    avgNormal += GerstnerWaterManager.GetWaterNormal(worldPt);
                }
            }

            if (submergedPointsCount > 0)
            {
                // Inside water damping
                float submersionRatio = (float)submergedPointsCount / activeFloatPoints.Length;
                rb.linearDamping = Mathf.Lerp(originalDrag, waterDrag, submersionRatio);
                rb.angularDamping = Mathf.Lerp(originalAngularDrag, waterAngularDrag, submersionRatio);

                // Align boat deck to wave normal
                avgNormal = (avgNormal / (submergedPointsCount + 1)).normalized;
                Quaternion targetRot = Quaternion.FromToRotation(transform.up, avgNormal) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * normalAlignmentSpeed);
            }
            else
            {
                rb.linearDamping = originalDrag;
                rb.angularDamping = originalAngularDrag;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (activeFloatPoints == null) SetupFloatPoints();

            Gizmos.color = Color.cyan;
            for (int i = 0; i < activeFloatPoints.Length; i++)
            {
                Vector3 pt = transform.TransformPoint(activeFloatPoints[i]);
                Gizmos.DrawWireSphere(pt, 0.12f);
            }
        }
    }
}
