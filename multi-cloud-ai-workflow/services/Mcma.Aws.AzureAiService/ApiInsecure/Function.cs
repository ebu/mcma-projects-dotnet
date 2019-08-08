using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AzureAiService.ApiInsecure
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static ApiGatewayApiController Controller { get; } = 
            new McmaApiRouteCollection()
                .AddRoute(HttpMethod.Post, "/job-assignments/{id}/notifications", ProcessNotificationAsync)
                .ToApiGatewayApiController();

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

            if (jobAssignment == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            var notification = requestContext.Request.QueryStringParameters;
            Logger.Debug("notification = {0}", notification);
            if (notification == null || !notification.Any())
            {
                requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                requestContext.Response.StatusMessage = "Missing notification in request Query String";
                return;
            }

            var lambdaWorkerInvoker = new LambdaWorkerInvoker();
            await lambdaWorkerInvoker.InvokeAsync(
                requestContext.WorkerFunctionId(),
                "ProcessNotification",
                requestContext.GetAllContextVariables().ToDictionary(),
                new
                {
                    jobAssignmentId,
                    notification
                });
        }
    }
}
