using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene hook on the root Canvas: (1) creates/moves content under <see cref="MobileUiRoots.SafeContentName"/>,
/// (2) reapplies anchors when rotation or split-screen changes <see cref="Screen.safeArea"/>,
/// (3) updates <see cref="CanvasScaler.matchWidthOrHeight"/> via <see cref="DeviceLayout.RecommendedCanvasMatchWidthOrHeight"/>
/// so reference resolution 1080×1920 scales sensibly on phones vs ~4:3 tablets.
/// Executes at <c>DefaultExecutionOrder(-200)</c> before most other Awake/Start logic.
/// </summary>
[DefaultExecutionOrder(-200)]
public class CanvasSafeAreaBootstrap : MonoBehaviour
{
    private RectTransform _safeRt;
    private CanvasScaler _scaler;
    private float _lastAppliedMatch = -1f;

    private void Awake()
    {
        _scaler = GetComponent<CanvasScaler>();
        ReparentUnderSafeArea();
        ApplyCanvasScalerMatch();
    }

    private void LateUpdate()
    {
        if (!DeviceLayout.SafeAreaOrScreenChanged())
            return;
        if (_safeRt != null)
            DeviceLayout.ApplySafeAreaAnchors(_safeRt);
        ApplyCanvasScalerMatch();
    }

    private void ApplyCanvasScalerMatch()
    {
        if (_scaler == null)
            return;
        float m = DeviceLayout.RecommendedCanvasMatchWidthOrHeight();
        if (Mathf.Approximately(m, _lastAppliedMatch))
            return;
        _lastAppliedMatch = m;
        _scaler.matchWidthOrHeight = m;
    }

    private void ReparentUnderSafeArea()
    {
        var canvasRt = GetComponent<RectTransform>();
        if (canvasRt == null)
            return;

        _safeRt = MobileUiRoots.GetSafeContentParent(canvasRt);

        var move = new List<Transform>();
        for (int i = 0; i < canvasRt.childCount; i++)
        {
            var c = canvasRt.GetChild(i);
            if (c != _safeRt.transform)
                move.Add(c);
        }

        foreach (var c in move)
            c.SetParent(_safeRt, false);
    }
}
