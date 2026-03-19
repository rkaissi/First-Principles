using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// SceneFader — full-screen Image alpha fade between Menu / LevelSelect / Game
// -----------------------------------------------------------------------------
// OnEnable picks white vs black overlay by scene name, then fades Out (opaque→clear).
// Public API loads scenes after fading In (clear→opaque) via coroutines. Assign both
// Images in the inspector on Menu (or shared prefab).
// -----------------------------------------------------------------------------

public class SceneFader : MonoBehaviour
{

	#region FIELDS
	/// <summary>Typically used on Menu (lighter fade).</summary>
	public Image fadeOutUIImage1; // White background
	/// <summary>Typically used on Game / LevelSelect (darker fade).</summary>
	public Image fadeOutUIImage2; // Black background

	private Image fadeOutUIImage;

	private TextMeshProUGUI faxasMenuButtonText;
	private TextMeshProUGUI menuLanguageButtonLabel;

	public float fadeSpeed = 0.8f;

	public enum FadeDirection
	{
		In, //Alpha = 1
		Out // Alpha = 0
	}
	#endregion

	#region MONOBEHAVIOR
	void OnEnable()
	{
        LocalizationManager.LanguageChanged += RefreshMenuLocalizedControls;

		Scene currentScene = SceneManager.GetActiveScene();

		if (currentScene.name == "Menu")
        {
			fadeOutUIImage = fadeOutUIImage1;
			fadeOutUIImage.gameObject.SetActive(true);
			fadeOutUIImage.enabled = true;
			fadeOutUIImage.raycastTarget = true;
			fadeOutUIImage2.gameObject.SetActive(false);

			ZoomIn();
			StartCoroutine(SpawnMenuExtrasNextFrame());

			// FindObjectOfType<AudioManager>().PlayMusic("RainMusic");
		}
		else if (currentScene.name == "Game")
		{
			fadeOutUIImage = fadeOutUIImage2;
			fadeOutUIImage.gameObject.SetActive(true);
			fadeOutUIImage.enabled = true;
			fadeOutUIImage.raycastTarget = true;
			fadeOutUIImage1.gameObject.SetActive(false);

			// FindObjectOfType<AudioManager>().PlayMusic("ClassicalMusic");
		}
		else if (currentScene.name == "LevelSelect")
		{
			fadeOutUIImage = fadeOutUIImage2;
			fadeOutUIImage.gameObject.SetActive(true);
			fadeOutUIImage.enabled = true;
			fadeOutUIImage.raycastTarget = true;
			fadeOutUIImage1.gameObject.SetActive(false);
		}

		StartCoroutine(Fade(FadeDirection.Out));
	}

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshMenuLocalizedControls;
    }
	#endregion

	#region FADE
	private IEnumerator Fade(FadeDirection fadeDirection)
	{
		float alpha = (fadeDirection == FadeDirection.Out) ? 1 : 0;
		float fadeEndValue = (fadeDirection == FadeDirection.Out) ? 0 : 1;
		if (fadeDirection == FadeDirection.Out)
		{
			while (alpha >= fadeEndValue)
			{
				SetColorImage(ref alpha, fadeDirection);
				yield return null;
			}
			fadeOutUIImage.enabled = false;
			// Full-screen fade must not eat clicks when invisible (alpha 0 + raycast off).
			fadeOutUIImage.raycastTarget = false;
		}
		else
		{
			fadeOutUIImage.enabled = true;
			fadeOutUIImage.raycastTarget = true;
			while (alpha <= fadeEndValue)
			{
				SetColorImage(ref alpha, fadeDirection);
				yield return null;
			}
		}
	}
	#endregion

	#region HELPERS
	public IEnumerator FadeAndLoadScene(FadeDirection fadeDirection, string sceneToLoad)
	{
		yield return Fade(fadeDirection);
		SceneManager.LoadScene(sceneToLoad);
	}

	private void SetColorImage(ref float alpha, FadeDirection fadeDirection)
	{
		fadeOutUIImage.color = new Color(fadeOutUIImage.color.r, fadeOutUIImage.color.g, fadeOutUIImage.color.b, alpha);
		alpha += Time.deltaTime * (1.0f / fadeSpeed) * ((fadeDirection == FadeDirection.Out) ? -1 : 1);
	}
    #endregion

    private void ZoomIn() { }

    private IEnumerator SpawnMenuExtrasNextFrame()
    {
        yield return null;
        TrySpawnFaxasGraphingMenuButton();
        TrySpawnLanguagePicker();
        RefreshMenuLocalizedControls();
    }

    private void RefreshMenuLocalizedControls()
    {
        if (faxasMenuButtonText != null)
        {
            faxasMenuButtonText.text = LocalizationManager.Get("ui.faxas_graphing", "Faxas-style graphing");
            LocalizationManager.ApplyTextDirection(faxasMenuButtonText);
        }

        if (menuLanguageButtonLabel != null)
        {
            menuLanguageButtonLabel.text =
                $"{LocalizationManager.Get("ui.language", "Language")}: {LocalizationManager.GetLanguagePickerLabel(LocalizationManager.CurrentLanguage)}";
            LocalizationManager.ApplyTextDirection(menuLanguageButtonLabel);
        }
    }

    /// <summary>Runtime entry for free graphing mode (no extra scene objects required).</summary>
    private void TrySpawnFaxasGraphingMenuButton()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "Menu", System.StringComparison.Ordinal))
            return;
        if (GameObject.Find("FaxasGraphingEntryButton") != null)
            return;

        var play = GameObject.FindGameObjectWithTag("PlayButton");
        if (play == null)
            return;

        var playRt = play.GetComponent<RectTransform>();
        if (playRt == null)
            return;

        var go = new GameObject("FaxasGraphingEntryButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(playRt.parent, false);
        rt.localScale = Vector3.one;
        rt.anchorMin = playRt.anchorMin;
        rt.anchorMax = playRt.anchorMax;
        rt.pivot = playRt.pivot;
        rt.sizeDelta = playRt.sizeDelta;
        rt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, 168f);

        var playImg = play.GetComponent<Image>();
        var img = go.AddComponent<Image>();
        if (playImg != null && playImg.sprite != null)
        {
            img.sprite = playImg.sprite;
            img.type = playImg.type;
        }

        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = new Color(0.16f, 0.48f, 0.40f, 0.98f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, img.color,
            Color.Lerp(img.color, Color.white, 0.2f),
            Color.Lerp(img.color, Color.black, 0.18f));
        RuntimeUiPolish.ApplyDropShadow(rt, new Vector2(2f, -3f), 0.28f);
        btn.onClick.AddListener(LoadGraphCalculator);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 6f);
        trt.offsetMax = new Vector2(-10f, -6f);

        faxasMenuButtonText = textGo.AddComponent<TextMeshProUGUI>();
        bool tablet = DeviceLayout.IsTabletLike();
        faxasMenuButtonText.text = LocalizationManager.Get("ui.faxas_graphing", "Faxas-style graphing");
        faxasMenuButtonText.fontSize = tablet ? 26 : 22;
        faxasMenuButtonText.alignment = TextAlignmentOptions.Center;
        faxasMenuButtonText.color = new Color(0.92f, 0.98f, 1f, 1f);
        faxasMenuButtonText.textWrappingMode = TextWrappingModes.Normal;
        faxasMenuButtonText.richText = true;
        LocalizationManager.ApplyTextDirection(faxasMenuButtonText);
        var refTmp = play.GetComponentInChildren<TextMeshProUGUI>();
        if (refTmp != null && refTmp.font != null)
        {
            faxasMenuButtonText.font = refTmp.font;
            if (refTmp.fontSharedMaterial != null)
                faxasMenuButtonText.fontSharedMaterial = refTmp.fontSharedMaterial;
        }
        else if (TMP_Settings.defaultFontAsset != null)
            faxasMenuButtonText.font = TMP_Settings.defaultFontAsset;
    }

    /// <summary>Tappable control to cycle UI language (compact for phones).</summary>
    private void TrySpawnLanguagePicker()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "Menu", System.StringComparison.Ordinal))
            return;
        if (GameObject.Find("MenuLanguagePicker") != null)
            return;

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var parentRt = MobileUiRoots.GetSafeContentParent(canvas.transform) as RectTransform ?? canvas.transform as RectTransform;
        if (parentRt == null)
            return;

        bool tablet = DeviceLayout.IsTabletLike();
        var go = new GameObject("MenuLanguagePicker");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parentRt, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(tablet ? 22f : 16f, tablet ? -18f : -14f);
        rt.sizeDelta = new Vector2(tablet ? 320f : 280f, tablet ? 48f : 44f);

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = new Color(0.16f, 0.2f, 0.28f, 0.92f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, img.color,
            Color.Lerp(img.color, Color.white, 0.15f),
            Color.Lerp(img.color, Color.black, 0.2f));
        btn.onClick.AddListener(LocalizationManager.CycleNext);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 4f);
        trt.offsetMax = new Vector2(-10f, -4f);

        menuLanguageButtonLabel = textGo.AddComponent<TextMeshProUGUI>();
        menuLanguageButtonLabel.fontSize = tablet ? 22 : 19;
        menuLanguageButtonLabel.alignment = TextAlignmentOptions.Center;
        menuLanguageButtonLabel.color = new Color(0.93f, 0.96f, 1f, 1f);
        menuLanguageButtonLabel.textWrappingMode = TextWrappingModes.Normal;

        var refTmp = Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (refTmp != null && refTmp.font != null)
        {
            menuLanguageButtonLabel.font = refTmp.font;
            if (refTmp.fontSharedMaterial != null)
                menuLanguageButtonLabel.fontSharedMaterial = refTmp.fontSharedMaterial;
        }
        else if (TMP_Settings.defaultFontAsset != null)
            menuLanguageButtonLabel.font = TMP_Settings.defaultFontAsset;
    }

    public void LoadGame()
    {
		fadeOutUIImage.gameObject.SetActive(false);
		fadeOutUIImage = fadeOutUIImage2;
		fadeOutUIImage.gameObject.SetActive(true);
		fadeOutUIImage.enabled = true;
		fadeOutUIImage.raycastTarget = true;

		// Coroutine allows developers to run different tasks simultaneously (for multitasking)
		StartCoroutine(FadeAndLoadScene(FadeDirection.In, "Game"));
    }

	public void LoadLevelSelect()
	{
		fadeOutUIImage.gameObject.SetActive(false);
		fadeOutUIImage = fadeOutUIImage2;
		fadeOutUIImage.gameObject.SetActive(true);
		fadeOutUIImage.enabled = true;
		fadeOutUIImage.raycastTarget = true;

		StartCoroutine(FadeAndLoadScene(FadeDirection.In, "LevelSelect"));
	}

	/// <summary>Faxas Instruments–style free graph (same <c>Game</c> scene, platformer off).</summary>
	public void LoadGraphCalculator()
	{
		GraphCalculatorSession.RequestEnterFromMenu();
		fadeOutUIImage.gameObject.SetActive(false);
		fadeOutUIImage = fadeOutUIImage2;
		fadeOutUIImage.gameObject.SetActive(true);
		fadeOutUIImage.enabled = true;
		fadeOutUIImage.raycastTarget = true;
		StartCoroutine(FadeAndLoadScene(FadeDirection.In, "Game"));
	}

	public void LoadMenu()
    {
		fadeOutUIImage.gameObject.SetActive(false);
		fadeOutUIImage = fadeOutUIImage1;
		fadeOutUIImage.gameObject.SetActive(true);
		fadeOutUIImage.enabled = true;
		fadeOutUIImage.raycastTarget = true;

		StartCoroutine(FadeAndLoadScene(FadeDirection.In, "Menu"));
    }

	public void QuitGame()
    {
		Application.Quit();
    }
}