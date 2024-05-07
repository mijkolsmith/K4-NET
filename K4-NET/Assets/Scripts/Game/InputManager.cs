using TMPro;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private GameGrid gameGrid;
    [SerializeField] private LayerMask gridLayer;
    [SerializeField] private SceneObjectReferences objectReferences;
    [SerializeField] private ClientBehaviour client;
    [SerializeField] private GridCell selectedGridCell;

    private void Start()
    {
        client = FindObjectOfType<ClientBehaviour>();
    }

    private void Update()
    {
        GridCell gridCell = GetMouseOverCell();

        if (gridCell != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (client.ActivePlayer)
                {
					selectedGridCell = gridCell;
					if (client.RoundStarted)
                    {
                        PlayerMoveMessage playerMoveMessage = new()
                        {
							name = client.LobbyName,
							x = (uint)gridCell.GetPosition().x,
							y = (uint)gridCell.GetPosition().y
						};

                        client.SendPackedMessage(playerMoveMessage);
                    }
                    else
                    {
                        PlaceObstacleMessage placeObstacleMessage = new()
                        {
                            name = client.LobbyName,
                            x = (uint)gridCell.GetPosition().x,
                            y = (uint)gridCell.GetPosition().y
                        };

                        client.SendPackedMessage(placeObstacleMessage);
                    }
                }
                else objectReferences.errorMessage.GetComponent<TextMeshProUGUI>().text = "It's not your turn!";
            }
        }
    }

    public void MovePlayerToSelectedGridCell()
    {

    }

    // Places an item at the selected grid cell if it is empty
    public void PlaceItemAtSelectedGridCell(ItemType item)
    {
        // Don't place any items if there already is an item in the selected grid cell
        if (selectedGridCell.itemType != ItemType.NONE && selectedGridCell.itemType != ItemType.FLAG)
            return;

        // Always overwrite a flag item
        if (selectedGridCell.itemType == ItemType.FLAG)
		    RemoveItemAtSelectedGridCell();

        // Place the item at the selected grid cell
		selectedGridCell.itemType = item;
		selectedGridCell.objectInThisGridSpace = Instantiate(objectReferences.gamePrefabs.itemVisuals[item].itemPrefab,
            new Vector3(
				selectedGridCell.transform.position.x + gameGrid.GridSpaceSize / 2,
				selectedGridCell.transform.position.y + gameGrid.GridSpaceSize / 2,
				selectedGridCell.transform.position.z),
		    Quaternion.identity,
			null);
	}
    
    // Selects the grid cell at the given position
    public void SelectGridCell(int x, int y)
    {
		selectedGridCell = gameGrid.GetGridCellAtPosition(x,y);
	}

    // Removes the item at the selected grid cell
    public void RemoveItemAtSelectedGridCell()
	{
        selectedGridCell.itemType = ItemType.NONE;
        Destroy(selectedGridCell.objectInThisGridSpace);
        selectedGridCell.objectInThisGridSpace = null;
	}

	// Returns the GridCell the mouse is currently hovering over
	private GridCell GetMouseOverCell()
	{
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, gridLayer))
		{
            return hit.transform.GetComponent<GridCell>();
		}
        return null;
	}
}