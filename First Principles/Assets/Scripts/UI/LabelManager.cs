using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public class LabelManager : MonoBehaviour
{
    [SerializeField] private GameObject labelPrefab;

    //Label placement every how many grid increments
    [SerializeField] private int horizontalIncrement = 1;
    [SerializeField] private int verticalIncrement = 1;

    //Show the 0 label for the x axis
    [SerializeField] private bool xAxisOriginLabel = true;

    [SerializeField] private Vector2 xStartPos = new Vector2(0, 0);
    [SerializeField] private Vector2 yStartPos = new Vector2(0, 0);

    private List<TextMeshProUGUI> xLabels = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> yLabels = new List<TextMeshProUGUI>();

    private GridRendererUI gridRenderer;

    private void Awake()
    {
        gridRenderer = FindFirstObjectByType<GridRendererUI>();
    }

    private void Start()
    {
        GenerateLabels();
    }

    public void GenerateLabels()
    {
        DeleteCurrentLabels();

        Vector2 xPos = xStartPos;
        Vector2 yPos = yStartPos;

        float xPosOffset = gridRenderer.rectTransform.rect.width / gridRenderer.gridSize.x;
        float yPosOffset = gridRenderer.rectTransform.rect.height / gridRenderer.gridSize.y;

        int xSize = gridRenderer.gridSize.x;
        int ySize = gridRenderer.gridSize.y;

        int xPositive = xSize / (2 * horizontalIncrement) + 1;
        int xNegative = (xPositive * 2) - 1;

        int yPositive = ySize / (2 * verticalIncrement) + 1;
        int yNegative = (yPositive * 2) - 1;

        //Horizontal Labels

        for (int i = 0; i < xPositive; i++)
        {
            if (i == 0 && !xAxisOriginLabel)
            {
                xLabels.Add(null);
                xPos.x += xPosOffset * horizontalIncrement;
                continue;
            }

            xLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(xPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            string labelTxt = (i * horizontalIncrement).ToString();
            xLabels[i].text = labelTxt;
            xPos.x += xPosOffset * horizontalIncrement;
        }

        xPos = xStartPos;
        xPos.x -= xPosOffset * horizontalIncrement;

        for (int i = xPositive; i < xNegative; i++)
        {
            xLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(xPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            string labelTxt = (-(i - (xPositive - 1)) * horizontalIncrement).ToString();//
            xLabels[i].text = labelTxt;
            xPos.x -= xPosOffset * horizontalIncrement;
        }

        //Vertical Labels
        for (int i = 0; i < yPositive; i++)
        {
            yLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(yPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            string labelTxt = (i * verticalIncrement).ToString();
            yLabels[i].text = labelTxt;
            yPos.y += yPosOffset * verticalIncrement;
        }

        yPos = yStartPos;
        yPos.y -= yPosOffset * verticalIncrement;

        for (int i = yPositive; i < yNegative; i++)
        {
            yLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(yPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            string labelTxt = (-(i - (yPositive - 1)) * verticalIncrement).ToString();
            yLabels[i].text = labelTxt;
            yPos.y -= yPosOffset * verticalIncrement;
        }
    }

    private void DeleteCurrentLabels()
    {
        xLabels.Clear();
        yLabels.Clear();

        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
