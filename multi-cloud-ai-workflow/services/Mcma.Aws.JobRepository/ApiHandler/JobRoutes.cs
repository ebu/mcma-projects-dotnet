using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.Core;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Serialization;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Mcma.Core.Logging;
using Mcma.Aws.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.JobRepository.ApiHandler
{
    public static class JobRoutes
    {
        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        public static async Task StopJobAsync(McmaApiRequestContext requestContext)
        {
            var table = new DynamoDbTable<Job>(requestContext.TableName());

            var jobId = requestContext.PublicUrl() + "/jobs/" + requestContext.Request.PathVariables["id"];

            if (!requestContext.ResourceIfFound(await table.GetAsync(jobId), false))
                return;

            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
        }
        
        public static async Task CancelJobAsync(McmaApiRequestContext requestContext)
        {
            var table = new DynamoDbTable<Job>(requestContext.TableName());

            var jobId = requestContext.PublicUrl() + "/jobs/" + requestContext.Request.PathVariables["id"];

            if (!requestContext.ResourceIfFound(await table.GetAsync(jobId), false))
                return;

            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
        }

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var table = new DynamoDbTable<Job>(requestContext.TableName());

            var job = await table.GetAsync(requestContext.PublicUrl() + "/jobs/" + requestContext.Request.PathVariables["id"]);
            if (!requestContext.ResourceIfFound(job, false))
                return;

            if (requestContext.IsBadRequestDueToMissingBody<Notification>(out var notification))
                return;

            if (job.JobProcess != notification.Source)
            {
                requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                requestContext.Response.StatusMessage = "Unexpected notification from '" + notification.Source + "'.";
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
                        jobId = job.Id,
                        notification = notification
                    }
                });
        }
    }
}
