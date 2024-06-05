using UnityEngine;
using TMPro;

public class ClientCurrentLobby : MonoBehaviour
{
	public ClientBehaviour client;
	[SerializeField] private SceneObjectReferences objectReferences;
	public GameObject lobbyNameObject;
	public GameObject player1Name;
	public GameObject player2Name;
	public GameObject player1Score;
	public GameObject player2Score;
	public GameObject startLobbyObject;
	public GameObject leaveLobbyObject;

	private void Start()
	{
		client = FindObjectOfType<ClientBehaviour>();
	}

	private void Update()
	{
		lobbyNameObject.GetComponent<TextMeshProUGUI>().text = client.LobbyName;
	}

	public void StartLobby()
	{
		StartGameMessage startGameMessage = new()
		{
			name = client.LobbyName
		};

		client.SendPackedMessage(startGameMessage);
	}

	public void LeaveLobby()
	{
		LeaveLobbyMessage leaveLobbyMessage = new()
		{
			name = client.LobbyName
		};

		client.SendPackedMessage(leaveLobbyMessage);

		client.LeaveLobby();
	}
}