using System.Collections;
using UnityEngine;

/// <summary>
/// Briefly scales derivative line thickness and tint for a "pop" when the player crosses stage thresholds.
/// </summary>
public class DerivativePopAnimator : MonoBehaviour
{
    private DerivRendererUI target;
    private Coroutine popRoutine;

    [SerializeField] private float thicknessMultiplier = 1.8f;
    [SerializeField] private float popDurationSeconds = 0.25f;

    private float baseThickness;
    private Color baseColor;

    public void SetTarget(DerivRendererUI target)
    {
        this.target = target;
        if (this.target != null)
        {
            baseThickness = this.target.thickness;
            baseColor = this.target.color;
        }
    }

    public void Pop(Color popColor)
    {
        if (target == null)
            return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopRoutine(popColor));
    }

    private IEnumerator PopRoutine(Color popColor)
    {
        float elapsed = 0f;

        baseThickness = target.thickness;
        baseColor = target.color;

        Color c = popColor;
        if (c.a <= 0f)
            c.a = 1f;
        target.color = c;

        float startThickness = baseThickness;
        float endThickness = baseThickness * thicknessMultiplier;

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
            target.thickness = Mathf.Lerp(fromThickness, startThickness, t);
            yield return null;
        }

        var restored = target.color;
        restored.a = baseColor.a;
        target.color = restored;
    }
}
