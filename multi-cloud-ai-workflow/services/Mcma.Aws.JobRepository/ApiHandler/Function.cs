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
using Mcma.Api.Routes.Defaults;
using Mcma.Core;
using Mcma.Aws.Lambda;
using Mcma.Api;
using Mcma.Api.Routes;
using System.Net.Http;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.JobRepository.ApiHandler
{
    public class Function
    {
        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        private static ApiGatewayApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(
                    AwsDefaultRoutes.WithDynamoDb<Job>()
                        .AddAll()
                        .Route(r => r.Create).Configure(r =>
                            r.OnCompleted((ctx, job) =>
                                WorkerInvoker.RunAsync(
                                    ctx.WorkerFunctionName(),
                                    new
                                    {
                                        operationName = "createJobProcess",
                                        contextVariables = ctx.GetAllContextVariables(),
                                        input = new { jobId = job.Id }
                                    })))
                        .Route(r => r.Delete).Configure(r =>
                            r.OnCompleted(async (ctx, job) =>
                            {
                                if (!string.IsNullOrWhiteSpace(job.JobProcess))
                                    await WorkerInvoker.RunAsync(
                                        ctx.WorkerFunctionName(),
                                        new
                                        {
                                            operationName = "deleteJobProcess",
                                            contextVariables = ctx.GetAllContextVariables(),
                                            input = new { jobProcessId = job.JobProcess }
                                        });
                            }))
                        .Route(r => r.Update).Remove()
                        .Build())
                .AddRoute(HttpMethod.Post.Method, "/jobs/{id}/stop", JobRoutes.StopJobAsync)
                .AddRoute(HttpMethod.Post.Method, "/jobs/{id}/cancel", JobRoutes.CancelJobAsync)
                .AddRoute(HttpMethod.Post.Method, "/jobs/{id}/notifications", JobRoutes.ProcessNotificationAsync)
                .ToController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
