using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    private int posX = 0;
    private int posY = 0;

    public GameObject itemInThisGridSpace = null;
    public GameObject playerInThisGridSpace = null;
    public ItemType itemType = ItemType.NONE;
    public PlayerFlag playerFlag = PlayerFlag.NONE;

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

    public void SetItem(ItemType itemType, GameObject itemPrefab)
    {
		this.itemType = itemType;
		itemInThisGridSpace = Instantiate(
            itemPrefab,
            new Vector3(
                transform.position.x + GameGrid.GridSpaceSize * .5f,
                transform.position.y + GameGrid.GridSpaceSize * .5f,
                transform.position.z),
            Quaternion.identity,
            null);
	}

    public void SetPlayer(PlayerFlag playerFlag, GameObject playerPrefab)
    {
		this.playerFlag |= playerFlag;
		playerInThisGridSpace = Instantiate(
            playerPrefab,
            new Vector3(
                transform.position.x + GameGrid.GridSpaceSize * .5f,
                transform.position.y + GameGrid.GridSpaceSize * .75f,
                transform.position.z - 1),
            Quaternion.identity,
            null);
	}

    public void RemoveItem()
    {
		itemType = ItemType.NONE;
		Destroy(itemInThisGridSpace);
		itemInThisGridSpace = null;
    }

    public void RemovePlayer(PlayerFlag playerFlag, Dictionary<PlayerFlag, PlayerVisual> playerVisuals)
    {
        if (playerFlag == PlayerFlag.NONE)
			return;

        this.playerFlag &= ~playerFlag;
		Destroy(playerInThisGridSpace);

		if (this.playerFlag == PlayerFlag.NONE)
        {
			playerInThisGridSpace = null;
            return;
		}

        SetPlayer(playerFlag, playerVisuals[playerFlag].playerPrefab);
	}
}