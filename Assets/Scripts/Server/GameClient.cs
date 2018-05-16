using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameClient : MonoBehaviour {

    public static string username;
    private static NetworkClient client;
    private static string playerSessionId;
    private static string ip;
    private static int port = 0;
    private static Logger log = new Logger(typeof(GameClient));
    private static Dictionary<short, NetworkMessageDelegate> handlers = new Dictionary<short, NetworkMessageDelegate>()
    {
        { MsgType.Connect, OnConnect },
        { MsgType.Disconnect, OnDisconnect },
        { MsgType.Error, OnNetworkError },
        { Messages.GAME_READY, OnGameReady },
        { Messages.TURN_EVENTS, OnTurnEvents },
        { Messages.WAITING_COMMANDS, OnOpponentWaiting },
        { Messages.SERVER_ERROR, OnServerError }
    };

    public static void ConnectToGameServer() {
        if (GameConstants.USE_SERVER)
        {
            try
            {
                client = new NetworkClient();
                foreach (KeyValuePair<short, NetworkMessageDelegate> pair in handlers)
                {
                    client.RegisterHandler(pair.Key, pair.Value);
                }
                client.Connect(ip, port);
                log.Info("Attempting to connect to " + ip + ":" + port);
            } catch (Exception e)
            {
                log.Fatal(e);
                Interpreter.ClientError("An unexpected error occurred! Please notify the developers.");
            }
        } else
        {
            App.Receive(MsgType.Connect, Messages.EMPTY);
        }
    }

    private static void Send(short msgType, MessageBase message)
    {
        if (GameConstants.USE_SERVER)
        {
            client.Send(msgType, message);
        } else
        {
            App.Receive(msgType, message);
        }
    }

    internal static void Receive(short msgType, MessageBase message)
    {
        NetworkMessage netMsg = new NetworkMessage();
        NetworkWriter writer = new NetworkWriter();
        message.Serialize(writer);
        NetworkReader reader = new NetworkReader(writer);
        netMsg.msgType = msgType;
        netMsg.reader = reader;
        NetworkMessageDelegate handler;
        if (handlers.TryGetValue(msgType, out handler))
        {
            handler(netMsg);
        }
    }

    private static void OnConnect(NetworkMessage netMsg)
    {
        log.Info("Connected");
        Interpreter.ClientError("");
        SendAcceptPlayerSessionRequest();
    }

    private static void OnDisconnect(NetworkMessage netMsg)
    {
        log.Info("Disconnected");
        Interpreter.ClientError("Disconnected, attempting to reconnect");
        client.Connect(ip, port);
    }

    private static void OnNetworkError(NetworkMessage netMsg)
    {
        log.Info("Network Error");
    }

    private static void OnGameReady(NetworkMessage netMsg)
    {
        Messages.GameReadyMessage msg = netMsg.ReadMessage<Messages.GameReadyMessage>();
        log.Info("Received Game Information");
        Interpreter.LoadBoard(msg.myTeam, msg.opponentTeam, msg.opponentname, msg.board, msg.isPrimary);
    }

    private static void OnTurnEvents(NetworkMessage netMsg)
    {
        Messages.TurnEventsMessage msg = netMsg.ReadMessage<Messages.TurnEventsMessage>();
        List<GameEvent> events = new List<GameEvent>(msg.events);
        Interpreter.PlayEvents(events, msg.turn);
    }

    private static void OnOpponentWaiting(NetworkMessage netMsg)
    {
        Interpreter.uiController.LightUpPanel(!GameConstants.LOCAL_MODE, false);
    }

    private static void OnServerError(NetworkMessage netMsg)
    {
        Messages.ServerErrorMessage msg = netMsg.ReadMessage<Messages.ServerErrorMessage>();
        log.Fatal(msg.serverMessage + ": " + msg.exceptionType + " - " + msg.exceptionMessage);
        Interpreter.ClientError(msg.serverMessage);
    }

    public static IEnumerator SendCreateGameRequest(string pId, Action callback)
    {
        username = pId;
        Messages.CreateGameRequest request = new Messages.CreateGameRequest
        {
            playerId = pId
        };
        UnityWebRequest www = UnityWebRequest.Put(GameConstants.GATEWAY_URL + "/games", JsonUtility.ToJson(request));
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            log.Fatal("Error creating new game: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.CreateGameResponse res = JsonUtility.FromJson<Messages.CreateGameResponse>(www.downloadHandler.text);
            if (res.IsError)
            {
                log.Fatal(res.ErrorMessage);
            }
            else
            {
                playerSessionId = res.playerSessionId;
                ip = res.ipAddress;
                port = res.port;
                callback();
            }
        }
    }

    public static IEnumerator SendJoinGameRequest(string pId, string gId, Action callback)
    {
        username = pId;
        Messages.JoinGameRequest request = new Messages.JoinGameRequest
        {
            playerId = pId,
            gameSessionId = gId
        };
        UnityWebRequest www = UnityWebRequest.Put(GameConstants.GATEWAY_URL + "/games", JsonUtility.ToJson(request));
        www.method = "POST"; //LOL you freaking suck Unity
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            log.Fatal("Error joining available game: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.JoinGameResponse res = JsonUtility.FromJson<Messages.JoinGameResponse>(www.downloadHandler.text);
            playerSessionId = res.playerSessionId;
            ip = res.ipAddress;
            port = res.port;
            callback();
        }
    }

    public static IEnumerator SendFindAvailableGamesRequest(Action<string[]> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(GameConstants.GATEWAY_URL + "/games");
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
           log.Fatal("Error finding available games: " + www.uploadHandler.contentType + "\n" + www.downloadHandler.text);
        } else
        {
            Messages.GetGamesResponse res = JsonUtility.FromJson<Messages.GetGamesResponse>(www.downloadHandler.text);
            callback(res.gameSessionIds);
        }
    }

    public static void SendAcceptPlayerSessionRequest()
    {
        Messages.AcceptPlayerSessionMessage msg = new Messages.AcceptPlayerSessionMessage();
        msg.playerSessionId = playerSessionId;
        Send(Messages.ACCEPT_PLAYER_SESSION, msg);

    }

    public static void SendLocalGameRequest(String[] myRobots, String[] opponentRobots, String myname, String opponentname)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        msg.opponentRobots = opponentRobots;
        msg.myName = myname;
        msg.opponentName = opponentname;
        Send(Messages.START_LOCAL_GAME, msg);
    }

    public static void SendGameRequest(String[] myRobots, String myname)
    {   
        Messages.StartGameMessage msg = new Messages.StartGameMessage();
        msg.myName = myname;
        msg.myRobots = myRobots;
        Send(Messages.START_GAME, msg);
    }
    
    public static void SendSubmitCommands (List<Command> commands, string owner) {
        Messages.SubmitCommandsMessage msg = new Messages.SubmitCommandsMessage();
        msg.commands = commands.ToArray();
        msg.owner = owner;
        Send(Messages.SUBMIT_COMMANDS, msg);
    }

    public static void SendEndGameRequest ()
    {
        Send(Messages.END_GAME, new Messages.EndGameMessage());
    }
}
