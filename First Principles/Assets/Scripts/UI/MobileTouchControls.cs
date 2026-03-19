using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds a bottom **◀ / ▶ / Jump** row parented under <see cref="MobileUiRoots"/>.
/// Phones: buttons expand to fill width. Tablets: fixed widths + flex spacers so landscape iPad
/// does not create kilometer-wide hit targets. Uses <see cref="IPointerDownHandler"/> for responsive hold-to-run.
/// </summary>
public class MobileTouchControls : MonoBehaviour
{
    private static MobileTouchControls _instance;

    public static void EnsureForGameCanvas(Transform canvasTransform)
    {
        if (canvasTransform == null || !ShouldShow())
            return;

        if (_instance != null)
            return;

        Transform existing = canvasTransform.Find("MobileTouchControlsRoot");
        if (existing == null)
        {
            var safe = canvasTransform.Find(MobileUiRoots.SafeContentName);
            if (safe != null)
                existing = safe.Find("MobileTouchControlsRoot");
        }

        if (existing != null)
        {
            _instance = existing.GetComponent<MobileTouchControls>();
            return;
        }

        var go = new GameObject("MobileTouchControlsRoot", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        var parent = MobileUiRoots.GetSafeContentParent(canvasTransform) ?? canvasTransform as RectTransform;
        rt.SetParent(parent, false);
        rt.SetAsLastSibling();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 10f);
        rt.sizeDelta = new Vector2(0f, DeviceLayout.TouchControlBarHeight);

        _instance = go.AddComponent<MobileTouchControls>();
        _instance.Build(rt);
    }

    private static bool ShouldShow() => DeviceLayout.PreferOnScreenGameControls;

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        MobileHoldAxis.Clear();
    }

    private void Build(RectTransform root)
    {
        bool tablet = DeviceLayout.IsTabletLike();
        float gap = tablet ? 20f : 16f;

        var row = new GameObject("Row", typeof(RectTransform));
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.SetParent(root, false);
        rowRt.anchorMin = Vector2.zero;
        rowRt.anchorMax = Vector2.one;
        rowRt.offsetMin = new Vector2(tablet ? 20f : 12f, 10f);
        rowRt.offsetMax = new Vector2(tablet ? -20f : -12f, -10f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = gap;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        // Phones: stretch buttons across width. Tablets: fixed widths + side spacers so bars are not huge in landscape.
        hlg.childForceExpandWidth = !tablet;

        if (tablet)
            AddLayoutSpacer(rowRt, flexibleWidth: 1f);

        CreateHoldButton(rowRt, "Left", -1f, new Color(0.15f, 0.16f, 0.2f, 0.82f), tablet);
        CreateHoldButton(rowRt, "Right", 1f, new Color(0.15f, 0.16f, 0.2f, 0.82f), tablet);
        CreateJumpButton(rowRt, tablet);

        if (tablet)
            AddLayoutSpacer(rowRt, flexibleWidth: 1f);
    }

    private static void AddLayoutSpacer(Transform parent, float flexibleWidth)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = flexibleWidth;
        le.minWidth = 0f;
        le.preferredWidth = 0f;
    }

    private static void CreateHoldButton(Transform parent, string label, float dir, Color bg, bool tablet)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = tablet ? 252f : 200f;
        le.flexibleWidth = tablet ? 0f : 1f;
        le.minHeight = tablet ? 132f : 120f;
        if (tablet)
            le.preferredHeight = 132f;

        var img = go.AddComponent<Image>();
        img.color = bg;

        var h = go.AddComponent<MobileHoldButton>();
        h.Init(dir);

        var tr = new GameObject("Text");
        var trt = tr.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tmp = tr.AddComponent<TextMeshProUGUI>();
        tmp.text = label == "Left" ? "\u25C0" : "\u25B6";
        tmp.fontSize = tablet ? 54f : 48f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.55f, 1f);
        CopyTmpFont(tmp);
    }

    private static void CreateJumpButton(Transform parent, bool tablet)
    {
        var go = new GameObject("Btn_Jump", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = tablet ? 268f : 220f;
        le.flexibleWidth = tablet ? 0f : 1.2f;
        le.minHeight = tablet ? 132f : 120f;
        if (tablet)
            le.preferredHeight = 132f;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.32f, 0.28f, 0.88f);

        var tr = new GameObject("Text");
        var trt = tr.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tmp = tr.AddComponent<TextMeshProUGUI>();
        tmp.text = "Jump";
        tmp.fontSize = tablet ? 34f : 30f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        CopyTmpFont(tmp);

        var ev = go.AddComponent<MobileJumpTouch>();
    }

    private static void CopyTmpFont(TextMeshProUGUI tmp)
    {
        var any = FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any.font != null)
            tmp.font = any.font;
        else if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }

    private class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private float _dir;
        private bool _held;

        public void Init(float dir) => _dir = dir;

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = true;
            if (_dir < 0f)
                MobileHoldAxis.PressLeft();
            else
                MobileHoldAxis.PressRight();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_held)
                return;
            _held = false;
            if (_dir < 0f)
                MobileHoldAxis.ReleaseLeft();
            else
                MobileHoldAxis.ReleaseRight();
        }

        private void OnDisable()
        {
            if (_held)
            {
                _held = false;
                if (_dir < 0f)
                    MobileHoldAxis.ReleaseLeft();
                else
                    MobileHoldAxis.ReleaseRight();
            }
        }
    }

    /// <summary>Fires jump on pointer down (not only click) for better touch response.</summary>
    private class MobileJumpTouch : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData) => MobileInputBridge.QueueJump();
    }
}

/// <summary>
/// Ref-count style axis: multiple pointers could press Left; release decrements. Player reads aggregated -1/0/+1.
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
