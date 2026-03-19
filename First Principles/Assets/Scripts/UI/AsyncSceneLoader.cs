using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene loads without freezing the UI; use after a fade-to-black or with a loading label.</summary>
public static class AsyncSceneLoader
{
    public static IEnumerator LoadCoroutine(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"AsyncSceneLoader: LoadSceneAsync failed for '{sceneName}'.");
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;
    }
}
