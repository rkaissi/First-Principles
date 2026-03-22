using System.Collections;
using UnityEngine;

/// <summary>
/// Briefly scales <see cref="DerivRendererUI"/> thickness and swaps tint when the player
/// crosses horizontal stage thresholds (see <c>LevelManager.stageTriggerXGrid</c>).
/// Coroutine-based; starting a new <see cref="Pop"/> stops the previous animation.
/// </summary>
public class DerivativePopAnimator : MonoBehaviour
{
    private DerivRendererUI target;
    private Coroutine popRoutine;

    [SerializeField] private float thicknessMultiplier = 1.8f;
    [SerializeField] private float popDurationSeconds = 0.25f;

    /// <summary>
    /// Baseline stroke width for f′ (not the momentary boosted value). Stopping a pop mid-animation used to leave
    /// <see cref="DerivRendererUI.thickness"/> elevated; the next pop then multiplied that again — runaway “thick line”.
    /// </summary>
    private float _restThickness = DerivRendererUI.DefaultThicknessPixels;

    public void SetTarget(DerivRendererUI target)
    {
        this.target = target;
        SyncRestThicknessFromTarget();
    }

    /// <summary>Call after resetting <see cref="DerivRendererUI.thickness"/> on level load / theme apply.</summary>
    public void SyncRestThicknessFromTarget()
    {
        if (target != null)
            _restThickness = Mathf.Max(0.25f, target.thickness);
    }

    public void Pop(Color popColor)
    {
        if (target == null)
            return;

        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
            // Interrupted mid-pulse — snap back so the next pop does not compound thickness.
            target.thickness = _restThickness;
        }

        popRoutine = StartCoroutine(PopRoutine(popColor));
    }

    private IEnumerator PopRoutine(Color popColor)
    {
        float elapsed = 0f;

        Color colorBeforePop = target.color;

        Color c = popColor;
        if (c.a <= 0f)
            c.a = 1f;
        target.color = c;

        float restT = _restThickness;
        float startThickness = restT;
        float endThickness = restT * thicknessMultiplier;

        while (elapsed < popDurationSeconds * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (popDurationSeconds * 0.5f));
            target.thickness = Mathf.Lerp(startThickness, endThickness, t);
            yield return null;
        }

        float settleT = 0f;
        float fromThickness = target.thickness;
        while (settleT < popDurationSeconds * 0.5f)
        {
            settleT += Time.deltaTime;
            float t = Mathf.Clamp01(settleT / (popDurationSeconds * 0.5f));
            target.thickness = Mathf.Lerp(fromThickness, restT, t);
            yield return null;
        }

        target.thickness = restT;

        var restored = target.color;
        restored.a = colorBeforePop.a;
        target.color = restored;

        popRoutine = null;
    }
}
