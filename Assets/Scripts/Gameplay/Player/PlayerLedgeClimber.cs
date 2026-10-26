using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách quản lý cơ chế bám mép vách (Ledge Grab) và leo trèo (Ledge Climb),
    /// tính toán quỹ đạo chuyển động mượt mà (Biomechanical Hoist) và thông báo cho Animator.
    /// </summary>
    public class PlayerLedgeClimber
    {
        private readonly Transform transform;
        private readonly CharacterController charController;
        private readonly LedgeGrabDetector ledgeDetector;

        public float ClimbDuration { get; set; } = 1.05f; // Tăng tốc độ trèo tường nhanh và linh hoạt hơn
        public float MaxClimbForwardLean { get; set; } = 24f;
        public bool AutoClimbOnLedgeGrab { get; set; } = false; // Mặc định bám treo tay vào vách đá, chờ người chơi bấm W hoặc Up để leo lên

        public Vector3 ClimbStartPos { get; private set; }
        public Vector3 ClimbEndPos { get; private set; }
        public float ClimbProgress { get; private set; }

        public PlayerLedgeClimber(
            Transform transform,
            CharacterController charController,
            LedgeGrabDetector ledgeDetector,
            float climbDuration = 1.05f,
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

        public void PrepareLedgeGrab(Vector3 grabPos, Vector3 landPos, float facingDirection)
        {
            if (charController != null) charController.enabled = false;

            ClimbStartPos = new Vector3(grabPos.x, grabPos.y, 0f);
            ClimbEndPos = new Vector3(landPos.x, landPos.y, 0f);
            ClimbProgress = 0f;

            // Đặt nhân vật bám gờ vách tại ClimbStartPos
            transform.position = ClimbStartPos;
        }

        public void StartClimbing()
        {
            ClimbProgress = 0f;

            var meshyAnim = transform.GetComponent<MeshyBoyAnimationController>();
            if (meshyAnim != null) meshyAnim.OnClimbStarted();

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnClimbStarted();
        }

        public void StartLedgeGrab(Vector3 grabPos, Vector3 landPos, float facingDirection)
        {
            PrepareLedgeGrab(grabPos, landPos, facingDirection);

            if (AutoClimbOnLedgeGrab)
            {
                StartClimbing();
            }
        }

        public bool UpdateClimb(float dt, float facingDirection, Transform visualTransform, float baseScaleX, float baseScaleY, float baseScaleZ)
        {
            ClimbProgress += dt / Mathf.Max(0.1f, ClimbDuration);
            float t = Mathf.Clamp01(ClimbProgress);

            // Di chuyển nhân vật mượt mà theo đường cong cơ sinh học từ mép tường lên đỉnh gờ
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

                var meshyAnim = transform.GetComponent<MeshyBoyAnimationController>();
                if (meshyAnim != null) meshyAnim.OnClimbFinished();

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

            // Tọa độ mép tường thực tế (start.x nằm cách mặt tường 0.28m về phía ngoài)
            float wallX = start.x + facing * 0.28f;
            float topY = end.y;

            // P0: Điểm bám ban đầu áp sát mặt ngoài của vách tường
            Vector3 p0 = start;
            // P1: Kéo người thẳng đứng dọc theo mặt ngoài vách tường lên sát mép gờ (không xuyên tường)
            Vector3 p1 = new Vector3(wallX - facing * 0.14f, topY - 0.08f, 0f);
            // P2: Chống tay vượt qua góc gờ (wallX, topY) đặt chân lên mặt sàn
            Vector3 p2 = new Vector3(wallX + facing * 0.24f, topY, 0f);
            // P3: Đứng thẳng dậy hoàn toàn tại điểm đáp trên platform
            Vector3 p3 = end;

            if (t <= 0.46f)
            {
                // Pha 1 (0% -> 46%): Bám vách và kéo thân người thẳng đứng lên dọc theo mặt tường
                float subT = t / 0.46f;
                float easeY = Mathf.SmoothStep(0f, 1f, subT);
                float easeX = subT * subT;
                return new Vector3(
                    Mathf.Lerp(p0.x, p1.x, easeX),
                    Mathf.Lerp(p0.y, p1.y, easeY),
                    0f);
            }
            else if (t <= 0.78f)
            {
                // Pha 2 (46% -> 78%): Vượt qua mép gờ (Mantle) bước chân lên mặt sàn
                float subT = (t - 0.46f) / 0.32f;
                float ease = Mathf.SmoothStep(0f, 1f, subT);
                return Vector3.Lerp(p1, p2, ease);
            }
            else
            {
                // Pha 3 (78% -> 100%): Đứng thẳng dậy và bước ổn định vào trong platform
                float subT = (t - 0.78f) / 0.22f;
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
