using UnityEngine;

public class ClientLobby : MonoBehaviour
{
	public string LobbyName { get; private set; }
	[SerializeField] private ClientBehaviour client;
	[SerializeField] private SceneObjectReferences objectReferences;
	
	private void Start()
	{
		client = FindObjectOfType<ClientBehaviour>();
	}

	public void SetLobbyName(string lobbyName)
	{
		LobbyName = lobbyName;
	}

	public void JoinLobby()
	{
		JoinLobbyMessage joinLobbyMessage = new()
		{
			name = LobbyName,
		};

		client.SendPackedMessage(joinLobbyMessage);
	}
}