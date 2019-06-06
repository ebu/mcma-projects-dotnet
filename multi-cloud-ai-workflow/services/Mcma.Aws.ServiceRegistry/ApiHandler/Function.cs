using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Core.Serialization;
using Mcma.Aws;
using Mcma.Core.Logging;
using Mcma.Aws.Api;
using Mcma.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Core;
using Mcma.Api.Routes;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller { get; } = 
            new McmaApiRouteCollection()
                .AddRoutes(AwsDefaultRoutes.WithDynamoDb<Service>().AddAll().Build())
                .AddRoutes(AwsDefaultRoutes.WithDynamoDb<JobProfile>().AddAll().Build())
                .ToController();

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            var resp = await Controller.HandleRequestAsync(request, context);
            Logger.Debug(resp.ToMcmaJson().ToString());
            return resp;
        }
    }
}
