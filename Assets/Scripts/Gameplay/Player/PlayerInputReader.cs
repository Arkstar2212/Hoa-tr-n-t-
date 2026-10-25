using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách đọc tín hiệu từ thiết bị ngoại vi (Bàn phím, Chuột, Gamepad)
    /// tích hợp với KeybindingManager để hỗ trợ tùy chỉnh phím linh hoạt.
    /// </summary>
    public class PlayerInputReader : IPlayerInput
    {
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool GrabCrateHeld { get; private set; }
        public bool AimThrowHeld { get; private set; }
        public bool MouseLeftPressed { get; private set; }
        public bool MouseLeftReleasedThisFrame { get; private set; }
        public Vector2 MousePosition { get; private set; }
        public bool MouseRightPressed { get; private set; }
        public bool RopeGrabPressed { get; private set; }

        public void ReadInput()
        {
            Horizontal = 0f;
            Vertical = 0f;
            JumpPressed = false;
            JumpHeld = false;
            CrouchPressed = false;
            SprintHeld = false;
            InteractPressed = false;
            GrabCrateHeld = false;
            AimThrowHeld = false;
            MouseLeftPressed = false;
            MouseLeftReleasedThisFrame = false;
            MouseRightPressed = false;
            RopeGrabPressed = false;

            // 1. Đọc phím theo cấu hình KeybindingManager
            if (KeybindingManager.IsActionHeld(GameAction.MoveLeft)) Horizontal -= 1f;
            if (KeybindingManager.IsActionHeld(GameAction.MoveRight)) Horizontal += 1f;
            if (KeybindingManager.IsActionHeld(GameAction.MoveUp)) Vertical += 1f;
            if (KeybindingManager.IsActionHeld(GameAction.MoveDown))
            {
                Vertical -= 1f;
                CrouchPressed = true;
            }

            if (KeybindingManager.IsActionHeld(GameAction.Sprint)) SprintHeld = true;
            if (KeybindingManager.IsActionPressed(GameAction.Crouch) || KeybindingManager.IsActionHeld(GameAction.Crouch)) CrouchPressed = true;

            if (KeybindingManager.IsActionPressed(GameAction.Jump)) JumpPressed = true;
            if (KeybindingManager.IsActionHeld(GameAction.Jump)) JumpHeld = true;

            if (KeybindingManager.IsActionPressed(GameAction.Interact)) InteractPressed = true;
            if (KeybindingManager.IsActionHeld(GameAction.Interact)) GrabCrateHeld = true;

            if (KeybindingManager.IsActionPressed(GameAction.RopeGrab)) RopeGrabPressed = true;

            // 2. Fallback cho New Input System nếu có
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed) Horizontal = Mathf.Clamp(Horizontal - 1f, -1f, 1f);
                if (kb.rightArrowKey.isPressed) Horizontal = Mathf.Clamp(Horizontal + 1f, -1f, 1f);
                if (kb.upArrowKey.isPressed) Vertical = Mathf.Clamp(Vertical + 1f, -1f, 1f);
                if (kb.downArrowKey.isPressed) { Vertical = Mathf.Clamp(Vertical - 1f, -1f, 1f); CrouchPressed = true; }
                if (kb.spaceKey.wasPressedThisFrame) JumpPressed = true;
                if (kb.spaceKey.isPressed) JumpHeld = true;
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) SprintHeld = true;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                MouseLeftPressed = mouse.leftButton.isPressed;
                MouseLeftReleasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
                MousePosition = mouse.position.ReadValue();
                MouseRightPressed = mouse.rightButton.isPressed;
                if (mouse.rightButton.isPressed) AimThrowHeld = true;
            }
#else
            MouseLeftPressed = Input.GetMouseButton(0);
            MouseLeftReleasedThisFrame = Input.GetMouseButtonUp(0);
            MousePosition = Input.mousePosition;
            MouseRightPressed = Input.GetMouseButton(1);
            if (Input.GetMouseButton(1)) AimThrowHeld = true;
#endif

            // Hold chuột trái hoặc phím E đều dùng để kéo/đẩy thùng
            if (MouseLeftPressed)
            {
                GrabCrateHeld = true;
            }
        }
    }
}
