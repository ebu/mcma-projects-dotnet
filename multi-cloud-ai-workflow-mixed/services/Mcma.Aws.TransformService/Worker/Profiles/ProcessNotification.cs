using System;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Worker;
using Mcma.Core.ContextVariables;
using Mcma.Client;
using Mcma.Data;

namespace Mcma.Aws.TransformService.Worker
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

        protected override async Task ExecuteAsync(WorkerRequest @event, ProcessNotificationRequest notificationRequest)
        {
            var jobAssignmentId = notificationRequest.JobAssignmentId;
            var notification = notificationRequest.Notification;

            var table = DbTableProvider.Table<JobAssignment>(@event.TableName());

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            var notificationJobAssignment = notification.Content.ToMcmaObject<JobAssignment>();
            jobAssignment.Status = notificationJobAssignment.Status;
            jobAssignment.StatusMessage = notificationJobAssignment.StatusMessage;
            if (notificationJobAssignment.Progress.HasValue)
                jobAssignment.Progress = notificationJobAssignment.Progress;
            jobAssignment.JobOutput = notificationJobAssignment.JobOutput;
            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobAssignmentId, jobAssignment);

            var resourceManager = ResourceManagerProvider.Get(@event);

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
