using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Api;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.WorkflowActivityCallbackHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Function()
        {
            Controller.AddRoute("POST", "/notifications", ProcessNotificationAsync);
        }

        private static async Task ProcessNotificationAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(ProcessNotificationAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var notification = request.JsonBody.ToMcmaObject<Notification>();

            if (notification == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification in request body";
                return;
            }

            if (notification.Content == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification content";
                return;
            }

            var job = notification.Content.ToMcmaObject<Job>();

            var taskToken = request.QueryStringParameters["taskToken"];

            var stepFunctionClient = new AmazonStepFunctionsClient();
            switch (job.Status)
            {
                case "COMPLETED":
                    Logger.Debug($"Sending task success for task token {taskToken}");
                    await stepFunctionClient.SendTaskSuccessAsync(new SendTaskSuccessRequest
                    {
                        TaskToken = taskToken,
                        Output = $"\"{notification.Source}\""
                    });
                    break;
                case "FAILED":
                    Logger.Debug($"Sending task failure for task token {taskToken}");
                    var error = job.Type + " failed execution";
                    var cause = job.Type + " with id '" + job.Id + "' failed execution with statusMessage '" + job.StatusMessage + "'";

                    await stepFunctionClient.SendTaskFailureAsync(new SendTaskFailureRequest
                    {
                        TaskToken = taskToken,
                        Error = error,
                        Cause = cause
                    });
                    break;
            }
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}