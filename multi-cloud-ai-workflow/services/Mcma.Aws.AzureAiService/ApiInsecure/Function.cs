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
using System.Net.Http;
using Mcma.Api.Routes;
using Mcma.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Core;
using System.Linq;
using System.Net;
using Mcma.Aws.Lambda;
using Mcma.Core.ContextVariables;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AzureAiService.ApiInsecure
{
    public class Function
    {
        private static ApiGatewayApiController Controller { get; } = 
            new McmaApiRouteCollection()
                .AddRoute(HttpMethod.Post.Method, "/job-assignments/{id}/notifications", ProcessNotificationAsync)
                .ToController();

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            Logger.Debug(nameof(ProcessNotificationAsync));
            Logger.Debug(requestContext.Request.ToMcmaJson().ToString());
            
            var table = new DynamoDbTable<JobAssignment>(requestContext.TableName());

            var jobAssignmentId = requestContext.PublicUrl() + "/job-assignments/" + requestContext.Request.PathVariables["id"];

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            Logger.Debug("jobAssignment = {0}", jobAssignment);

            if (!requestContext.ResourceIfFound(jobAssignment, false))
                return;

            var notification = requestContext.Request.QueryStringParameters;
            Logger.Debug("notification = {0}", notification);
            if (notification == null || !notification.Any())
            {
                requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                requestContext.Response.StatusMessage = "Missing notification in request Query String";
                return;
            }

            var lambdaWorkerInvoker = new LambdaWorkerInvoker();
            await lambdaWorkerInvoker.RunAsync(
                requestContext.WorkerFunctionName(),
                new
                {
                    operationName = "ProcessNotification",
                    contextVariables = requestContext.GetAllContextVariables(),
                    input = new
                    {
                        jobAssignmentId,
                        notification
                    }
                });
        }
    }
}
