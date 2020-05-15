using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;

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
        public class GameView { 
            public string gameSessionId { get; set; }
            public string creatorId { get; set; }
            public bool isPrivate { get; set; }
        }

        [Serializable]
        public class GetGamesResponse {
            public GameView[] gameViews { get; set; }
        }

        public async Task<APIGatewayProxyResponse> Get(ILambdaContext context)
        {
            AmazonGameLiftClient amazonClient = new AmazonGameLiftClient(Amazon.RegionEndpoint.USEast1);
            
            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "WarshopServer";
            Alias aliasRes = (await amazonClient.ListAliasesAsync(aliasReq)).Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId = (await amazonClient.DescribeAliasAsync(describeAliasReq.AliasId)).Alias.RoutingStrategy.FleetId;
            
            DescribeGameSessionsRequest describeReq = new DescribeGameSessionsRequest();
            describeReq.FleetId = fleetId;
            describeReq.StatusFilter = GameSessionStatus.ACTIVE;
            DescribeGameSessionsResponse describeRes = await amazonClient.DescribeGameSessionsAsync(describeReq);
            List<GameView> gameViews = describeRes.GameSessions
                .FindAll((GameSession g) => g.CurrentPlayerSessionCount < g.MaximumPlayerSessionCount)
                .ConvertAll((GameSession g) => new GameView(){
                    gameSessionId = g.GameSessionId,
                    creatorId = g.CreatorId,
                    isPrivate = true.ToString() == g.GameProperties.Find((GameProperty gp) => gp.Key.Equals("IsPrivate")).Value
                });

            GetGamesResponse response = new GetGamesResponse() {
                gameViews = gameViews.ToArray()
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
