using UnityEngine;

public class SceneObjectReferences : MonoBehaviour
{
	[field: SerializeField] public GameObject LoginRegister { get; private set; }
	[field: SerializeField] public GameObject ErrorMessage { get; private set; }
	[field: SerializeField] public GameObject JoinLobby { get; private set; }
	[field: SerializeField] public GameObject CurrentLobby { get; private set; }
	[field: SerializeField] public GameObject EndGame { get; private set; }
	[field: SerializeField] public MouseFollow Cursor { get; private set; }
	[field: SerializeField] public InputManager InputManager { get; private set; }
	[field: SerializeField] public GamePrefabsScriptableObject GamePrefabs { get; private set; }
	[field: SerializeField] public GameData GameData { get; private set; }
}