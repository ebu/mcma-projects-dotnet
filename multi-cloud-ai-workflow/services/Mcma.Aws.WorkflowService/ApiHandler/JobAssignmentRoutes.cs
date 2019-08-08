using System.Threading.Tasks;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.WorkflowService.ApiHandler
{
    public static class JobAssignmentRoutes
    {
        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var notification = requestContext.GetRequestBody<Notification>();
            if (notification == null)
            {
                requestContext.SetResponseBadRequestDueToMissingBody();
                return;
            }

            var table = new DynamoDbTable<JobAssignment>(requestContext.TableName());

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
