using System;
using System.Net;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Logging;
using Mcma.Data;

namespace Mcma.Azure.JobRepository.ApiHandler
{
    public static class JobRoutes
    {
        public static Func<McmaApiRequestContext, Task> StopJobAsync() => requestContext =>
        {
            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
            return Task.CompletedTask;
        };
        
        public static Func<McmaApiRequestContext, Task> CancelJobAsync() => requestContext =>
        {
            requestContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
            requestContext.Response.StatusMessage = "Stopping job is not implemented";
            return Task.CompletedTask;
        };

        public static Func<McmaApiRequestContext, Task> ProcessNotificationAsync(IDbTableProvider dbTableProvider, Func<McmaApiRequestContext, IWorkerInvoker> createWorkerInvoker)
            =>
            async requestContext =>
            {
                var table = dbTableProvider.Table<Job>(requestContext.Variables.TableName());

                var notification = requestContext.GetRequestBody<Notification>();
                if (notification == null)
                {
                    requestContext.SetResponseBadRequestDueToMissingBody();
                    return;
                }

                var job = await table.GetAsync(requestContext.Variables.PublicUrl().TrimEnd('/') + "/jobs/" + requestContext.Request.PathVariables["id"]);
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

                await createWorkerInvoker(requestContext).InvokeAsync(
                    requestContext.Variables.WorkerFunctionId(),
                    "ProcessNotification",
                    requestContext.Variables.GetAll().ToDictionary(),
                    new
                    {
                        jobId = job.Id,
                        notification = notification
                    });
            };
    }
}
