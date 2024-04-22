using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Game Prefabs")]
public class GamePrefabsScriptableObject : SerializedScriptableObject
{
	public Dictionary<ItemType, ItemVisual> itemVisuals = new();
}