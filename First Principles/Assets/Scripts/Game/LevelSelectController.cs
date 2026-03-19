using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds a simple Limbo-style level list UI at runtime and loads <c>Game</c> with <see cref="LevelSelection"/>.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.22f, 0.22f, 0.22f, 1f);

    private void Start()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("LevelSelectController: No Canvas in scene.");
            return;
        }

        var panel = new GameObject("LevelSelectPanel");
        var prt = panel.AddComponent<RectTransform>();
        prt.SetParent(canvas.transform, false);
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        var pimg = panel.AddComponent<Image>();
        pimg.color = backgroundColor;
        pimg.raycastTarget = true;

        var titleGo = new GameObject("Title");
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.SetParent(panel.transform, false);
        titleRt.anchorMin = new Vector2(0.5f, 0.88f);
        titleRt.anchorMax = new Vector2(0.5f, 0.88f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(900f, 90f);
        titleRt.anchoredPosition = Vector2.zero;

        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Choose a graph stage";
        titleTmp.fontSize = 44;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        CopyFontFromAny(titleTmp);

        var listGo = new GameObject("LevelList");
        var listRt = listGo.AddComponent<RectTransform>();
        listRt.SetParent(panel.transform, false);
        listRt.anchorMin = new Vector2(0.5f, 0.5f);
        listRt.anchorMax = new Vector2(0.5f, 0.5f);
        listRt.pivot = new Vector2(0.5f, 0.5f);
        listRt.sizeDelta = new Vector2(560f, 420f);
        listRt.anchoredPosition = new Vector2(0f, -20f);

        var vlg = listGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 18f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        for (int i = 0; i < GameLevelCatalog.LevelCount; i++)
        {
            int idx = i;
            CreateLevelButton(listRt, GameLevelCatalog.DisplayNames[i], () => StartGameAt(idx));
        }

        CreateBackButton(panel.transform);
    }

    private static void CopyFontFromAny(TextMeshProUGUI target)
    {
        var any = FindFirstObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
            target.font = any.font;
    }

    private void CreateLevelButton(Transform parent, string label, UnityAction onClick)
    {
        var go = new GameObject("LevelButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 68f;
        le.minHeight = 68f;

        var img = go.AddComponent<Image>();
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 8f);
        trt.offsetMax = new Vector2(-20f, -8f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        CopyFontFromAny(tmp);

        btn.onClick.AddListener(onClick);
    }

    private void CreateBackButton(Transform parent)
    {
        var go = new GameObject("BackButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.06f);
        rt.anchorMax = new Vector2(0.5f, 0.06f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(240f, 52f);
        rt.anchoredPosition = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Back to Menu";
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        CopyFontFromAny(tmp);

        btn.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
    }

    private static void StartGameAt(int index)
    {
        LevelSelection.SetSelectedLevel(index);
        SceneManager.LoadScene("Game");
    }
}
