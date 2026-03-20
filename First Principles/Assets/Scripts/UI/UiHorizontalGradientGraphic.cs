using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple horizontal vertex-color gradient for uGUI (left → right). Used for the level exit strip.
/// </summary>
public class UiHorizontalGradientGraphic : Graphic
{
    public Color colorLeft = new Color(0.6f, 0.88f, 1f, 0f);
    public Color colorRight = new Color(0.35f, 0.72f, 0.98f, 0.5f);

    public void SetGradientColors(Color left, Color right)
    {
        colorLeft = left;
        colorRight = right;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float x0 = r.xMin;
        float x1 = r.xMax;
        float y0 = r.yMin;
        float y1 = r.yMax;

        UIVertex v = UIVertex.simpleVert;

        v.position = new Vector3(x0, y0);
        v.color = colorLeft;
        vh.AddVert(v);

        v.position = new Vector3(x0, y1);
        v.color = colorLeft;
        vh.AddVert(v);

        v.position = new Vector3(x1, y1);
        v.color = colorRight;
        vh.AddVert(v);

        v.position = new Vector3(x1, y0);
        v.color = colorRight;
        vh.AddVert(v);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
