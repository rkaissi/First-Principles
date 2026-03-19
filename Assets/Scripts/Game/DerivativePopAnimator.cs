using System.Collections;
using UnityEngine;

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
        float startT = 0f;

        baseThickness = target.thickness;
        baseColor = target.color;

        // Make sure popColor is used (keeping the derivative theme).
        Color c = popColor;
        if (c.a <= 0f)
            c.a = 1f;
        target.color = c;

        float startThickness = baseThickness;
        float endThickness = baseThickness * thicknessMultiplier;

        // Quick "pop up"
        while (startT < popDurationSeconds * 0.5f)
        {
            startT += Time.deltaTime;
            float t = Mathf.Clamp01(startT / (popDurationSeconds * 0.5f));
            target.thickness = Mathf.Lerp(startThickness, endThickness, t);
            yield return null;
        }

        // Slight settle.
        float settleT = 0f;
        float fromThickness = target.thickness;
        while (settleT < popDurationSeconds * 0.5f)
        {
            settleT += Time.deltaTime;
            float t = Mathf.Clamp01(settleT / (popDurationSeconds * 0.5f));
            target.thickness = Mathf.Lerp(fromThickness, startThickness, t);
            yield return null;
        }

        // Restore derivative alpha to base (while keeping the selected pop color's RGB).
        var restored = target.color;
        restored.a = baseColor.a;
        target.color = restored;
    }
}

