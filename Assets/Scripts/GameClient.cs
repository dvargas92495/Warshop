using System;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public class GameClient : MonoBehaviour {

    private static NetworkClient client;
    private static AmazonGameLiftClient amazonClient;
    private static string playerSessionId;
    private static Logger log = new Logger(typeof(GameClient));
    private static Dictionary<short, NetworkMessageDelegate> handlers = new Dictionary<short, NetworkMessageDelegate>()
    {
        { MsgType.Connect, OnConnect },
        { Messages.GAME_READY, OnGameReady },
        { Messages.TURN_EVENTS, OnTurnEvents },
        { Messages.WAITING_COMMANDS, OnOpponentWaiting }
    };

    public static void Initialize(string playerId, string boardFile) {
        if (GameConstants.USE_SERVER)
        {
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback; //DO NOT REMOVE THIS
            amazonClient = new AmazonGameLiftClient(GameConstants.AWS_PUBLIC_KEY, GameConstants.AWS_SECRET_KEY, Amazon.RegionEndpoint.USWest2);
            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = GameConstants.PRODUCTION_ALIAS;
            Alias aliasRes = amazonClient.ListAliases(aliasReq).Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId = amazonClient.DescribeAlias(describeAliasReq.AliasId).Alias.RoutingStrategy.FleetId;
            DescribeGameSessionsRequest describeReq = new DescribeGameSessionsRequest();
            describeReq.FleetId = fleetId;
            describeReq.StatusFilter = GameSessionStatus.ACTIVE;
            DescribeGameSessionsResponse describeRes = amazonClient.DescribeGameSessions(describeReq);
            log.Info("Game Sessions found: " + describeRes.GameSessions.Count);
            GameSession gameSession = describeRes.GameSessions.Find((GameSession g) => g.CurrentPlayerSessionCount < g.MaximumPlayerSessionCount);
            if (gameSession == null)
            {
                log.Info("No Game Session Available, creating one...");
                GameProperty gp = new GameProperty();
                gp.Key = GameConstants.GAME_SESSION_PROPERTIES.BOARDFILE;
                gp.Value = boardFile;
                CreateGameSessionRequest req = new CreateGameSessionRequest();
                req.MaximumPlayerSessionCount = (GameConstants.LOCAL_MODE ? 1 : 2);
                req.FleetId = fleetId;
                req.GameProperties.Add(gp);
                try
                {
                    CreateGameSessionResponse res = amazonClient.CreateGameSession(req);
                    gameSession = res.GameSession;
                    int retries = 0;
                    while (gameSession.Status.Equals(GameSessionStatus.ACTIVATING) && retries < 100)
                    {
                        describeReq = new DescribeGameSessionsRequest();
                        describeReq.GameSessionId = res.GameSession.GameSessionId;
                        gameSession = amazonClient.DescribeGameSessions(describeReq).GameSessions[0];
                        retries++;
                    }
                    if (!gameSession.Status.Equals(GameSessionStatus.ACTIVE))
                    {
                        log.Info(gameSession.Status);
                        return;
                    }
                } catch (NotFoundException e) {
                    log.Fatal(e);
                    Interpreter.ClientError("Your game is out of date! Download the newest version");
                    return;
                } catch (FleetCapacityExceededException) {
                    log.Error("No more processes available to reserve a fleet");
                    Interpreter.ClientError("There's no more room to create a new game. Come back later.");
                    return;
                } catch (Exception e)
                {
                    log.Fatal(e);
                    Interpreter.ClientError("An unexpected error occurred! Please notify the developers.");
                    return;
                }
            }
            log.Info("Game Session - " + gameSession.GameSessionId);
            CreatePlayerSessionRequest playerSessionRequest = new CreatePlayerSessionRequest();
            playerSessionRequest.PlayerId = playerId;
            playerSessionRequest.GameSessionId = gameSession.GameSessionId;
            try
            {
                CreatePlayerSessionResponse playerSessionResponse = amazonClient.CreatePlayerSession(playerSessionRequest);
                playerSessionId = playerSessionResponse.PlayerSession.PlayerSessionId;
                log.Info("Player Session - " + playerSessionResponse.PlayerSession.PlayerSessionId);
                string ip = playerSessionResponse.PlayerSession.IpAddress;
                int port = playerSessionResponse.PlayerSession.Port;
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

    private static bool MyRemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true; //DO NOT REMOVE THIS FUNCTION
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
