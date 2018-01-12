using System;
using System.Collections;
using System.Collections.Generic;
using Z8.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;

public class App : MonoBehaviour {

    // Use this for initialization
    public static void StartServer()
    {
        GenericOutcome outcome = GameLiftServerAPI.InitSDK();
        GameLiftServerAPI.ProcessReady(new ProcessParameters(
            OnGameSession,
            OnProcessTerminate,
            OnHealthCheck,
            GameConstants.PORT,
            new LogParameters(new List<string>()
            {
              "/local/game/logs",
              "/local/game/error"
            })
        ));
        NetworkServer.RegisterHandler(MsgType.Connect, OnConnect);
        NetworkServer.RegisterHandler(Messages.START_LOCAL_GAME, OnStartLocalGame);
        NetworkServer.RegisterHandler(Messages.START_GAME, OnStartGame);
        NetworkServer.RegisterHandler(Messages.SUBMIT_COMMANDS, OnSubmitCommands);
        NetworkServer.Listen(GameConstants.PORT);
        Console.WriteLine("Listening");
    }

    // begin GameLift callbacks

    static void OnGameSession(GameSession gameSession)
    {
        // game-specific tasks when starting a new game session, such as loading map
        // When ready to receive players
        GenericOutcome activateGameSessionOutcome = GameLiftServerAPI.ActivateGameSession();
    }

    static void OnProcessTerminate()
    {
        // game-specific tasks required to gracefully shut down a game session, 
        // such as notifying players, preserving game state data, and other cleanup
        GenericOutcome ProcessEndingOutcome = GameLiftServerAPI.ProcessEnding();
        GameLiftServerAPI.Destroy();
    }

    static bool OnHealthCheck()
    {
        // complete health evaluation within 60 seconds and set health
        return true;
    }

    // end GameLift callbacks

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
