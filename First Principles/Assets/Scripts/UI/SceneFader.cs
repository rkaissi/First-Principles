using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

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
		Scene currentScene = SceneManager.GetActiveScene();

		if (currentScene.name == "Menu")
        {
			fadeOutUIImage = fadeOutUIImage1;
			fadeOutUIImage.gameObject.SetActive(true);
			fadeOutUIImage2.gameObject.SetActive(false);

			ZoomIn();

			// FindObjectOfType<AudioManager>().PlayMusic("RainMusic");
		}
		else if (currentScene.name == "Game")
		{
			fadeOutUIImage = fadeOutUIImage2;
			fadeOutUIImage.gameObject.SetActive(true);
			fadeOutUIImage1.gameObject.SetActive(false);

			// FindObjectOfType<AudioManager>().PlayMusic("ClassicalMusic");
		}
		else if (currentScene.name == "LevelSelect")
		{
			fadeOutUIImage = fadeOutUIImage2;
			fadeOutUIImage.gameObject.SetActive(true);
			fadeOutUIImage1.gameObject.SetActive(false);
		}

		StartCoroutine(Fade(FadeDirection.Out));
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
		}
		else
		{
			fadeOutUIImage.enabled = true;
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

    private void ZoomIn() => Debug.Log("Zoomed In");

    public void LoadGame()
    {
		fadeOutUIImage.gameObject.SetActive(false);
		fadeOutUIImage = fadeOutUIImage2;
		fadeOutUIImage.gameObject.SetActive(true);

		// Coroutine allows developers to run different tasks simultaneously (for multitasking)
		StartCoroutine(FadeAndLoadScene(FadeDirection.In, "Game"));
    }

	public void LoadLevelSelect()
	{
		fadeOutUIImage.gameObject.SetActive(false);
		fadeOutUIImage = fadeOutUIImage2;
		fadeOutUIImage.gameObject.SetActive(true);

		StartCoroutine(FadeAndLoadScene(FadeDirection.In, "LevelSelect"));
	}

	public void LoadMenu()
    {
		fadeOutUIImage.gameObject.SetActive(false);
		fadeOutUIImage = fadeOutUIImage1;
		fadeOutUIImage.gameObject.SetActive(true);

		StartCoroutine(FadeAndLoadScene(FadeDirection.In, "Menu"));
    }

	public void QuitGame()
    {
		Application.Quit();
    }
}