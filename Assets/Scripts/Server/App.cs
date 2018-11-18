using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;

public abstract class App
{
    private static App instance;

    private static Game appgame;
    private static readonly Logger log = new Logger(typeof(App));

    public static void StartServer()
    {
        instance = new AwsApp();
    }

    protected NetworkMessageDelegate GetHandler(short messageType)
    {
        switch(messageType)
        {
            case MsgType.Connect:
                return OnConnect;
            case MsgType.Disconnect:
                return OnDisconnect;
            case Messages.START_LOCAL_GAME:
                return OnStartLocalGame;
            case Messages.START_GAME:
                return OnStartGame;
            case Messages.SUBMIT_COMMANDS:
                return OnSubmitCommands;
            case Messages.END_GAME:
                return OnEndGame;
            default:
                return OnUnsupportedMessage;
        }
    }

    protected abstract void Send(int connId, short msgType, MessageBase msg);

    internal void Receive(short msgType, MessageBase message)
    {
        NetworkMessage netMsg = new NetworkMessage();
        NetworkWriter writer = new NetworkWriter();
        message.Serialize(writer);
        NetworkReader reader = new NetworkReader(writer);
        netMsg.msgType = msgType;
        netMsg.reader = reader;
        netMsg.conn = new NetworkConnection();
        netMsg.conn.connectionId = -1;
        GetHandler(msgType)(netMsg);
    }

    // begin GameLift callbacks

    protected void OnGameSession(GameSession gameSession)
    {
        Logger.ConfigureNewGame(gameSession.GameSessionId);
        appgame = new Game();
        string boardFile = "8 5\nA W W W W W W a\nB W W W W W W b\nW P W W W W p W\nC W W W W W W c\nD W W W W W W d";
        appgame.board = new Map(boardFile);
        appgame.gameSessionId = gameSession.GameSessionId;
        GameLiftServerAPI.ActivateGameSession();
    }

    protected void OnProcessTerminate()
    {
        GameLiftServerAPI.ProcessEnding();
        GameLiftServerAPI.Destroy();
    }

    protected bool OnHealthCheck()
    {
        log.Info("Heartbeat");
        DescribePlayerSessionsOutcome result =  GameLiftServerAPI.DescribePlayerSessions(new DescribePlayerSessionsRequest
        {
            GameSessionId = appgame.gameSessionId
        });
        bool timedOut = result.Result.PlayerSessions.Count > 0;
        foreach (PlayerSession ps in result.Result.PlayerSessions)
        {
            timedOut = timedOut && (ps.Status == PlayerSessionStatus.TIMEDOUT);
        }
        if (timedOut) EndGame();
        return true;
    }

    static void EndGame()
    {
        Logger.RemoveGame();
        GameLiftServerAPI.TerminateGameSession();
    }

    protected void OnUnsupportedMessage(NetworkMessage netMsg)
    {
        log.Info("Unsupported message type: " + netMsg.msgType);
    }

    static void OnEndGame(NetworkMessage netMsg)
    {
        log.Info(netMsg, "End Game");
        EndGame();
    }

    // end GameLift callbacks

    protected void OnConnect(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Connected");
    }

    private static void OnDisconnect(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Disconnected");
    }

    protected void OnStartLocalGame(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Starting Local Game");
        if (appgame == null) OnGameSession(new GameSession());
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        appgame.Join(msg.myRobots, msg.myName, netMsg.conn.connectionId);
        appgame.Join(msg.opponentRobots, msg.opponentName, netMsg.conn.connectionId);
        Send(netMsg.conn.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(true));
    }

    protected void OnStartGame(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Starting Game");
        Messages.StartGameMessage msg = netMsg.ReadMessage<Messages.StartGameMessage>();
        appgame.Join(msg.myRobots, msg.myName, netMsg.conn.connectionId);
        if (appgame.primary.joined && appgame.secondary.joined)
        {
            Send(appgame.primary.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(true));
            Send(appgame.secondary.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(false));
        }
    }

    protected void OnSubmitCommands(NetworkMessage netMsg)
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
            errorMsg.serverMessage = "Game server crashed when processing your submitted commands";
            errorMsg.exceptionType = e.GetType().ToString();
            errorMsg.exceptionMessage = e.Message;
            foreach (int cid in appgame.connectionIds())
            {
                Send(cid, Messages.SERVER_ERROR, errorMsg);
            }
            EndGame();
        }
    }
}
