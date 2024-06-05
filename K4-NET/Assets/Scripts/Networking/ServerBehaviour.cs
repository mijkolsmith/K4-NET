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
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

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

	private const int gridsizeX = 8; //8
	private const int gridsizeY = 5; //5
	private const int itemLimit = 5; //20

	[SerializeField] private readonly List<ItemType> startingItems = new()
	{
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE,
		ItemType.MINE,
	};
	[SerializeField] private readonly List<ItemType> itemSet = new()
	{
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
	};

	private string databaseUrl = "https://studenthome.hku.nl/~michael.smith/K4/";

	private Dictionary<NetworkConnection, int> idList = new();
	private Dictionary<int, string> nameList = new();
	private Dictionary<NetworkConnection, PingPong> pongDict = new();
	private Dictionary<string, List<NetworkConnection>> lobbyList = new();
	private Dictionary<string, NetworkConnection> lobbyActivePlayer = new();
	private Dictionary<string, List<ItemType>> lobbyItems = new();
	private Dictionary<string, uint[]> lobbyHealth = new();
	private Dictionary<string, ItemType[,]> lobbyGrid = new();
	private Dictionary<string, PlayerFlag[,]> lobbyPlayerLocations = new();
	private Dictionary<string, ItemType> lobbyCurrentItem = new();
	private Dictionary<string, bool[]> lobbyRematch = new();

	public ChatCanvas chat;

	private string PhpConnectionID;

	async void Start()
	{
		//var getJson = await GetRequest<List<Id>>(databaseUrl + "server_login.php?id=1&pw=A5FJDKeSdKI49dnR49JFRIVJWJf92JF9R8Gg98GG3");
		//Debug.Log(getJson[0].id);

		//List<User> json = await GetRequest<List<User>>(databaseUrl + "user_register.php?PHPSESSID=" + getJson[0].id + "&un=test1&pw=test1");

		//int playerId = Convert.ToInt32(json[0].id);
		//string playerName = json[0].username;

		//Debug.Log(playerId);
		//Debug.Log(playerName);

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
	//  Or else you well get lingering thread sockets, and will have trouble starting new ones!
	void OnDestroy()
	{
		m_Driver.Dispose();
		m_Connections.Dispose();
	}

	void Update()
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
					chat.NewMessage($"{ idList[m_Connections[i]]} has disconnected.", ChatCanvas.leaveColor);
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
		while ((c = m_Driver.Accept()) != default(NetworkConnection))
		{
			m_Connections.Add(c);
			Debug.Log("Accepted a connection");
		}

		DataStreamReader stream;
		for (int i = 0; i < m_Connections.Length; i++)
		{
			if (!m_Connections[i].IsCreated)
				continue;

			// Loop through available events
			NetworkEvent.Type cmd;
			while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
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

						// FIXME: for some reason, sometimes this isn't in the list?
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
			foreach (NetworkConnection player in lobbyList[lobbyName])
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
		DataStreamWriter writer;
		int result = m_Driver.BeginSend(reliable ? m_Pipeline : NetworkPipeline.Null, connection, out writer);
		if (result == 0)
		{
			header.SerializeObject(ref writer);
			m_Driver.EndSend(writer);
		}
	}

	public void SendLobbyBroadcast(string lobbyName, MessageHeader header, bool reliable = true)
	{
		DataStreamWriter writer;
		foreach (NetworkConnection connection in lobbyList[lobbyName])
		{
			int result = m_Driver.BeginSend(reliable ? m_Pipeline : NetworkPipeline.Null, connection, out writer);
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

			DataStreamWriter writer;
			int result = m_Driver.BeginSend(reliable ? m_Pipeline : NetworkPipeline.Null, m_Connections[i], out writer);
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
	//      - Player move                   (DONE)
	//      - Continue choice               (WIP)

	static void HandlePong(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		serv.pongDict[con].status = 3;
	}

	static async void HandleHandshake(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		// Connect to database
		var json = await serv.GetRequest<List<Id>>(serv.databaseUrl + "server_login.php?id=1&pw=A5FJDKeSdKI49dnR49JFRIVJWJf92JF9R8Gg98GG3");
		serv.PhpConnectionID = json[0].id;

		HandshakeResponseMessage handshakeResponseMessage = new();
		serv.SendUnicast(con, handshakeResponseMessage);
	}

	static async void HandleRegister(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		RegisterMessage message = header as RegisterMessage;
		var json = await serv.GetRequest<List<User>>(serv.databaseUrl + "user_register.php?PHPSESSID=" + serv.PhpConnectionID + "&un=" + message.username + "&pw=" + message.password);
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
		var json = await serv.GetRequest<List<User>>(serv.databaseUrl + "user_login.php?PHPSESSID=" + serv.PhpConnectionID + "&un=" + message.username + "&pw=" + message.password);
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
		if (!serv.lobbyList.ContainsKey(lobbyName))
		{
			// It doesn't, player joins new lobby
			serv.lobbyList.Add(lobbyName, new List<NetworkConnection>() { con });
			JoinLobbyNewMessage joinLobbyNewMessage = new();
			serv.SendUnicast(con, joinLobbyNewMessage);
		}
		else if (serv.lobbyList[lobbyName].Count == 1)
		{
			// Get the scores of the two players against each other
			var json = await serv.GetRequest<List<UserScore>>(serv.databaseUrl + "score_get.php?PHPSESSID=" + serv.PhpConnectionID + "&player1=" + serv.idList[serv.lobbyList[lobbyName][0]] + "&player2=" + serv.idList[con]);
			uint score1 = 0;
			uint score2 = 0;
			if (json.Count > 0)
			{
				score1 = Convert.ToUInt32(json[0].score);
				if (json.Count == 2) score2 = Convert.ToUInt32(json[1].score);

				// Flip the scores if the joining player has the higher score
				if (serv.nameList[serv.idList[serv.lobbyList[lobbyName][0]]] != json[0].username)
				{
					(score1, score2) = (score2, score1);
				}
			}

			// Create a JoinLobbyExistingMessage for player 2
			JoinLobbyExistingMessage joinLobbyExistingMessage = new()
			{
				score1 = score1,
				score2 = score2,
				name = serv.nameList[serv.idList[serv.lobbyList[lobbyName][0]]]
			};

			// Create a LobbyUpdateMessage for player 1
			LobbyUpdateMessage lobbyUpdateMessage = new()
			{
				score1 = score1,
				score2 = score2,
				name = serv.nameList[serv.idList[con]]
			};
			serv.SendUnicast(con, joinLobbyExistingMessage);
			serv.SendUnicast(serv.lobbyList[lobbyName][0], lobbyUpdateMessage);

			// Add player 2 to the players in this lobby
			serv.lobbyList[lobbyName].Add(con);
		}
		else
		{
			JoinLobbyFailMessage joinLobbyFailMessage = new();
			serv.SendUnicast(con, joinLobbyFailMessage);
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
		string lobbyName = Convert.ToString(message.name);

		if (serv.lobbyList[lobbyName].Count == 2)
		{
			// Initialize grid with empty cells
			serv.lobbyGrid.Add(lobbyName, new ItemType[gridsizeX, gridsizeY]);

			// Initialize starting player
			int activePlayer = UnityEngine.Random.Range(0, 2);
			serv.lobbyActivePlayer[lobbyName] = serv.lobbyList[lobbyName][activePlayer];

			// Initialize starting items
			serv.lobbyItems.Add(lobbyName, serv.startingItems);

			// Give the active player a random item
			serv.lobbyCurrentItem[lobbyName] = serv.GetRandomItem(lobbyName);
			StartGameResponseMessage startGameResponseMessage = new()
			{
				activePlayer = (uint)activePlayer,
				itemId = (uint)serv.lobbyCurrentItem[lobbyName]
			};

			// We're sending the information to both players so we can
			// inform the other player what item the active player is placing
			serv.SendLobbyBroadcast(lobbyName, startGameResponseMessage);
		}
		else
		{
			StartGameFailMessage startGameFailMessage = new();
			serv.SendLobbyBroadcast(lobbyName, startGameFailMessage);
		}
	}

	static void HandlePlaceObstacle(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		PlaceObstacleMessage message = header as PlaceObstacleMessage;
		string lobbyName = Convert.ToString(message.name);
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);

		Debug.Log("serv: currentitem: " + serv.lobbyCurrentItem[lobbyName] + "   -gridcoords: " + x + " " + y + "   -itemthere: " + serv.lobbyGrid[lobbyName][x, y]);

		// Handle failed obstacle placement (not the active player, space already occupied, using wrong removal)
		if (serv.lobbyActivePlayer[lobbyName] != con ||
			(serv.lobbyGrid[lobbyName][x, y] != ItemType.NONE &&
				(serv.lobbyCurrentItem[lobbyName] == ItemType.MINE ||
				serv.lobbyCurrentItem[lobbyName] == ItemType.WALL ||
				serv.lobbyCurrentItem[lobbyName] == ItemType.START ||
				serv.lobbyCurrentItem[lobbyName] == ItemType.FINISH)) ||
			(serv.lobbyGrid[lobbyName][x, y] == ItemType.WALL && serv.lobbyCurrentItem[lobbyName] == ItemType.MINESWEEPER) ||
			(serv.lobbyGrid[lobbyName][x, y] == ItemType.MINE && serv.lobbyCurrentItem[lobbyName] == ItemType.WRECKINGBALL))
		{
			PlaceObstacleFailMessage placeObstacleFailMessage = new();
			serv.SendUnicast(con, placeObstacleFailMessage);
			return;
		}

		// Handle minesweeper and wrecking ball use, bool is used to inform players of successful removal
		bool removal = false;
		if ((serv.lobbyGrid[lobbyName][x, y] == ItemType.WALL && serv.lobbyCurrentItem[lobbyName] == ItemType.WRECKINGBALL) ||
			(serv.lobbyGrid[lobbyName][x, y] == ItemType.MINE && serv.lobbyCurrentItem[lobbyName] == ItemType.MINESWEEPER))
		{
			serv.lobbyGrid[lobbyName][x, y] = ItemType.NONE;
			removal = true;
		}

		// Handle placement of mines, walls, start and finish
		if (serv.lobbyCurrentItem[lobbyName] == ItemType.MINE || serv.lobbyCurrentItem[lobbyName] == ItemType.WALL ||
			serv.lobbyCurrentItem[lobbyName] == ItemType.START || serv.lobbyCurrentItem[lobbyName] == ItemType.FINISH)
		{
			serv.lobbyGrid[lobbyName][x, y] = serv.lobbyCurrentItem[lobbyName];
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
		int otherPlayerId = (serv.lobbyList[lobbyName][0] == con) ? 1 : 0;
		if (removal) serv.SendUnicast(serv.lobbyList[lobbyName][otherPlayerId], placeObstacleSuccessMessage);

		// The next player becomes the active player
		serv.lobbyActivePlayer[lobbyName] = serv.lobbyList[lobbyName][otherPlayerId];

		// Check whether enough obstacles have been placed and game should start
		if (serv.RoundShouldStart(lobbyName))
		{
			// Update the other player's grid if a start or finish item got placed
			if (serv.lobbyCurrentItem[lobbyName] == ItemType.FINISH || serv.lobbyCurrentItem[lobbyName] == ItemType.START)
				serv.SendUnicast(serv.lobbyList[lobbyName][otherPlayerId], placeObstacleSuccessMessage);

			serv.PreStartRound(lobbyName, x, y, otherPlayerId);
		}
		else
		{
			// Give the active player a random item
			serv.lobbyCurrentItem[lobbyName] = serv.GetRandomItem(lobbyName);
		}

		PlaceNewObstacleMessage placeNewObstacleMessage = new()
		{
			activePlayer = (uint)otherPlayerId,
			itemId = (uint)serv.lobbyCurrentItem[lobbyName]
		};

		serv.SendLobbyBroadcast(lobbyName, placeNewObstacleMessage);
	}

	static async void HandlePlayerMove(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		PlayerMoveMessage message = header as PlayerMoveMessage;
		string lobbyName = Convert.ToString(message.name);
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);

		PlayerFlag player = (serv.lobbyList[lobbyName][0] == con) ? PlayerFlag.PLAYER1 : PlayerFlag.PLAYER2;
		Vector2 playerLocation = serv.GetPlayerLocation(lobbyName, player);

		// Player not found in grid (lobby will close in GetPlayerLocation function)
		if (playerLocation.x == -1) return;

		// Check if move is legal (is the active player, space not already occupied by wall, move is distance 1)
		MoveFailReason moveFailReason = MoveFailReason.NONE;

		if (serv.lobbyActivePlayer[lobbyName] != con)
			moveFailReason = MoveFailReason.NOT_ACTIVE;
		if (serv.lobbyGrid[lobbyName][x, y] == ItemType.WALL)
			moveFailReason = MoveFailReason.WALL;
		if (Vector2.Distance(new Vector2(x,y), playerLocation) != 1)
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
		serv.lobbyPlayerLocations[lobbyName][(int)playerLocation.x, (int)playerLocation.y] &= ~player;

		// Bitwise add player to new location in lobbyPlayerLocations
		serv.lobbyPlayerLocations[lobbyName][x, y] |= player;

		int otherPlayerId = (serv.lobbyList[lobbyName][0] == con) ? 1 : 0;

		// Check if player hit mine
		if (serv.lobbyGrid[lobbyName][x, y] == ItemType.MINE)
		{
			// Decrease player health
			serv.lobbyHealth[lobbyName][(int)player] -= 1;

			// Check if player is dead
			if (serv.lobbyHealth[lobbyName][(int)player] == 0)
			{
				// OTHER player wins
				serv.EndRound(con, lobbyName, otherPlayerId);
				return;
			}
		}

		// Player wins if they reach the finish first
		if (serv.lobbyGrid[lobbyName][x, y] == ItemType.FINISH)
		{
			serv.EndRound(con, lobbyName, serv.idList[con]);
			return;
		}

		// Inform players of successful move attempt
		PlayerMoveSuccessMessage playerMoveSuccessMessage = new()
		{
			activePlayer = (uint)otherPlayerId,
			x = (uint)x,
			y = (uint)y,
			health = serv.lobbyHealth[lobbyName][(int)player],
			playerToMove = (uint)player
		};

		serv.SendLobbyBroadcast(lobbyName, playerMoveSuccessMessage);
	}

	static void HandleContinueChoice(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
		ContinueChoiceMessage message = header as ContinueChoiceMessage;
		string lobbyName = Convert.ToString(message.name);

		int otherPlayerId = (serv.lobbyList[lobbyName][0] == con) ? 1 : 0;

		// Initialize rematch choice array
		serv.lobbyRematch[lobbyName] = new bool[2];

		if (message.choice)
		{
			serv.lobbyRematch[lobbyName][serv.idList[con]] = true;

			if (serv.lobbyRematch[lobbyName][0] && serv.lobbyRematch[lobbyName][1])
			{
				// Rematch accepted by both players
				EndGameMessage endGameMessage = new()
				{
					rematch = true
				};
				serv.SendLobbyBroadcast(lobbyName, endGameMessage);

				serv.ResetLobby(lobbyName);
				return;
			}

			// Inform the other player of rematch choice
			ContinueChoiceResponseMessage continueChoiceSuccessMessage = new();

			serv.SendUnicast(serv.lobbyList[lobbyName][otherPlayerId], continueChoiceSuccessMessage);
		}
		else
		{
			// End game and inform the other player
			EndGameMessage endGameMessage = new()
			{
				rematch = false
			};
			serv.SendLobbyBroadcast(lobbyName, endGameMessage);

			// Reset & close lobby
			serv.ResetLobby(lobbyName);
			serv.lobbyList.Remove(lobbyName);
		}
	}
	#endregion

	private void RemovePlayerFromLobby(string lobbyName, NetworkConnection player)
	{
		lobbyList[lobbyName].Remove(player);

		if (lobbyList[lobbyName].Count == 0)
		{
			// Reset & close lobby
			ResetLobby(lobbyName);
			lobbyList.Remove(lobbyName);
		}
		else if (lobbyList[lobbyName].Count == 1)
		{
			// Create an empty LobbyUpdateMessage for player left in lobby
			LobbyUpdateMessage lobbyUpdateMessage = new()
			{
				score1 = 0,
				score2 = 0,
				name = ""
			};

			SendUnicast(lobbyList[lobbyName][0], lobbyUpdateMessage);
		}
	}

	private ItemType GetRandomItem(string lobbyName)
	{
		if (lobbyItems[lobbyName].Count == 0)
		{
			// Get a new set of items
			lobbyItems[lobbyName].AddRange(itemSet);
		}

		// Get an available placeable item
		ItemType item = lobbyItems[lobbyName][UnityEngine.Random.Range(0, lobbyItems[lobbyName].Count)];
		lobbyItems[lobbyName].Remove(item);
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
	private bool StartFinishGotPlaced(string lobbyName)
	{
		var lobbyItems = Flatten(lobbyGrid[lobbyName]);
		return lobbyItems.Contains(ItemType.START) && lobbyItems.Contains(ItemType.FINISH);
	}

	private bool RoundShouldStart(string lobbyName)
	{
		return Flatten(lobbyGrid[lobbyName]).Where(x => !x.Equals(ItemType.NONE)).Count() >= itemLimit;
	}

	private void PreStartRound(string lobbyName, int x, int y, int otherPlayerId)
	{
		if (StartFinishGotPlaced(lobbyName))
		{
			StartRound(lobbyName, x, y, otherPlayerId);
			return;
		}

		// One player chooses the finish, the other player then chooses the start
		// TODO: A pathfinding algorithm should check if the path is possible
		if (lobbyCurrentItem[lobbyName] == ItemType.FINISH)
			lobbyCurrentItem[lobbyName] = ItemType.START;
		else lobbyCurrentItem[lobbyName] = ItemType.FINISH;
	}

	private Vector2 GetPlayerLocation(string lobbyName, PlayerFlag player)
	{
		int w = lobbyPlayerLocations[lobbyName].GetLength(0);
		int h = lobbyPlayerLocations[lobbyName].GetLength(1);

		// Loop through all locations to find the player
		for (int x = 0; x < w; ++x)
		{
			for (int y = 0; y < h; ++y)
			{
				// Bitwise AND to check if the player is at this location
				if ((lobbyPlayerLocations[lobbyName][x, y] & PlayerFlag.PLAYER1) == PlayerFlag.PLAYER1)
					return new Vector2(x, y);
			}
		}

		Debug.Log("Player " + player + " was not found in grid of lobby " + lobbyName + ". Lobby will now close.");
		DisconnectPlayersFromLobby(lobbyName);
		return new Vector2(-1, -1);
	}

	private void DisconnectPlayersFromLobby(string lobbyName)
	{
		// Disconnect all players in the lobby from the server, and close the lobby.
		// Simplest way to resolve any error in the game.
		foreach (NetworkConnection playerConnection in lobbyList[lobbyName])
		{
			if (idList.ContainsKey(playerConnection))
			{
				nameList.Remove(idList[playerConnection]);
				idList.Remove(playerConnection);
			}

			pongDict.Remove(playerConnection);
			playerConnection.Disconnect(m_Driver);
		}

		ResetLobby(lobbyName);
		lobbyList.Remove(lobbyName);
	}

	private void StartRound(string lobbyName, int x, int y, int activePlayer)
	{
		Debug.Log("Lobby " + lobbyName + " round will now start!");

		// Initialize player locations
		lobbyPlayerLocations.Add(lobbyName, new PlayerFlag[gridsizeX, gridsizeY]);
		lobbyPlayerLocations[lobbyName][x, y] = PlayerFlag.PLAYER1 | PlayerFlag.PLAYER2;

		// Initialize player health
		lobbyHealth.Add(lobbyName, new uint[2] { GameData.defaultPlayerHealth, GameData.defaultPlayerHealth });

		// Communicate the location of the start to both players so they start at the right spot
		StartRoundMessage startRoundMessage = new()
		{
			activePlayer = (uint)activePlayer,
			x = (uint)x,
			y = (uint)y
		};

		SendLobbyBroadcast(lobbyName, startRoundMessage);
		return;
	}

	private async Task EndRound(NetworkConnection con, string lobbyName, int winnerId)
	{
		// Submit the score to the database and inform the players of the winner
		EndRoundMessage endRoundMessage = new()
		{
			winnerId = (uint)winnerId
		};

		SendLobbyBroadcast(lobbyName, endRoundMessage);

		var json = await GetRequest<List<Error>>(databaseUrl + "score_insert.php?PHPSESSID=" + PhpConnectionID + "&winner_id=" + idList[lobbyList[lobbyName][winnerId]] + "&loser_id=" + idList[con]);
		Debug.Log(lobbyName + (Convert.ToUInt32(json[0].result) == 1 ? ": successfully submitted score" : ": error submitting score"));
		return;
	}

	private void ResetLobby(string lobbyName)
	{
		lobbyActivePlayer.Remove(lobbyName);
		lobbyItems.Remove(lobbyName);
		lobbyHealth.Remove(lobbyName);
		lobbyGrid.Remove(lobbyName);
		lobbyPlayerLocations.Remove(lobbyName);
		lobbyCurrentItem.Remove(lobbyName);
		lobbyRematch.Remove(lobbyName);
	}
}