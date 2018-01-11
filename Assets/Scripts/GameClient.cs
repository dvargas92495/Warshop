using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Z8.Generic;

public class GameClient : MonoBehaviour {

    private static Action onConnect;
    private static NetworkClient client = new NetworkClient();

    public static void Initialize () {
        client.RegisterHandler(MsgType.Connect, OnConnect);
        client.RegisterHandler(Messages.GAME_READY, OnGameReady);
        client.RegisterHandler(Messages.TURN_EVENTS, OnTurnEvents);
        client.Connect(GameConstants.SERVER_IP, GameConstants.PORT);
    }

    private static void OnConnect(NetworkMessage netMsg)
    {
        Debug.Log("Client Connected");
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
        client.Send(Messages.START_LOCAL_GAME, msg);
    }

    public static void SendGameRequest(String[] myRobots)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        client.Send(Messages.START_LOCAL_GAME, msg);
    }
    
    public static void SendSubmitCommands (List<RobotCommand> commands) {
        Messages.SubmitCommandsMessage msg = new Messages.SubmitCommandsMessage();
        client.Send(Messages.SUBMIT_COMMANDS, msg);
    }
}
