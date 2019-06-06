using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.Core;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Serialization;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Mcma.Core.Logging;
using Mcma.Aws.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public static class JobProcessRoutes
    {
        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var request = requestContext.Request;
            var response = requestContext.Response;

            var table = new DynamoDbTable<JobProcess>(requestContext.TableName());

            var jobProcess = await table.GetAsync(requestContext.PublicUrl() + "/job-processes/" + request.PathVariables["id"]);
            if (!requestContext.ResourceIfFound(jobProcess, false) ||
                requestContext.IsBadRequestDueToMissingBody<Notification>(out var notification))
                return;

            if (jobProcess.JobAssignment != notification.Source)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Unexpected notification from '" + notification.Source + "'.";
                return;
            }

            await WorkerInvoker.RunAsync(
                requestContext.WorkerFunctionName(),
                new
                {
                    operationName = "processNotification",
                    contextVariables = requestContext.GetAllContextVariables(),
                    input = new
                    {
                        jobProcessId = jobProcess.Id,
                        notification = notification
                    }
                });
        }
    }
}
