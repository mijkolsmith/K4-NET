using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectReferences : MonoBehaviour
{
    public GameObject loginRegister;
    public GameObject errorMessage;
    public GameObject joinLobby;
    public GameObject currentLobby;
    public string lobbyName;

    public List<Item> items = new List<Item>();
    public List<Image> sprites = new List<Image>();
    //0: no item
    //1: mine
    //2: minesweeper
    //3: wall
    //4: wrecking ball
    //5: ?
}