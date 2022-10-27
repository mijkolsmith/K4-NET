using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
	[SerializeField] private string lobbyName;
	[SerializeField] private ClientBehaviour client;
	[SerializeField] private ObjectReferences objectReferences;
	
	private void Start()
	{
		client = FindObjectOfType<ClientBehaviour>();
	}

	public void SetLobbyName(string lobbyName)
	{
		this.lobbyName = lobbyName;
	}

	public void JoinLobby()
	{
		objectReferences.lobbyName = lobbyName;

		JoinLobbyMessage joinLobbyMessage = new JoinLobbyMessage()
		{
			name = lobbyName,
		};

		client.SendPackedMessage(joinLobbyMessage);
	}
}