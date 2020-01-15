using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;

namespace Mcma.Azure.AzureAiService.Notifications
{
    public static class Notifications
    {
        public static Func<McmaApiRequestContext, Task> Handler(ILoggerProvider loggerProvider, IDbTableProvider dbTableProvider, Func<IContextVariableProvider, IWorkerInvoker> createWorkerInvoker)
        {
            return async requestContext =>
            {
                var logger = loggerProvider.Get(requestContext.GetTracker());

                logger.Debug($"{nameof(Notifications)}.{nameof(Handler)}");
                logger.Debug(requestContext.Request.ToMcmaJson().ToString());
                
                var table = dbTableProvider.Table<JobAssignment>(requestContext.TableName());

                var jobAssignmentId = requestContext.PublicUrl().TrimEnd('/') + "/job-assignments/" + requestContext.Request.PathVariables["id"];

                var jobAssignment = await table.GetAsync(jobAssignmentId);

                logger.Debug("jobAssignment = {0}", jobAssignment);

                if (jobAssignment == null)
                {
                    requestContext.SetResponseResourceNotFound();
                    return;
                }

                var notification = requestContext.Request.QueryStringParameters;
                logger.Debug("notification = {0}", notification);
                if (notification == null || !notification.Any())
                {
                    requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    requestContext.Response.StatusMessage = "Missing notification in request Query String";
                    return;
                }

                var workerInvoker = createWorkerInvoker(requestContext);
                await workerInvoker.InvokeAsync(
                    requestContext.WorkerFunctionId(),
                    "ProcessNotification",
                    requestContext.GetAllContextVariables().ToDictionary(),
                    new
                    {
                        jobAssignmentId,
                        notification
                    });
            };
        }
    }
}