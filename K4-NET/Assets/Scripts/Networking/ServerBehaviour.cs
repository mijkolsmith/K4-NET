using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Networking.Transport.Utilities;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;
using Cysharp.Threading.Tasks;
using System.Linq;
using System.Collections.Immutable;

public delegate void ServerMessageHandler(ServerBehaviour server, NetworkConnection con, MessageHeader header);

public class PingPong
{
	public float lastSendTime = 0;
	public int status = -1;
	public string name = ""; // because of weird issues...
}

public class Error
{
	public string result;
}

public class Id
{
	public string id;
}

public class User
{
	public string id;
	public string username;
}

public class UserScore
{
	public string username;
	public string score;
}	

public class ServerBehaviour : MonoBehaviour
{
	static Dictionary<NetworkMessageType, ServerMessageHandler> networkMessageHandlers = new()
	{
		{ NetworkMessageType.PONG, HandlePong },
		{ NetworkMessageType.HANDSHAKE, HandleHandshake },
		{ NetworkMessageType.REGISTER, HandleRegister },
		{ NetworkMessageType.LOGIN, HandleLogin },
		{ NetworkMessageType.JOIN_LOBBY, HandleJoinLobby },
		{ NetworkMessageType.LEAVE_LOBBY, HandleLeaveLobby },
		{ NetworkMessageType.START_GAME, HandleStartGame },
		{ NetworkMessageType.PLACE_OBSTACLE, HandlePlaceObstacle },
		{ NetworkMessageType.PLAYER_MOVE, HandlePlayerMove },
		{ NetworkMessageType.CONTINUE_CHOICE, HandleContinueChoice },
	};

	public NetworkDriver m_Driver;
	public NetworkPipeline m_Pipeline;
	private NativeList<NetworkConnection> m_Connections;

	public const int lobbySize = 2; // default 2
	public const int gridsizeX = 8; // default 8
	public const int gridsizeY = 5; // default 5
	private const int itemLimit = 15; // default 15

	public static ImmutableArray<ItemType> startingItems = ImmutableArray.Create
	(
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE
	);
	
	public static ImmutableArray<ItemType> itemSet = ImmutableArray.Create
	(
		ItemType.MINE,
		ItemType.MINE,
		ItemType.WALL,
		ItemType.WALL,
		ItemType.WALL,
		ItemType.WALL,
		ItemType.MINESWEEPER,
		ItemType.MINESWEEPER,
		ItemType.WRECKINGBALL,
		ItemType.WRECKINGBALL
	);

	private const string phpBaseUrl = "https://studenthome.hku.nl/~michael.smith/K4/";

	private Dictionary<NetworkConnection, int> idList = new();
	private Dictionary<int, string> nameList = new();
	private Dictionary<NetworkConnection, PingPong> pongDict = new();
	private Dictionary<string, ServerLobby> lobbyList = new();

	private string PhpConnectionID;

	private void Start()
	{
		// Create Driver
		m_Driver = NetworkDriver.Create(new ReliableUtility.Parameters { WindowSize = 32 });
		m_Pipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

		// Open listener on server port
		NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
		endpoint.Port = 1511;
		if (m_Driver.Bind(endpoint) != 0)
			Debug.Log("Failed to bind to port 1511");
		else
			m_Driver.Listen();

		m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
	}

	// Write this immediately after creating the above Start calls, so you don't forget
	// Or else you well get lingering thread sockets, and will have trouble starting new ones!
	private void OnDestroy()
	{
		m_Driver.Dispose();
		m_Connections.Dispose();
	}

	private void Update()
	{
		// This is a jobified system, so we need to tell it to handle all its outstanding tasks first
		m_Driver.ScheduleUpdate().Complete();

		// Clean up connections, remove stale ones
		for (int i = 0; i < m_Connections.Length; i++)
		{
			if (!m_Connections[i].IsCreated)
			{
				if (idList.ContainsKey(m_Connections[i]))
				{
					nameList.Remove(idList[m_Connections[i]]);
					idList.Remove(m_Connections[i]);
				}

				m_Connections.RemoveAtSwapBack(i);
				// This little trick means we can alter the contents of the list without breaking/skipping instances
				--i;
			}
		}

		// Accept new connections
		NetworkConnection c;
		while ((c = m_Driver.Accept()) != default)
		{
			m_Connections.Add(c);
			Debug.Log("Accepted a connection");
		}

		for (int i = 0; i < m_Connections.Length; i++)
		{
			if (!m_Connections[i].IsCreated)
				continue;

			// Loop through available events
			NetworkEvent.Type cmd;
			while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out DataStreamReader stream)) != NetworkEvent.Type.Empty)
			{
				if (cmd == NetworkEvent.Type.Data)
				{
					// First UShort is always message type
					NetworkMessageType msgType = (NetworkMessageType)stream.ReadUShort();

					// Create instance and deserialize (Read the UInt)
					MessageHeader header = (MessageHeader)System.Activator.CreateInstance(NetworkMessageInfo.TypeMap[msgType]);
					header.DeserializeObject(ref stream);

					if (networkMessageHandlers.ContainsKey(msgType))
					{
						try
						{
							if (msgType != NetworkMessageType.PONG)
								Debug.Log($"Message successfully received on Server: {msgType}");

							networkMessageHandlers[msgType].Invoke(this, m_Connections[i], header);
						}
						catch
						{
							Debug.LogError($"Badly formatted message received: {msgType}");
						}
					}
					else Debug.LogWarning($"Unsupported message type received: {msgType}", this);
				}
			}
		}

		// Ping Pong stuff for timeout disconnects
		for (int i = 0; i < m_Connections.Length; i++)
		{
			if (!m_Connections[i].IsCreated)
				continue;

			if (pongDict.ContainsKey(m_Connections[i]))
			{
				if (Time.time - pongDict[m_Connections[i]].lastSendTime > 5f)
				{
					pongDict[m_Connections[i]].lastSendTime = Time.time;
					if (pongDict[m_Connections[i]].status == 0)
					{
						// Remove from all the dicts, save name / id for msg
						if (idList.ContainsKey(m_Connections[i]))
						{
							nameList.Remove(idList[m_Connections[i]]);
							idList.Remove(m_Connections[i]);
						}

						pongDict.Remove(m_Connections[i]);

						// Disconnect this player
						m_Connections[i].Disconnect(m_Driver);

						// Clean up
						RemoveDisconnectedPlayerFromAnyLobby(m_Connections[i]);
						m_Connections[i] = default;
					}
					else
					{
						pongDict[m_Connections[i]].status -= 1;
						PingMessage pingMsg = new();
						SendUnicast(m_Connections[i], pingMsg);
					}
				}
			}
			else if (idList.ContainsKey(m_Connections[i]))
			{ //means they've succesfully handshaked
				PingPong ping = new()
				{
					lastSendTime = Time.time,
					status = 3,    // 3 retries
					name = nameList[idList[m_Connections[i]]]
				};
				pongDict.Add(m_Connections[i], ping);

				PingMessage pingMsg = new();
				SendUnicast(m_Connections[i], pingMsg);
			}
		}
	}

	private void RemoveDisconnectedPlayerFromAnyLobby(NetworkConnection disconnectedPlayer)
	{
		foreach (string lobbyName in lobbyList.Keys)
		{
			foreach (NetworkConnection player in lobbyList[lobbyName].Connections)
			{
				if (player == disconnectedPlayer)
				{
					RemovePlayerFromLobby(lobbyName, player);
					return;
				}
			}
		}
	}

	public void SendUnicast(NetworkConnection connection, MessageHeader header, bool reliable = true)
	{
		int result = m_Driver.BeginSend(reliable ? m_Pipeline : NetworkPipeline.Null, connection, out DataStreamWriter writer);
		if (result == 0)
		{
			header.SerializeObject(ref writer);
			m_Driver.EndSend(writer);
		}
	}

	public void SendLobbyBroadcast(ServerLobby lobby, MessageHeader header, bool reliable = true)
	{
		foreach (NetworkConnection connection in lobby.Connections)
		{
			int result = m_Driver.BeginSend(reliable ? m_Pipeline : NetworkPipeline.Null, connection, out DataStreamWriter writer);
			if (result == 0)
			{
				header.SerializeObject(ref writer);
				m_Driver.EndSend(writer);
			}
		}
	}

	public void SendBroadcast(MessageHeader header, NetworkConnection toExclude = default, bool reliable = true)
	{
		for (int i = 0; i < m_Connections.Length; i++)
		{
			if (!m_Connections[i].IsCreated || m_Connections[i] == toExclude)
				continue;

			int result = m_Driver.BeginSend(reliable ? m_Pipeline : NetworkPipeline.Null, m_Connections[i], out DataStreamWriter writer);
			if (result == 0)
			{
				header.SerializeObject(ref writer);
				m_Driver.EndSend(writer);
			}
		}
	}

	async private UniTask<T> GetRequest<T>(string url)
	{
		UnityWebRequest request = UnityWebRequest.Get(url);
		request.SetRequestHeader("Content-Type", "application/json");

		await request.SendWebRequest();

		Debug.Log(request.downloadHandler.text);
		T result;

		//The null value handling shouldn't be necessary using the COALESCE sql function, still I'm ignoring null values here so I don't get errors
		try
		{
			result = JsonConvert.DeserializeObject<T>(request.downloadHandler.text, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
		}
		catch (Exception)
		{
			result = default;
		}
		return result;
	}

	#region Static message handler functions
	// TODO: Server functions
	//      - Ping                          (DONE)
	//      - Handshake                     (DONE)
	//      - Register                      (DONE)
	//      - Login                         (DONE)
	//      - Join lobby                    (DONE)
	//      - Leave lobby                   (DONE)
	//      - Start game                    (DONE)
	//      - Place obstacle                (DONE)
	//      - Player move                   (WIP)
	//      - Continue choice               (DONE)

	static void HandlePong(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		serv.pongDict[con].status = 3;
	}

	static async void HandleHandshake(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		// Connect to database
		var json = await serv.GetRequest<List<Id>>(phpBaseUrl + "server_login.php?id=1&pw=A5FJDKeSdKI49dnR49JFRIVJWJf92JF9R8Gg98GG3");
		serv.PhpConnectionID = json[0].id;

		HandshakeResponseMessage handshakeResponseMessage = new();
		serv.SendUnicast(con, handshakeResponseMessage);
	}

	static async void HandleRegister(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		RegisterMessage message = header as RegisterMessage;
		var json = await serv.GetRequest<List<User>>(phpBaseUrl + "user_register.php?PHPSESSID=" + serv.PhpConnectionID + "&un=" + message.username + "&pw=" + message.password);
		int playerId = Convert.ToInt32(json[0].id);
		string playerName = json[0].username;

		// Add to id list & name list
		if (playerId != 0 && playerName != null)
		{
			RegisterSuccessMessage registerSuccessMessage = new();
			serv.SendUnicast(con, registerSuccessMessage);
		}
		else
		{
			// Something went wrong with the query
			RegisterFailMessage registerFailMessage = new();
			serv.SendUnicast(con, registerFailMessage);
		}
	}

	static async void HandleLogin(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		LoginMessage message = header as LoginMessage;

		if (message.username == "" || message.password == "")
		{
			LoginFailMessage loginFailMessage = new();
			serv.SendUnicast(con, loginFailMessage);
			return;
		}

		var json = await serv.GetRequest<List<User>>(phpBaseUrl + "user_login.php?PHPSESSID=" + serv.PhpConnectionID + "&un=" + message.username + "&pw=" + message.password);
		if (json.Count == 0)
		{
			// Something went wrong with the query (wrong login)
			LoginFailMessage loginFailMessage = new();
			serv.SendUnicast(con, loginFailMessage);
			return;
		}

		int playerId = Convert.ToInt32(json[0].id);
		string playerName = json[0].username;

		// Add to id list & name list
		if (playerId != 0 && playerName != null)
		{
			serv.idList.Add(con, playerId);
			serv.nameList.Add(playerId, playerName);

			LoginSuccessMessage loginSuccessMessage = new();
			serv.SendUnicast(con, loginSuccessMessage);
		}
		else
		{
			// Something went wrong with the query (wrong login)
			LoginFailMessage loginFailMessage = new();
			serv.SendUnicast(con, loginFailMessage);
		}
	}

	static async void HandleJoinLobby(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		JoinLobbyMessage message = header as JoinLobbyMessage;
		string lobbyName = Convert.ToString(message.name);

		// Check if lobby exists
		if (serv.lobbyList.TryGetValue(lobbyName, out ServerLobby lobby))
		{
			if (lobby.Connections.Count == 1)
			{
				// Get the scores of the two players against each other
				var json = await serv.GetRequest<List<UserScore>>(phpBaseUrl + "score_get.php?PHPSESSID=" + serv.PhpConnectionID + "&player1=" + serv.idList[lobby.Connections[0]] + "&player2=" + serv.idList[con]);
				uint score1 = 0;
				uint score2 = 0;
				if (json.Count > 0)
				{
					score1 = Convert.ToUInt32(json[0].score);
					if (json.Count == 2) score2 = Convert.ToUInt32(json[1].score);

					// Flip the scores if the joining player has the higher score
					if (serv.nameList[serv.idList[lobby.Connections[0]]] != json[0].username)
					{
						(score1, score2) = (score2, score1);
					}
				}

				// Create a JoinLobbyExistingMessage for player 2
				JoinLobbyExistingMessage joinLobbyExistingMessage = new()
				{
					score1 = score1,
					score2 = score2,
					name = serv.nameList[serv.idList[lobby.Connections[0]]]
				};

				// Create a LobbyUpdateMessage for player 1
				LobbyUpdateMessage lobbyUpdateMessage = new()
				{
					score1 = score1,
					score2 = score2,
					name = serv.nameList[serv.idList[con]]
				};
				serv.SendUnicast(con, joinLobbyExistingMessage);
				serv.SendUnicast(lobby.Connections[0], lobbyUpdateMessage);

				// Add player 2 to the players in this lobby
				lobby.Connections.Add(con);
			}
			else
			{
				// Lobby is full
				JoinLobbyFailMessage joinLobbyFailMessage = new();
				serv.SendUnicast(con, joinLobbyFailMessage);
			}
		}
		else
		{
			// It doesn't, player joins new lobby
			serv.lobbyList.Add(lobbyName, new ServerLobby(new List<NetworkConnection>() { con }));
			JoinLobbyNewMessage joinLobbyNewMessage = new();
			serv.SendUnicast(con, joinLobbyNewMessage);
		}
	}

	static void HandleLeaveLobby(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		LeaveLobbyMessage message = header as LeaveLobbyMessage;
		string lobbyName = Convert.ToString(message.name);

		// Remove the leaving player from the lobby
		serv.RemovePlayerFromLobby(lobbyName, con);
	}

	static void HandleStartGame(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		StartGameMessage message = header as StartGameMessage;
		ServerLobby lobby = serv.lobbyList[Convert.ToString(message.name)];

		if (lobby.Connections.Count == lobbySize)
		{
			// Initialize item grid with empty cells
			lobby.InitializeItemGrid();

			// Initialize starting player
			lobby.activePlayerId = (uint)UnityEngine.Random.Range(0, 2);

			// Initialize starting items
			lobby.InitializeItems();

			// Give the active player a random item
			lobby.currentItem = serv.GetRandomItem(lobby);
			StartGameResponseMessage startGameResponseMessage = new()
			{
				activePlayer = lobby.activePlayerId,
				itemId = (uint)lobby.currentItem
			};

			// We're sending the information to both players so we can
			// inform the other player what item the active player is placing
			serv.SendLobbyBroadcast(lobby, startGameResponseMessage);
		}
		else
		{
			StartGameFailMessage startGameFailMessage = new();
			serv.SendLobbyBroadcast(lobby, startGameFailMessage);
		}
	}

	static void HandlePlaceObstacle(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		PlaceObstacleMessage message = header as PlaceObstacleMessage;
		string lobbyName = Convert.ToString(message.name);
		ServerLobby lobby = serv.lobbyList[lobbyName];
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);

		Debug.Log("serv: currentitem: " + lobby.currentItem + "   -gridcoords: " + x + " " + y + "   -itemthere: " + lobby.ItemGrid[x, y]);

		// Handle failed obstacle placement (not the active player, space already occupied, using wrong removal)
		if (lobby.Connections[(int)lobby.activePlayerId] != con ||
			(lobby.ItemGrid[x, y] != ItemType.NONE &&
				(lobby.currentItem == ItemType.MINE ||
				lobby.currentItem == ItemType.WALL ||
				lobby.currentItem == ItemType.START ||
				lobby.currentItem == ItemType.FINISH)) ||
			(lobby.ItemGrid[x, y] == ItemType.WALL && lobby.ItemGrid[x, y] == ItemType.MINESWEEPER) ||
			(lobby.ItemGrid[x, y] == ItemType.MINE && lobby.ItemGrid[x, y] == ItemType.WRECKINGBALL))
		{
			PlaceObstacleFailMessage placeObstacleFailMessage = new();
			serv.SendUnicast(con, placeObstacleFailMessage);
			return;
		}

		// Handle minesweeper and wrecking ball use, bool is used to inform players of successful removal
		bool removal = false;
		if ((lobby.ItemGrid[x, y] == ItemType.WALL && lobby.currentItem == ItemType.WRECKINGBALL) ||
			(lobby.ItemGrid[x, y] == ItemType.MINE && lobby.currentItem == ItemType.MINESWEEPER))
		{
			lobby.ItemGrid[x, y] = ItemType.NONE;
			removal = true;
		}

		// Handle placement of mines, walls, start and finish
		if (lobby.currentItem == ItemType.MINE || lobby.currentItem == ItemType.WALL ||
			lobby.currentItem == ItemType.START || lobby.currentItem == ItemType.FINISH)
		{
			lobby.ItemGrid[x, y] = lobby.currentItem;
		}

		// Obstacle placed successfully, send a message back to update visual
		PlaceObstacleSuccessMessage placeObstacleSuccessMessage = new()
		{
			x = (uint)x,
			y = (uint)y,
			removal = (uint)(removal ? 1 : 0)
		};
		serv.SendUnicast(con, placeObstacleSuccessMessage);

		// Update the other player's grid if an item got removed
		int otherPlayerId = (lobby.activePlayerId == 0) ? 1 : 0;
		if (removal) serv.SendUnicast(lobby.Connections[otherPlayerId], placeObstacleSuccessMessage);

		// The next player becomes the active player
		lobby.activePlayerId = (uint)otherPlayerId;

		// Check whether enough obstacles have been placed and game should start
		if (serv.RoundShouldStart(lobby))
		{
			// Update the other player's grid if a start or finish item got placed
			if (lobby.currentItem == ItemType.FINISH || lobby.currentItem == ItemType.START)
				serv.SendUnicast(lobby.Connections[(int)lobby.activePlayerId], placeObstacleSuccessMessage);

			if (serv.PreStartRound(lobby, x, y))
				return;
		}
		else
		{
			// Give the active player a random item
			lobby.currentItem = serv.GetRandomItem(lobby);
		}

		PlaceNewObstacleMessage placeNewObstacleMessage = new()
		{
			activePlayer = lobby.activePlayerId,
			itemId = (uint)lobby.currentItem
		};

		serv.SendLobbyBroadcast(lobby, placeNewObstacleMessage);
	}

	static void HandlePlayerMove(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		PlayerMoveMessage message = header as PlayerMoveMessage;
		string lobbyName = Convert.ToString(message.name);
		ServerLobby lobby = serv.lobbyList[lobbyName];
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);

		PlayerFlag player = (lobby.Connections[0] == con) ? PlayerFlag.PLAYER1 : PlayerFlag.PLAYER2;
		Vector2Int playerLocation = serv.GetPlayerLocation(lobbyName, player);

		// Player not found in grid (lobby will close in GetPlayerLocation function)
		if (playerLocation.x == -1) return;

		// Check if move is legal (is the active player, space not already occupied by wall, move is distance 1)
		MoveFailReason moveFailReason = MoveFailReason.NONE;

		if (lobby.Connections[(int)lobby.activePlayerId] != con)
			moveFailReason = MoveFailReason.NOT_ACTIVE;
		if (lobby.ItemGrid[x, y] == ItemType.WALL)
			moveFailReason = MoveFailReason.WALL;
		if (Vector2Int.Distance(new Vector2Int(x,y), playerLocation) != 1)
			moveFailReason = MoveFailReason.RANGE;

		// Inform player of failed move attempt
		if (moveFailReason != MoveFailReason.NONE)
		{
			PlayerMoveFailMessage playerMoveFailMessage = new()
			{
				x = (uint)x,
				y = (uint)y,
				moveFailReason = (uint)moveFailReason
			};

			serv.SendUnicast(con, playerMoveFailMessage);
			return;
		}

		// Bitwise remove player from old location in lobbyPlayerLocations
		lobby.playerGrid[playerLocation.x, playerLocation.y] &= ~player;

		// Bitwise add player to new location in lobbyPlayerLocations
		lobby.playerGrid[x, y] |= player;

		// otherPlayerId is the id of the player that is not the active player
		int otherPlayerId = (lobby.activePlayerId == 0) ? 1 : 0;

		// Check if player hit mine
		if (lobby.ItemGrid[x, y] == ItemType.MINE)
		{
			// Decrease player health, remove mine
			lobby.playerHealth[lobby.activePlayerId] -= 1;
			lobby.ItemGrid[x, y] = ItemType.NONE;

			// Check if player is dead
			if (lobby.playerHealth[lobby.activePlayerId] == 0)
			{
				// OTHER player wins (discard to hide warning about await)
				_ = serv.EndRound(lobby, otherPlayerId, (int)lobby.activePlayerId);
				return;
			}

			// Player has to skip a turn
			lobby.playerHitMine[lobby.activePlayerId] = true;
		}

		// Next active player is the same player if the other player hit a mine last turn
		uint nextActivePlayerId = (uint)otherPlayerId;
		if (lobby.playerHitMine[otherPlayerId])
		{
			lobby.playerHitMine[otherPlayerId] = false;
			nextActivePlayerId = lobby.activePlayerId;
		}

		// Inform players of successful move attempt
		PlayerMoveSuccessMessage playerMoveSuccessMessage = new()
		{
			activePlayer = nextActivePlayerId,
			x = (uint)x,
			y = (uint)y,
			otherHealth = lobby.playerHealth[nextActivePlayerId == 0 ? 1 : 0],
			playerToMove = (uint)player
		};

		serv.SendLobbyBroadcast(lobby, playerMoveSuccessMessage);

		// Player wins if they reach the finish first
		if (lobby.ItemGrid[x, y] == ItemType.FINISH)
		{
			// Discard to hide warning about await
			_ = serv.EndRound(lobby, (int)lobby.activePlayerId, otherPlayerId);
			return;
		}

		// Next player becomes active player
		lobby.activePlayerId = nextActivePlayerId;
	}

	static void HandleContinueChoice(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		ContinueChoiceMessage message = header as ContinueChoiceMessage;
		string lobbyName = Convert.ToString(message.name);
		ServerLobby lobby = serv.lobbyList[lobbyName];

		// Initialize rematch choice array
		if (lobby.rematch == null)
			lobby.InitializeRematch();

		if (message.choice)
		{
			lobby.rematch[(lobby.Connections[0] == con) ? 0 : 1]  = true;

			if (lobby.rematch[0] && lobby.rematch[1])
			{
				// Rematch accepted by both players
				EndGameMessage endGameMessage = new()
				{
					rematch = true
				};

				// Close lobby and send last message
				serv.lobbyList.Remove(lobbyName);
				serv.SendLobbyBroadcast(lobby, endGameMessage);

				return;
			}

			// Inform the other player of rematch choice
			int otherPlayerId = (lobby.Connections[0] == con) ? 1 : 0;

			ContinueChoiceResponseMessage continueChoiceResponseMessage = new();

			serv.SendUnicast(lobby.Connections[otherPlayerId], continueChoiceResponseMessage);
		}
		else
		{
			// End game and inform the other player
			EndGameMessage endGameMessage = new()
			{
				rematch = false
			};

			// Close lobby and send last message
			serv.lobbyList.Remove(lobbyName);
			serv.SendLobbyBroadcast(lobby, endGameMessage);
		}
	}
	#endregion

	private void RemovePlayerFromLobby(string lobbyName, NetworkConnection player)
	{
		ServerLobby lobby = lobbyList[lobbyName];
		lobby.Connections.Remove(player);

		if (lobby.Connections.Count == 0)
		{
			// Close lobby
			lobbyList.Remove(lobbyName);
		}
		else if (lobby.Connections.Count == 1)
		{
			if (lobby.ItemGrid != null)
			{
				// Create an empty LobbyUpdateMessage for player left in lobby (if lobby hasn't started already)
				LobbyUpdateMessage lobbyUpdateMessage = new()
				{
					score1 = 0,
					score2 = 0,
					name = ""
				};

				SendUnicast(lobby.Connections[0], lobbyUpdateMessage);
			}
			else
			{
				// End game and inform the other player
				EndGameMessage endGameMessage = new()
				{
					rematch = false
				};

				// Close lobby and send last message
				lobbyList.Remove(lobbyName);
				SendUnicast(lobby.Connections[0], endGameMessage);
			}
		}
	}

	private ItemType GetRandomItem(ServerLobby lobby)
	{
		if (lobby.Items.Count == 0)
		{
			// Get a new set of items
			lobby.AddNewItemSet();
		}

		// Get an available placeable item
		ItemType item = lobby.Items[UnityEngine.Random.Range(0, lobby.Items.Count)];
		lobby.Items.Remove(item);
		return item;
	}

	public static IEnumerable<T> Flatten<T>(T[,] map)
	{
		for (int row = 0; row < map.GetLength(0); row++)
		{
			for (int col = 0; col < map.GetLength(1); col++)
			{
				yield return map[row, col];
			}
		}
	}
	
	private bool StartFinishGotPlaced(ServerLobby lobby)
	{
		var lobbyItems = Flatten(lobby.ItemGrid);
		return lobbyItems.Contains(ItemType.START) && lobbyItems.Contains(ItemType.FINISH);
	}

	private bool RoundShouldStart(ServerLobby lobby)
	{
		return Flatten(lobby.ItemGrid).Where(x => !x.Equals(ItemType.NONE)).Count() >= itemLimit;
	}

	private bool PreStartRound(ServerLobby lobby, int x, int y)
	{
		if (StartFinishGotPlaced(lobby))
		{
			StartRound(lobby, x, y);
			return true;
		}

		// One player chooses the finish, the other player then chooses the start
		// TODO: A pathfinding algorithm should check if the path is possible
		if (lobby.currentItem == ItemType.FINISH)
			lobby.currentItem = ItemType.START;
		else lobby.currentItem = ItemType.FINISH;
		return false;
	}

	private void StartRound(ServerLobby lobby, int x, int y)
	{
		// Initialize player locations
		lobby.InitializePlayerGrid();
		lobby.playerGrid[x, y] = PlayerFlag.PLAYER1 | PlayerFlag.PLAYER2;

		// Initialize player health
		lobby.InitializePlayerHealth();

		// Initialize hit mine bools for turn skipping
		lobby.InitializePlayerHitMine();

		// Communicate the location of the start to both players so they start at the right spot
		StartRoundMessage startRoundMessage = new()
		{
			activePlayer = lobby.activePlayerId,
			x = (uint)x,
			y = (uint)y
		};

		SendLobbyBroadcast(lobby, startRoundMessage);
	}

	private Vector2Int GetPlayerLocation(string lobbyName, PlayerFlag player)
	{
		ServerLobby lobby = lobbyList[lobbyName];
		int w = lobby.playerGrid.GetLength(0);
		int h = lobby.playerGrid.GetLength(1);

		// Loop through all locations to find the player
		for (int x = 0; x < w; ++x)
		{
			for (int y = 0; y < h; ++y)
			{
				// Bitwise AND to check if the player is at this location
				if ((lobby.playerGrid[x, y] & player) == player)
					return new Vector2Int(x, y);
			}
		}

		Debug.Log("Player " + player + " was not found in grid of lobby " + lobbyName + ". Lobby will now close.");
		DisconnectPlayersInLobby(lobbyName);
		return new Vector2Int(-1, -1);
	}

	private void DisconnectPlayersInLobby(string lobbyName)
	{
		// Disconnect all players in the lobby from the server, and close the lobby.
		// Simplest way to resolve any error in the game.
		foreach (NetworkConnection playerConnection in lobbyList[lobbyName].Connections)
		{
			if (idList.ContainsKey(playerConnection))
			{
				nameList.Remove(idList[playerConnection]);
				idList.Remove(playerConnection);
			}

			pongDict.Remove(playerConnection);
			playerConnection.Disconnect(m_Driver);
		}

		lobbyList.Remove(lobbyName);
	}

	private async UniTask EndRound(ServerLobby lobby, int winnerId, int loserId)
	{
		// Submit the score to the database and inform the players of the winner
		EndRoundMessage endRoundMessage = new()
		{
			winnerId = (uint)winnerId
		};

		SendLobbyBroadcast(lobby, endRoundMessage);

		var json = await GetRequest<List<Error>>(phpBaseUrl + "score_insert.php?PHPSESSID=" + PhpConnectionID + "&winner_id=" + idList[lobby.Connections[winnerId]] + "&loser_id=" + idList[lobby.Connections[loserId]]);
		Debug.Log(Convert.ToUInt32(json[0].result) == 1 ? ": successfully submitted score" : ": error submitting score");
	}
}