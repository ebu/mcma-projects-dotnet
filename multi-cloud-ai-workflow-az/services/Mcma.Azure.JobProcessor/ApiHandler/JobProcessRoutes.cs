using System;
using System.Net;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Data;

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public static class JobProcessRoutes
    {
        public static Func<McmaApiRequestContext, Task> ProcessNotificationAsync(
            IDbTableProvider dbTableProvider,
            Func<McmaApiRequestContext, IWorkerInvoker> workerInvoker) 
            =>
            async requestContext =>
            {
                var request = requestContext.Request;
                var response = requestContext.Response;

                var notification = requestContext.GetRequestBody<Notification>();
                if (notification == null)
                {
                    requestContext.SetResponseBadRequestDueToMissingBody();
                    return;
                }

                var table = dbTableProvider.Table<JobProcess>(requestContext.TableName());

                var jobProcess = await table.GetAsync(requestContext.PublicUrl().TrimEnd('/') + "/job-processes/" + request.PathVariables["id"]);
                
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

                await workerInvoker(requestContext).InvokeAsync(
                    requestContext.WorkerFunctionId(),
                    "ProcessNotification",
                    requestContext.GetAllContextVariables().ToDictionary(),
                    new
                    {
                        jobProcessId = jobProcess.Id,
                        notification = notification
                    });
            };
    }
}
