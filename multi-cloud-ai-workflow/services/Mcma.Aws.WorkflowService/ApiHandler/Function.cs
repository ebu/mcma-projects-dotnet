using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Aws.Api;
using Mcma.Api.Routes;
using Mcma.Core;
using Mcma.Aws.Lambda;
using Mcma.Api.Routes.Defaults;
using System.Net.Http;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.WorkflowService.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller =
            new McmaApiRouteCollection()
                .AddRoutes(AwsDefaultRoutes.WithDynamoDb<JobAssignment>().ForJobAssignments<LambdaWorkerInvoker>())
                .AddRoute(HttpMethod.Post.Method, "/job-assignments/{id}/notifications", JobAssignmentRoutes.ProcessNotificationAsync)
                .ToController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
