using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Two-finger pinch zoom on the math window (<see cref="FunctionPlotter.xStart"/> / <c>xEnd</c>).
/// Only used in graphic calculator mode.
/// </summary>
public class GraphPinchZoom : MonoBehaviour
{
    private FunctionPlotter plot;
    private float lastDist;
    private bool pinching;

    public void Setup(FunctionPlotter plotter)
    {
        plot = plotter;
        enabled = plotter != null;
    }

    private void Update()
    {
        if (plot == null)
            return;

        if (Touch.activeTouches.Count == 2)
        {
            var t0 = Touch.activeTouches[0];
            var t1 = Touch.activeTouches[1];
            float d = Vector2.Distance(t0.screenPosition, t1.screenPosition);
            if (pinching && lastDist > 2f)
            {
                // Fingers closer => smaller d => ratio < 1 => narrower half-width => zoom in.
                float ratio = d / lastDist;
                ApplyHalfWidthScale(ratio);
            }
            lastDist = d;
            pinching = true;
        }
        else
        {
            pinching = false;
            lastDist = 0f;
        }
    }

    private void ApplyHalfWidthScale(float ratio)
    {
        float mid = (plot.xStart + plot.xEnd) * 0.5f;
        float half = (plot.xEnd - plot.xStart) * 0.5f * ratio;
        half = Mathf.Clamp(half, 0.32f, 160f);
        plot.xStart = mid - half;
        plot.xEnd = mid + half;
        plot.step = Mathf.Clamp((plot.xEnd - plot.xStart) / 520f, 0.004f, 0.42f);
    }
}
