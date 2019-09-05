using System.Linq;
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

namespace Mcma.Aws.JobRepository.ApiHandler
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
            new DefaultRouteCollectionBuilder<Job>(DbTableProvider)
                .AddAll()
                .Route(r => r.Create).Configure(r =>
                    r.OnCompleted(
                        (ctx, job) =>
                            WorkerInvoker.InvokeAsync(
                                ctx.WorkerFunctionId(),
                                "CreateJobProcess",
                                ctx.GetAllContextVariables().ToDictionary(),
                                new { jobId = job.Id }
                            )
                    )
                )
                .Route(r => r.Delete).Configure(r =>
                    r.OnCompleted(
                        async (ctx, job) =>
                        {
                            if (!string.IsNullOrWhiteSpace(job.JobProcess))
                                await WorkerInvoker.InvokeAsync(
                                    ctx.WorkerFunctionId(),
                                    "DeleteJobProcess",
                                    ctx.GetAllContextVariables().ToDictionary(),
                                    new { jobProcessId = job.JobProcess }
                                );
                        }
                    )
                )
                .Route(r => r.Update).Remove()
                .Build();

        private static ApiGatewayApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(Routes)
                .AddRoute(HttpMethod.Post, "/jobs/{id}/stop", JobRoutes.StopJobAsync)
                .AddRoute(HttpMethod.Post, "/jobs/{id}/cancel", JobRoutes.CancelJobAsync)
                .AddRoute(HttpMethod.Post, "/jobs/{id}/notifications", JobRoutes.ProcessNotificationAsync)
                .ToApiGatewayApiController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
