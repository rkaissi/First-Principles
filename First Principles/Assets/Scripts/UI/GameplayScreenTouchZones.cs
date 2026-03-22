using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Game scene: invisible full-screen touch layer. Hold <b>left half</b> → move left, hold <b>right half</b> → move right.
/// While holding movement, put down a <b>second finger</b> anywhere → jump (one shot per touch-down).
/// </summary>
public class GameplayScreenTouchZones : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public const string RootObjectName = "GameplayScreenTouchZonesRoot";

    private static GameplayScreenTouchZones _instance;

    private readonly Dictionary<int, int> _pointerSide = new Dictionary<int, int>();
    private readonly HashSet<int> _jumpOnlyPointerIds = new HashSet<int>();

    public static void EnsureForGameCanvas(Transform canvasTransform)
    {
        if (canvasTransform == null || !DeviceLayout.PreferOnScreenGameControls)
            return;

        DestroyLegacyButtonBar(canvasTransform);

        // Deep find: after ReparentTouchLayerAboveGraph the root lives under GraphContainer, not a direct canvas child.
        Transform existing = FindDeepChildByName(canvasTransform, RootObjectName);

        RectTransform rt;
        if (existing != null)
        {
            rt = existing as RectTransform;
            _instance = existing.GetComponent<GameplayScreenTouchZones>();
            if (_instance == null)
                _instance = existing.gameObject.AddComponent<GameplayScreenTouchZones>();
        }
        else
        {
            var parentRt = MobileUiRoots.GetSafeContentParent(canvasTransform) ?? canvasTransform as RectTransform;
            if (parentRt == null)
                return;

            var go = new GameObject(RootObjectName, typeof(RectTransform));
            rt = go.GetComponent<RectTransform>();
            rt.SetParent(parentRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            _instance = go.AddComponent<GameplayScreenTouchZones>();
        }

        _instance.EnsureRaycastGraphic();

        // Must sit above the full-screen graph stack (GraphicRaycaster hits top-first). Under _SafeContent
        // as first sibling put this layer *behind* GraphContainer — touches never reached gameplay.
        ReparentTouchLayerAboveGraph(canvasTransform, rt);
    }

    /// <summary>
    /// Full-screen touch layer is created whenever mobile gameplay prefers it, but disabled in graphing calculator
    /// so the same scene path always has the object (avoids missing controls after Menu → calculator → level).
    /// </summary>
    public static void SetActiveForGameplayMode(bool gameplayMovementEnabled)
    {
        if (_instance != null)
        {
            if (_instance.gameObject.activeSelf != gameplayMovementEnabled)
                _instance.gameObject.SetActive(gameplayMovementEnabled);
            return;
        }

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;
        var t = FindDeepChildByName(canvas.transform, RootObjectName);
        if (t != null && t.gameObject.activeSelf != gameplayMovementEnabled)
            t.gameObject.SetActive(gameplayMovementEnabled);
    }

    /// <summary>
    /// Inserts the touch catcher just above <c>Graph</c> inside <c>GraphContainer</c> so Footer/HUD stays on top.
    /// </summary>
    static void ReparentTouchLayerAboveGraph(Transform canvasTransform, RectTransform touchRt)
    {
        if (touchRt == null || canvasTransform == null)
            return;

        var graphContainer = FindDeepChildByName(canvasTransform, "GraphContainer") as RectTransform;
        if (graphContainer == null)
        {
            var fallback = MobileUiRoots.GetSafeContentParent(canvasTransform) ?? canvasTransform as RectTransform;
            if (fallback == null)
                return;
            touchRt.SetParent(fallback, false);
            touchRt.anchorMin = Vector2.zero;
            touchRt.anchorMax = Vector2.one;
            touchRt.offsetMin = Vector2.zero;
            touchRt.offsetMax = Vector2.zero;
            int gc = -1;
            for (int i = 0; i < fallback.childCount; i++)
            {
                if (fallback.GetChild(i).name == "GraphContainer")
                {
                    gc = i;
                    break;
                }
            }

            if (gc >= 0)
            {
                int fallbackMaxSibling = Mathf.Max(0, fallback.childCount - 1);
                touchRt.SetSiblingIndex(Mathf.Clamp(gc + 1, 0, fallbackMaxSibling));
            }
            else
                touchRt.SetAsLastSibling();
            return;
        }

        touchRt.SetParent(graphContainer, false);
        touchRt.anchorMin = Vector2.zero;
        touchRt.anchorMax = Vector2.one;
        touchRt.offsetMin = Vector2.zero;
        touchRt.offsetMax = Vector2.zero;

        int graphIdx = 0;
        for (int i = 0; i < graphContainer.childCount; i++)
        {
            if (graphContainer.GetChild(i).name == "Graph")
            {
                graphIdx = i;
                break;
            }
        }

        int graphMaxSibling = Mathf.Max(0, graphContainer.childCount - 1);
        touchRt.SetSiblingIndex(Mathf.Clamp(graphIdx + 1, 0, graphMaxSibling));
    }

    static Transform FindDeepChildByName(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = FindDeepChildByName(root.GetChild(i), name);
            if (c != null)
                return c;
        }

        return null;
    }

    private static void DestroyLegacyButtonBar(Transform canvasTransform)
    {
        Transform t = canvasTransform.Find("MobileTouchControlsRoot");
        if (t == null)
        {
            var safe = canvasTransform.Find(MobileUiRoots.SafeContentName);
            if (safe != null)
                t = safe.Find("MobileTouchControlsRoot");
        }

        if (t != null)
            UnityEngine.Object.Destroy(t.gameObject);
    }

    private void Awake()
    {
        EnsureRaycastGraphic();
    }

    private void EnsureRaycastGraphic()
    {
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
        }

        img.raycastTarget = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        MobileInputBridge.ClearTouchRouting();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        int id = eventData.pointerId;

        // Second finger while already steering: jump (do not stack another left/right hold).
        if (_pointerSide.Count >= 1)
        {
            MobileInputBridge.QueueJump();
            _jumpOnlyPointerIds.Add(id);
            return;
        }

        Vector2 pos = eventData.position;
        float mid = Screen.width * 0.5f;
        int side = pos.x < mid ? -1 : 1;
        _pointerSide[id] = side;
        if (side < 0)
            MobileHoldAxis.PressLeft();
        else
            MobileHoldAxis.PressRight();
    }

    public void OnPointerUp(PointerEventData eventData) =>
        EndPointer(eventData.pointerId);

    public void OnPointerExit(PointerEventData eventData) =>
        EndPointer(eventData.pointerId);

    private void EndPointer(int pointerId)
    {
        if (_jumpOnlyPointerIds.Remove(pointerId))
            return;

        if (!_pointerSide.TryGetValue(pointerId, out int side))
            return;

        if (side < 0)
            MobileHoldAxis.ReleaseLeft();
        else
            MobileHoldAxis.ReleaseRight();
        _pointerSide.Remove(pointerId);
    }

    /// <summary>
    /// Clears pointer bookkeeping when <see cref="MobileInputBridge.ClearTouchRouting"/> runs.
    /// Must match <see cref="MobileHoldAxis.Clear"/> — otherwise <c>_pointerSide.Count &gt;= 1</c> stays true while axis is 0
    /// and every new touch is misclassified as a second-finger jump (movement appears dead).
    /// </summary>
    internal static void ClearAuxiliaryPointerState()
    {
        if (_instance == null)
            return;
        _instance._jumpOnlyPointerIds.Clear();
        _instance._pointerSide.Clear();
    }
}
