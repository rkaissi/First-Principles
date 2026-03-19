using UnityEngine;

/// <summary>
/// Central place for **screen-dependent layout policy** used by HUD, level select, touch bar, CanvasScaler tuning.
/// <list type="bullet">
/// <item><description><see cref="PreferOnScreenGameControls"/> — show touch gameplay chrome.</description></item>
/// <item><description><see cref="IsTabletLike"/> — dp-based; avoids treating phones as tablets.</description></item>
/// <item><description><see cref="ApplySafeAreaAnchors"/> — normalized <see cref="Screen.safeArea"/> → RectTransform anchors.</description></item>
/// </list>
/// </summary>
public static class DeviceLayout
{
    private static Rect _lastSafeArea;
    private static Vector2 _lastScreen;

    /// <summary>True when we show on-screen move/jump controls in the Game scene.</summary>
    public static bool PreferOnScreenGameControls =>
        Application.isMobilePlatform ||
        SystemInfo.deviceType == DeviceType.Handheld ||
        Input.touchSupported;

    /// <summary>
    /// iPad-class devices: shortest side in approximate dp (avoids mislabeling phones by pixel count alone).
    /// </summary>
    public static bool IsTabletLike()
    {
        if (!PreferOnScreenGameControls)
            return false;

        float dpi = Screen.dpi > 1f ? Screen.dpi : 163f;
        float minPx = Mathf.Min(Screen.width, Screen.height);
        float minDp = minPx / dpi * 160f;
        return minDp >= 592f;
    }

    /// <summary>CanvasScaler <c>matchWidthOrHeight</c> tuned for phone vs ~4:3 tablet / split view.</summary>
    public static float RecommendedCanvasMatchWidthOrHeight()
    {
        float w = Screen.width;
        float h = Screen.height;
        float min = Mathf.Min(w, h);
        float max = Mathf.Max(w, h);
        float aspect = max / Mathf.Max(1f, min);

        if (IsTabletLike())
        {
            if (aspect < 1.42f)
                return 0.52f;
            if (aspect > 1.75f)
                return 0.48f;
            return 0.5f;
        }

        if (aspect > 1.85f)
            return 0.42f;
        if (aspect < 1.55f)
            return 0.48f;
        return 0.45f;
    }

    public static float TouchControlBarHeight => IsTabletLike() ? 188f : 168f;

    public static float TouchHintVerticalOffset => IsTabletLike() ? 212f : 188f;

    public static Vector2 LevelSelectScrollAnchorMin =>
        IsTabletLike() ? new Vector2(0.12f, 0.11f) : new Vector2(0.04f, 0.12f);

    public static Vector2 LevelSelectScrollAnchorMax =>
        IsTabletLike() ? new Vector2(0.88f, 0.73f) : new Vector2(0.96f, 0.74f);

    public static float LevelSelectScrollSensitivity => IsTabletLike() ? 44f : 28f;

    /// <summary>Apply normalized Screen.safeArea to a full-bleed RectTransform (typical content root).</summary>
    public static void ApplySafeAreaAnchors(RectTransform rt)
    {
        if (rt == null)
            return;

        Rect sa = Screen.safeArea;
        float w = Mathf.Max(1f, Screen.width);
        float h = Mathf.Max(1f, Screen.height);

        Vector2 min = new Vector2(sa.xMin / w, sa.yMin / h);
        Vector2 max = new Vector2(sa.xMax / w, sa.yMax / h);

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static bool SafeAreaOrScreenChanged()
    {
        Rect sa = Screen.safeArea;
        Vector2 s = new Vector2(Screen.width, Screen.height);
        if (sa != _lastSafeArea || s != _lastScreen)
        {
            _lastSafeArea = sa;
            _lastScreen = s;
            return true;
        }

        return false;
    }
}
