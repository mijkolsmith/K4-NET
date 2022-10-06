using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectReferences : MonoBehaviour
{
    public GameObject loginRegister;
    public GameObject errorMessage;
    public GameObject joinLobby;
    public GameObject currentLobby;
    public MouseFollow cursor;
    public InputManager inputManager;
    public string lobbyName;

    public Item currentItem;
    public Dictionary<int, System.Type> items = new Dictionary<int, System.Type>()
    {
        { 0, null },
        { 1, typeof(Mine) },
        { 2, typeof(MineSweeper) },
        { 3, typeof(Wall) },
        { 4, typeof(WreckingBall) }
    };
    public List<Sprite> itemSprites = new List<Sprite>();
    public List<Sprite> cursorSprites = new List<Sprite>();
    //0: no item
    //1: mine
    //2: minesweeper
    //3: wall
    //4: wrecking ball
    //5: ?
}