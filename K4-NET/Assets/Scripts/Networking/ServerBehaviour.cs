using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Networking.Transport.Utilities;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Cysharp.Threading.Tasks;
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
    static Dictionary<NetworkMessageType, ServerMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, ServerMessageHandler> {
            { NetworkMessageType.HANDSHAKE, HandleHandshake },
            { NetworkMessageType.CHAT_MESSAGE, HandleChatMessage },
            { NetworkMessageType.REGISTER, HandleRegister },
            { NetworkMessageType.LOGIN, HandleLogin },
            { NetworkMessageType.JOIN_LOBBY, HandleJoinLobby },
            { NetworkMessageType.START_GAME, HandleStartGame },
            { NetworkMessageType.PLACE_OBSTACLE, HandlePlaceObstacle },
            { NetworkMessageType.PLAYER_MOVE, HandlePlayerMove },
            { NetworkMessageType.CONTINUE_CHOICE, HandleContinueChoice },
        };

    public NetworkDriver m_Driver;
    public NetworkPipeline m_Pipeline;
    private NativeList<NetworkConnection> m_Connections;

    private Dictionary<NetworkConnection, int> idList = new Dictionary<NetworkConnection, int>();
    private Dictionary<int, string> nameList = new Dictionary<int, string>();
    private Dictionary<NetworkConnection, PingPong> pongDict = new Dictionary<NetworkConnection, PingPong>();
    private Dictionary<string, List<int>> lobbyList = new Dictionary<string, List<int>>();

    public ChatCanvas chat;

    private string PhpConnectionID;

    List<Id> ids = null;

    async void Start()
    {
        var x = await request<List<Id>>("https://studenthome.hku.nl/~michael.smith/K4/server_login.php?id=1&pw=A5FJDKeSdKI49dnR49JFRIVJWJf92JF9R8Gg98GG3");
        Debug.Log(x[0].id);

        List<User> json = await request<List<User>>("https://studenthome.hku.nl/~michael.smith/K4/user_register.php?PHPSESSID=" + x[0].id + "&un=test1&pw=test1");
        
        int playerId = Convert.ToInt32(json[0].id);
        string playerName = json[0].username;

        Debug.Log(playerId);
        Debug.Log(playerName);

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
                    // First UInt is always message type
                    NetworkMessageType msgType = (NetworkMessageType)stream.ReadUInt();

                    // Create instance and deserialize
                    MessageHeader header = (MessageHeader)System.Activator.CreateInstance(NetworkMessageInfo.TypeMap[msgType]);
                    header.DeserializeObject(ref stream);

                    if (networkMessageHandlers.ContainsKey(msgType)) {
                        try {
                            networkMessageHandlers[msgType].Invoke(this, m_Connections[i], header);
                        }
                        catch {
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

                        /*uint destroyId = playerInstances[m_Connections[i]].networkId;
                        networkManager.DestroyWithId(destroyId);
                        playerInstances.Remove(m_Connections[i]);*/

                        string name = pongDict[m_Connections[i]].name;
                        pongDict.Remove(m_Connections[i]);

                        // Disconnect this player
                        m_Connections[i].Disconnect(m_Driver);

                        // Build messages
                        string msg = $"{name} has been Disconnected (connection timed out)";
                        chat.NewMessage(msg, ChatCanvas.leaveColor);

                        /*ChatMessage quitMsg = new ChatMessage
                        {
                            message = msg,
                            messageType = NetworkMessageType.QUIT
                        };

                        DestroyMessage destroyMsg = new DestroyMessage
                        {
                            networkId = destroyId
                        };

                        // Broadcast
                        SendBroadcast(quitMsg);
                        SendBroadcast(destroyMsg, m_Connections[i]);*/

                        // Clean up
                        m_Connections[i] = default;
                    }
                    else
                    {
                        pongDict[m_Connections[i]].status -= 1;
                        PingMessage pingMsg = new PingMessage();
                        SendUnicast(m_Connections[i], pingMsg);
                    }
                }
            }
            else if (idList.ContainsKey(m_Connections[i]))
            { //means they've succesfully handshaked
                PingPong ping = new PingPong();
                ping.lastSendTime = Time.time;
                ping.status = 3;    // 3 retries
                ping.name = nameList[idList[m_Connections[i]]];
                pongDict.Add(m_Connections[i], ping);

                PingMessage pingMsg = new PingMessage();
                SendUnicast(m_Connections[i], pingMsg);
            }
        }
    }

    public void SendUnicast(NetworkConnection connection, MessageHeader header, bool realiable = true)
    {
        DataStreamWriter writer;
        int result = m_Driver.BeginSend(realiable ? m_Pipeline : NetworkPipeline.Null, connection, out writer);
        if (result == 0)
        {
            header.SerializeObject(ref writer);
            m_Driver.EndSend(writer);
        }
    }
    public void SendBroadcast(MessageHeader header, NetworkConnection toExclude = default, bool realiable = true)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated || m_Connections[i] == toExclude)
                continue;

            DataStreamWriter writer;
            int result = m_Driver.BeginSend(realiable ? m_Pipeline : NetworkPipeline.Null, m_Connections[i], out writer);
            if (result == 0)
            {
                header.SerializeObject(ref writer);
                m_Driver.EndSend(writer);
            }
        }
    }

    async private UniTask<T> request<T>(string url)
	{
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();

        //The null value handling shouldn't be necessary using the COALESCE sql function, still I'm ignoring null values here so I don't get errors
        var result = JsonConvert.DeserializeObject<T>(request.downloadHandler.text, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        return result;
    }

    // TODO: Static handler functions
    //      - Handshake                     (DONE)
    //      - Chat message                  (WIP)
    //      - Register                      (DONE)
    //      - Login                         (DONE)
    //      - Join lobby                    (DONE)
    //      - Start game                    (WIP)
    //      - Place obstacle                (WIP)
    //      - Player move                   (WIP)
    //      - Continue Choice               (WIP)

    static async void HandleHandshake(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
    {
        // Connect to database
        var json = await serv.request<List<Id>>("https://studenthome.hku.nl/~michael.smith/K4/server_login.php?id=1&pw=A5FJDKeSdKI49dnR49JFRIVJWJf92JF9R8Gg98GG3");
        serv.PhpConnectionID = json[0].id;

        HandshakeResponseMessage responseMsg = new HandshakeResponseMessage { };

        serv.SendUnicast(con, responseMsg);
    }

    static async void HandleRegister(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
        RegisterMessage message = header as RegisterMessage;

        var json = await serv.request<List<User>>("https://studenthome.hku.nl/~michael.smith/K4/user_register.php?PHPSESSID=" + serv.PhpConnectionID + "&un=" + message.username + "&pw=" + message.password);
        int playerId = Convert.ToInt32(json[0].id);
        string playerName = json[0].username;

        // Add to id list & name list
        if (playerId != 0 && playerName != null)
        {
            serv.idList.Add(con, playerId);
            serv.nameList.Add(playerId, playerName);

            RegisterSuccessMessage registerSuccessMessage = new RegisterSuccessMessage { };
            serv.SendUnicast(con, registerSuccessMessage);
        }
        else
		{
            // Something went wrong with the query
            RegisterFailMessage registerFailMessage = new RegisterFailMessage { };
            serv.SendUnicast(con, registerFailMessage);
        }
    }

    static async void HandleLogin(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
        LoginMessage message = header as LoginMessage;

        var json = await serv.request<List<User>>("https://studenthome.hku.nl/~michael.smith/K4/user_login.php?PHPSESSID=" + serv.PhpConnectionID + "&un=" + message.username + "&pw=" + message.password);
        int playerId = Convert.ToInt32(json[0].id);
        string playerName = json[0].username;

        // Add to id list & name list
        if (playerId != 0 && playerName != null)
        {
            serv.idList.Add(con, playerId);
            serv.nameList.Add(playerId, playerName);

            LoginSuccessMessage loginSuccessMessage = new LoginSuccessMessage { };
            serv.SendUnicast(con, loginSuccessMessage);
        }
        else
        {
            // Something went wrong with the query
            LoginFailMessage loginFailMessage = new LoginFailMessage { };
            serv.SendUnicast(con, loginFailMessage);
        }
    }

    static async void HandleJoinLobby(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
        JoinLobbyMessage message = header as JoinLobbyMessage;

		// Check if lobby exists
		if (!serv.lobbyList.ContainsKey(message.name))
		{
			// Player joins new lobby
			serv.lobbyList.Add(message.name, new List<int>() { serv.idList[con] });
			JoinLobbyNewMessage joinLobbyNewMessage = new JoinLobbyNewMessage { };
			serv.SendUnicast(con, joinLobbyNewMessage);
		}
		else if (serv.lobbyList[message.name].Count == 1)
		{
			// Player joins existing lobby
			// Get the scores of the two players against each other
			var json = await serv.request<List<UserScore>>("https://studenthome.hku.nl/~michael.smith/K4/score_get.php?PHPSESSID=" + serv.PhpConnectionID + "&player1=" + serv.lobbyList[message.name][0] + "&player2=" + serv.idList[con]);
            int score1 = Convert.ToInt32(json[0].score);
            int score2 = Convert.ToInt32(json[1].score);

            if (score1 != 0 || score2 != 0)
            {
                // Something went wrong with the query
                JoinLobbyExistingMessage joinLobbyExistingMessage = new JoinLobbyExistingMessage
                {
                    score1 = score1,
                    score2 = score2
                };
                serv.SendUnicast(con, joinLobbyExistingMessage);
            }
            else //TODO: Make this one piece of code
			{
                JoinLobbyFailMessage joinLobbyFailMessage = new JoinLobbyFailMessage() { };
                serv.SendUnicast(con, joinLobbyFailMessage);
            }
		}
        else
		{
            JoinLobbyFailMessage joinLobbyFailMessage = new JoinLobbyFailMessage() { };
            serv.SendUnicast(con, joinLobbyFailMessage);
        }
	}
    
    static void HandleStartGame(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
        StartGameMessage message = header as StartGameMessage;
	}
    
    static void HandlePlaceObstacle(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
        PlaceObstacleMessage message = header as PlaceObstacleMessage;
	}
    
    static void HandlePlayerMove(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
        PlayerMoveMessage message = header as PlayerMoveMessage;
	}
    
    static void HandleContinueChoice(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
	{
        ContinueChoiceMessage message = header as ContinueChoiceMessage;
	}

    static void HandleChatMessage(ServerBehaviour serv, NetworkConnection con, MessageHeader header)
    {
        ChatMessage message = header as ChatMessage;
    }
}