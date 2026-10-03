using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách đọc tín hiệu từ thiết bị ngoại vi (Bàn phím, Chuột, Gamepad)
    /// và cung cấp dữ liệu sạch cho các hệ thống gameplay khác.
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

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) Horizontal -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) Horizontal += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) Vertical += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                {
                    Vertical -= 1f;
                    CrouchPressed = true;
                }

                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) SprintHeld = true;
                if (kb.cKey.isPressed || kb.leftCtrlKey.isPressed) CrouchPressed = true;

                if (kb.spaceKey.wasPressedThisFrame) JumpPressed = true;
                JumpHeld = kb.spaceKey.isPressed;

                if (kb.eKey.wasPressedThisFrame) InteractPressed = true;
                if (kb.eKey.isPressed) GrabCrateHeld = true;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                MouseLeftPressed = mouse.leftButton.isPressed;
                MouseLeftReleasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
                if (mouse.rightButton.isPressed) AimThrowHeld = true;
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) Horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) Horizontal += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) Vertical += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                Vertical -= 1f;
                CrouchPressed = true;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) SprintHeld = true;
            if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl)) CrouchPressed = true;

            if (Input.GetKeyDown(KeyCode.Space)) JumpPressed = true;
            JumpHeld = Input.GetKey(KeyCode.Space);

            if (Input.GetKeyDown(KeyCode.E)) InteractPressed = true;
            if (Input.GetKey(KeyCode.E)) GrabCrateHeld = true;

            MouseLeftPressed = Input.GetMouseButton(0);
            MouseLeftReleasedThisFrame = Input.GetMouseButtonUp(0);
            if (Input.GetMouseButton(1)) AimThrowHeld = true;
#endif
        }
    }
}
