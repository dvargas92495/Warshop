using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using System.Net.Sockets;

public class App {

    private static int MIN_PORT = 12350;
    private static int MAX_PORT = 12360;
    private static Game appgame;
    private static Dictionary<string,string> boardFiles;
    private static readonly Logger log = new Logger(typeof(App));

    private static Dictionary<short, NetworkMessageDelegate> handlers = new Dictionary<short, NetworkMessageDelegate>()
    {
        { MsgType.Connect, OnConnect },
        { MsgType.Disconnect, OnDisconnect },
        { Messages.START_LOCAL_GAME, OnStartLocalGame },
        { Messages.START_GAME, OnStartGame },
        { Messages.SUBMIT_COMMANDS, OnSubmitCommands}
    };

    public static void LinkAssets(TextAsset[] boards)
    {
        boardFiles = new Dictionary<string, string>();
        Array.ForEach(boards, (TextAsset t) => boardFiles[t.name] = t.text);
    }

    // Use this for initialization
    public static void StartServer()
    {
        GenericOutcome outcome = GameLiftServerAPI.InitSDK();
        Application.targetFrameRate = 60;
        if (outcome.Success)
        {
            foreach (KeyValuePair<short, NetworkMessageDelegate> pair in handlers)
            {
                NetworkServer.RegisterHandler(pair.Key, pair.Value);
            }
            int port = MIN_PORT;
            for (; port < MAX_PORT; port++)
            {
                if (NetworkServer.Listen(port)) break;
            }
            GameLiftServerAPI.ProcessReady(new ProcessParameters(
                OnGameSession,
                OnProcessTerminate,
                OnHealthCheck,
                port,
                new LogParameters(new List<string>()
                {
                    GameConstants.APP_LOG_DIR
                })
            ));
            log.Info("Listening on: " + port);
        } else
        {
            log.Error(outcome);
        }
    }

    private static void Send(int connId, short msgType, MessageBase msg)
    {
        if (connId >= 0)
        {
            NetworkServer.connections[connId].Send(msgType, msg);
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
        netMsg.conn = new NetworkConnection();
        netMsg.conn.connectionId = -1;
        NetworkMessageDelegate handler;
        if (handlers.TryGetValue(msgType, out handler))
        {
            handler(netMsg);
        }
    }

    // begin GameLift callbacks

    static void OnGameSession(GameSession gameSession)
    {
        appgame = new Game();
        string boardContent = boardFiles[Get(gameSession, "boardFile")];
        appgame.board = new Map(boardContent);
        GameLiftServerAPI.ActivateGameSession();
    }

    static void OnProcessTerminate()
    {
        GameLiftServerAPI.ProcessEnding();
        GameLiftServerAPI.Destroy();
    }

    static bool OnHealthCheck()
    {
        return true;
    }

    // end GameLift callbacks

    private static void OnConnect(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Connected");
        if (!GameConstants.USE_SERVER)
        {
            Messages.FakeConnectMessage msg = netMsg.ReadMessage<Messages.FakeConnectMessage>();
            GameSession gs = new GameSession();
            GameProperty gp = new GameProperty();
            gp.Key = "boardFile";
            gp.Value = msg.boardFile;
            gs.GameProperties.Add(gp);
            OnGameSession(gs);
            GameClient.Receive(MsgType.Connect, Messages.EMPTY);
        }
    }

    private static void OnDisconnect(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Disconnected");
        GameLiftServerAPI.TerminateGameSession();
    }

    private static void OnStartLocalGame(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Starting Local Game");
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(msg.playerSessionId);
        if (!outcome.Success && GameConstants.USE_SERVER)
        {
            log.Error(outcome);
            return;
        }
        appgame.Join(msg.myRobots, msg.myName, netMsg.conn.connectionId);
        appgame.Join(msg.opponentRobots, msg.opponentName, netMsg.conn.connectionId);
        Send(netMsg.conn.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(true));
    }

    private static void OnStartGame(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Starting Game");
        Messages.StartGameMessage msg = netMsg.ReadMessage<Messages.StartGameMessage>();
        GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(msg.playerSessionId);
        if (!outcome.Success && GameConstants.USE_SERVER)
        {
            log.Error(outcome);
            return;
        }
        appgame.Join(msg.myRobots, msg.myName, netMsg.conn.connectionId);
        if (appgame.primary.ready && appgame.secondary.ready)
        {
            Send(appgame.primary.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(true));
            Send(appgame.secondary.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(false));
        }
    }

    private static void OnSubmitCommands(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Submitting Commands");
        Messages.SubmitCommandsMessage msg = netMsg.ReadMessage<Messages.SubmitCommandsMessage>();
        try
        {
            Game.Player p = (appgame.primary.name.Equals(msg.owner) ? appgame.primary : appgame.secondary);
            p.StoreCommands(new List<Command>(msg.commands));
            if (appgame.primary.ready && appgame.secondary.ready)
            {
                Messages.TurnEventsMessage resp = new Messages.TurnEventsMessage();
                List<GameEvent> events = appgame.CommandsToEvents();
                resp.events = events.ToArray();
                resp.turn = appgame.GetTurn();
                foreach (int cid in appgame.connectionIds())
                {
                    if (cid != appgame.primary.connectionId && cid == appgame.secondary.connectionId) Array.ForEach(resp.events, (GameEvent g) => g.Flip());
                    Send(cid, Messages.TURN_EVENTS, resp);
                }
            }
            else
            {
                int cid = (p.Equals(appgame.primary) ? appgame.secondary.connectionId : appgame.primary.connectionId);
                Send(cid, Messages.WAITING_COMMANDS, new Messages.OpponentWaitingMessage());
            }
        } catch(Exception e)
        {
            log.Fatal(e);
            Messages.ServerErrorMessage errorMsg = new Messages.ServerErrorMessage();
            errorMsg.serverMessage = "Exception thrown when submitting commands";
            errorMsg.exceptionType = e.GetType().ToString();
            errorMsg.exceptionMessage = e.Message;
            foreach (int cid in appgame.connectionIds())
            {
                Send(cid, Messages.SERVER_ERROR, errorMsg);
            }
        }
    }

    private static string Get(GameSession g, string k)
    {
        GameProperty p = g.GameProperties.Find((GameProperty gp) => gp.Key.Equals(k));
        if (p == null)
        {
            return "";
        } else
        {
            return p.Value;
        }
    }
}
