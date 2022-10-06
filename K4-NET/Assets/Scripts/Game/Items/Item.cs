using System.Collections.Generic;
using UnityEngine;

public enum Items
{
    MINE,
    MINESWEEPER,
    WALL,
    WRECKINGBALL
}

public class Item : MonoBehaviour
{
    protected int gridPosition;

    //0: no item
    //1: mine
    //2: minesweeper
    //3: wall
    //4: wrecking ball
    //5: ?
}