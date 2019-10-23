using System;
using System.Net;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Data;

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public static class Notifications
    {
        public static Func<McmaApiRequestContext, Task> Handler(
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

                var table = dbTableProvider.Table<JobProcess>(requestContext.Variables.TableName());

                var jobProcess = await table.GetAsync(requestContext.Variables.PublicUrl().TrimEnd('/') + "/job-processes/" + request.PathVariables["id"]);
                
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
                    requestContext.Variables.WorkerFunctionId(),
                    "ProcessNotification",
                    requestContext.Variables.GetAll().ToDictionary(),
                    new
                    {
                        jobProcessId = jobProcess.Id,
                        notification = notification
                    });
            };
    }
}
