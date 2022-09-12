using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoginRegister : MonoBehaviour
{
	[SerializeField] private string username;
	[SerializeField] private string password;
	[SerializeField] private ClientBehaviour client;

	public void SetUsername(string username)
	{
		this.username = username;
	}
	
	public void SetPassword(string password)
	{
		this.password = password;
	}

    public void RegisterUser()
	{
		RegisterMessage registerMessage = new RegisterMessage()
		{
			username = username,
			password = password
		};

		client.username = username;
		client.SendPackedMessage(registerMessage);
	}
	
	public void LoginUser()
	{
		LoginMessage loginMessage = new LoginMessage()
		{
			username = username,
			password = password
		};

		client.username = username;
		client.SendPackedMessage(loginMessage);
	}
}