using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using System;
using Unity.Networking.Transport.Utilities;
using TMPro;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public delegate void ClientMessageHandler(ClientBehaviour client, MessageHeader header);

public class ClientBehaviour : MonoBehaviour
{
	static Dictionary<NetworkMessageType, ClientMessageHandler> networkMessageHandlers = new() 
	{
		{ NetworkMessageType.PING, HandlePing },
		{ NetworkMessageType.HANDSHAKE_RESPONSE, HandleServerHandshakeResponse },
		{ NetworkMessageType.REGISTER_SUCCESS, HandleRegisterSuccess },
		{ NetworkMessageType.REGISTER_FAIL, HandleRegisterFail },
		{ NetworkMessageType.LOGIN_SUCCESS, HandleLoginSuccess },
		{ NetworkMessageType.LOGIN_FAIL, HandleLoginFail },
		{ NetworkMessageType.JOIN_LOBBY_NEW, HandleJoinLobbyNew },
		{ NetworkMessageType.JOIN_LOBBY_EXISTING, HandleJoinLobbyExisting },
		{ NetworkMessageType.JOIN_LOBBY_FAIL, HandleJoinLobbyFail },
		{ NetworkMessageType.LOBBY_UPDATE, HandleLobbyUpdate },
		{ NetworkMessageType.START_GAME_RESPONSE, HandleStartGameResponse },
		{ NetworkMessageType.START_GAME_FAIL, HandleStartGameFail },
		{ NetworkMessageType.PLACE_OBSTACLE_SUCCESS, HandlePlaceObstacleSuccess },
		{ NetworkMessageType.PLACE_OBSTACLE_FAIL, HandlePlaceObstacleFail },
		{ NetworkMessageType.PLACE_NEW_OBSTACLE, HandlePlaceNewObstacle },
		{ NetworkMessageType.START_ROUND, HandleStartRound },
		{ NetworkMessageType.PLAYER_MOVE_SUCCESS, HandlePlayerMoveSuccess },
		{ NetworkMessageType.PLAYER_MOVE_FAIL, HandlePlayerMoveFail },
		{ NetworkMessageType.END_ROUND, HandleEndRound },
		{ NetworkMessageType.END_GAME, HandleEndGame },
		{ NetworkMessageType.CONTINUE_CHOICE_RESPONSE, HandleContinueChoiceResponse },
	};

	public NetworkDriver m_Driver;
	public NetworkPipeline m_Pipeline;
	public NetworkConnection m_Connection;
	public bool Done;

	public SceneObjectReferences objectReferences;

	public string username;
	public string LobbyName { get; private set; } = "";
	public ItemType CurrentItem { get; private set; } = ItemType.NONE;
	public uint Player { get; private set; }
	public PlayerFlag PlayerFlag { get; private set; }
	public bool ActivePlayer { get; private set; } = false;
	public bool RoundStarted { get; private set; } = false;

	//Workaround for sceneloading race condition
	private float objectReferencesTimer;
	private float objectReferencesTimeNeeded;

	private void Start()
	{
		DontDestroyOnLoad(this);
		m_Driver = NetworkDriver.Create(new ReliableUtility.Parameters { WindowSize = 32 });
		m_Pipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
		m_Connection = default;

		var endpoint = NetworkEndPoint.LoopbackIpv4;
		endpoint.Port = 1511;

		m_Connection = m_Driver.Connect(endpoint);
	}

	private void OnDestroy()
	{
		m_Driver.Dispose();
	}

	private void Update()
	{
		// Connection Update
		m_Driver.ScheduleUpdate().Complete();

		if (!m_Connection.IsCreated)
		{
			if (!Done)
			{
				Debug.Log("Something went wrong during connect");
				objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Lost connection to server. Reconnecting...";
			}
			return;
		}
		NetworkEvent.Type cmd;
		while ((cmd = m_Connection.PopEvent(m_Driver, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
		{
			if (cmd == NetworkEvent.Type.Connect)
			{
				Debug.Log("We are now connected to the server");

				var header = new HandshakeMessage { };

				SendPackedMessage(header);
			}
			else if (cmd == NetworkEvent.Type.Data)
			{
				// First UShort is always message type
				NetworkMessageType msgType = (NetworkMessageType)stream.ReadUShort();

				// Create instance and deserialize (Read the UInt)
				MessageHeader header = (MessageHeader)System.Activator.CreateInstance(NetworkMessageInfo.TypeMap[msgType]);
				header.DeserializeObject(ref stream);

				if (networkMessageHandlers.ContainsKey(msgType))
				{
					if (msgType != NetworkMessageType.PING)
						Debug.Log($"Message successfully received on Client: {msgType}");

					networkMessageHandlers[msgType].Invoke(this, header);
				}
				else
				{
					Debug.LogWarning($"Unsupported message type received: {msgType}", this);
				}
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				Debug.Log("Client got disconnected from server");
				m_Connection = default;
			}
		}

		// Game Update

		// Workaround for race condition
		if (objectReferencesTimer < objectReferencesTimeNeeded)
		{
			objectReferencesTimer += Time.deltaTime;
			if (objectReferences == null) objectReferences = FindObjectOfType<SceneObjectReferences>();
		}
		else if (objectReferencesTimeNeeded != 0f)
		{
			// Reset timer
			objectReferencesTimeNeeded = 0f;
			objectReferencesTimer = 0f;

			// Start turn
			objectReferences.Cursor.SetSprite(objectReferences.GamePrefabs.itemVisuals[CurrentItem].cursorSprite);
			objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = 
				ActivePlayer ? "Your turn!" : "Waiting for other player...";
			objectReferences.InputManager.checkInput = true;
		}
	}

	public void SendPackedMessage(MessageHeader header)
	{
		int result = m_Driver.BeginSend(m_Pipeline, m_Connection, out DataStreamWriter writer);

		// non-0 is an error code
		if (result == 0)
		{
			header.SerializeObject(ref writer);
		}
		else
		{
			Debug.LogError($"Could not write message to driver: {result}", this);
		}
		m_Driver.EndSend(writer);
	}

	#region Static message handler functions
	// TODO: Client functions
	//      - Pong                      (DONE)
	//      - Server handshake response (DONE)
	//      - Register success          (DONE)
	//      - Register fail             (DONE)
	//      - Login success             (DONE)
	//      - Login fail                (DONE)
	//      - Join lobby new            (DONE)
	//      - Join lobby existing       (DONE)
	//      - Join lobby fail           (DONE)
	//      - Handle lobby update       (DONE)
	//      - Start game response       (DONE)
	//      - Start game fail           (DONE)
	//      - Place obstacle success    (DONE)
	//      - Place obstacle fail       (DONE)
	//      - Place new obstacle        (DONE)
	//      - Start round               (DONE)
	//      - Player move success       (DONE)
	//      - Player move fail          (DONE)
	//      - End round                 (DONE)
	//      - Continue choice response  (DONE)
	//      - End game                  (DONE)

	private static void HandlePing(ClientBehaviour client, MessageHeader header)
	{
		PongMessage pongMessage = new();

		client.SendPackedMessage(pongMessage);
	}

	private static void HandleServerHandshakeResponse(ClientBehaviour client, MessageHeader header)
	{
		Debug.Log("Successfully Handshaked");
		if (client.objectReferences.LoginRegister != null)
			client.objectReferences.LoginRegister.SetActive(true);

		if (client.objectReferences.ErrorMessage != null)
			client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Please register or login";
	}

	private static void HandleRegisterSuccess(ClientBehaviour client, MessageHeader header)
	{
		Debug.Log("register success");
		if (client.objectReferences.ErrorMessage != null)
			client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Register success, please login";
	}

	private static void HandleRegisterFail(ClientBehaviour client, MessageHeader header)
	{
		Debug.Log("register fail");
		if (client.objectReferences.ErrorMessage != null)
			client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Register fail, please try again";
	}

	private static void HandleLoginSuccess(ClientBehaviour client, MessageHeader header)
	{
		Debug.Log("login success");

		// Open a new scene without closing the server (discard to hide warning about await)
		_ = LoadSceneWithoutClosingAdditiveScenes(SceneManager.GetActiveScene().buildIndex + 1);
	}

	private static void HandleLoginFail(ClientBehaviour client, MessageHeader header)
	{
		Debug.Log("login fail");

		if (client.objectReferences.ErrorMessage != null)
			client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Login fail, please try again";
	}

	private static void HandleJoinLobbyNew(ClientBehaviour client, MessageHeader header)
	{
		client.Player = 0;

		if (client.objectReferences == null) client.objectReferences = FindObjectOfType<SceneObjectReferences>();

		if (client.LobbyName == "")
			client.LobbyName = client.objectReferences.JoinLobby.GetComponent<ClientLobby>().LobbyName;

		client.objectReferences.JoinLobby.SetActive(false);
		client.objectReferences.CurrentLobby.SetActive(true);
		ClientCurrentLobby currentLobby = client.objectReferences.CurrentLobby.GetComponent<ClientCurrentLobby>();
		currentLobby.client = client;
		currentLobby.player1Name.GetComponent<TextMeshProUGUI>().text = client.username;
		currentLobby.startLobbyObject.SetActive(true);

		// Reset error message
		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "";
	}

	private static void HandleJoinLobbyExisting(ClientBehaviour client, MessageHeader header)
	{
		JoinLobbyExistingMessage message = header as JoinLobbyExistingMessage;
		int score1 = Convert.ToInt32(message.score1);
		int score2 = Convert.ToInt32(message.score2);
		string name = Convert.ToString(message.name);

		client.Player = 1;

		if (client.objectReferences == null) client.objectReferences = FindObjectOfType<SceneObjectReferences>();

		if (client.LobbyName == "")
			client.LobbyName = client.objectReferences.JoinLobby.GetComponent<ClientLobby>().LobbyName;

		client.objectReferences.JoinLobby.SetActive(false);
		client.objectReferences.CurrentLobby.SetActive(true);
		ClientCurrentLobby currentLobby = client.objectReferences.CurrentLobby.GetComponent<ClientCurrentLobby>();
		currentLobby.client = client;
		currentLobby.player1Name.GetComponent<TextMeshProUGUI>().text = name;
		currentLobby.player2Name.GetComponent<TextMeshProUGUI>().text = client.username;
		currentLobby.player1Score.GetComponent<TextMeshProUGUI>().text = score1.ToString();
		currentLobby.player2Score.GetComponent<TextMeshProUGUI>().text = score2.ToString();
		currentLobby.startLobbyObject.SetActive(false);
		
		// Reset error message
		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "";
	}

	private static void HandleJoinLobbyFail(ClientBehaviour client, MessageHeader header)
	{
		if (client.objectReferences == null) client.objectReferences = FindObjectOfType<SceneObjectReferences>();

		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Lobby is already full";

		client.LeaveLobby();
	}

	private static void HandleLobbyUpdate(ClientBehaviour client, MessageHeader header)
	{
		LobbyUpdateMessage message = header as LobbyUpdateMessage;
		int score1 = Convert.ToInt32(message.score1);
		int score2 = Convert.ToInt32(message.score2);
		string username = Convert.ToString(message.name);

		if (client.objectReferences == null) client.objectReferences = FindObjectOfType<SceneObjectReferences>();

		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "";

		ClientCurrentLobby currentLobby = client.objectReferences.CurrentLobby.GetComponent<ClientCurrentLobby>();
		currentLobby.player1Name.GetComponent<TextMeshProUGUI>().text = client.username;
		currentLobby.player2Name.GetComponent<TextMeshProUGUI>().text = username;
		currentLobby.player1Score.GetComponent<TextMeshProUGUI>().text = score1.ToString();
		currentLobby.player2Score.GetComponent<TextMeshProUGUI>().text = score2.ToString();

		// If the first player leaves, become the first player
		if (username == "")
		{
			currentLobby.startLobbyObject.SetActive(false);
			client.Player = 0;
		}
		else currentLobby.startLobbyObject.SetActive(true);
	}

	private static void HandleStartGameResponse(ClientBehaviour client, MessageHeader header)
	{
		StartGameResponseMessage message = header as StartGameResponseMessage;
		uint activePlayer = Convert.ToUInt32(message.activePlayer);
		ItemType itemType = (ItemType)Convert.ToUInt32(message.itemId);

		client.objectReferencesTimeNeeded = .2f;
		client.CurrentItem = itemType;
		client.ActivePlayer = activePlayer == client.Player;

		// Open a new scene without closing the server (discard to hide warning about await)
		_ = LoadSceneWithoutClosingAdditiveScenes(SceneManager.GetActiveScene().buildIndex + 1);
	}

	private static void HandleStartGameFail(ClientBehaviour client, MessageHeader header)
	{
		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "ERROR: Game failed to start. Lobby didn't have 2 players.";
	}
	
	private static void HandlePlaceObstacleSuccess(ClientBehaviour client, MessageHeader header)
	{
		PlaceObstacleSuccessMessage message = header as PlaceObstacleSuccessMessage;
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);
		bool removal = Convert.ToBoolean(message.removal);

		// Handle minesweeper and wrecking ball
		if (removal)
		{
			client.objectReferences.InputManager.SelectGridCell(x, y);
			client.objectReferences.InputManager.RemoveItemAtSelectedGridCell();
			if (client.CurrentItem == ItemType.MINESWEEPER)
			{
				client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = 
					"Mine removed! Waiting for other player...";
			}
			else if (client.CurrentItem == ItemType.WRECKINGBALL)
			{
				client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text =
					"Wall removed! Waiting for other player...";
			}
			return;
		}
		else if (client.CurrentItem == ItemType.MINESWEEPER)
		{
			client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = 
				"You used a minesweeper but there was no mine here! Waiting for other player...";
			return;
		}
		else if (client.CurrentItem == ItemType.WRECKINGBALL)
		{
			client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text =
				"You used a wrecking ball but there was no wall here! Waiting for other player...";
			return;
		}
		
		// Handle placement of mines and walls, and when round should start placement of start and finish
		client.objectReferences.InputManager.SelectGridCell(x, y);
		client.objectReferences.InputManager.PlaceItemAtSelectedGridCell(client.CurrentItem);
		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = 
			"Object placed! Waiting for other player...";
	}
	
	private static void HandlePlaceObstacleFail(ClientBehaviour client, MessageHeader header)
	{
		client.objectReferences.InputManager.PlaceItemAtSelectedGridCell(ItemType.FLAG);

		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = client.CurrentItem switch
		{
			ItemType.MINE or ItemType.WALL or ItemType.START or ItemType.FINISH => "There's already something here... Try somewhere else!",
			ItemType.MINESWEEPER => "There is no mine here to remove, but something else... Try somewhere else!",
			ItemType.WRECKINGBALL => "There is no wall here to remove, but something else... Try somewhere else!",
			ItemType.NONE or ItemType.FLAG or _ => "Unknown obstacle placement failure reason (NONE/FLAG/_)"
		};
	}
	
	private static void HandlePlaceNewObstacle(ClientBehaviour client, MessageHeader header)
	{
		PlaceNewObstacleMessage message = header as PlaceNewObstacleMessage;
		uint activePlayer = Convert.ToUInt32(message.activePlayer);
		ItemType itemType = (ItemType)Convert.ToUInt32(message.itemId);

		client.ActivePlayer = activePlayer == client.Player;

		// Change the cursor so both players know what item is being placed
		client.objectReferences.Cursor.SetSprite(client.objectReferences.GamePrefabs.itemVisuals[itemType].cursorSprite);

		// Change the currentItem specifically so the start and finish can be placed on both sides
		client.CurrentItem = itemType;

		// Let the active player know it's their turn
		if (client.ActivePlayer)
		{
			client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Your turn!";
		}
	}
	
	private static void HandleStartRound(ClientBehaviour client, MessageHeader header)
	{
		StartRoundMessage message = header as StartRoundMessage;
		uint activePlayer = Convert.ToUInt32(message.activePlayer);
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);

		client.ActivePlayer = activePlayer == client.Player;

		// Define which player this client is
		client.PlayerFlag = client.Player == 0 ? PlayerFlag.PLAYER1 : PlayerFlag.PLAYER2;

		// Inform the players that the round has started and inform the active player it's their turn
		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = 
			"Round started!" + (client.ActivePlayer ? " Your turn to move!" : " Waiting for other player...");

		// Start round with start coordinates
		client.StartRound(x, y);
	}

	private static void HandlePlayerMoveSuccess(ClientBehaviour client, MessageHeader header)
	{
		PlayerMoveSuccessMessage message = header as PlayerMoveSuccessMessage;
		uint activePlayer = Convert.ToUInt32(message.activePlayer);
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);
		uint otherHealth = Convert.ToUInt32(message.otherHealth);
		uint playerToMove = Convert.ToUInt32(message.playerToMove);

		client.ActivePlayer = activePlayer == client.Player;

		// Move the player
		client.objectReferences.InputManager.SelectGridCell(x, y);
		client.objectReferences.InputManager.MovePlayerToSelectedGridCell((PlayerFlag)playerToMove);

		// Check if the previous player hit a mine
		if (client.ActivePlayer)
		{
			// If we are the active player, check the other lives
			if (client.objectReferences.GameData.OtherLives != otherHealth)
			{
				client.objectReferences.GameData.DecreaseOtherLives();
				client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Other player hit a mine! You get to go twice.";

				// Remove the mine visual
				client.objectReferences.InputManager.RemoveItemAtSelectedGridCell();
			}
			else client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Your turn to move!";
		}
		else
		{
			// If we're not the active player, check our lives
			if (client.objectReferences.GameData.Lives != otherHealth)
			{
				client.objectReferences.GameData.DecreaseLives();
				client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "You hit a mine! Skip a turn.";

				// Remove the mine visual
				client.objectReferences.InputManager.RemoveItemAtSelectedGridCell();
			}
			else client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Waiting for other player...";
		}
	}
	
	private static void HandlePlayerMoveFail(ClientBehaviour client, MessageHeader header)
	{
		PlayerMoveFailMessage message = header as PlayerMoveFailMessage;
		int x = Convert.ToInt32(message.x);
		int y = Convert.ToInt32(message.y);
		MoveFailReason moveFailReason = (MoveFailReason)Convert.ToUInt32(message.moveFailReason);

		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = moveFailReason switch
		{
			MoveFailReason.NOT_ACTIVE => "It's not your turn to move!",
			MoveFailReason.WALL => "There's a wall in the way!",
			MoveFailReason.RANGE => "Can't move there!",
			MoveFailReason.NONE or _ => "Unknown move fail reason (NONE/_)",
		};

		// Place a wall visual if the player tried to move into one
		if (moveFailReason == MoveFailReason.WALL)
		{
			client.objectReferences.InputManager.SelectGridCell(x, y);
			client.objectReferences.InputManager.PlaceItemAtSelectedGridCell(ItemType.WALL);
		}
	}
	
	private static void HandleEndRound(ClientBehaviour client, MessageHeader header)
	{
		EndRoundMessage message = header as EndRoundMessage;
		uint winnerId = Convert.ToUInt32(message.winnerId);

		// Display regular mouse cursor and stop checking for input on the grid
		client.objectReferences.Cursor.SetSprite(null);
		client.CurrentItem = ItemType.NONE;
		client.objectReferences.InputManager.checkInput = false;

		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = client.Player == winnerId ? "You won!" : "You lost.";

		// Enable rematch/leave buttons
		client.objectReferences.EndGame.SetActive(true);
	}
	
	private static void HandleContinueChoiceResponse(ClientBehaviour client, MessageHeader header)
	{
		// Update visual
		client.objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "Other player wants a rematch!";
	}

	private async static void HandleEndGame(ClientBehaviour client, MessageHeader header)
	{
		EndGameMessage message = header as EndGameMessage;

		// Reset the game
		client.RoundStarted = false;

		// Go back to lobby screen
		await LoadSceneWithoutClosingAdditiveScenes(SceneManager.GetActiveScene().buildIndex - 1);

		if (message.rematch)
		{
			JoinLobbyMessage joinLobbyMessage = new()
			{
				name = client.LobbyName,
			};

			client.SendPackedMessage(joinLobbyMessage);
		}
		else client.LobbyName = "";
	}
	#endregion

	private async static UniTask LoadSceneWithoutClosingAdditiveScenes(int sceneBuildIndex)
	{
		Scene sceneToUnload = SceneManager.GetActiveScene();
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);

		await asyncLoad;

		SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));
		// Discard to hide warning about await
		_ = SceneManager.UnloadSceneAsync(sceneToUnload);
	}

	public void LeaveLobby()
	{
		if (objectReferences == null) objectReferences = FindObjectOfType<SceneObjectReferences>();

		// Reset the lobby visuals
		objectReferences.ErrorMessage.GetComponent<TextMeshProUGUI>().text = "";

		// Reset lobby name
		LobbyName = "";

		// Reset current lobby visuals
		if (objectReferences.CurrentLobby.TryGetComponent(out ClientCurrentLobby currentLobby))
		{
			currentLobby.player1Name.GetComponent<TextMeshProUGUI>().text = "";
			currentLobby.player1Score.GetComponent<TextMeshProUGUI>().text = "";
			currentLobby.player2Name.GetComponent<TextMeshProUGUI>().text = "";
			currentLobby.player2Score.GetComponent<TextMeshProUGUI>().text = "";
			currentLobby.startLobbyObject.SetActive(false);
		}

		// Go back to join lobby screen
		objectReferences.CurrentLobby.SetActive(false);
		objectReferences.JoinLobby.SetActive(true);
	}

	private void StartRound(int x, int y)
	{
		RoundStarted = true;

		// Reset the cursor
		objectReferences.Cursor.SetSprite(null);

		// Initialize game data
		objectReferences.GameData.StartRound();

		// Place players at start
		objectReferences.InputManager.SelectGridCell(x, y);
		objectReferences.InputManager.MovePlayerToSelectedGridCell(PlayerFlag.BOTH);
	}
}