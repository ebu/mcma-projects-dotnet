using System.Net;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Data;

namespace Mcma.Aws.JobRepository.ApiHandler
{
    public static class JobRoutes
    {
        private static IDbTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        public static Task StopJobAsync(McmaApiRequestContext requestContext)
        {
            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
            return Task.CompletedTask;
        }
        
        public static Task CancelJobAsync(McmaApiRequestContext requestContext)
        {
            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
            return Task.CompletedTask;
        }

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var table = DbTableProvider.Table<Job>(requestContext.TableName());

            var notification = requestContext.GetRequestBody<Notification>();
            if (notification == null)
            {
                requestContext.SetResponseBadRequestDueToMissingBody();
                return;
            }

            var job = await table.GetAsync(requestContext.PublicUrl() + "/jobs/" + requestContext.Request.PathVariables["id"]);
            if (job == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            if (job.JobProcess != notification.Source)
            {
                requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                requestContext.Response.StatusMessage = "Unexpected notification from '" + notification.Source + "'.";
                return;
            }

            await WorkerInvoker.InvokeAsync(
                requestContext.WorkerFunctionId(),
                "ProcessNotification",
                requestContext.GetAllContextVariables().ToDictionary(),
                new
                {
                    jobId = job.Id,
                    notification = notification
                });
        }
    }
}
