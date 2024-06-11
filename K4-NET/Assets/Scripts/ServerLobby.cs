using System.Collections.Generic;
using Unity.Networking.Transport;

public class ServerLobby
{
	public ServerLobby(List<NetworkConnection> connections)
	{
		//this.lobbyName = lobbyName;
		Connections = connections;
	}

	~ServerLobby()
	{
		Connections = null;
	}

	//public string lobbyName { get; private set; }
	public List<NetworkConnection> Connections { get; private set; }
	public uint activePlayerId;
	public List<ItemType> Items { get; private set; } = new();
	public ItemType currentItem;
	public ItemType[,] ItemGrid { get; private set; }
	public uint[] playerHealth { get; private set; }
	public bool[] playerHitMine { get; private set; }
	public PlayerFlag[,] playerGrid { get; private set; }
	public bool[] rematch { get; private set; }

	public void InitializeItemGrid()
	{
		ItemGrid = new ItemType[ServerBehaviour.gridsizeX, ServerBehaviour.gridsizeY];
	}

	public void InitializeItems()
	{
		Items.AddRange(ServerBehaviour.startingItems);
	}

	public void AddNewItemSet()
	{
		Items.AddRange(ServerBehaviour.itemSet);
	}

	public void InitializePlayerGrid()
	{
		playerGrid = new PlayerFlag[ServerBehaviour.gridsizeX, ServerBehaviour.gridsizeY];
	}

	public void InitializePlayerHealth()
	{
		playerHealth = new uint[ServerBehaviour.lobbySize] { GameData.defaultPlayerHealth, GameData.defaultPlayerHealth };
	}

	public void InitializePlayerHitMine()
	{
		playerHitMine = new bool[ServerBehaviour.lobbySize];
	}

	public void InitializeRematch()
	{
		rematch = new bool[ServerBehaviour.lobbySize];
	}

	public void ResetLobby()
	{
		Items = null;
		currentItem = ItemType.NONE;
		ItemGrid = null;
		playerHealth = null;
		playerGrid = null;
		rematch = null;
	}
}