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

    public ItemType currentItem = ItemType.NONE;
    public GamePrefabsScriptableObject gamePrefabs;
}