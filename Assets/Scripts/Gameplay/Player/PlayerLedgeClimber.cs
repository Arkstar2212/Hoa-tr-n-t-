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

        public float ClimbDuration { get; set; } = 1.22f; // Tốc độ trèo tường linh hoạt, khớp chuẩn nhịp động tác
        public float MaxClimbForwardLean { get; set; } = 24f;
        public bool AutoClimbOnLedgeGrab { get; set; } = false; // Mặc định bám treo tay vào vách đá, chờ người chơi bấm W hoặc Up để leo lên

        public Vector3 ClimbStartPos { get; private set; }
        public Vector3 ClimbEndPos { get; private set; }
        public Vector3 LedgeCornerPos { get; private set; }
        public float ClimbProgress { get; private set; }

        public float EffectiveClimbDuration => (ClimbDuration > 1.45f) ? 1.22f : Mathf.Max(0.75f, ClimbDuration);

        public PlayerLedgeClimber(
            Transform transform,
            CharacterController charController,
            LedgeGrabDetector ledgeDetector,
            float climbDuration = 1.22f,
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

            float laneZ = grabPos.z;
            ClimbStartPos = new Vector3(grabPos.x, grabPos.y, laneZ);
            ClimbEndPos = new Vector3(landPos.x, landPos.y, laneZ);
            if (ledgeDetector != null && ledgeDetector.HasValidLedgeCorner)
            {
                Vector3 detCorner = ledgeDetector.LastLedgeCorner;
                LedgeCornerPos = new Vector3(detCorner.x, detCorner.y, laneZ);
            }
            else
            {
                LedgeCornerPos = new Vector3(grabPos.x + facingDirection * 0.24f, landPos.y, laneZ);
            }
            ClimbProgress = 0f;

            // Đặt nhân vật bám gờ vách tại ClimbStartPos
            transform.position = ClimbStartPos;
        }

        public void StartClimbing()
        {
            ClimbProgress = 0f;

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
            ClimbProgress += dt / EffectiveClimbDuration;
            float t = Mathf.Clamp01(ClimbProgress);

            // Di chuyển nhân vật mượt mà theo đường cong cơ sinh học 3 pha vượt mép gờ
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
            float laneZ = start.z;
            float wallX = LedgeCornerPos.x;
            float topY = end.y;
            float s = Mathf.Clamp(Mathf.Abs(topY - start.y) / 0.86f, 0.70f, 1.85f);

            // P0: Bám treo chuẩn ngay mép ngoài tường, hai bàn tay bám đúng góc gờ (wallX, topY)
            Vector3 p0 = start;
            // P1 (t=0.34): Kéo thân người thẳng đứng lên dọc mặt ngoài vách, chống tay lên đỉnh gờ
            Vector3 p1 = new Vector3(wallX - facing * (0.04f * s), topY - 0.44f * s, laneZ);
            // P2 (t=0.66): Vượt hẳn qua góc gờ, đặt cả hai bàn chân lên mặt sàn trên của vách
            Vector3 p2 = new Vector3(wallX + facing * (0.24f * s), topY, laneZ);
            // P3 (t=1.00): Đứng thẳng dậy hoàn toàn ổn định trên mặt sàn
            Vector3 p3 = end;

            if (t <= 0.34f)
            {
                // Pha 1 (0% -> 34%): Kéo người thẳng đứng lên dọc theo mặt tường (tay ghim trên gờ)
                float subT = t / 0.34f;
                float easeY = Mathf.SmoothStep(0f, 1f, subT);
                float easeX = subT * subT;
                return new Vector3(
                    Mathf.Lerp(p0.x, p1.x, easeX),
                    Mathf.Lerp(p0.y, p1.y, easeY),
                    laneZ);
            }
            else if (t <= 0.66f)
            {
                // Pha 2 (34% -> 66%): Nâng hông vượt qua góc gờ trước rồi mới tiến vào mặt sàn (không xuyên góc đá)
                float subT = (t - 0.34f) / 0.32f;
                float easeY = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(subT * 1.18f));
                float easeX = Mathf.SmoothStep(0f, 1f, subT);
                return new Vector3(
                    Mathf.Lerp(p1.x, p2.x, easeX),
                    Mathf.Lerp(p1.y, p2.y, easeY),
                    laneZ);
            }
            else
            {
                // Pha 3 (66% -> 100%): Đứng thẳng dậy từ tư thế chùng gối và bước vào vị trí đứng vững
                float subT = (t - 0.66f) / 0.34f;
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
