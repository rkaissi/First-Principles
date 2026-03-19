using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridRendererUI : Graphic
{
    public Vector2Int gridSize = new Vector2Int(2, 2);
    public float thickness = 2f;
    public float centerLineThickness = 6f;

    public Color centerLine  = new Color32(255, 255, 255, 100);
    public Color outsideLine = new Color32(255, 255, 255, 40);

    private float width;
    private float height;
    private float cellWidth;
    private float cellHeight;

    private LabelManager labelManager;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (gridSize.x % 2 == 1)
            gridSize.x += 1;

        if (gridSize.y % 2 == 1)
            gridSize.y += 1;
    }

    protected override void Reset()
    {
        base.Reset();

        if (labelManager == null)
            labelManager = FindAnyObjectByType<LabelManager>();
        labelManager.GenerateLabels();
        SetVerticesDirty();
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        width = rectTransform.rect.width;
        height = rectTransform.rect.height;

        cellWidth = width / (float)gridSize.x;
        cellHeight = height / (float)gridSize.y;

        int count = 0;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                DrawCell(x, y, count, vh);
                count++;
            }
        }

        DrawCenterLine(count, vh);
    }

    private void DrawCell(int x, int y, int index, VertexHelper vh)
    {
        float xPos = cellWidth * x;
        float yPos = cellHeight * y;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = outsideLine;

        vertex.position = new Vector3(xPos, yPos);
        vh.AddVert(vertex);

        vertex.position = new Vector3(xPos, yPos + cellHeight);
        vh.AddVert(vertex);

        vertex.position = new Vector3(xPos + cellWidth, yPos + cellHeight);
        vh.AddVert(vertex);

        vertex.position = new Vector3(xPos + cellWidth, yPos);
        vh.AddVert(vertex);

        float widthSqr = Mathf.Pow(thickness, 2);
        float distanceSqr = widthSqr / 2;
        float distance = Mathf.Sqrt(distanceSqr);

        vertex.position = new Vector3(xPos + distance, yPos + distance);
        vh.AddVert(vertex);

        vertex.position = new Vector3(xPos + distance, yPos + (cellHeight - distance));
        vh.AddVert(vertex);

        vertex.position = new Vector3(xPos + (cellWidth - distance), yPos + (cellHeight - distance));
        vh.AddVert(vertex);

        vertex.position = new Vector3(xPos + (cellWidth - distance), yPos + distance);
        vh.AddVert(vertex);

        int offset = index * 8;

        //Left Edge
        vh.AddTriangle(offset + 0, offset + 1, offset + 5);
        vh.AddTriangle(offset + 5, offset + 4, offset + 0);

        //Top Edge
        vh.AddTriangle(offset + 1, offset + 2, offset + 6);
        vh.AddTriangle(offset + 6, offset + 5, offset + 1);

        //Right Edge
        vh.AddTriangle(offset + 2, offset + 3, offset + 7);
        vh.AddTriangle(offset + 7, offset + 6, offset + 2);

        //Bottom Edge
        vh.AddTriangle(offset + 3, offset + 0, offset + 4);
        vh.AddTriangle(offset + 4, offset + 7, offset + 3);
    }

    private void DrawCenterLine(int index, VertexHelper vh)
    {
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        float centerLineOffset = centerLineThickness / 2;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = centerLine;

        //Horizontal Line
        vertex.position = new Vector3(0, (height / 2) - centerLineOffset);
        vh.AddVert(vertex);

        vertex.position = new Vector3(0, (height / 2) + centerLineOffset);
        vh.AddVert(vertex);

        vertex.position = new Vector3(width, (height / 2) + centerLineOffset);
        vh.AddVert(vertex);

        vertex.position = new Vector3(width, (height / 2) - centerLineOffset);
        vh.AddVert(vertex);

        int offset = index * 8;

        vh.AddTriangle(offset + 0, offset + 1, offset + 2);
        vh.AddTriangle(offset + 2, offset + 3, offset + 0);

        //Vertical Line
        vertex.position = new Vector3((width / 2) - centerLineOffset, 0);
        vh.AddVert(vertex);

        vertex.position = new Vector3((width / 2) - centerLineOffset, height);
        vh.AddVert(vertex);

        vertex.position = new Vector3((width / 2) + centerLineOffset, height);
        vh.AddVert(vertex);

        vertex.position = new Vector3((width / 2) + centerLineOffset, 0);
        vh.AddVert(vertex);

        vh.AddTriangle(offset + 4, offset + 5, offset + 6);
        vh.AddTriangle(offset + 6, offset + 7, offset + 4);
    }
}