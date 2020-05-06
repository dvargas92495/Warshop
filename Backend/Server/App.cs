using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using WarshopCommon;

namespace Server 
{
    public class App 
    {
        
        private static readonly Logger log = new Logger(typeof(App).ToString());
        private const int PORT = 12345;
        private static string gameSessionId;
        private static Game appgame;
        private static int healthChecks = 0;
        public App() {}

        static void Main(string[] args)
        {
            Console.WriteLine("Starting the server at port: " + PORT);
            GenericOutcome outcome = GameLiftServerAPI.InitSDK();
            if (outcome.Success)
            {
                /*
                short[] messageTypes = {
                    MsgType.Connect, MsgType.Disconnect, Messages.ACCEPT_PLAYER_SESSION, Messages.START_LOCAL_GAME,
                    Messages.START_GAME, Messages.SUBMIT_COMMANDS, Messages.END_GAME,
                };
                Util.ToList(messageTypes).ForEach(messageType => NetworkServer.RegisterHandler(messageType, GetHandler(messageType)));
                */
                UdpClient udpClient = new UdpClient(PORT);
                udpClient.BeginReceive(DataReceived, udpClient);
                LogParameters paths = new LogParameters();
                paths.LogPaths.Add("C:\\Game\\logs");
                GameLiftServerAPI.ProcessReady(new ProcessParameters(
                    OnGameSession,
                    OnProcessTerminate,
                    OnHealthCheck,
                    PORT,
                    paths
                ));
                Console.WriteLine("Listening on: " + PORT);
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine(outcome);
            }
        }

        private static void OnGameSession(GameSession gameSession)
        {
            log.ConfigureNewGame(gameSession.GameSessionId);
            appgame = new Game();
            string boardFile = "8 5\nA W W W W W W a\nB W W W W W W b\nW P W W W W p W\nC W W W W W W c\nD W W W W W W d";
            appgame.board = new Map(boardFile);
            appgame.gameSessionId = gameSession.GameSessionId;
            gameSessionId = gameSession.GameSessionId;
            GameLiftServerAPI.ActivateGameSession();
        }

        private static void OnProcessTerminate()
        {
            GameLiftServerAPI.ProcessEnding();
            GameLiftServerAPI.Destroy();
            System.Environment.Exit(0);
        }

        private static bool OnHealthCheck()
        {
            healthChecks++;
            log.Info("Heartbeat - " + healthChecks);
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
            if (healthChecks == 10 && result.Result.PlayerSessions.Count == 0) EndGame();
            return true;
        }
        
        static void EndGame()
        {
            Logger.RemoveGame();
            GameLiftServerAPI.TerminateGameSession();
        }

        private static void DataReceived(IAsyncResult ar)
        {
            UdpClient c = (UdpClient)ar.AsyncState;
            IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Byte[] receivedBytes = c.EndReceive(ar, ref receivedIpEndPoint);

            // Convert data to ASCII and print in console
            string receivedText = ASCIIEncoding.ASCII.GetString(receivedBytes);
            Console.Write(receivedIpEndPoint + ": " + receivedText + Environment.NewLine);

            // Restart listening for udp data packages
            c.BeginReceive(DataReceived, ar.AsyncState);
        }

        private static void OnAcceptPlayerSession() // (NetworkMessage netMsg)
        {
           // Messages.AcceptPlayerSessionMessage msg = netMsg.ReadMessage<Messages.AcceptPlayerSessionMessage>();
            GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(""); // msg.playerSessionId);
            if (!outcome.Success)
            {
                log.Error(outcome);
                return;
            }
        }

        protected void OnStartGame() // (NetworkMessage netMsg)
        {
            log.Info("Client Starting Game");
            /*
            Messages.StartGameMessage msg = netMsg.ReadMessage<Messages.StartGameMessage>();
            appgame.Join(msg.myRobots, msg.myName, netMsg.conn.connectionId);
            if (appgame.primary.joined && appgame.secondary.joined)
            {
                Send(appgame.primary.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(true));
                Send(appgame.secondary.connectionId, Messages.GAME_READY, appgame.GetGameReadyMessage(false));
            }
            */
        }

        protected void OnSubmitCommands() // (NetworkMessage netMsg)
        {
            log.Info("Client Submitting Commands");
            /*
            Messages.SubmitCommandsMessage msg = netMsg.ReadMessage<Messages.SubmitCommandsMessage>();
            try
            {
                Game.Player p = (appgame.primary.name.Equals(msg.owner) ? appgame.primary : appgame.secondary);
                p.StoreCommands(Util.ToList(msg.commands));
                if (appgame.primary.ready && appgame.secondary.ready)
                {
                    Messages.TurnEventsMessage resp = new Messages.TurnEventsMessage();
                    List<GameEvent> events = appgame.CommandsToEvents();
                    resp.events = events.ToArray();
                    resp.turn = appgame.GetTurn();
                    appgame.connectionIds().ForEach(cid =>
                    {
                        if (cid != appgame.primary.connectionId && cid == appgame.secondary.connectionId) Util.ToList(resp.events).ForEach(g => g.Flip());
                        Send(cid, Messages.TURN_EVENTS, resp);
                    });
                }
                else
                {
                    int cid = (p.Equals(appgame.primary) ? appgame.secondary.connectionId : appgame.primary.connectionId);
                    Send(cid, Messages.WAITING_COMMANDS, new Messages.OpponentWaitingMessage());
                }
            } catch(ZException e)
            {
                log.Fatal(e);
                Messages.ServerErrorMessage errorMsg = new Messages.ServerErrorMessage();
                errorMsg.serverMessage = "Game server crashed when processing your submitted commands";
                errorMsg.exceptionType = e.GetType().ToString();
                errorMsg.exceptionMessage = e.Message;
                appgame.connectionIds().ForEach(cid =>
                {
                    Send(cid, Messages.SERVER_ERROR, errorMsg);
                });
                EndGame();
            }
            */
        }
    }
}