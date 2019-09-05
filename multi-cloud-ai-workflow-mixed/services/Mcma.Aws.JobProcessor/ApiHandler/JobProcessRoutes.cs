using System.Net;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Data;

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public static class JobProcessRoutes
    {
        private static IDbTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var request = requestContext.Request;
            var response = requestContext.Response;

            var notification = requestContext.GetRequestBody<Notification>();
            if (notification == null)
            {
                requestContext.SetResponseBadRequestDueToMissingBody();
                return;
            }

            var table = DbTableProvider.Table<JobProcess>(requestContext.TableName());

            var jobProcess = await table.GetAsync(requestContext.PublicUrl() + "/job-processes/" + request.PathVariables["id"]);
            
            if (jobProcess == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            if (jobProcess.JobAssignment != notification.Source)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Unexpected notification from '" + notification.Source + "'.";
                return;
            }

            await WorkerInvoker.InvokeAsync(
                requestContext.WorkerFunctionId(),
                "ProcessNotification",
                requestContext.GetAllContextVariables().ToDictionary(),
                new
                {
                    jobProcessId = jobProcess.Id,
                    notification = notification
                });
        }
    }
}
