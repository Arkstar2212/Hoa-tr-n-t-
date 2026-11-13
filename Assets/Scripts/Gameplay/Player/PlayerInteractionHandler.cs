using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách các tương tác với đồ vật trong môi trường (Interactable Objects):
    /// - Bám, kéo và đẩy thùng gỗ nặng (InteractableCrate) bằng cách Giữ Chuột Trái + A/D.
    /// - Nhặt đồ (PortableItem), ngắm canh quỹ đạo ném Parabol tự do (Aim & Throw)
    ///   với khả năng tùy chỉnh góc và lực theo khoảng cách/hướng con trỏ chuột.
    /// </summary>
    public class PlayerInteractionHandler
    {
        private readonly Transform transform;
        private readonly Transform carrySocket;
        private readonly TrajectoryPredictor trajectoryPredictor;

        private InteractableCrate currentCrate;
        private PortableItem carriedItem;
        private float currentCrateOffset = 0.44f;

        // Aim & Throw state
        private bool isAiming = false;
        private Vector3 aimVelocity = Vector3.zero;
        private float currentAimDistance = 0f;
        private float currentAimPower = 0f;
        private float currentAimAngle = 0f;

        public float MinThrowSpeed { get; set; } = 4.2f;
        public float MaxThrowSpeed { get; set; } = 15.5f;
        public float ThrowChargeRate { get; set; } = 9.0f;

        public InteractableCrate CurrentCrate => currentCrate;
        public PortableItem CarriedItem => carriedItem;
        public bool IsCarrying => carriedItem != null;
        public bool IsPushingPulling => currentCrate != null;
        public bool IsAiming => isAiming;
        public Vector3 AimVelocity => aimVelocity;
        public float CurrentAimDistance => currentAimDistance;
        public float CurrentAimPower => currentAimPower;
        public float CurrentAimAngle => currentAimAngle;

        public PlayerInteractionHandler(
            Transform transform,
            Transform carrySocket,
            TrajectoryPredictor trajectoryPredictor,
            float minThrowSpeed = 4.2f,
            float maxThrowSpeed = 15.5f,
            float throwChargeRate = 9.0f)
        {
            this.transform = transform;
            this.carrySocket = carrySocket;
            this.trajectoryPredictor = trajectoryPredictor;
            this.MinThrowSpeed = minThrowSpeed;
            this.MaxThrowSpeed = maxThrowSpeed;
            this.ThrowChargeRate = throwChargeRate;
        }

        public void CheckInteractions(
            PlayerController player,
            bool interactPressed,
            bool grabCrateHeld,
            bool mouseLeftPressed,
            bool mouseRightPressed)
        {
            // 1. Nếu CHƯA cầm vật gì: Kiểm tra bám kéo/đẩy thùng gỗ
            if (carriedItem == null && player.CurrentState == PlayerState.Grounded)
            {
                if (grabCrateHeld || mouseLeftPressed)
                {
                    InteractableCrate crate = FindNearbyCrate(out int side);
                    if (crate != null)
                    {
                        StartPushPull(player, crate, side);
                        return;
                    }
                }
            }

            // 2. Phím [E]: Nhặt đồ nếu đang trống tay, hoặc đặt nhẹ xuống nếu đang cầm
            if (interactPressed)
            {
                if (carriedItem != null)
                {
                    DropCarriedItem(player);
                    return;
                }
                else
                {
                    PortableItem item = FindNearbyItem();
                    if (item != null)
                    {
                        PickupItem(player, item);
                        return;
                    }
                }
            }

            // 3. Nếu ĐANG cầm đồ và nhấn giữ Chuột Trái: Bắt đầu ngắm ném (Aim)
            if (carriedItem != null && (mouseLeftPressed || player.InputReader.AimThrowHeld))
            {
                StartAimingThrow(player);
            }
            // 4. Nếu đang cầm đồ và bấm Chuột Phải: Đặt nhẹ đồ vật xuống
            else if (carriedItem != null && mouseRightPressed && !isAiming)
            {
                DropCarriedItem(player);
            }
        }

        public InteractableCrate FindNearbyCrate(out int side)
        {
            side = 0;
            var crates = Object.FindObjectsByType<InteractableCrate>(FindObjectsInactive.Exclude);
            foreach (var crate in crates)
            {
                if (crate != null && crate.CanPlayerGrab(transform.position, out side))
                {
                    return crate;
                }
            }
            return null;
        }

        public PortableItem FindNearbyItem()
        {
            var items = Object.FindObjectsByType<PortableItem>(FindObjectsInactive.Exclude);
            PortableItem closest = null;
            float minDist = float.MaxValue;

            foreach (var item in items)
            {
                if (item != null && item.CanPickup(transform.position))
                {
                    float d = Vector3.Distance(transform.position, item.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = item;
                    }
                }
            }
            return closest;
        }

        // ================= KÉO / ĐẨY THÙNG GỖ =================

        public void StartPushPull(PlayerController player, InteractableCrate crate, int side)
        {
            currentCrate = crate;
            player.SetState(PlayerState.PushPull);
            player.SetFacingDirection(-side);

            crate.StartGrab(player, side);

            var anAnim = player.GetComponent<AnAnimationController>();
            if (anAnim != null)
            {
                anAnim.PlayCutsceneAnimation("PushPull", 0.08f);
            }

            Vector3 anchorPos = crate.GetPlayerAnchorPosition(side);
            float groundY = GetGroundElevationAt(anchorPos.x, crate.transform.position.y);

            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                transform.position = new Vector3(anchorPos.x, groundY, 0f);
                cc.enabled = true;
            }
            else
            {
                transform.position = new Vector3(anchorPos.x, groundY, 0f);
            }
        }

        public void UpdatePushPull(PlayerController player, float horizontalInput, bool grabCrateHeld, bool jumpPressed, float dt)
        {
            if (currentCrate == null || !grabCrateHeld || jumpPressed)
            {
                StopPushPull(player);
                return;
            }

            float clampedInput = Mathf.Clamp(horizontalInput, -1f, 1f);

            // Kiểm tra vật cản phía sau lưng nhân vật khi đang kéo thùng lùi lại (ngăn chặn thụt lùi vào vách đá)
            float moveDir = (Mathf.Abs(clampedInput) > 0.05f) ? Mathf.Sign(clampedInput) : 0f;
            bool isPulling = (moveDir != 0f && moveDir == currentCrate.PlayerSide);
            bool isPushing = (moveDir != 0f && moveDir != currentCrate.PlayerSide);
            if (isPulling)
            {
                Vector3 playerCenter = transform.position + Vector3.up * 0.9f;
                int mask = ~0;
                if (currentCrate != null) mask &= ~(1 << currentCrate.gameObject.layer);
                mask &= ~(1 << transform.gameObject.layer);

                if (Physics.Raycast(playerCenter, Vector3.right * moveDir, out RaycastHit playerWallHit, 0.45f, mask, QueryTriggerInteraction.Ignore))
                {
                    clampedInput = 0f;
                }
            }

            currentCrate.MoveWithPlayer(clampedInput, dt);

            float targetOffset = isPushing ? 0.48f : (isPulling ? 0.35f : 0.44f);
            currentCrateOffset = Mathf.MoveTowards(currentCrateOffset, targetOffset, 2.5f * dt);
            Vector3 anchorPos = currentCrate.GetPlayerAnchorPosition(currentCrate.PlayerSide, currentCrateOffset);
            float groundY = GetGroundElevationAt(anchorPos.x, currentCrate.transform.position.y);

            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                transform.position = new Vector3(anchorPos.x, groundY, 0f);
                cc.enabled = true;
            }
            else
            {
                transform.position = new Vector3(anchorPos.x, groundY, 0f);
            }

            player.SetFacingDirection(-currentCrate.PlayerSide);
        }

        public void StopPushPull(PlayerController player)
        {
            if (currentCrate != null)
            {
                currentCrate.ReleaseGrab();
                currentCrate = null;
            }
            player.SetState(PlayerState.Grounded);

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null)
            {
                anAnim.ResetAnimatorSpeed();
            }
        }

        private float GetGroundElevationAt(float x, float fallbackY)
        {
            Vector3 origin = new Vector3(x, fallbackY + 1.5f, 0f);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 3.5f, ~0, QueryTriggerInteraction.Ignore);
            float highestGroundY = float.MinValue;
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null || col.isTrigger) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (currentCrate != null && (col == currentCrate.GetComponent<Collider>() || col.transform.IsChildOf(currentCrate.transform))) continue;

                if (hits[i].point.y <= fallbackY + 0.40f && hits[i].point.y > highestGroundY)
                {
                    highestGroundY = hits[i].point.y;
                    foundGround = true;
                }
            }

            return foundGround ? highestGroundY : fallbackY;
        }

        // ================= BƯNG BÊ & NHẶT ĐỒ =================

        public void PickupItem(PlayerController player, PortableItem item)
        {
            Vector3 itemGroundPos = item.transform.position;
            carriedItem = item;
            carriedItem.OnPickedUp(player, carrySocket);
            player.SetState(PlayerState.Carrying);
            UpdateCarrySocketTransform(player.FacingDirection);

            isAiming = false;

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnPickupStarted(itemGroundPos, carrySocket);
        }

        public void DropCarriedItem(PlayerController player)
        {
            if (carriedItem == null) return;

            Vector3 dropPos = transform.position + new Vector3(player.FacingDirection * 0.72f, 0.45f, 0f);
            Vector3 gentleNudge = new Vector3(player.FacingDirection * 1.5f, 0.5f, 0f);

            carriedItem.OnDropped(dropPos, gentleNudge);
            carriedItem = null;
            isAiming = false;
            if (trajectoryPredictor != null) trajectoryPredictor.Hide();
            player.SetState(PlayerState.Grounded);

            UpdateCarrySocketTransform(player.FacingDirection);

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnAimThrowCancelled();
        }

        // ================= CANH GÓC & LỰC NÉM PARABOL (AIM & THROW) =================

        public void StartAimingThrow(PlayerController player)
        {
            if (carriedItem == null) return;
            isAiming = true;
            player.SetState(PlayerState.AimingThrow);

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnAimThrowStarted(carrySocket);
        }

        public void UpdateAimingThrow(
            PlayerController player,
            Vector2 mouseScreenPos,
            bool mouseReleased,
            bool cancelRequested,
            float dt)
        {
            if (carriedItem == null || cancelRequested)
            {
                CancelAiming(player);
                return;
            }

            // Sử dụng điểm phóng chuẩn ổn định theo hướng mặt nhân vật để đường ngắm Parabol không bị rung theo tay
            Vector3 launchPos = transform.position + new Vector3(player.FacingDirection * 0.44f, 1.05f, 0f);
            launchPos.z = 0f;

            // Chuyển đổi tọa độ chuột màn hình sang không gian thế giới mặt phẳng 2D (Z = 0)
            Camera cam = Camera.main;
            Vector3 mouseWorldPos;
            if (cam != null)
            {
                Plane plane2D = new Plane(Vector3.forward, Vector3.zero);
                Ray ray = cam.ScreenPointToRay(mouseScreenPos);
                if (plane2D.Raycast(ray, out float enter))
                {
                    mouseWorldPos = ray.GetPoint(enter);
                }
                else
                {
                    mouseWorldPos = launchPos + new Vector3(player.FacingDirection * 4f, 2f, 0f);
                }
            }
            else
            {
                mouseWorldPos = launchPos + new Vector3(player.FacingDirection * 4f, 2f, 0f);
            }
            mouseWorldPos.z = 0f;

            Vector3 diff = mouseWorldPos - launchPos;
            diff.z = 0f;

            // 1. Chống lỗi flicking trái/phải khi trỏ chuột quá gần nhân vật (Deadzone & Hysteresis)
            float facingFlipDeadzone = 0.65f; // Vùng an toàn 0.65m quanh nhân vật
            if (Mathf.Abs(diff.x) >= facingFlipDeadzone)
            {
                float desiredDir = Mathf.Sign(diff.x);
                if (desiredDir != player.FacingDirection)
                {
                    player.SetFacingDirection(desiredDir);
                }
            }

            // 2. Ổn định vector hướng ném và lực ném (hoàn toàn triệt tiêu jitter/flicking)
            Vector3 throwDir;
            if (diff.magnitude < facingFlipDeadzone || Mathf.Abs(diff.x) < 0.30f)
            {
                // Khi chuột ở quá sát người hoặc trục X quá bé:
                // Khóa chắc chắn hướng ném về phía trước theo hướng mặt (player.FacingDirection)
                float forwardX = player.FacingDirection * Mathf.Max(Mathf.Abs(diff.x), 0.55f);
                float upwardY = Mathf.Max(diff.y, 0.45f);
                throwDir = new Vector3(forwardX, upwardY, 0f).normalized;
                currentAimDistance = Mathf.Max(diff.magnitude, 1.2f);
                currentAimPower = Mathf.Clamp(currentAimDistance * 2.3f, MinThrowSpeed, MaxThrowSpeed);
            }
            else
            {
                // Giữ hướng X luôn đồng bộ với player.FacingDirection để không bị ném lùi ra sau lưng
                float throwX = player.FacingDirection * Mathf.Abs(diff.x);
                float throwY = diff.y;
                throwDir = new Vector3(throwX, throwY, 0f).normalized;

                currentAimDistance = diff.magnitude;
                currentAimPower = Mathf.Clamp(currentAimDistance * 2.3f, MinThrowSpeed, MaxThrowSpeed);

                // Đảm bảo góc ném luôn hướng vòng lên tối thiểu để tạo cung Parabol đẹp mắt
                if (throwDir.y < 0.18f)
                {
                    throwDir.y = 0.22f;
                    throwDir = throwDir.normalized;
                }
            }

            currentAimAngle = Mathf.Atan2(throwDir.y, Mathf.Abs(throwDir.x)) * Mathf.Rad2Deg;
            aimVelocity = throwDir * currentAimPower;

            // Vẽ đường cong Parabol dự đoán điểm rơi và sợi dây cao su nhiều đốt co giãn
            if (trajectoryPredictor != null)
            {
                trajectoryPredictor.RenderTrajectory(launchPos, aimVelocity, Physics.gravity.y);
                Vector3 rockPos = (carriedItem != null) ? carriedItem.transform.position : (launchPos - throwDir * 0.45f);
                rockPos.z = 0f;
                trajectoryPredictor.RenderRubberBand(launchPos, rockPos, currentAimPower / MaxThrowSpeed);
            }

            // Nhả Chuột Trái: Thực hiện ném bóng đi theo đúng quỹ đạo đã canh!
            if (mouseReleased)
            {
                ExecuteAimThrow(player, launchPos, aimVelocity);
            }
        }

        public void ExecuteAimThrow(PlayerController player, Vector3 launchPos, Vector3 velocity)
        {
            if (trajectoryPredictor != null) trajectoryPredictor.Hide();
            if (carriedItem == null) return;

            carriedItem.OnThrown(launchPos, velocity);
            carriedItem = null;
            isAiming = false;
            player.SetState(PlayerState.Grounded);

            UpdateCarrySocketTransform(player.FacingDirection);

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnThrowReleased();
        }

        public void CancelAiming(PlayerController player)
        {
            if (trajectoryPredictor != null) trajectoryPredictor.Hide();
            isAiming = false;
            if (carriedItem != null)
            {
                player.SetState(PlayerState.Carrying);
            }
            else
            {
                player.SetState(PlayerState.Grounded);
            }

            UpdateCarrySocketTransform(player.FacingDirection);

            var anAnim = transform.GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnAimThrowCancelled();
        }

        public void UpdateCarrySocketTransform(float facingDirection)
        {
            if (carrySocket != null)
            {
                carrySocket.localPosition = new Vector3(facingDirection * 0.44f, 1.05f, 0f);
            }
        }
    }
}
