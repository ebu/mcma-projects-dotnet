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

namespace Mcma.Azure.AzureAiService.ApiHandler
{
    public static class Notifications
    {
        public static Func<McmaApiRequestContext, Task> Handler(IDbTableProvider dbTableProvider, Func<IContext, IWorkerInvoker> createWorkerInvoker)
        {
            return async requestContext =>
            {
                requestContext.Logger.Debug($"{nameof(Notifications)}.{nameof(Handler)}");
                requestContext.Logger.Debug(requestContext.Request.ToMcmaJson().ToString());
                
                var table = dbTableProvider.Table<JobAssignment>(requestContext.Variables.TableName());

                var jobAssignmentId = requestContext.Variables.PublicUrl().TrimEnd('/') + "/job-assignments/" + requestContext.Request.PathVariables["id"];

                var jobAssignment = await table.GetAsync(jobAssignmentId);

                requestContext.Logger.Debug("jobAssignment = {0}", jobAssignment);

                if (jobAssignment == null)
                {
                    requestContext.SetResponseResourceNotFound();
                    return;
                }

                var notification = requestContext.Request.QueryStringParameters;
                requestContext.Logger.Debug("notification = {0}", notification);
                if (notification == null || !notification.Any())
                {
                    requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    requestContext.Response.StatusMessage = "Missing notification in request Query String";
                    return;
                }

                var workerInvoker = createWorkerInvoker(requestContext);
                await workerInvoker.InvokeAsync(
                    requestContext.Variables.WorkerFunctionId(),
                    "ProcessNotification",
                    requestContext.Variables.GetAll().ToDictionary(),
                    new
                    {
                        jobAssignmentId,
                        notification
                    });
            };
        }
    }
}