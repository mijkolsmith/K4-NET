using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using System;
using Unity.Networking.Transport.Utilities;
using TMPro;
using UnityEngine.SceneManagement;

public delegate void ClientMessageHandler(ClientBehaviour client, MessageHeader header);

public class ClientBehaviour : MonoBehaviour
{
    static Dictionary<NetworkMessageType, ClientMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ClientMessageHandler> {
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

    public ObjectReferences objectReferences;

    public string username;
    public string otherUsername;
    uint player;

    void Start()
    {
        DontDestroyOnLoad(this);
        m_Driver = NetworkDriver.Create(new ReliableUtility.Parameters { WindowSize = 32 });
        m_Pipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        m_Connection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 1511;

        m_Connection = m_Driver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            if (!Done)
            {
                Debug.Log("Something went wrong during connect");
                objectReferences.errorMessage.GetComponent<TextMeshProUGUI>().text = "Something went wrong during connect";
            }
            return;
        }
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
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
                m_Connection = default(NetworkConnection);
            }
        }
    }

    public void SendPackedMessage(MessageHeader header)
    {
        DataStreamWriter writer;
        int result = m_Driver.BeginSend(m_Pipeline, m_Connection, out writer);

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

    // TODO: Static handler functions
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
    //      - Start game response       (WIP)
    //      - Place obstacle success    (WIP)
    //      - Place obstacle fail       (WIP)
    //      - Place new obstacle        (WIP)
    //      - Start round               (WIP)
    //      - Player move success       (WIP)
    //      - Player move fail          (WIP)
    //      - End round                 (WIP)
    //      - End game                  (WIP)
    //      - Continue choice response  (WIP)

    private static void HandlePing(ClientBehaviour client, MessageHeader header)
    {
        PongMessage pongMessage = new PongMessage { };

        client.SendPackedMessage(pongMessage);
    }

    private static void HandleServerHandshakeResponse(ClientBehaviour client, MessageHeader header)
    {
        Debug.Log("Successfully Handshaked");
        client.objectReferences.loginRegister.SetActive(true);
        client.objectReferences.errorMessage.GetComponent<TextMeshProUGUI>().text = "Please Register or Login";
    }

    private static void HandleRegisterSuccess(ClientBehaviour client, MessageHeader header)
	{
        Debug.Log("register success");
        client.objectReferences.errorMessage.GetComponent<TextMeshProUGUI>().text = "Register Success, please Login";
	}

    private static void HandleRegisterFail(ClientBehaviour client, MessageHeader header)
	{
        Debug.Log("register fail");
        client.objectReferences.errorMessage.GetComponent<TextMeshProUGUI>().text = "Register fail, please try again";
    }

    private static void HandleLoginSuccess(ClientBehaviour client, MessageHeader header)
	{
        Debug.Log("login success");

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private static void HandleLoginFail(ClientBehaviour client, MessageHeader header)
	{
        Debug.Log("login fail");

        client.objectReferences.errorMessage.GetComponent<TextMeshProUGUI>().text = "Login fail, please try again";
    }

    private static void HandleJoinLobbyNew(ClientBehaviour client, MessageHeader header)
	{
        client.player = 0;

        if (client.objectReferences == null) client.objectReferences = FindObjectOfType<ObjectReferences>();

        string lobbyName = client.objectReferences.lobbyName;

        client.objectReferences.joinLobby.SetActive(false);
        client.objectReferences.currentLobby.SetActive(true);
        CurrentLobby currentLobby = client.objectReferences.currentLobby.GetComponent<CurrentLobby>();
        currentLobby.lobbyNameObject.GetComponent<TextMeshProUGUI>().text = lobbyName;
        currentLobby.client = client;
        currentLobby.player1Name.GetComponent<TextMeshProUGUI>().text = client.username;
    }

    private static void HandleJoinLobbyExisting(ClientBehaviour client, MessageHeader header)
	{
        JoinLobbyExistingMessage message = header as JoinLobbyExistingMessage;
        int score1 = Convert.ToInt32(message.score1);
        int score2 = Convert.ToInt32(message.score2);
        string name = Convert.ToString(message.name);

        client.player = 1;

        if (client.objectReferences == null) client.objectReferences = FindObjectOfType<ObjectReferences>();

        string lobbyName = client.objectReferences.joinLobby.GetComponent<Lobby>().name;
        client.objectReferences.joinLobby.SetActive(false);
        client.objectReferences.currentLobby.SetActive(true);
        CurrentLobby currentLobby = client.objectReferences.currentLobby.GetComponent<CurrentLobby>();
        currentLobby.lobbyNameObject.GetComponent<TextMeshProUGUI>().text = lobbyName;
        currentLobby.player1Name.GetComponent<TextMeshProUGUI>().text = name;
        currentLobby.player2Name.GetComponent<TextMeshProUGUI>().text = client.username;
        currentLobby.player1Score.GetComponent<TextMeshProUGUI>().text = score1.ToString();
        currentLobby.player2Score.GetComponent<TextMeshProUGUI>().text = score2.ToString();
    }
    
    private static void HandleJoinLobbyFail(ClientBehaviour client, MessageHeader header)
	{
        if (client.objectReferences == null) client.objectReferences = FindObjectOfType<ObjectReferences>();

        client.objectReferences.errorMessage.GetComponent<TextMeshProUGUI>().text = "Lobby full/player left lobby, please try again";
        
        client.objectReferences.currentLobby.SetActive(false); 
        client.objectReferences.joinLobby.SetActive(true);
    }

    private static void HandleLobbyUpdate(ClientBehaviour client, MessageHeader header)
    {
        LobbyUpdateMessage message = header as LobbyUpdateMessage;
        int score1 = Convert.ToInt32(message.score1);
        int score2 = Convert.ToInt32(message.score2);
        string username = Convert.ToString(message.name);

        if (client.objectReferences == null) client.objectReferences = FindObjectOfType<ObjectReferences>();

        CurrentLobby currentLobby = client.objectReferences.currentLobby.GetComponent<CurrentLobby>();
        currentLobby.player2Name.GetComponent<TextMeshProUGUI>().text = username;
        currentLobby.player1Score.GetComponent<TextMeshProUGUI>().text = score1.ToString();
        currentLobby.player2Score.GetComponent<TextMeshProUGUI>().text = score2.ToString();
        currentLobby.startLobbyObject.SetActive(true);
    }

    private static void HandleStartGameResponse(ClientBehaviour client, MessageHeader header)
	{
        StartGameResponseMessage message = header as StartGameResponseMessage;
        
        if (Convert.ToInt32(message.startPlayer) == client.player)
		{
            Debug.Log(Convert.ToInt32(message.startPlayer));
		}
        uint obstacleId = Convert.ToUInt32(message.obstacleId);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        client.objectReferences = FindObjectOfType<ObjectReferences>();
    }
    
    private static void HandlePlaceObstacleSuccess(ClientBehaviour client, MessageHeader header)
	{
        PlaceObstacleSuccessMessage message = header as PlaceObstacleSuccessMessage;
	}
    
    private static void HandlePlaceObstacleFail(ClientBehaviour client, MessageHeader header)
	{
        PlaceObstacleFailMessage message = header as PlaceObstacleFailMessage;
    }
    
    private static void HandlePlaceNewObstacle(ClientBehaviour client, MessageHeader header)
	{
        PlaceNewObstacleMessage message = header as PlaceNewObstacleMessage;
    }
    
    private static void HandleStartRound(ClientBehaviour client, MessageHeader header)
	{
        StartRoundMessage message = header as StartRoundMessage;

    }
    
    private static void HandlePlayerMoveSuccess(ClientBehaviour client, MessageHeader header)
	{
        PlayerMoveSuccessMessage message = header as PlayerMoveSuccessMessage;
    }
    
    private static void HandlePlayerMoveFail(ClientBehaviour client, MessageHeader header)
	{
        PlayerMoveFailMessage message = header as PlayerMoveFailMessage;
    }
    
    private static void HandleEndRound(ClientBehaviour client, MessageHeader header)
	{
        EndRoundMessage message = header as EndRoundMessage;
    }
    
    private static void HandleEndGame(ClientBehaviour client, MessageHeader header)
	{
        EndGameMessage message = header as EndGameMessage;
    }
    
    private static void HandleContinueChoiceResponse(ClientBehaviour client, MessageHeader header)
	{
        ContinueChoiceResponseMessage message = header as ContinueChoiceResponseMessage;

    }
}