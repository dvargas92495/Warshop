using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Amazon.GameLift;

public class GameClient : MonoBehaviour {

    private static NetworkClient client;
    private static AmazonGameLiftClient amazonClient;
    private static Dictionary<short, NetworkMessageDelegate> handlers = new Dictionary<short, NetworkMessageDelegate>()
    {
        { MsgType.Connect, OnConnect },
        { Messages.GAME_READY, OnGameReady },
        { Messages.TURN_EVENTS, OnTurnEvents }
    };

    public static void Initialize () {
        if (GameConstants.USE_SERVER)
        {
            client = new NetworkClient();
            foreach (KeyValuePair<short, NetworkMessageDelegate> pair in handlers)
            {
                client.RegisterHandler(pair.Key, pair.Value);
            }
            client.Connect(GameConstants.SERVER_IP, GameConstants.PORT);
            Logger.ClientLog("Attempting to connect to " + GameConstants.SERVER_IP + ":" + GameConstants.PORT);
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
        Logger.ClientLog("Connected");
        Interpreter.SendPlayerInfo();
    }

    private static void OnGameReady(NetworkMessage netMsg)
    {
        Messages.GameReadyMessage msg = netMsg.ReadMessage<Messages.GameReadyMessage>();
        Game.Player[] playerTurnObjects = new Game.Player[2];
        playerTurnObjects[0] = new Game.Player(msg.myTeam, msg.myname);
        playerTurnObjects[1] = new Game.Player(msg.opponentTeam, msg.opponentname);
        Logger.ClientLog("Received Game Information");
        Interpreter.LoadBoard(playerTurnObjects, msg.board);
    }

    private static void OnTurnEvents(NetworkMessage netMsg)
    {
        Messages.TurnEventsMessage msg = netMsg.ReadMessage<Messages.TurnEventsMessage>();
        List<GameEvent> events = new List<GameEvent>(msg.events);
        Interpreter.PlayEvents(events);
    }

    public static void SendLocalGameRequest(String[] myRobots, String[] opponentRobots, String boardFile)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        msg.opponentRobots = opponentRobots;
        msg.boardFile = boardFile;
        Send(Messages.START_LOCAL_GAME, msg);
    }

    public static void SendGameRequest(String[] myRobots, String boardFile)
    {
        Messages.StartGameMessage msg = new Messages.StartGameMessage();
        msg.myRobots = myRobots;
        msg.boardFile = boardFile;
        Send(Messages.START_GAME, msg);
    }
    
    public static void SendSubmitCommands (List<Command> commands) {
        Messages.SubmitCommandsMessage msg = new Messages.SubmitCommandsMessage();
        msg.commands = commands.ToArray();
        msg.owner = "ME";
        Send(Messages.SUBMIT_COMMANDS, msg);
    }
}
