using System;
using System.Collections;
using System.Collections.Generic;
using Z8.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;

public class App {

    private static Game appgame;

    private static Dictionary<short, NetworkMessageDelegate> handlers = new Dictionary<short, NetworkMessageDelegate>()
    {
        { MsgType.Connect, OnConnect },
        { Messages.START_LOCAL_GAME, OnStartLocalGame },
        { Messages.START_GAME, OnStartGame },
        { Messages.SUBMIT_COMMANDS, OnSubmitCommands}
    };

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
              GameConstants.APP_LOG_DIR,
              GameConstants.APP_ERROR_DIR
            })
        ));
        foreach(KeyValuePair<short, NetworkMessageDelegate> pair in handlers)
        {
            NetworkServer.RegisterHandler(pair.Key, pair.Value);
        }
        NetworkServer.Listen(GameConstants.PORT);
        Logger.ServerLog("Listening");
    }

    private static void Send(NetworkConnection conn, short msgType, MessageBase msg)
    {
        if (conn != null)
        {
            conn.Send(msgType, msg);
        }
        else
        {
            GameClient.Receive(msgType, msg);
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
        Logger.ServerLog("Client Connected");
        if (!GameConstants.USE_SERVER)
        {
            GameClient.Receive(MsgType.Connect, Messages.EMPTY);
        }
    }

    private static void OnStartLocalGame(NetworkMessage netMsg)
    {
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.myname = "ME";
        resp.opponentname = "OPPONENT";
        resp.numRobots = (byte) (msg.myRobots.Length + msg.opponentRobots.Length);
        resp.robotNames = new string[resp.numRobots];
        resp.robotHealth = new byte[resp.numRobots];
        resp.robotAttacks = new byte[resp.numRobots];
        resp.robotPriorities = new byte[resp.numRobots];
        resp.robotIsOpponents = new bool[resp.numRobots];
        Robot[] myrobots = new Robot[msg.myRobots.Length];
        Robot[] opponentrobots = new Robot[msg.opponentRobots.Length];
        for (short i = 0; i < msg.myRobots.Length; i++)
        {
            myrobots[i] = Robot.create(msg.myRobots[i]);
            myrobots[i].id = i;
            resp.robotNames[i] = myrobots[i].name;
            resp.robotHealth[i] = (byte)myrobots[i].health;
            resp.robotAttacks[i] = (byte)myrobots[i].attack;
            resp.robotPriorities[i] = myrobots[i].priority;
            resp.robotIsOpponents[i] = false;
        }
        for (short i = (short)msg.myRobots.Length; i < resp.numRobots; i++)
        {
            opponentrobots[i - msg.myRobots.Length] = Robot.create(msg.opponentRobots[i - msg.myRobots.Length]);
            opponentrobots[i - msg.myRobots.Length].id = i;
            resp.robotNames[i] = opponentrobots[i - msg.myRobots.Length].name;
            resp.robotHealth[i] = (byte)opponentrobots[i - msg.myRobots.Length].health;
            resp.robotAttacks[i] = (byte)opponentrobots[i - msg.myRobots.Length].attack;
            resp.robotPriorities[i] = opponentrobots[i - msg.myRobots.Length].priority;
            resp.robotIsOpponents[i] = true;
        }
        appgame = new Game(myrobots, opponentrobots, resp.myname, resp.opponentname);
        Send(netMsg.conn, Messages.GAME_READY, resp);
    }

    private static void OnStartGame(NetworkMessage netMsg)
    {
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.myname = "ME";
        resp.opponentname = "YOU";
        Send(netMsg.conn, Messages.GAME_READY, resp);
    }

    private static void OnSubmitCommands(NetworkMessage netMsg)
    {
        Messages.SubmitCommandsMessage msg = netMsg.ReadMessage<Messages.SubmitCommandsMessage>();
        Messages.TurnEventsMessage resp = new Messages.TurnEventsMessage();
        Logger.ServerLog(string.Join<Command>(" | ",msg.commands));
        List<GameEvent> events = appgame.CommandsToEvents(new List<Command>(msg.commands));
        resp.events = events.ToArray();
        Send(netMsg.conn, Messages.TURN_EVENTS, resp);
    }
}
