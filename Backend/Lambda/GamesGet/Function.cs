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

        public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
            AmazonGameLiftClient amazonClient = new AmazonGameLiftClient(Amazon.RegionEndpoint.USEast1);
            
            ListAliasesRequest aliasReq = new ListAliasesRequest();
            aliasReq.Name = "WarshopServer";
            Alias aliasRes = amazonClient.ListAliasesAsync(aliasReq).Result.Aliases[0];
            DescribeAliasRequest describeAliasReq = new DescribeAliasRequest();
            describeAliasReq.AliasId = aliasRes.AliasId;
            string fleetId =  amazonClient.DescribeAliasAsync(describeAliasReq.AliasId).Result.Alias.RoutingStrategy.FleetId;
            
            DescribeGameSessionsRequest describeReq = new DescribeGameSessionsRequest();
            describeReq.FleetId = fleetId;
            describeReq.StatusFilter = GameSessionStatus.ACTIVE;
            DescribeGameSessionsResponse describeRes = amazonClient.DescribeGameSessionsAsync(describeReq).Result;
            List<Tuple<string,string,string>> gameSessions = describeRes.GameSessions
                .FindAll((GameSession g) => g.CurrentPlayerSessionCount < g.MaximumPlayerSessionCount)
                .ConvertAll((GameSession g) => new Tuple<string,string,string>(
                    g.GameSessionId,
                    g.CreatorId,
                    g.GameProperties.Find((GameProperty gp) => gp.Key.Equals("IsPrivate")).Value
                 ));

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = gameSessions.ToString(),
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
        }
    }
}
