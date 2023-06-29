using System.Collections.Generic;
using UnityEngine;

public class SceneObjectReferences : MonoBehaviour
{
    public GameObject loginRegister;
    public GameObject errorMessage;
    public GameObject joinLobby;
    public GameObject currentLobby;
    public MouseFollow cursor;
    public InputManager inputManager;
    public string lobbyName;

    public int currentItem;
    public GamePrefabsScriptableObject gamePrefabs;
    //0: no item
    //1: mine
    //2: minesweeper
    //3: wall
    //4: wrecking ball
    //5: ?
}