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
	public ItemType[,] ItemGrid { get; private set; } = null;
	public uint[] PlayerHealth { get; private set; }
	public bool[] PlayerHitMine { get; private set; }
	public PlayerFlag[,] PlayerGrid { get; private set; }
	public bool[] Rematch { get; private set; }

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
		PlayerGrid = new PlayerFlag[ServerBehaviour.gridsizeX, ServerBehaviour.gridsizeY];
	}

	public void InitializePlayerHealth()
	{
		PlayerHealth = new uint[ServerBehaviour.lobbySize] { GameData.defaultPlayerHealth, GameData.defaultPlayerHealth };
	}

	public void InitializePlayerHitMine()
	{
		PlayerHitMine = new bool[ServerBehaviour.lobbySize];
	}

	public void InitializeRematch()
	{
		Rematch = new bool[ServerBehaviour.lobbySize];
	}

	public void ResetLobby()
	{
		Items = null;
		currentItem = ItemType.NONE;
		ItemGrid = null;
		PlayerHealth = null;
		PlayerGrid = null;
		Rematch = null;
	}
}