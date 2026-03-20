using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// SceneTransitionHost — survives scene unload during LoadSceneAsync
// -----------------------------------------------------------------------------
// LevelSelect (and similar) used to drive async loads from a MonoBehaviour that
// lives in the scene being unloaded. On some Unity versions / timings the host can
// be destroyed before the AsyncOperation finishes, leaving the "Loading" veil up
// and the next scene never activated. This helper parents a short-lived runner on
// DontDestroyOnLoad so the load always runs to completion, then self-destructs.
// -----------------------------------------------------------------------------

/// <summary>
/// Starts a single-scene async load from a temporary DontDestroyOnLoad object.
/// </summary>
public sealed class SceneTransitionHost : MonoBehaviour
{
    /// <summary>
    /// Loads <paramref name="sceneName"/> with <see cref="LoadSceneMode.Single"/>.
    /// Safe to call from UI buttons on scenes that will unload as part of the transition.
    /// </summary>
    public static void LoadSingleScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        var go = new GameObject(nameof(SceneTransitionHost));
        DontDestroyOnLoad(go);
        var host = go.AddComponent<SceneTransitionHost>();
        host._sceneName = sceneName;
        host.Begin();
    }

    private string _sceneName;

    private void Begin()
    {
        CreateBlockingOverlay();
        StartCoroutine(Run());
    }

    /// <summary>
    /// Full-screen dim + loading hint so the screen is never left blank if the
    /// originating scene’s UI is destroyed mid-load.
    /// </summary>
    private void CreateBlockingOverlay()
    {
        var canvasGo = new GameObject("TransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = DeviceLayout.RecommendedCanvasMatchWidthOrHeight();

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimGo.transform.SetParent(canvasGo.transform, false);
        var dimRt = dimGo.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dim = dimGo.GetComponent<Image>();
        dim.color = new Color(0.05f, 0.06f, 0.09f, 0.94f);
        dim.raycastTarget = true;

        var labelGo = new GameObject("LoadingText", typeof(RectTransform));
        labelGo.transform.SetParent(canvasGo.transform, worldPositionStays: false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(720f, 120f);

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = LocalizationManager.Get("ui.loading", "Loading…");
        tmp.fontSize = UiTypography.Scale(40);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.94f, 0.96f, 1f, 0.96f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.richText = true;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        LocalizationManager.ApplyTextDirection(tmp);
    }

    private IEnumerator Run()
    {
        yield return AsyncSceneLoader.LoadCoroutine(_sceneName);
        Destroy(gameObject);
    }
}
