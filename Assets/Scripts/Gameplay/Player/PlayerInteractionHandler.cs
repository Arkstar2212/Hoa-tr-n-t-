using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách các tương tác với đồ vật trong môi trường (Interactable Objects):
    /// - Kéo và đẩy thùng gỗ nặng (InteractableCrate).
    /// - Nhặt đồ, bưng bê theo socket, đặt xuống nhẹ nhàng hoặc ném quán tính (PortableItem & TrajectoryPredictor).
    /// </summary>
    public class PlayerInteractionHandler
    {
        private readonly Transform transform;
        private readonly Transform carrySocket;
        private readonly TrajectoryPredictor trajectoryPredictor;

        private InteractableCrate currentCrate;
        private PortableItem carriedItem;

        // Drag inertia
        private bool lmbDragActive = false;
        private Vector3 dragItemVelocity = Vector3.zero;
        private Vector3 lastCarrySocketPos = Vector3.zero;
        private float dragInertiaSmoothing = 12f;
        private float dragThrowScale = 1.6f;

        public float MinThrowSpeed { get; set; } = 5.0f;
        public float MaxThrowSpeed { get; set; } = 14.5f;
        public float ThrowChargeRate { get; set; } = 8.5f;

        public InteractableCrate CurrentCrate => currentCrate;
        public PortableItem CarriedItem => carriedItem;
        public bool IsCarrying => carriedItem != null;
        public bool IsPushingPulling => currentCrate != null;
        public bool LmbDragActive => lmbDragActive;

        public PlayerInteractionHandler(
            Transform transform,
            Transform carrySocket,
            TrajectoryPredictor trajectoryPredictor,
            float minThrowSpeed = 5.0f,
            float maxThrowSpeed = 14.5f,
            float throwChargeRate = 8.5f)
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
            bool aimThrowHeld)
        {
            // 1. Check for Push/Pull Crate
            if (grabCrateHeld && carriedItem == null && player.CurrentState == PlayerState.Grounded)
            {
                InteractableCrate crate = FindNearbyCrate(out int side);
                if (crate != null)
                {
                    StartPushPull(player, crate, side);
                    return;
                }
            }

            // 2. E key: Pick up if nothing held, or gently drop if holding
            if (interactPressed)
            {
                if (carriedItem != null)
                {
                    DropCarriedItem(player);
                }
                else
                {
                    PortableItem item = FindNearbyItem();
                    if (item != null)
                    {
                        PickupItem(player, item);
                    }
                }
            }

            // 3. Planet of Lana drag throw: LMB released after dragging → fling with inertia
            if (carriedItem != null && aimThrowHeld)
            {
                ExecuteDragThrow(player);
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

        public void StartPushPull(PlayerController player, InteractableCrate crate, int side)
        {
            currentCrate = crate;
            player.SetState(PlayerState.PushPull);
            player.SetFacingDirection(-side);

            crate.StartGrab(player, side);

            Vector3 anchorPos = crate.GetPlayerAnchorPosition(side);
            transform.position = new Vector3(anchorPos.x, transform.position.y, 0f);
        }

        public void UpdatePushPull(PlayerController player, float horizontalInput, bool grabCrateHeld, bool jumpPressed, float dt)
        {
            if (currentCrate == null || !grabCrateHeld || jumpPressed)
            {
                StopPushPull(player);
                return;
            }

            float clampedInput = Mathf.Clamp(horizontalInput, -1f, 1f);
            currentCrate.MoveWithPlayer(clampedInput, dt);

            Vector3 anchorPos = currentCrate.GetPlayerAnchorPosition(currentCrate.PlayerSide);
            transform.position = new Vector3(anchorPos.x, transform.position.y, 0f);
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
        }

        public void PickupItem(PlayerController player, PortableItem item)
        {
            carriedItem = item;
            carriedItem.OnPickedUp(player, carrySocket);
            player.SetState(PlayerState.Carrying);
            UpdateCarrySocketTransform(player.FacingDirection);

            lmbDragActive = false;
            dragItemVelocity = Vector3.zero;
            lastCarrySocketPos = carrySocket != null ? carrySocket.position : transform.position;
        }

        public void DropCarriedItem(PlayerController player)
        {
            if (carriedItem == null) return;

            Vector3 dropPos = transform.position + new Vector3(player.FacingDirection * 0.72f, 0.45f, 0f);
            Vector3 gentleNudge = new Vector3(player.FacingDirection * 1.5f, 0.5f, 0f);

            carriedItem.OnDropped(dropPos, gentleNudge);
            carriedItem = null;
            lmbDragActive = false;
            dragItemVelocity = Vector3.zero;
            player.SetState(PlayerState.Grounded);
        }

        public void SetDragActive(bool active)
        {
            lmbDragActive = active;
        }

        public void UpdateDragCarryInertia(PlayerController player, float dt)
        {
            if (carrySocket == null || dt <= 0f) return;

            Vector3 currentSocketPos = carrySocket.position;
            Vector3 rawVel = (currentSocketPos - lastCarrySocketPos) / dt;
            dragItemVelocity = Vector3.Lerp(dragItemVelocity, rawVel, dragInertiaSmoothing * dt);
            lastCarrySocketPos = currentSocketPos;

            float dragOffsetX = -player.Velocity.x * 0.035f;
            float dragOffsetY = -player.Velocity.y * 0.025f;

            Vector3 baseLocal = new Vector3(player.FacingDirection * 0.44f, 1.05f, 0f);
            carrySocket.localPosition = new Vector3(
                baseLocal.x + dragOffsetX,
                baseLocal.y + dragOffsetY,
                baseLocal.z
            );

            Vector3 predictVelocity = dragItemVelocity * dragThrowScale;
            predictVelocity = Vector3.ClampMagnitude(predictVelocity, MaxThrowSpeed);
            if (trajectoryPredictor != null && predictVelocity.sqrMagnitude > 0.5f)
            {
                Vector3 origin = currentSocketPos;
                origin.z = 0f;
                trajectoryPredictor.RenderTrajectory(origin, predictVelocity, Physics.gravity.y);
            }
            else if (trajectoryPredictor != null)
            {
                trajectoryPredictor.Hide();
            }
        }

        public void ExecuteDragThrow(PlayerController player)
        {
            if (trajectoryPredictor != null) trajectoryPredictor.Hide();
            if (carriedItem == null) return;

            Vector3 launchPos = (carrySocket != null) ? carrySocket.position : transform.position + Vector3.up * 1.1f;
            launchPos.z = 0f;

            Vector3 throwVelocity = dragItemVelocity * dragThrowScale;
            throwVelocity = Vector3.ClampMagnitude(throwVelocity, MaxThrowSpeed);

            if (throwVelocity.sqrMagnitude < MinThrowSpeed * MinThrowSpeed)
            {
                Vector3 forward = new Vector3(player.FacingDirection, 0.55f, 0f).normalized;
                throwVelocity = forward * MinThrowSpeed;
            }

            carriedItem.OnThrown(launchPos, throwVelocity);
            carriedItem = null;
            lmbDragActive = false;
            dragItemVelocity = Vector3.zero;
            player.SetState(PlayerState.Grounded);
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
