using UnityEngine;

/// <summary>
/// IPC between <see cref="GameplayScreenTouchZones"/> (full-screen touch) and <see cref="PlayerControllerUI2D"/>.
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

    /// <summary>Clears jump queue, axis holds, and auxiliary touch ids after overlays or before unlocking control.</summary>
    public static void ClearTouchRouting()
    {
        JumpQueued = false;
        MobileHoldAxis.Clear();
        GameplayScreenTouchZones.ClearAuxiliaryPointerState();
    }
}

/// <summary>
/// Ref-count style axis: multiple pointers can press left/right; release decrements. Player reads aggregated -1/0/+1.
/// </summary>
public static class MobileHoldAxis
{
    private static int _left;
    private static int _right;

    public static void PressLeft() => _left++;

    public static void ReleaseLeft() => _left = Mathf.Max(0, _left - 1);

    public static void PressRight() => _right++;

    public static void ReleaseRight() => _right = Mathf.Max(0, _right - 1);

    public static float Axis
    {
        get
        {
            if (_left > 0 && _right > 0)
                return 0f;
            if (_left > 0)
                return -1f;
            if (_right > 0)
                return 1f;
            return 0f;
        }
    }

    public static void Clear()
    {
        _left = 0;
        _right = 0;
    }
}
