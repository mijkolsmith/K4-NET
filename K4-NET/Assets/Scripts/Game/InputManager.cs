using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] GameGrid gameGrid;
    [SerializeField] LayerMask gridLayer;
    [SerializeField] ObjectReferences objectReferences;
    public bool activePlayer = false;

    private void Update()
    {
        GridCell gridCell = getMouseOverCell();

        if (gridCell != null)
		{
            if (Input.GetMouseButtonDown(0) && activePlayer)
			{
                // Execute code depending on the state of the game


                Instantiate(objectReferences.currentItem, gridCell.transform.position, Quaternion.identity, null);
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