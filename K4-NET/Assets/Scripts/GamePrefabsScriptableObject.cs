using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Game Prefabs")]
public class GamePrefabsScriptableObject : ScriptableObject
{
    public List<GameObject> itemPrefabs = new();
    public List<Sprite> itemSprites = new();
    public List<Sprite> cursorSprites = new();
}