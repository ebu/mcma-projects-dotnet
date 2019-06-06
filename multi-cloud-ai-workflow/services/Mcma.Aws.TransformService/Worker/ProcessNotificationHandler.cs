using System;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Aws.DynamoDb;
using Mcma.Worker;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.TransformService.Worker
{
    internal class ProcessNotificationHandler : WorkerOperationHandler<ProcessNotificationRequest>
    {
        public const string OperationName = "ProcessNotification";

        protected override async Task ExecuteAsync(WorkerRequest @event, ProcessNotificationRequest notificationRequest)
        {
            var jobAssignmentId = notificationRequest.JobAssignmentId;
            var notification = notificationRequest.Notification;

            var table = new DynamoDbTable<JobAssignment>(@event.TableName());

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            var notificationJobAssignment = notification.Content.ToMcmaObject<JobAssignment>();
            jobAssignment.Status = notificationJobAssignment.Status;
            jobAssignment.StatusMessage = notificationJobAssignment.StatusMessage;
            if (notificationJobAssignment.Progress.HasValue)
                jobAssignment.Progress = notificationJobAssignment.Progress;
            jobAssignment.JobOutput = notificationJobAssignment.JobOutput;
            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobAssignmentId, jobAssignment);

            var resourceManager = @event.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
