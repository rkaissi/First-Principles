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

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError($"AsyncSceneLoader: LoadSceneAsync failed for '{sceneName}'. Is it in Build Settings?");
            yield break;
        }

        // Explicit: some Editor/player paths leave activation false if touched elsewhere.
        op.allowSceneActivation = true;

        // Do not use allowSceneActivation = false + 0.9 progress gate: on some Unity versions / loads
        // progress may never reach 0.9, leaving an indefinite "loading" screen (e.g. Level select → Menu).
        yield return null;

        while (!op.isDone)
            yield return null;
    }
}
