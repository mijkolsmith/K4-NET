using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CurrentLobby : MonoBehaviour
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
		lobbyNameObject.GetComponent<TextMeshProUGUI>().text = objectReferences.lobbyName;
	}

	public void StartLobby()
	{
		StartGameMessage startGameMessage = new()
		{
			name = objectReferences.lobbyName
		};

		client.SendPackedMessage(startGameMessage);
	}

	public void LeaveLobby()
	{
		LeaveLobbyMessage leaveLobbyMessage = new()
		{
			name = objectReferences.lobbyName
		};

		client.SendPackedMessage(leaveLobbyMessage);

		client.LeaveLobby();
	}
}