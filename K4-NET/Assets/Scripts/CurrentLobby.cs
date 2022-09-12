using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CurrentLobby : MonoBehaviour
{
	[SerializeField] private string lobbyName;
	[SerializeField] private ClientBehaviour client;
	public GameObject lobbyNameObject;
	public GameObject player1Name;
	public GameObject player2Name;
	public GameObject player1Score;
	public GameObject player2Score;
	public GameObject startLobbyObject;

	private void Update()
	{
		lobbyNameObject.GetComponent<TextMeshProUGUI>().text = lobbyName;
	}

	public void StartLobby()
	{
		StartGameMessage startGameMessage = new StartGameMessage()
		{
			name = lobbyName
		};

		client.SendPackedMessage(startGameMessage);
	}
}