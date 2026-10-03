using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Interface Segregation Principle:
    /// Interface trừu tượng hóa toàn bộ input của người chơi,
    /// cho phép thay thế bàn phím, gamepad, mobile touch hoặc AI Bot test mà không chạm vào logic nhân vật.
    /// </summary>
    public interface IPlayerInput
    {
        float Horizontal { get; }
        float Vertical { get; }
        bool JumpPressed { get; }
        bool JumpHeld { get; }
        bool CrouchPressed { get; }
        bool SprintHeld { get; }
        bool InteractPressed { get; }
        bool GrabCrateHeld { get; }
        bool AimThrowHeld { get; }
        bool MouseLeftPressed { get; }
        bool MouseLeftReleasedThisFrame { get; }

        void ReadInput();
    }
}
