using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Api.Routes.Defaults;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.Client;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global));

        private static IDbTableProvider DbTableProvider { get; } =
            new DynamoDbTableProvider();

        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        private static McmaApiRouteCollection Routes { get; } =
            new DefaultRouteCollectionBuilder<JobProcess>(DbTableProvider)
                .AddAll()
                .Route(r => r.Create).Configure(r =>
                    r.OnCompleted(
                        (requestContext, jobProcess) =>
                            WorkerInvoker.InvokeAsync(
                                requestContext.WorkerFunctionId(),
                                "CreateJobAssignment",
                                requestContext.GetAllContextVariables().ToDictionary(),
                                new { jobProcessId = jobProcess.Id })
                    )
                )
                .Route(r => r.Update).Remove()
                .Build();

        private static ApiGatewayApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(Routes)
                .AddRoute(HttpMethod.Post.Method, "/job-processes/{id}/notifications", JobProcessRoutes.ProcessNotificationAsync)
                .ToApiGatewayApiController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
