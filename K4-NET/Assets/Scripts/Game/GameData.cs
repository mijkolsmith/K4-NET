using TMPro;
using UnityEngine;

public class GameData : MonoBehaviour
{
	public ClientBehaviour client;
	[SerializeField] private GameObject livesObject;
	[SerializeField] private GameObject otherLivesObject;
	public const uint defaultPlayerHealth = 3;

	private uint lives;
	public uint Lives
	{
		get => lives;
		private set
		{
			lives = value;
			livesObject.GetComponent<TextMeshProUGUI>().text = "HP: " + value;
		}
	}
	
	private uint otherLives;
	public uint OtherLives 
	{
		get => otherLives;
		private set
		{
			otherLives = value;
			otherLivesObject.GetComponent<TextMeshProUGUI>().text = "Enemy HP: " + value;
		}
	}

	private void Start()
	{
		client = FindObjectOfType<ClientBehaviour>();
	}

	public void DecreaseLives()
    {
		Lives--;
	}

	public void DecreaseOtherLives()
	{
		OtherLives--;
	}

	public void StartRound()
    {
		Lives = defaultPlayerHealth;
		OtherLives = defaultPlayerHealth;
    }

	public void Rematch(bool choice)
	{
		ContinueChoiceMessage continueChoiceMessage = new()
		{
			name = client.LobbyName,
			choice = choice
		};

		client.SendPackedMessage(continueChoiceMessage);
	}
}
