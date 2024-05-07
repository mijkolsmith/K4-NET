using UnityEngine;

public class GridCell : MonoBehaviour
{
    private int posX = 0;
    private int posY = 0;

    public GameObject objectInThisGridSpace = null;
    public ItemType itemType = ItemType.NONE;

    private bool set = false;
    public bool occupied = false;

    public void SetPosition(int x, int y)
	{
        if (!set)
        {
            posX = x;
            posY = y;
            set = true;
        }
        else Debug.Log("Position already set");
	}

    public Vector2Int GetPosition()
	{
        return new Vector2Int(posX, posY);
	}
}