using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Newtonsoft.Json.Linq;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Aws.DynamoDb;
using Mcma.Worker;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.WorkflowService.Worker
{
    internal class ProcessNotificationHandler : WorkerOperationHandler<ProcessNotificationRequest>
    {
        public const string OperationName = "ProcessNotification";

        protected override async Task ExecuteAsync(WorkerRequest @event, ProcessNotificationRequest notificationRequest)
        {
            var jobAssignmentId = notificationRequest.JobAssignmentId;
            var notification = notificationRequest.Notification;
            var notificationJobPayload = notification.Content.ToMcmaObject<JobBase>();

            var table = new DynamoDbTable<JobAssignment>(@event.TableName());

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            jobAssignment.Status = notificationJobPayload.Status;
            jobAssignment.StatusMessage = notificationJobPayload.StatusMessage;
            if (notificationJobPayload.Progress != null)
                jobAssignment.Progress = notificationJobPayload.Progress;

            jobAssignment.JobOutput = notificationJobPayload.JobOutput;
            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobAssignmentId, jobAssignment);

            var resourceManager = @event.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
