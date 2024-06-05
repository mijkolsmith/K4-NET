using TMPro;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public bool checkInput = false;
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
        if (!checkInput)
            return;

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

    public void MovePlayerToSelectedGridCell(PlayerFlag playerFlagToMove)
    {
        if (playerFlagToMove == PlayerFlag.NONE)
			return;

        // Only true when spawning players
        if (playerFlagToMove == PlayerFlag.BOTH)
        {
            // Spawn players at selected grid cell
            selectedGridCell.AddPlayerFlag(playerFlagToMove, objectReferences.gamePrefabs.playerVisuals);
            gameGrid.playerLocations[0] = selectedGridCell;
			gameGrid.playerLocations[1] = selectedGridCell;
			return;
        }

        // Move correct player to selected grid cell
        if (playerFlagToMove == PlayerFlag.PLAYER1)
        {
			gameGrid.playerLocations[0].RemovePlayerFlag(
                playerFlagToMove, 
                objectReferences.gamePrefabs.playerVisuals);

			selectedGridCell.AddPlayerFlag(
                playerFlagToMove, 
                objectReferences.gamePrefabs.playerVisuals);

			gameGrid.playerLocations[0] = selectedGridCell;
		}
        else if (playerFlagToMove == PlayerFlag.PLAYER2)
        {
			gameGrid.playerLocations[1].RemovePlayerFlag(
                playerFlagToMove, 
                objectReferences.gamePrefabs.playerVisuals);

			selectedGridCell.AddPlayerFlag(
                playerFlagToMove,
				objectReferences.gamePrefabs.playerVisuals);

			gameGrid.playerLocations[1] = selectedGridCell;
		}
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
        selectedGridCell.SetItem(item, objectReferences.gamePrefabs.itemVisuals[item].itemPrefab);
	}
    
    // Selects the grid cell at the given position
    public void SelectGridCell(int x, int y)
    {
		selectedGridCell = gameGrid.GetGridCellAtPosition(x,y);
	}

    // Removes the item at the selected grid cell
    public void RemoveItemAtSelectedGridCell()
	{
        selectedGridCell.RemoveItem();
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