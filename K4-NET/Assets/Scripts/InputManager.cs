using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] GameGrid gameGrid;
    [SerializeField] LayerMask gridLayer;

    private void Update()
    {
        GridCell gridCell = getMouseOverCell();

        if (gridCell != null)
		{
            if (Input.GetMouseButtonDown(0))
			{
                // Execute code depending on the state of the game
                gridCell.GetComponentInChildren<SpriteRenderer>().material.color = Color.green;
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