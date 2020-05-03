using System;

using Amazon.Lambda.Core;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;

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
            public string playerId { get; set; }
            public bool isPrivate { get; set; }
            public string password { get; set; }
        }

        [Serializable]
        public class CreateGameResponse
        {
            public string playerSessionId { get; set; }
            public string ipAddress { get; set; }
            public int port { get; set; }
        }

        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
            CreateGameRequest body = JsonSerializer.Deserialize<CreateGameRequest>(request.Body);
            AmazonGameLiftClient amazonClient = new AmazonGameLiftClient(Amazon.RegionEndpoint.USEast1);

            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "WarshopServer";
            Alias aliasRes = (await amazonClient.ListAliasesAsync(aliasReq)).Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId = (await amazonClient.DescribeAliasAsync(describeAliasReq.AliasId)).Alias.RoutingStrategy.FleetId;

            CreateGameSessionRequest req = new CreateGameSessionRequest();
            req.MaximumPlayerSessionCount = 2;
            req.FleetId = fleetId;
            req.CreatorId = body.playerId;
            req.GameProperties.Add(new GameProperty()
            {
                Key = "IsPrivate",
                Value = body.isPrivate.ToString()
            });
            req.GameProperties.Add(new GameProperty()
            {
                Key = "Password",
                Value = body.password == null ? "" : body.password
            });
            try
            {
                CreateGameSessionResponse res = await amazonClient.CreateGameSessionAsync(req);
                
                GameSession gameSession = res.GameSession;
                int retries = 0;
                while (gameSession.Status.Equals(GameSessionStatus.ACTIVATING) && retries < 100)
                {
                    DescribeGameSessionsRequest describeReq = new DescribeGameSessionsRequest();
                    describeReq.GameSessionId = res.GameSession.GameSessionId;
                    gameSession = (await amazonClient.DescribeGameSessionsAsync(describeReq)).GameSessions[0];
                    retries++;
                }

                CreatePlayerSessionRequest playerSessionRequest = new CreatePlayerSessionRequest();
                playerSessionRequest.PlayerId = body.playerId;
                playerSessionRequest.GameSessionId = gameSession.GameSessionId;
                CreatePlayerSessionResponse playerSessionResponse = await amazonClient.CreatePlayerSessionAsync(playerSessionRequest);
                
                CreateGameResponse response =  new CreateGameResponse {
                    playerSessionId = playerSessionResponse.PlayerSession.PlayerSessionId,
                    ipAddress = playerSessionResponse.PlayerSession.IpAddress,
                    port = playerSessionResponse.PlayerSession.Port
                };
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonSerializer.Serialize(response),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
            catch (NotFoundException e)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = "Your game is out of date! Download the newest version\n"+e.Message,
                };
            }
            catch (FleetCapacityExceededException e)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = "Our game servers are too busy right now, come back later!\n" + e.Message,
                };
            }
            catch (Exception e)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = "An unexpected error occurred! Please notify the developers.\n" + e.Message,
                };
            }
        }
    }
}
