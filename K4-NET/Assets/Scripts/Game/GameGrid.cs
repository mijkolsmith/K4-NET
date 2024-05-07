using UnityEngine;

public class GameGrid : MonoBehaviour
{
    public int Width { get; private set; } = 8;
    public int Height { get; private set; } = 5;
    public float GridSpaceSize { get; private set; } = 5f;

    [SerializeField] private GameObject gridCellPrefab;
    private GameObject[,] gameGrid;

    void Start()
    {
        gameGrid = new GameObject[Width, Height];
        CreateGrid();
        transform.Rotate(-90, 0, 0);
    }

    // Creates a grid
    private void CreateGrid()
	{
        if (gridCellPrefab != null)
		{
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    gameGrid[x, y] = Instantiate(gridCellPrefab, new Vector3(x, 0, y) * GridSpaceSize, Quaternion.identity);
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
        int x = Mathf.FloorToInt(worldPosition.x / GridSpaceSize);
        int y = Mathf.FloorToInt(worldPosition.z / GridSpaceSize);

        x = Mathf.Clamp(x, 0, Width);
        y = Mathf.Clamp(y, 0, Height);

        return new Vector2Int(x, y);
	}

    // Get the worldPosition from the gridPosition
    public Vector3 GetWorldPosFromGridPos(Vector2Int gridPosition)
	{
        float x = gridPosition.x * GridSpaceSize;
        float y = gridPosition.y * GridSpaceSize;

        return new Vector3(x, 0, y);
	}

    // Get the grid cell at the given position
    public GridCell GetGridCellAtPosition(int x, int y)
    {
        return gameGrid[x, y].GetComponent<GridCell>();
    }
}