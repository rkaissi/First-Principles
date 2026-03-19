using UnityEngine;

/// <summary>
/// Ensures a child <see cref="SafeContentName"/> exists under each Canvas; <see cref="CanvasSafeAreaBootstrap"/>
/// reparents designer-created UI into it so runtime HUD (LevelManager, LevelSelect) shares the same inset.
/// </summary>
public static class MobileUiRoots
{
    public const string SafeContentName = "_SafeContent";

    /// <summary>Returns the RectTransform parents should use for fullscreen UI under this canvas.</summary>
    public static RectTransform GetSafeContentParent(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(SafeContentName);
        if (existing != null)
            return existing as RectTransform;

        var go = new GameObject(SafeContentName, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvasTransform, false);
        rt.SetAsFirstSibling();
        DeviceLayout.ApplySafeAreaAnchors(rt);
        return rt;
    }

    public static void RefreshSafeParent(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return;
        var t = canvasTransform.Find(SafeContentName);
        if (t is RectTransform rt)
            DeviceLayout.ApplySafeAreaAnchors(rt);
    }
}
