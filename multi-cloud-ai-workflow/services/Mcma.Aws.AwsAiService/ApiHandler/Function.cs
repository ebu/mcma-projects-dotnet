using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Api;
using Mcma.Api.Routes.Defaults;
using Mcma.Aws;
using Mcma.Aws.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AwsAiService.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller { get; } =
            AwsDefaultRoutes.WithDynamoDb<JobAssignment>()
                            .ForJobAssignments<LambdaWorkerInvoker>()
                            .ToController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
