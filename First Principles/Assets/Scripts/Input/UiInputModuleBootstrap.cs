#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures uGUI receives pointer/navigation input via the Input System. Scenes still
/// serialize the default <see cref="StandaloneInputModule"/>, but that module calls
/// <see cref="UnityEngine.Input"/>, which throws when <b>Active Input Handling</b> is
/// <i>Input System Package</i> only.
/// </summary>
/// <remarks>
/// Hooks <see cref="SceneManager.sceneLoaded"/>, which runs after Awake/OnEnable for the new
/// scene but before the first <see cref="EventSystem.Update"/>, so <see cref="StandaloneInputModule"/>
/// never reaches <c>UpdateModule</c> while legacy input is disabled.
/// </remarks>
public static class UiInputModuleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude))
        {
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy == null)
                continue;

            Object.DestroyImmediate(legacy);

            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();

            es.UpdateModules();
        }
    }
}
#endif
