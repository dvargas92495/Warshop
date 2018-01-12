using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Z8.Generic;

public class GameClient : MonoBehaviour {

    private static Action onConnect;
    private static NetworkClient client;
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
        PlayerTurnObject[] playerTurnObjects = new PlayerTurnObject[2];
        playerTurnObjects[0] = new PlayerTurnObject(msg.myname);
        playerTurnObjects[1] = new PlayerTurnObject(msg.opponentname);
        for (int i = 0; i < msg.numRobots; i++)
        {
            RobotObject currentRobot = new RobotObject()
            {
                Id = i,
                Name = msg.robotNames[i],
                Health = msg.robotHealth[i],
                Attack = msg.robotAttacks[i],
                Priority = msg.robotPriorities[i]
            };
            currentRobot.Owner = msg.robotIsOpponents[i] ? msg.opponentname : msg.myname;
            int playerIndex = msg.robotIsOpponents[i] ? 1 : 0;
            playerTurnObjects[playerIndex].AddRobot(currentRobot);
        }
        Interpreter.LoadBoard(playerTurnObjects);
    }

    private static void OnTurnEvents(NetworkMessage netMsg)
    {
        Messages.TurnEventsMessage msg = netMsg.ReadMessage<Messages.TurnEventsMessage>();
        List<GameEvent> events = new List<GameEvent>();
        Interpreter.PlayEvents(events);
    }

    public static void SendLocalGameRequest(String[] myRobots, String[] opponentRobots)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        msg.opponentRobots = opponentRobots;
        Send(Messages.START_LOCAL_GAME, msg);
    }

    public static void SendGameRequest(String[] myRobots)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        Send(Messages.START_LOCAL_GAME, msg);
    }
    
    public static void SendSubmitCommands (List<RobotCommand> commands) {
        Messages.SubmitCommandsMessage msg = new Messages.SubmitCommandsMessage();
        Send(Messages.SUBMIT_COMMANDS, msg);
    }
}
