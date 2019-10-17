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
            var notificationJobPayload = notification.Content.ToMcmaObject<JobBase>();

            var table = DbTableProvider.Table<JobAssignment>(request.Variables.TableName());

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            jobAssignment.Status = notificationJobPayload.Status;
            jobAssignment.StatusMessage = notificationJobPayload.StatusMessage;
            if (notificationJobPayload.Progress != null)
                jobAssignment.Progress = notificationJobPayload.Progress;

            jobAssignment.JobOutput = notificationJobPayload.JobOutput;
            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobAssignmentId, jobAssignment);

            var resourceManager = ResourceManagerProvider.Get(request.Variables);

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
