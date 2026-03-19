#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Replaces legacy <see cref="StandaloneInputModule"/> with <see cref="InputSystemUIInputModule"/>
/// when <b>Active Input Handling</b> is <b>Input System Package</b> only.
/// </summary>
/// <remarks>
/// Uses <see cref="SceneManager.sceneLoaded"/> so the swap runs <b>after</b> <c>Awake</c>/<c>OnEnable</c>
/// on all objects in the new scene, but <b>before</b> the first <c>Update</c>. That avoids both
/// <see cref="StandaloneInputModule"/> touching <see cref="UnityEngine.Input"/> and ordering issues
/// where swapping in <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/> was too early for some Unity versions.
/// </remarks>
public static class EventSystemInputModuleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySwap();
    }

    private static void ApplySwap()
    {
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        foreach (var es in eventSystems)
        {
            if (es == null)
                continue;

            var go = es.gameObject;
            var standalone = go.GetComponent<StandaloneInputModule>();
            var inputModule = go.GetComponent<InputSystemUIInputModule>();

            if (standalone != null)
            {
                Object.DestroyImmediate(standalone);
                inputModule = go.GetComponent<InputSystemUIInputModule>();
            }

            if (inputModule == null)
                inputModule = go.AddComponent<InputSystemUIInputModule>();

            inputModule.AssignDefaultActions();
            es.UpdateModules();
        }
    }
}
#endif
