using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.LambdaJsonSerializer))]

namespace JoinPost
{
    public class Function
    {
        public Function(){}

        [Serializable]
        public class JoinGameRequest
        {
            public string playerId;
            public string gameSessionId;
            public string password;
        }

        [Serializable]
        public class JoinGameResponse
        {
            public string playerSessionId;
            public string ipAddress;
            public int port;
        }

        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
            JoinGameRequest input = JsonSerializer.Deserialize<JoinGameRequest>(request.Body);
            AmazonGameLiftClient amazonClient = new AmazonGameLiftClient(Amazon.RegionEndpoint.USEast1);

            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "WarshopServer";
            Alias aliasRes = (await amazonClient.ListAliasesAsync(aliasReq)).Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId = (await amazonClient.DescribeAliasAsync(describeAliasReq.AliasId)).Alias.RoutingStrategy.FleetId;

            DescribeGameSessionsResponse gameSession = await amazonClient.DescribeGameSessionsAsync(new DescribeGameSessionsRequest() {
                GameSessionId = input.gameSessionId
            });
            bool IsPrivate = gameSession.GameSessions[0].GameProperties.Find((GameProperty gp) => gp.Key.Equals("IsPrivate")).Value.Equals("True");
            string Password = IsPrivate ? gameSession.GameSessions[0].GameProperties.Find((GameProperty gp) => gp.Key.Equals("Password")).Value : "";
            if (!IsPrivate || input.password.Equals(Password))
            {
                CreatePlayerSessionRequest playerSessionRequest = new CreatePlayerSessionRequest();
                playerSessionRequest.PlayerId = input.playerId;
                playerSessionRequest.GameSessionId = input.gameSessionId;
                CreatePlayerSessionResponse playerSessionResponse = amazonClient.CreatePlayerSessionAsync(playerSessionRequest).Result;
                JoinGameResponse response = new JoinGameResponse
                {
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
            } else
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Incorrect password for private game",
                };
            }
        }
    }
}
