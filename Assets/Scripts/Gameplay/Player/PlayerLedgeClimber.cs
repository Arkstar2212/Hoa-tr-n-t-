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
        public float ExitHorizontalSpeed { get; private set; }

        private float forwardCarryBlend = 0f;

        public float EffectiveClimbDuration => (ClimbDuration > 1.45f) ? 1.15f : Mathf.Max(0.75f, ClimbDuration);

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
            float skinLift = (charController != null) ? Mathf.Clamp(charController.skinWidth, 0f, 0.05f) : 0f;
            ClimbStartPos = new Vector3(grabPos.x, grabPos.y, laneZ);
            ClimbEndPos = new Vector3(landPos.x, landPos.y + skinLift, laneZ);
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
            ExitHorizontalSpeed = 0f;
            forwardCarryBlend = 0f;

            // Đặt nhân vật bám gờ vách tại ClimbStartPos
            transform.position = ClimbStartPos;
        }

        public void StartClimbing()
        {
            ClimbProgress = 0f;
            ExitHorizontalSpeed = 0f;

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

        public bool UpdateClimb(
            float dt,
            float facingDirection,
            Transform visualTransform,
            float baseScaleX,
            float baseScaleY,
            float baseScaleZ,
            bool wantsMoveForward = false)
        {
            float targetCarry = wantsMoveForward ? 1f : 0f;
            forwardCarryBlend = Mathf.MoveTowards(forwardCarryBlend, targetCarry, 5.5f * dt);

            // Khi người chơi giữ phím tiến ở nửa cuối pha đứng dậy -> tăng nhẹ nhịp bước lên để nối mượt vào chạy/đi bộ
            float standUpSpeedBoost = (ClimbProgress > 0.50f) ? Mathf.Lerp(1.0f, 1.20f, forwardCarryBlend) : 1.0f;
            float duration = EffectiveClimbDuration;
            ClimbProgress += (dt / duration) * standUpSpeedBoost;
            float t = Mathf.Clamp01(ClimbProgress);

            // Di chuyển nhân vật theo đường cong đơn liên tục đạo hàm bậc 1 (không phanh khựng giữa chừng)
            transform.position = EvaluateClimbTrajectory(t, facingDirection, forwardCarryBlend);

            bool hasClimbAnimation = (transform.GetComponent<Animator>() != null);
            if (!hasClimbAnimation)
            {
                ApplyClimbVisualDynamics(t, facingDirection, visualTransform, baseScaleX, baseScaleY, baseScaleZ);
            }

            if (ClimbProgress >= 1.0f)
            {
                float horizSpan = Mathf.Abs(ClimbEndPos.x - ClimbStartPos.x);
                float rawCarrySpeed = (horizSpan / Mathf.Max(0.4f, 0.86f * duration)) * 1.5f * standUpSpeedBoost;
                ExitHorizontalSpeed = rawCarrySpeed * forwardCarryBlend;

                // Đặt nhẹ cao hơn 1.5cm rồi cho CharacterController hạ chạm mặt sàn ngay trong frame này
                // để charController.isGrounded = true tức thì (ngăn nháy sang trạng thái Airborne/Fall gây giật)
                transform.position = new Vector3(ClimbEndPos.x, ClimbEndPos.y + 0.015f, ClimbEndPos.z);
                Physics.SyncTransforms();
                if (charController != null)
                {
                    charController.enabled = true;
                    charController.Move(new Vector3(0f, -0.04f, 0f));
                    Vector3 settled = transform.position;
                    settled.z = ClimbEndPos.z;
                    transform.position = settled;
                }

                var anAnim = transform.GetComponent<AnAnimationController>();
                if (anAnim != null) anAnim.OnClimbFinished();

                return true; // Finished climbing
            }

            return false; // Still climbing
        }

        public Vector3 EvaluateClimbTrajectory(float t, float facing)
        {
            return EvaluateClimbTrajectory(t, facing, forwardCarryBlend);
        }

        public Vector3 EvaluateClimbTrajectory(float t, float facing, float carryBlend)
        {
            Vector3 start = ClimbStartPos;
            Vector3 end = ClimbEndPos;
            float laneZ = start.z;

            // 1. Trục Y: Một đường cong chữ S duy nhất từ vị trí bám lên đỉnh gờ trong [0..0.68],
            // sau đó giữ phẳng tuyệt đối trên mặt sàn ở [0.68..1.0] khi nhân vật đứng dậy bước tới (không khựng 3 đoạn)
            float uY = Mathf.Clamp01(t / 0.68f);
            float easeY = uY * uY * (3f - 2f * uY);
            float posY = Mathf.Lerp(start.y, end.y, easeY);

            // 2. Trục X: Giữ sát mặt ngoài tường ở [0..0.14] để vượt góc đá an toàn,
            // sau đó lướt mượt một mạch tới điểm đáp. Nếu người chơi đang giữ phím tiến (carryBlend > 0),
            // dùng đa thức không phanh cuối u^2*(1.5 - 0.5*u) để giữ trọn quán tính bước chạy lên!
            float uX = Mathf.Clamp01((t - 0.14f) / 0.86f);
            float easeStop = uX * uX * (3f - 2f * uX);          // Đạo hàm cuối = 0 (đứng lại êm ái)
            float easeCarry = uX * uX * (1.5f - 0.5f * uX);     // Đạo hàm cuối = 1.5 (lướt thẳng vào bước chạy)
            float easeX = Mathf.Lerp(easeStop, easeCarry, Mathf.Clamp01(carryBlend));
            float posX = Mathf.Lerp(start.x, end.x, easeX);

            return new Vector3(posX, posY, laneZ);
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
