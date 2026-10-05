namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Các trạng thái chuyển động của người chơi trong hệ thống State Machine.
    /// </summary>
    public enum PlayerState
    {
        Grounded,
        Airborne,
        Carrying,
        PushPull,
        AimingThrow,
        LedgeGrab,
        LedgeClimb,
        RopeGrab,
        RopeClimb
    }

    /// <summary>
    /// Chế độ phối cảnh không gian gameplay.
    /// </summary>
    public enum PerspectiveMode
    {
        TwoD,
        TwoPointFiveD,
        ThreeD
    }
}
