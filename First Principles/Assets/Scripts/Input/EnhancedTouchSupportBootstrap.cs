#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>Enables Enhanced Touch for multi-touch APIs used by <see cref="GraphPinchZoom"/>.</summary>
public static class EnhancedTouchSupportBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Enable()
    {
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
    }
}
#endif
