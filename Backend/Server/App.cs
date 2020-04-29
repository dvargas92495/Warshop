using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;

namespace Server 
{
    public class App 
    {
        private const int PORT = 12345;
        public App() {}

        static void Main(string[] args)
        {
            Console.WriteLine("Starting the server at port: " + PORT);
            GenericOutcome outcome = GameLiftServerAPI.InitSDK();
            if (outcome.Success)
            {
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
            /*Logger.ConfigureNewGame(gameSession.GameSessionId);
            appgame = new Game();
            string boardFile = "8 5\nA W W W W W W a\nB W W W W W W b\nW P W W W W p W\nC W W W W W W c\nD W W W W W W d";
            appgame.board = new Map(boardFile);
            appgame.gameSessionId = gameSession.GameSessionId;*/
            GameLiftServerAPI.ActivateGameSession();
        }

        private static void OnProcessTerminate()
        {
            GameLiftServerAPI.ProcessEnding();
            GameLiftServerAPI.Destroy();
            System.Environment.Exit(0);
        }

        private static bool OnHealthCheck()
        {/*
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
            */
            return true;
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
    }
}