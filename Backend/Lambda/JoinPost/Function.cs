using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.GameLift;
using Amazon.GameLift.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.LambdaJsonSerializer))]

namespace GamesGet
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
            public bool IsError;
            public string ErrorMessage;
            public string playerSessionId;
            public string ipAddress;
            public int port;
        }

        public JoinGameResponse Post(JoinGameRequest input, ILambdaContext context)
        {
            AmazonGameLiftClient amazonClient = new AmazonGameLiftClient(Amazon.RegionEndpoint.USEast1);

            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "WarshopServer";
            Alias aliasRes = amazonClient.ListAliasesAsync(aliasReq).Result.Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId = amazonClient.DescribeAliasAsync(describeAliasReq.AliasId).Result.Alias.RoutingStrategy.FleetId;

            DescribeGameSessionsResponse gameSession = amazonClient.DescribeGameSessionsAsync(new DescribeGameSessionsRequest() {
                GameSessionId = input.gameSessionId
            }).Result;
            bool IsPrivate = gameSession.GameSessions[0].GameProperties.Find((GameProperty gp) => gp.Key.Equals("IsPrivate")).Value.Equals("True");
            string Password = gameSession.GameSessions[0].GameProperties.Find((GameProperty gp) => gp.Key.Equals("Password")).Value;
            if (!IsPrivate || input.password.Equals(Password))
            {
                CreatePlayerSessionRequest playerSessionRequest = new CreatePlayerSessionRequest();
                playerSessionRequest.PlayerId = input.playerId;
                playerSessionRequest.GameSessionId = input.gameSessionId;
                CreatePlayerSessionResponse playerSessionResponse = amazonClient.CreatePlayerSessionAsync(playerSessionRequest).Result;
                return new JoinGameResponse
                {
                    playerSessionId = playerSessionResponse.PlayerSession.PlayerSessionId,
                    ipAddress = playerSessionResponse.PlayerSession.IpAddress,
                    port = playerSessionResponse.PlayerSession.Port
                };
            } else
            {
                return new JoinGameResponse
                {
                    IsError = true,
                    ErrorMessage = "Incorrect password for private game"
                };
            }
        }
    }
}
