using System.Collections.Generic;
using UnityEngine;
using System;
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

    public int currentItem;
    public List<GameObject> itemPrefabs = new();
    public List<Sprite> itemSprites = new();
    public List<Sprite> cursorSprites = new();
    //0: no item
    //1: mine
    //2: minesweeper
    //3: wall
    //4: wrecking ball
    //5: ?
}