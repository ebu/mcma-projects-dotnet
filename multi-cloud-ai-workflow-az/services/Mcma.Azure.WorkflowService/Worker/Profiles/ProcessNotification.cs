using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.WorkflowService.Worker
{
    internal class ProcessNotification : WorkerOperationHandler<ProcessNotificationRequest>
    {
        public ProcessNotification(IResourceManagerProvider resourceManagerProvider, IDbTableProvider dbTableProvider)
        {
            ResourceManagerProvider = resourceManagerProvider;
            DbTableProvider = dbTableProvider;
        }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDbTableProvider DbTableProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessNotificationRequest notificationRequest)
        {
            var jobAssignmentId = notificationRequest.JobAssignmentId;
            var notification = notificationRequest.Notification;

            var workflowStatePayload = notification.Content.ToMcmaObject<WorkflowState>();

            var table = DbTableProvider.Table<JobAssignment>(request.Variables.TableName());

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            jobAssignment.Status = workflowStatePayload.Status?.ToUpper();
            jobAssignment.StatusMessage = workflowStatePayload.Errors?.ToString();

            if (workflowStatePayload.Progress != null)
                jobAssignment.Progress = workflowStatePayload.Progress;

            if (workflowStatePayload.Output != null)
                foreach (var output in workflowStatePayload.Output)
                    jobAssignment.JobOutput[output.Key] = output.Value;

            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobAssignmentId, jobAssignment);

            var resourceManager = ResourceManagerProvider.Get(request.Variables);

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
