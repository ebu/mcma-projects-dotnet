using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Core.Serialization;
using Mcma.Aws;
using Mcma.Core.Logging;
using Mcma.Aws.Api;
using Mcma.Core;
using Mcma.Aws.Lambda;
using Mcma.Api;
using Mcma.Api.Routes;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public class Function
    {
        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        private static ApiGatewayApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(
                    AwsDefaultRoutes.WithDynamoDb<JobProcess>()
                        .AddAll()
                        .Route(r => r.Create).Configure(r =>
                            r.OnCompleted((requestContext, jobProcess) =>
                                WorkerInvoker.RunAsync(requestContext.WorkerFunctionName(),
                                    new
                                    {
                                        operationName = "createJobAssignment",
                                        contextVariables = requestContext.GetAllContextVariables(),
                                        input = new
                                        {
                                            jobProcessId = jobProcess.Id
                                        }
                                    })))
                        .Route(r => r.Update).Remove()
                        .Build())
                .AddRoute(HttpMethod.Post.Method, "/job-processes/{id}/notifications", JobProcessRoutes.ProcessNotificationAsync)
                .ToController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
