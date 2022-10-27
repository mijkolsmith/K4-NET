using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private GameGrid gameGrid;
    [SerializeField] private LayerMask gridLayer;
    [SerializeField] private ObjectReferences objectReferences;
    [SerializeField] public bool activePlayer = false;
    [SerializeField] private ClientBehaviour client;

    private void Start()
    {
        client = FindObjectOfType<ClientBehaviour>();
    }

    private void Update()
    {
        GridCell gridCell = getMouseOverCell();

        if (gridCell != null)
		{
            if (Input.GetMouseButtonDown(0) && activePlayer)
			{
                // Execute code depending on the state of the game

                // Place Item
                if (objectReferences.currentItem == 1 || objectReferences.currentItem == 3)
				{
                    Instantiate(objectReferences.itemPrefabs[objectReferences.currentItem], gridCell.transform.position, Quaternion.identity, null);
                }

                // Remove Item


                PlaceObstacleMessage placeObstacleMessage = new()
                {
                    x = (int)gridCell.transform.position.x,
                    y = (int)gridCell.transform.position.y
                };

                client.SendPackedMessage(placeObstacleMessage);
            }
		}
    }

    // Returns the GridCell the mouse is currently hovering over
    private GridCell getMouseOverCell()
	{
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, gridLayer))
		{
            return hit.transform.GetComponent<GridCell>();
		}
        return null;
	}
}