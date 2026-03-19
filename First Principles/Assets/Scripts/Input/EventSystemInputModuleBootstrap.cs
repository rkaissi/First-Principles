#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Replaces legacy <see cref="StandaloneInputModule"/> with <see cref="InputSystemUIInputModule"/>
/// so uGUI works when <b>Active Input Handling</b> is <b>Input System Package</b>.
/// Runs at <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/> (after scene Awakes) but before the first
/// <c>Update</c>, so <see cref="StandaloneInputModule"/> never ticks and never touches <see cref="UnityEngine.Input"/>.
/// </summary>
public static class EventSystemInputModuleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        ApplySwap();
    }

    private static void ApplySwap()
    {
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude);
        foreach (var es in eventSystems)
        {
            if (es == null)
                continue;

            var go = es.gameObject;
            var standalone = go.GetComponent<StandaloneInputModule>();
            var inputModule = go.GetComponent<InputSystemUIInputModule>();
            var swappedFromLegacy = false;

            if (standalone != null)
            {
                // Remove immediately so we never have two BaseInputModules (Destroy() would defer and break UI input).
                Object.DestroyImmediate(standalone);
                swappedFromLegacy = true;
                inputModule = go.GetComponent<InputSystemUIInputModule>();
                if (inputModule == null)
                    inputModule = go.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule != null)
                EnsureUiActions(inputModule, forceAssign: swappedFromLegacy);

            es.UpdateModules();
        }
    }

    private static void EnsureUiActions(InputSystemUIInputModule module, bool forceAssign)
    {
        if (module == null)
            return;

        if (forceAssign || module.actionsAsset == null || module.point == null || module.leftClick == null)
            module.AssignDefaultActions();
    }
}
#endif
