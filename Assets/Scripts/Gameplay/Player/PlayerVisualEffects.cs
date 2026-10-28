using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách các hiệu ứng thị giác mang tính cơ học (Procedural Visual Dynamics):
    /// - Độ nghiêng thân người khi chạy nhanh hoặc đẩy/kéo đồ vật (Procedural Lean).
    /// - Độ co giãn khi tiếp đất hoặc bật nhảy (Squash & Stretch).
    /// - Xoay hướng mặt và quản lý các điểm hiển thị bàn tay (Hand Indicators).
    /// </summary>
    public class PlayerVisualEffects
    {
        private readonly Transform transform;
        private readonly Transform visualTransform;
        private Transform handLeft;
        private Transform handRight;

        private float baseVisualScaleX = 0.64f;
        private float baseVisualScaleY = 1.0f;
        private float baseVisualScaleZ = 0.64f;
        private float squashStretchY = 1.0f;

        public float RunLeanAngle { get; set; } = 10.5f;
        public float PushLeanAngle { get; set; } = 14.0f;
        public float PullLeanAngle { get; set; } = -11.0f;
        public float RopeAngle { get; set; } = 0f;

        public float BaseVisualScaleX => baseVisualScaleX;
        public float BaseVisualScaleY => baseVisualScaleY;
        public float BaseVisualScaleZ => baseVisualScaleZ;
        public Transform VisualTransform => visualTransform;

        public PlayerVisualEffects(Transform transform, Transform visualTransform)
        {
            this.transform = transform;
            this.visualTransform = visualTransform;

            if (this.visualTransform != null)
            {
                baseVisualScaleX = this.visualTransform.localScale.x;
                baseVisualScaleY = this.visualTransform.localScale.y;
                baseVisualScaleZ = this.visualTransform.localScale.z;
            }

            SetupHandVisuals();
        }

        public void SetSquashStretch(float factor)
        {
            squashStretchY = factor;
        }

        public void UpdatePostures(
            PlayerState currentState,
            float facingDirection,
            float horizontalInput,
            bool isSprinting,
            bool isCrouching,
            bool isCrawling,
            float standingHeight,
            float crouchHeight,
            float crawlHeight,
            float dt)
        {
            if (visualTransform == null) return;
            if (currentState == PlayerState.LedgeClimb) return;

            float yRot = (facingDirection > 0) ? 0f : 180f;
            float zRot = 0f;
            float targetHeight = standingHeight;
            float widthScale = 1.0f;

            squashStretchY = Mathf.MoveTowards(squashStretchY, 1.0f, 3.2f * dt);

            if (currentState == PlayerState.PushPull)
            {
                bool pushing = (horizontalInput != 0 && Mathf.Sign(horizontalInput) == facingDirection);
                bool pulling = (horizontalInput != 0 && Mathf.Sign(horizontalInput) != facingDirection);

                if (pushing) zRot = -facingDirection * PushLeanAngle;
                else if (pulling) zRot = -facingDirection * PullLeanAngle;
                else zRot = -facingDirection * (PushLeanAngle * 0.5f);

                targetHeight = standingHeight * 0.94f;
                widthScale = 1.05f;

                if (handLeft != null) handLeft.localPosition = new Vector3(0.55f, 0.54f, 0.18f);
                if (handRight != null) handRight.localPosition = new Vector3(0.55f, 0.54f, -0.18f);
            }
            else if (isCrawling)
            {
                targetHeight = crawlHeight;
                widthScale = 1.35f;
                zRot = -facingDirection * 8f;

                if (handLeft != null) handLeft.localPosition = new Vector3(0.35f, 0.20f, 0.18f);
                if (handRight != null) handRight.localPosition = new Vector3(0.35f, 0.20f, -0.18f);
            }
            else if (isCrouching)
            {
                targetHeight = crouchHeight;
                widthScale = 1.15f;
            }
            else if (isSprinting && currentState == PlayerState.Grounded)
            {
                zRot = -facingDirection * RunLeanAngle;
            }
            else if (currentState == PlayerState.RopeGrab || currentState == PlayerState.RopeClimb)
            {
                // In Unity, when facing left (facingDirection < 0), yRot is 180 deg.
                // Rotating around Z when Y=180 inverts world Z rotation.
                // Multiplying by facingDirection guarantees correct world tilt along the rope in both directions.
                zRot = facingDirection * RopeAngle;
            }

            visualTransform.localEulerAngles = new Vector3(0f, yRot, zRot);

            bool has3DAnimator = (transform.GetComponent<Animator>() != null || transform.GetComponentInChildren<Animator>() != null);
            if (!has3DAnimator)
            {
                float heightRatio = (targetHeight / standingHeight) * squashStretchY;
                visualTransform.localScale = new Vector3(baseVisualScaleX * widthScale, baseVisualScaleY * heightRatio, baseVisualScaleZ * widthScale);
            }
            else
            {
                visualTransform.localScale = new Vector3(baseVisualScaleX, baseVisualScaleY, baseVisualScaleZ);
            }
            visualTransform.localPosition = Vector3.zero;

            if (currentState != PlayerState.PushPull && currentState != PlayerState.Carrying && !isCrawling)
            {
                if (handLeft != null) handLeft.localPosition = new Vector3(0.20f, 0.52f, 0.18f);
                if (handRight != null) handRight.localPosition = new Vector3(0.20f, 0.52f, -0.18f);
            }
        }

        public void UpdateFacing(float facingDirection)
        {
            if (visualTransform != null)
            {
                Vector3 rot = visualTransform.localEulerAngles;
                rot.y = (facingDirection > 0) ? 0f : 180f;
                visualTransform.localEulerAngles = rot;
            }
        }

        public void ResetScale()
        {
            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(baseVisualScaleX, baseVisualScaleY, baseVisualScaleZ);
                visualTransform.localPosition = Vector3.zero;
            }
        }

        private void SetupHandVisuals()
        {
            if (visualTransform == null) return;

            Transform hl = visualTransform.Find("Hand_L");
            if (hl == null)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Hand_L";
                go.transform.SetParent(visualTransform);
                go.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                go.transform.localPosition = new Vector3(0.22f, 0.52f, 0.18f);
                Object.Destroy(go.GetComponent<Collider>());
                handLeft = go.transform;
            }
            else handLeft = hl;

            Transform hr = visualTransform.Find("Hand_R");
            if (hr == null)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Hand_R";
                go.transform.SetParent(visualTransform);
                go.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                go.transform.localPosition = new Vector3(0.22f, 0.52f, -0.18f);
                Object.Destroy(go.GetComponent<Collider>());
                handRight = go.transform;
            }
            else handRight = hr;
        }
    }
}
