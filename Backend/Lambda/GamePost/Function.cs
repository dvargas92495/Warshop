using System;

using Amazon.Lambda.Core;
using Amazon.GameLift;
using Amazon.GameLift.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.LambdaJsonSerializer))]

namespace GamePost
{
    public class Function
    {
        public Function(){}

        [Serializable]
        public class CreateGameRequest
        {
            public string playerId;
            public bool isPrivate;
            public string password;
        }

        [Serializable]
        public class CreateGameResponse
        {
            public bool IsError;
            public string ErrorMessage;
            public string playerSessionId;
            public string ipAddress;
            public int port;
        }

        public CreateGameResponse Post(CreateGameRequest request, ILambdaContext context)
        {
            AmazonGameLiftClient amazonClient = new AmazonGameLiftClient(Amazon.RegionEndpoint.USEast1);

            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "WarshopServer";
            Alias aliasRes = amazonClient.ListAliasesAsync(aliasReq).Result.Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId =  amazonClient.DescribeAliasAsync(describeAliasReq.AliasId).Result.Alias.RoutingStrategy.FleetId;

            CreateGameSessionRequest req = new CreateGameSessionRequest();
            req.MaximumPlayerSessionCount = 2;
            req.FleetId = fleetId;
            req.CreatorId = request.playerId;
            req.GameProperties.Add(new GameProperty()
            {
                Key = "IsPrivate",
                Value = request.isPrivate.ToString()
            });
            req.GameProperties.Add(new GameProperty()
            {
                Key = "Password",
                Value = request.password
            });
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

                CreatePlayerSessionRequest playerSessionRequest = new CreatePlayerSessionRequest();
                playerSessionRequest.PlayerId = request.playerId;
                playerSessionRequest.GameSessionId = gameSession.GameSessionId;
                CreatePlayerSessionResponse playerSessionResponse = amazonClient.CreatePlayerSessionAsync(playerSessionRequest).Result;
                
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
    }
}
