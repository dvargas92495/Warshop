using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameClient : MonoBehaviour {

    private static NetworkClient client;
    private static string playerSessionId;
    private static string ip;
    private static int port = 0;
    private static Logger log = new Logger(typeof(GameClient));
    private static Dictionary<short, NetworkMessageDelegate> handlers = new Dictionary<short, NetworkMessageDelegate>()
    {
        { MsgType.Connect, OnConnect },
        { Messages.GAME_READY, OnGameReady },
        { Messages.TURN_EVENTS, OnTurnEvents },
        { Messages.WAITING_COMMANDS, OnOpponentWaiting },
        { Messages.SERVER_ERROR, OnServerError }
    };

    public static void Initialize(string playerId, string boardFile) {
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
            Messages.FakeConnectMessage msg = new Messages.FakeConnectMessage();
            msg.boardFile = boardFile;
            App.Receive(MsgType.Connect, msg);
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
        Interpreter.SendPlayerInfo();
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
        Interpreter.uiController.BackToSetup();
        Interpreter.ClientError(msg.serverMessage);
    }

    public static IEnumerator SendCreateGameRequest(string pId, Action callback)
    {
        Messages.CreateGameRequest request = new Messages.CreateGameRequest
        {
            playerId = pId
        };
        UnityWebRequest www = UnityWebRequest.Put(GameConstants.GATEWAY_URL + "/games", JsonUtility.ToJson(request));
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Error creating available games: " + www.uploadHandler.contentType + "\n" + www.downloadHandler.text);
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
            Messages.CreateGameResponse res = JsonUtility.FromJson<Messages.CreateGameResponse>(www.downloadHandler.text);
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
           Debug.LogError("Error finding available games: " + www.uploadHandler.contentType + "\n" + www.downloadHandler.text);
        } else
        {
            Messages.GetGamesResponse res = JsonUtility.FromJson<Messages.GetGamesResponse>(www.downloadHandler.text);
            callback(res.gameSessionIds);
        }
    }

    public static void SendLocalGameRequest(String[] myRobots, String[] opponentRobots, String myname, String opponentname)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.playerSessionId = playerSessionId;
        msg.myRobots = myRobots;
        msg.opponentRobots = opponentRobots;
        msg.myName = myname;
        msg.opponentName = opponentname;
        Send(Messages.START_LOCAL_GAME, msg);
    }

    public static void SendGameRequest(String[] myRobots, String myname)
    {   
        Messages.StartGameMessage msg = new Messages.StartGameMessage();
        msg.playerSessionId = playerSessionId;
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
}
