using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Api;
using Mcma.Aws;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Aws.Api;
using Mcma.Core;
using Mcma.Api.Routes;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.MediaRepository.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(AwsDefaultRoutes.WithDynamoDb<BMContent>("bm-contents").AddAll().Build())
                .AddRoutes(AwsDefaultRoutes.WithDynamoDb<BMEssence>("bm-essences").AddAll().Build())
                .ToController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
