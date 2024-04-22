using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private GameGrid gameGrid;
    [SerializeField] private LayerMask gridLayer;
    [SerializeField] private SceneObjectReferences objectReferences;
    [SerializeField] public bool activePlayer = false;
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
            if (Input.GetMouseButtonDown(0) && activePlayer)
			{
                selectedGridCell = gridCell;

                PlaceObstacleMessage placeObstacleMessage = new()
                {
                    name = objectReferences.lobbyName,
                    x = (uint)gridCell.GetPosition().x,
                    y = (uint)gridCell.GetPosition().y
				};

                client.SendPackedMessage(placeObstacleMessage);
            }
		}
    }

	public void PlaceItemAtSelectedGridCell()
	{
		selectedGridCell.objectInThisGridSpace = Instantiate(objectReferences.gamePrefabs.itemVisuals[objectReferences.currentItem].itemPrefab,
            new Vector3(
				selectedGridCell.transform.position.x + gameGrid.GridSpaceSize / 2,
				selectedGridCell.transform.position.y + gameGrid.GridSpaceSize / 2,
				selectedGridCell.transform.position.z),
		    Quaternion.identity,
			null);
	}
    
    public void RemoveItemAtSelectedGridCell()
	{
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