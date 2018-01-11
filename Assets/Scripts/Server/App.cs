using System;
using System.Collections;
using System.Collections.Generic;
using Z8.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class App : MonoBehaviour {

    // Use this for initialization
    public static void StartServer()
    {
        NetworkServer.RegisterHandler(MsgType.Connect, OnConnect);
        NetworkServer.RegisterHandler(Messages.START_LOCAL_GAME, OnStartLocalGame);
        NetworkServer.RegisterHandler(Messages.START_GAME, OnStartGame);
        NetworkServer.RegisterHandler(Messages.SUBMIT_COMMANDS, OnSubmitCommands);
        NetworkServer.Listen(GameConstants.PORT);
        Console.WriteLine("Listening");
    }

    private static void OnConnect(NetworkMessage netMsg)
    {
        Console.WriteLine("Client Connected");
    }

    private static void OnStartLocalGame(NetworkMessage netMsg)
    {
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.myname = "ME";
        resp.opponentname = "OPPONENT";
        resp.numRobots = msg.myRobots.Length + msg.opponentRobots.Length;
        resp.robotNames = new String[resp.numRobots];
        resp.robotHealth = new int[resp.numRobots];
        resp.robotAttacks = new int[resp.numRobots];
        resp.robotPriorities = new int[resp.numRobots];
        resp.robotIsOpponents = new bool[resp.numRobots];
        for (int i = 0; i < msg.myRobots.Length; i++)
        {
            resp.robotNames[i] = msg.myRobots[i];
            resp.robotHealth[i] = 1;
            resp.robotAttacks[i] = 2;
            resp.robotPriorities[i] = 5;
            resp.robotIsOpponents[i] = false;
        }
        for (int i = msg.myRobots.Length; i < resp.numRobots; i++)
        {
            resp.robotNames[i] = msg.opponentRobots[i - msg.myRobots.Length];
            resp.robotHealth[i] = 1;
            resp.robotAttacks[i] = 2;
            resp.robotPriorities[i] = 5;
            resp.robotIsOpponents[i] = true;
        }
        netMsg.conn.Send(Messages.GAME_READY, resp);
    }

    private static void OnStartGame(NetworkMessage netMsg)
    {
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.myname = "ME";
        resp.opponentname = "YOU";
        netMsg.conn.Send(Messages.GAME_READY, resp);
    }

    private static void OnSubmitCommands(NetworkMessage netMsg)
    {
        Messages.SubmitCommandsMessage msg = netMsg.ReadMessage<Messages.SubmitCommandsMessage>();
        Messages.TurnEventsMessage resp = new Messages.TurnEventsMessage();
        netMsg.conn.Send(Messages.TURN_EVENTS, resp);
    }
}
