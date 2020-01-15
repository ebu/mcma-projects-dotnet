using System;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;

namespace Mcma.Azure.WorkflowService.ApiHandler
{
    public static class Notifications
    {
        public static Func<McmaApiRequestContext, Task> Handler(ILoggerProvider loggerProvider, IDbTableProvider dbTableProvider, Func<McmaApiRequestContext, IWorkerInvoker> createWorkerInvoker)
            =>
            async requestContext =>
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

                await createWorkerInvoker(requestContext).InvokeAsync(
                    requestContext.WorkerFunctionId(),
                    "ProcessNotification",
                    input: new
                    {
                        jobAssignmentId,
                        notification = new Notification { Content = requestContext.GetRequestBodyJson() }
                    });
            };
    }
}
