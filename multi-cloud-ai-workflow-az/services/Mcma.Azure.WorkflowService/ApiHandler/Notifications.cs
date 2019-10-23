using System;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Serialization;
using Mcma.Data;

namespace Mcma.Azure.WorkflowService.ApiHandler
{
    public static class Notifications
    {
        public static Func<McmaApiRequestContext, Task> Handler(IDbTableProvider dbTableProvider, Func<McmaApiRequestContext, IWorkerInvoker> createWorkerInvoker)
            =>
            async requestContext =>
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

                await createWorkerInvoker(requestContext).InvokeAsync(
                    requestContext.Variables.WorkerFunctionId(),
                    "ProcessNotification",
                    input: new
                    {
                        jobAssignmentId,
                        notification = new Notification { Content = requestContext.GetRequestBodyJson() }
                    });
            };
    }
}
