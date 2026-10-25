using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách quản lý cơ chế bám mép vách (Ledge Grab) và leo trèo (Ledge Climb),
    /// tính toán quỹ đạo chuyển động mượt mà (Biomechanical Hoist) và đồng bộ hoàn toàn với Animator.
    /// </summary>
    public class PlayerLedgeClimber
    {
        private readonly Transform transform;
        private readonly CharacterController charController;
        private readonly LedgeGrabDetector ledgeDetector;

        public float ClimbDuration { get; set; } = 1.15f;
        public float MaxClimbForwardLean { get; set; } = 24f;
        public bool AutoClimbOnLedgeGrab { get; set; } = false;

        public Vector3 ClimbStartPos { get; private set; }
        public Vector3 ClimbEndPos { get; private set; }
        public float LedgeWallX { get; private set; }
        public float LedgeTopY { get; private set; }
        public float ClimbProgress { get; private set; }

        public PlayerLedgeClimber(
            Transform transform,
            CharacterController charController,
            LedgeGrabDetector ledgeDetector,
            float climbDuration = 1.15f,
            float maxClimbForwardLean = 24f,
            bool autoClimbOnLedgeGrab = false)
        {
            this.transform = transform;
            this.charController = charController;
            this.ledgeDetector = ledgeDetector;
            this.ClimbDuration = climbDuration;
            this.MaxClimbForwardLean = maxClimbForwardLean;
            this.AutoClimbOnLedgeGrab = autoClimbOnLedgeGrab;
        }

        public bool TryDetectLedge(float facingDirection, float velY, out Vector3 grabPos, out Vector3 landPos)
        {
            grabPos = Vector3.zero;
            landPos = Vector3.zero;

            if (ledgeDetector != null && velY < 3.5f)
            {
                return ledgeDetector.TryDetectLedge(facingDirection, out grabPos, out landPos);
            }
            return false;
        }

        public void StartLedgeGrab(Vector3 grabPos, Vector3 landPos, float facingDirection)
        {
            if (charController != null) charController.enabled = false;

            ClimbStartPos = new Vector3(grabPos.x, grabPos.y, 0f);
            ClimbEndPos = new Vector3(landPos.x, landPos.y, 0f);
            float wallOffset = (ledgeDetector != null) ? ledgeDetector.WallHangOffset : 0.28f;
            LedgeWallX = (ledgeDetector != null && Mathf.Abs(ledgeDetector.LastDetectedWallX) > 0.0001f)
                ? ledgeDetector.LastDetectedWallX
                : (ClimbStartPos.x + facingDirection * wallOffset);
            LedgeTopY = ClimbEndPos.y;
            ClimbProgress = 0f;

            // Đặt nhân vật tại vị trí bám gờ (ClimbStartPos) áp sát vách đá
            transform.position = ClimbStartPos;

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnLedgeHangStarted();
        }

        public void BeginClimbFromGrab()
        {
            ClimbProgress = 0f;

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnClimbStarted();
        }

        public bool UpdateClimb(float dt, float facingDirection, Transform visualTransform, float baseScaleX, float baseScaleY, float baseScaleZ)
        {
            ClimbProgress += dt / Mathf.Max(0.1f, ClimbDuration);
            float t = Mathf.Clamp01(ClimbProgress);

            // Di chuyển nhân vật mượt mà theo đường cong cơ sinh học từ vị trí bám vách lên đỉnh gờ
            transform.position = EvaluateClimbTrajectory(t, facingDirection);

            bool hasClimbAnimation = (transform.GetComponent<Animator>() != null);
            if (!hasClimbAnimation)
            {
                ApplyClimbVisualDynamics(t, facingDirection, visualTransform, baseScaleX, baseScaleY, baseScaleZ);
            }

            if (ClimbProgress >= 1.0f)
            {
                transform.position = ClimbEndPos;
                Physics.SyncTransforms();
                if (charController != null) charController.enabled = true;

                var anAnim = transform.GetComponent<AnAnimationController>();
                if (anAnim != null) anAnim.OnClimbFinished();

                return true; // Finished climbing
            }

            return false; // Still climbing
        }

        public Vector3 EvaluateClimbTrajectory(float t, float facing)
        {
            Vector3 start = ClimbStartPos;
            Vector3 end = ClimbEndPos;

            float wallX = LedgeWallX;
            float topY = LedgeTopY;

            // P0: Điểm bám ban đầu áp sát mặt ngoài của vách tường (đồng bộ 100% với tư thế LedgeHang)
            Vector3 p0 = start;
            // P1: Kéo thân người thẳng đứng dọc theo mặt ngoài vách tường lên ngang mép gờ (0% -> 42%)
            Vector3 p1 = new Vector3(wallX - facing * 0.14f, topY - 0.10f, 0f);
            // P2: Chống tay vượt qua góc gờ (wallX, topY) đặt chân lên mặt sàn (42% -> 76%)
            Vector3 p2 = new Vector3(wallX + facing * 0.24f, topY, 0f);
            // P3: Đứng thẳng dậy hoàn toàn tại điểm đáp trên platform (76% -> 100%)
            Vector3 p3 = end;

            if (t <= 0.42f)
            {
                // Pha 1 (0% -> 42%): Kéo người từ vị trí treo vách lên mép gờ
                float subT = t / 0.42f;
                float easeY = Mathf.SmoothStep(0f, 1f, subT);
                float easeX = subT * subT;
                return new Vector3(
                    Mathf.Lerp(p0.x, p1.x, easeX),
                    Mathf.Lerp(p0.y, p1.y, easeY),
                    0f);
            }
            else if (t <= 0.76f)
            {
                // Pha 2 (42% -> 76%): Vượt qua mép gờ (Mantle) bước hai chân lên mặt sàn
                float subT = (t - 0.42f) / 0.34f;
                float ease = Mathf.SmoothStep(0f, 1f, subT);
                return Vector3.Lerp(p1, p2, ease);
            }
            else
            {
                // Pha 3 (76% -> 100%): Đứng thẳng dậy hoàn toàn trên platform
                float subT = (t - 0.76f) / 0.24f;
                float ease = Mathf.SmoothStep(0f, 1f, subT);
                return Vector3.Lerp(p2, p3, ease);
            }
        }

        public void ApplyClimbVisualDynamics(float t, float facing, Transform visualTransform, float baseScaleX, float baseScaleY, float baseScaleZ)
        {
            if (visualTransform == null) return;

            float leanAngle = 0f;
            float heightFactor = 1f;
            float widthFactor = 1f;

            if (t <= 0.52f)
            {
                float subT = t / 0.52f;
                leanAngle = Mathf.Lerp(0f, 14f, subT);
                heightFactor = Mathf.Lerp(1.04f, 0.90f, subT);
                widthFactor = Mathf.Lerp(0.96f, 1.08f, subT);
            }
            else if (t <= 0.80f)
            {
                float subT = (t - 0.52f) / 0.28f;
                leanAngle = Mathf.Lerp(14f, MaxClimbForwardLean, Mathf.Sin(subT * Mathf.PI));
                heightFactor = Mathf.Lerp(0.90f, 0.68f, subT);
                widthFactor = Mathf.Lerp(1.08f, 1.20f, subT);
            }
            else
            {
                float subT = (t - 0.80f) / 0.20f;
                leanAngle = Mathf.Lerp(MaxClimbForwardLean * 0.4f, 0f, subT);
                heightFactor = Mathf.Lerp(0.68f, 1.0f, subT);
                widthFactor = Mathf.Lerp(1.20f, 1.0f, subT);
            }

            float targetZRot = -facing * leanAngle;
            float yRot = facing > 0 ? 0f : 180f;
            visualTransform.localEulerAngles = new Vector3(0f, yRot, targetZRot);
            visualTransform.localScale = new Vector3(baseScaleX * widthFactor, baseScaleY * heightFactor, baseScaleZ * widthFactor);
            visualTransform.localPosition = Vector3.zero;
        }
    }
}
