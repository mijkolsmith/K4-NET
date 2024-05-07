using TMPro;
using UnityEngine;

public class GameData : MonoBehaviour
{
    [SerializeField] private GameObject livesObject;
	[SerializeField] private GameObject otherLivesObject;
	public const int defaultPlayerHealth = 3;

	private int lives;
	public int Lives
	{
		get => lives;
		private set
		{
			lives = value;
			livesObject.GetComponent<TextMeshProUGUI>().text = "Enemy HP: " + value;
		}
	}
	
	private int otherLives;
	public int OtherLives 
	{
		get => otherLives;
		private set
		{
			otherLives = value;
			otherLivesObject.GetComponent<TextMeshProUGUI>().text = "Enemy HP: " + value;
		}
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
}
