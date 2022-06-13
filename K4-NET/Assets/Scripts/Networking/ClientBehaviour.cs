using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Networking.Transport.Utilities;
using System;


public delegate void ClientMessageHandler(ClientBehaviour client, MessageHeader header);

public class ClientBehaviour : MonoBehaviour
{

    static Dictionary<NetworkMessageType, ClientMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ClientMessageHandler> {
            { NetworkMessageType.HANDSHAKE_RESPONSE, HandleServerHandshakeResponse },
            { NetworkMessageType.REGISTER_SUCCESS, HandleRegisterSuccess },
            { NetworkMessageType.REGISTER_FAIL, HandleRegisterFail },
            { NetworkMessageType.LOGIN_SUCCESS, HandleLoginSuccess },
            { NetworkMessageType.LOGIN_FAIL, HandleLoginFail },
            { NetworkMessageType.JOIN_LOBBY_NEW, HandleJoinLobbyNew },
            { NetworkMessageType.JOIN_LOBBY_EXISTING, HandleJoinLobbyExisting },
            { NetworkMessageType.JOIN_LOBBY_FAIL, HandleJoinLobbyFail },
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

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Pipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        m_Connection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 9000;
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
                Debug.Log("Something went wrong during connect");
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
                // First UInt is always message type (this is our own first design choice)
                NetworkMessageType msgType = (NetworkMessageType)stream.ReadUInt();

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

                m_Connection.Disconnect(m_Driver);
                m_Connection = default(NetworkConnection);
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
            m_Driver.EndSend(writer);
        }
        else
        {
            Debug.LogError($"Could not wrote message to driver: {result}", this);
        }
    }

    // TODO: Static handler functions
    //      - Server handshake response (DONE)
    //      - Register success          (WIP)
    //      - Register fail             (WIP)
    //      - Login success             (WIP)
    //      - Login fail                (WIP)
    //      - Join lobby new            (WIP)
    //      - Join lobby existing       (WIP)
    //      - Join lobby fail           (WIP)
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

    private static void HandleServerHandshakeResponse(ClientBehaviour client, MessageHeader header)
    {
        HandshakeResponseMessage message = header as HandshakeResponseMessage;
        Debug.Log("Successfully Handshaked");

        //TODO: Show the register/login screen
    }

    private static void HandleRegisterSuccess(ClientBehaviour client, MessageHeader header)
	{
        RegisterSuccessMessage message = header as RegisterSuccessMessage;
	}

    private static void HandleRegisterFail(ClientBehaviour client, MessageHeader header)
	{
        RegisterFailMessage message = header as RegisterFailMessage;
        // Retry register
    }

    private static void HandleLoginSuccess(ClientBehaviour client, MessageHeader header)
	{
        LoginSuccessMessage message = header as LoginSuccessMessage;
    }

    private static void HandleLoginFail(ClientBehaviour client, MessageHeader header)
	{
        LoginFailMessage message = header as LoginFailMessage;
        // Retry login
    }

    private static void HandleJoinLobbyNew(ClientBehaviour client, MessageHeader header)
	{
        JoinLobbyNewMessage message = header as JoinLobbyNewMessage;
    }

    private static void HandleJoinLobbyExisting(ClientBehaviour client, MessageHeader header)
	{
        JoinLobbyExistingMessage message = header as JoinLobbyExistingMessage;
        int score1 = Convert.ToInt32(message.score1);
        int score2 = Convert.ToInt32(message.score1);
    }
    
    private static void HandleJoinLobbyFail(ClientBehaviour client, MessageHeader header)
	{
        JoinLobbyFailMessage message = header as JoinLobbyFailMessage;
        // Retry joining lobby
    }
    
    private static void HandleStartGameResponse(ClientBehaviour client, MessageHeader header)
	{
        StartGameResponseMessage message = header as StartGameResponseMessage;
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