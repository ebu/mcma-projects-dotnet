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
    internal class ProcessNotification : WorkerOperation<ProcessNotificationRequest>
    {
        public ProcessNotification(ProviderCollection providerCollection)
            : base(providerCollection)
        {
        }

        public override string Name => nameof(ProcessNotification);

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessNotificationRequest notificationRequest)
        {
            var jobAssignmentId = notificationRequest.JobAssignmentId;
            var notification = notificationRequest.Notification;

            var workflowStatePayload = notification.Content.ToMcmaObject<WorkflowState>();

            var table = ProviderCollection.DbTableProvider.Table<JobAssignment>(request.TableName());

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            jobAssignment.Status = workflowStatePayload.Status?.ToUpper();
            jobAssignment.StatusMessage = workflowStatePayload.Errors?.ToString();

            if (workflowStatePayload.Progress != null)
                jobAssignment.Progress = workflowStatePayload.Progress;

            if (workflowStatePayload.Output != null)
            {
                if (jobAssignment.JobOutput == null)
                    jobAssignment.JobOutput = new JobParameterBag();

                foreach (var output in workflowStatePayload.Output)
                    jobAssignment.JobOutput[output.Key] = output.Value;
            }

            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobAssignmentId, jobAssignment);

            var resourceManager = ProviderCollection.ResourceManagerProvider.Get(request);

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
