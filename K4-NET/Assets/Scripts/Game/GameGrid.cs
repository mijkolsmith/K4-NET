using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGrid : MonoBehaviour
{
    private int width = 8;
    private int height = 5;
    private float gridSpaceSize = 5;

    [SerializeField] private GameObject gridCellPrefab;
    private GameObject[,] gameGrid;

    void Start()
    {
        gameGrid = new GameObject[width, height];
        CreateGrid();
    }

    // Creates a grid
    private void CreateGrid()
	{
        if (gridCellPrefab != null)
		{
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gameGrid[x, y] = Instantiate(gridCellPrefab, new Vector3(x, 0, y) * gridSpaceSize, Quaternion.identity);
                    gameGrid[x, y].GetComponent<GridCell>().SetPosition(x, y);
                    gameGrid[x, y].transform.parent = transform;
                    gameGrid[x, y].transform.name = x + ", " + y;
                }
            }
        }
	}

    // Get the gridPosition from the worldPosition
    public Vector2Int GetGridPosFromWorldPos(Vector3 worldPosition)
	{
        int x = Mathf.FloorToInt(worldPosition.x / gridSpaceSize);
        int y = Mathf.FloorToInt(worldPosition.z / gridSpaceSize);

        x = Mathf.Clamp(x, 0, width);
        y = Mathf.Clamp(y, 0, height);

        return new Vector2Int(x, y);
	}

    // Get the worldPosition from the gridPosition
    public Vector3 GetWorldPosFromGridPos(Vector2Int gridPosition)
	{
        float x = gridPosition.x * gridSpaceSize;
        float y = gridPosition.y * gridSpaceSize;

        return new Vector3(x, 0, y);
	}
}