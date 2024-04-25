using System.Collections.Generic;

public static class NetworkMessageInfo
{
    public static Dictionary<NetworkMessageType, System.Type> TypeMap = new()
    {
        { NetworkMessageType.HANDSHAKE,                 typeof(HandshakeMessage) },
        { NetworkMessageType.HANDSHAKE_RESPONSE,        typeof(HandshakeResponseMessage) },
        { NetworkMessageType.REGISTER,                  typeof(RegisterMessage) },
        { NetworkMessageType.REGISTER_SUCCESS,          typeof(RegisterSuccessMessage) },
        { NetworkMessageType.REGISTER_FAIL,             typeof(RegisterFailMessage) },
        { NetworkMessageType.LOGIN,                     typeof(LoginMessage) },
        { NetworkMessageType.LOGIN_SUCCESS,             typeof(LoginSuccessMessage) },
        { NetworkMessageType.LOGIN_FAIL,                typeof(LoginFailMessage) },
        { NetworkMessageType.JOIN_LOBBY,                typeof(JoinLobbyMessage) },
        { NetworkMessageType.JOIN_LOBBY_NEW,            typeof(JoinLobbyNewMessage) },
        { NetworkMessageType.JOIN_LOBBY_FAIL,           typeof(JoinLobbyFailMessage) },
        { NetworkMessageType.JOIN_LOBBY_EXISTING,       typeof(JoinLobbyExistingMessage) },
        { NetworkMessageType.LEAVE_LOBBY,               typeof(LeaveLobbyMessage) },
        { NetworkMessageType.LOBBY_UPDATE,              typeof(LobbyUpdateMessage) },
        { NetworkMessageType.START_GAME,                typeof(StartGameMessage) },
        { NetworkMessageType.START_GAME_RESPONSE,       typeof(StartGameResponseMessage) },
        { NetworkMessageType.START_GAME_FAIL,           typeof(StartGameFailMessage) },
        { NetworkMessageType.PLACE_OBSTACLE,            typeof(PlaceObstacleMessage) },
        { NetworkMessageType.PLACE_OBSTACLE_SUCCESS,    typeof(PlaceObstacleSuccessMessage) },
        { NetworkMessageType.PLACE_OBSTACLE_FAIL,       typeof(PlaceObstacleFailMessage) },
        { NetworkMessageType.PLACE_NEW_OBSTACLE,        typeof(PlaceNewObstacleMessage) },
        { NetworkMessageType.START_ROUND,               typeof(StartRoundMessage) },
        { NetworkMessageType.PLAYER_MOVE,               typeof(PlayerMoveMessage) },
        { NetworkMessageType.PLAYER_MOVE_SUCCESS,       typeof(PlayerMoveSuccessMessage) },
        { NetworkMessageType.PLAYER_MOVE_FAIL,          typeof(PlayerMoveFailMessage) },
        { NetworkMessageType.END_ROUND,                 typeof(EndRoundMessage) },
        { NetworkMessageType.END_GAME,                  typeof(EndGameMessage) },
        { NetworkMessageType.CONTINUE_CHOICE,           typeof(ContinueChoiceMessage) },
        { NetworkMessageType.CONTINUE_CHOICE_RESPONSE,  typeof(ContinueChoiceResponseMessage) },
        { NetworkMessageType.CHAT_MESSAGE,              typeof(ChatMessage) },
        { NetworkMessageType.PING,                      typeof(PingMessage) },
        { NetworkMessageType.PONG,                      typeof(PongMessage) },
    };
}