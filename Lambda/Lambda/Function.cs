using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.GameLift;
using Amazon.GameLift.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Lambda
{
    public class GameLiftClient
    {
        private static AmazonGameLiftClient amazonClient;

        public GetGamesResponse GetGames(ILambdaContext context)
        {
            string fleetId = GetFleetId();
            DescribeGameSessionsRequest describeReq = new DescribeGameSessionsRequest();
            describeReq.FleetId = fleetId;
            describeReq.StatusFilter = GameSessionStatus.ACTIVE;
            DescribeGameSessionsResponse describeRes = amazonClient.DescribeGameSessionsAsync(describeReq).Result;
            List<string> gameSessionIds = describeRes.GameSessions
                .FindAll((GameSession g) => g.CurrentPlayerSessionCount < g.MaximumPlayerSessionCount)
                .ConvertAll((GameSession g) => g.GameSessionId);
            return new GetGamesResponse
            {
                gameSessionIds = gameSessionIds.ToArray()
            };
        }

        public ZResponse CreateGame(CreateGameRequest input, ILambdaContext context)
        {
            GameProperty gp = new GameProperty(); //TODO: delete this once redeploy happens
            gp.Key = "boardFile";
            gp.Value = "Battery";
            CreateGameSessionRequest req = new CreateGameSessionRequest();
            req.MaximumPlayerSessionCount = 2;
            req.FleetId = GetFleetId();
            req.GameProperties.Add(gp);
            try
            {
                CreateGameSessionResponse res = amazonClient.CreateGameSessionAsync(req).Result;
                
                GameSession gameSession = res.GameSession;
                int retries = 0;
                while (gameSession.Status.Equals(GameSessionStatus.ACTIVATING) && retries < 100)
                {
                    DescribeGameSessionsRequest describeReq = new DescribeGameSessionsRequest();
                    describeReq.GameSessionId = res.GameSession.GameSessionId;
                    gameSession = amazonClient.DescribeGameSessionsAsync(describeReq).Result.GameSessions[0];
                    retries++;
                }

                CreatePlayerSessionResponse playerSessionResponse = CreatePlayerSession(input.playerId, gameSession.GameSessionId);
                return new CreateGameResponse {
                    playerSessionId = playerSessionResponse.PlayerSession.PlayerSessionId,
                    ipAddress = playerSessionResponse.PlayerSession.IpAddress,
                    port = playerSessionResponse.PlayerSession.Port
                };
            }
            catch (NotFoundException e)
            {
                return new CreateGameResponse
                {
                    IsError = true,
                    ErrorMessage = "Your game is out of date! Download the newest version\n"+e.Message,
                };
            }
            catch (FleetCapacityExceededException e)
            {
                return new CreateGameResponse
                {
                    IsError = true,
                    ErrorMessage = "No more processes available to reserve a fleet\n" + e.Message,
                };
            }
            catch (Exception e)
            {
                return new CreateGameResponse
                {
                    IsError = true,
                    ErrorMessage = "An unexpected error occurred! Please notify the developers.\n" + e.Message,
                };
            }
        }

        public ZResponse JoinGame(JoinGameRequest input, ILambdaContext context)
        {
            ConfigureClient();
            CreatePlayerSessionResponse playerSessionResponse = CreatePlayerSession(input.playerId, input.gameSessionId);
            return new JoinGameResponse
            {
                playerSessionId = playerSessionResponse.PlayerSession.PlayerSessionId,
                ipAddress = playerSessionResponse.PlayerSession.IpAddress,
                port = playerSessionResponse.PlayerSession.Port
            };
        }

        [Serializable]
        public class CreateGameRequest
        {
            public string playerId;
        }

        [Serializable]
        public class JoinGameRequest
        {
            public string playerId;
            public string gameSessionId;
        }

        [Serializable]
        public class ZResponse
        {
            public bool IsError;
            public string ErrorMessage;
        }

        [Serializable]
        public class GetGamesResponse : ZResponse
        {
            public string[] gameSessionIds;
        }

        [Serializable]
        public class GameSessionResponse : ZResponse
        {
            public string playerSessionId;
            public string ipAddress;
            public int port;
        }
        [Serializable]
        public class CreateGameResponse : GameSessionResponse { }
        [Serializable]
        public class JoinGameResponse : GameSessionResponse { }

        private void ConfigureClient()
        {
            string publicKey = Environment.GetEnvironmentVariable("AWSPUBLIC");
            string secretKey = Environment.GetEnvironmentVariable("AWSSECRET");
            amazonClient = new AmazonGameLiftClient(publicKey, secretKey, Amazon.RegionEndpoint.USWest2);
        }

        private string GetFleetId()
        {
            ConfigureClient();
            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "Z8_App";
            Alias aliasRes = amazonClient.ListAliasesAsync(aliasReq).Result.Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            return amazonClient.DescribeAliasAsync(describeAliasReq.AliasId).Result.Alias.RoutingStrategy.FleetId;
        }

        private CreatePlayerSessionResponse CreatePlayerSession(string playerId, string gameSessionId)
        {
            CreatePlayerSessionRequest playerSessionRequest = new CreatePlayerSessionRequest();
            playerSessionRequest.PlayerId = playerId;
            playerSessionRequest.GameSessionId = gameSessionId;
            return amazonClient.CreatePlayerSessionAsync(playerSessionRequest).Result;
        }
    }

    public class Example
    {
        public string key1 { get; set; }
        public string key2 { get; set; }
        public string key3 { get; set; }
    }
}
