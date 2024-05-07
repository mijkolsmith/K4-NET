using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public abstract ItemType ItemType { get; }

	//0: no item
	//1: mine
	//2: wall
	//3: minesweeper
	//4: wrecking ball
	//5: ?
}