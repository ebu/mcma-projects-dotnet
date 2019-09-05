using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Data;

namespace Mcma.Aws.TransformService.ApiHandler
{
    public static class JobAssignmentRoutes
    {
        private static IDbTableProvider DbTableProvider { get; } = new DynamoDbTableProvider();

        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var notification = requestContext.GetRequestBody<Notification>();
            if (notification == null)
            {
                requestContext.SetResponseBadRequestDueToMissingBody();
                return;
            }

            var table = DbTableProvider.Table<JobAssignment>(requestContext.TableName());

            var jobAssignment =
                await table.GetAsync(requestContext.PublicUrl() + "/job-assignments/" + requestContext.Request.PathVariables["id"]);

            if (jobAssignment == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }
                
            await WorkerInvoker.InvokeAsync(
                requestContext.WorkerFunctionId(),
                "ProcessNotification",
                requestContext.GetAllContextVariables().ToDictionary(),
                new
                {
                    jobAssignmentId = jobAssignment.Id,
                    notification = notification
                });
        }
    }
}
