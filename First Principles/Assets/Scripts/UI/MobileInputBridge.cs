using UnityEngine;

/// <summary>
/// Tiny IPC between <see cref="MobileTouchControls"/> (UI events) and <see cref="PlayerControllerUI2D"/>.
/// Horizontal motion uses <see cref="MobileHoldAxis"/> counts; jump uses a one-shot queue consumed each Update.
/// </summary>
public static class MobileInputBridge
{
    public static float TouchHorizontal => MobileHoldAxis.Axis;

    public static bool JumpQueued { get; private set; }

    public static void QueueJump() => JumpQueued = true;

    public static bool ConsumeJump()
    {
        if (!JumpQueued)
            return false;
        JumpQueued = false;
        return true;
    }
}
