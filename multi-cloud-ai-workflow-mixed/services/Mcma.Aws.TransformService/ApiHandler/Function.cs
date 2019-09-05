using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routes;
using Mcma.Api.Routes.Defaults;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.TransformService.ApiHandler
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static IDbTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static McmaApiRouteCollection Routes { get; } =
            new DefaultRouteCollectionBuilder<JobAssignment>(DbTableProvider).ForJobAssignments<LambdaWorkerInvoker>();

        private static ApiGatewayApiController Controller =
            new McmaApiRouteCollection()
                .AddRoutes(Routes)
                .AddRoute(HttpMethod.Post, "/job-assignments/{id}/notifications", JobAssignmentRoutes.ProcessNotificationAsync)
                .ToApiGatewayApiController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
