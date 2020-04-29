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

        public async Task<APIGatewayProxyResponse> Get(ILambdaContext context)
        {
            Console.WriteLine("Starting to GET");
            AmazonGameLiftClient amazonClient = new AmazonGameLiftClient(Amazon.RegionEndpoint.USEast1);
            Console.WriteLine("Initialized client");
            
            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "WarshopServer";
            Console.WriteLine("Sending Alias");
            Alias aliasRes = (await amazonClient.ListAliasesAsync(aliasReq)).Aliases[0];
            Console.WriteLine("Found Alias " + aliasRes.AliasId);
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId = (await amazonClient.DescribeAliasAsync(describeAliasReq.AliasId)).Alias.RoutingStrategy.FleetId;
            Console.WriteLine("Got fleet id: " + fleetId);
            
            DescribeGameSessionsRequest describeReq = new DescribeGameSessionsRequest();
            describeReq.FleetId = fleetId;
            describeReq.StatusFilter = GameSessionStatus.ACTIVE;
            DescribeGameSessionsResponse describeRes = await amazonClient.DescribeGameSessionsAsync(describeReq);
            List<Tuple<string,string,string>> gameSessions = describeRes.GameSessions
                .FindAll((GameSession g) => g.CurrentPlayerSessionCount < g.MaximumPlayerSessionCount)
                .ConvertAll((GameSession g) => new Tuple<string,string,string>(
                    g.GameSessionId,
                    g.CreatorId,
                    g.GameProperties.Find((GameProperty gp) => gp.Key.Equals("IsPrivate")).Value
                 ));
            Console.WriteLine("Received game sessions: " + gameSessions.ToString());

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize(gameSessions),
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
        }
    }
}
