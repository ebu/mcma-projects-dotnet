using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Aws.ApiGateway;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.WorkflowActivityCallbackHandler
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static ApiGatewayApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoute(HttpMethod.Post, "/notifications", ProcessNotificationAsync)
                .ToApiGatewayApiController();

        private static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            Logger.Debug(nameof(ProcessNotificationAsync));
            Logger.Debug(requestContext.Request.ToMcmaJson().ToString());

            var notification = requestContext.GetRequestBody<Notification>();
            if (notification == null)
            {
                requestContext.SetResponseBadRequestDueToMissingBody();
                return;
            }

            if (notification.Content == null)
            {
                requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                requestContext.Response.StatusMessage = "Missing notification content";
                return;
            }

            var job = notification.Content.ToMcmaObject<Job>();

            var taskToken = requestContext.Request.QueryStringParameters["taskToken"];
            
            if (job.Status == JobStatus.Completed)
            {
                using (var stepFunctionClient = new AmazonStepFunctionsClient())
                    await stepFunctionClient.SendTaskSuccessAsync(new SendTaskSuccessRequest
                    {
                        TaskToken = taskToken,
                        Output = $"\"{notification.Source}\""
                    });
            }
            else if (job.Status == JobStatus.Failed)
            {
                using (var stepFunctionClient = new AmazonStepFunctionsClient())
                    await stepFunctionClient.SendTaskFailureAsync(new SendTaskFailureRequest
                    {
                        TaskToken = taskToken,
                        Error = job.Type + " failed execution",
                        Cause = job.Type + " with id '" + job.Id + "' failed execution with statusMessage '" + job.StatusMessage + "'"
                    });
            }
            else
                Logger.Debug($"Ignoring notification for updated status of '{job.Status}'");
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}