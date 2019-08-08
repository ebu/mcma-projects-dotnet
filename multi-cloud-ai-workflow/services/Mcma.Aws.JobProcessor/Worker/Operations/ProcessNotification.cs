using System;
using System.Threading.Tasks;
using Mcma.Aws.DynamoDb;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Aws.JobProcessor.Worker
{
    internal class ProcessNotification : WorkerOperationHandler<ProcessNotificationRequest>
    {
        public ProcessNotification(IResourceManagerProvider resourceManagerProvider, IDbTableProvider<JobProcess> dbTableProvider)
        {
            ResourceManagerProvider = resourceManagerProvider;
            DbTableProvider = dbTableProvider;
        }
        
        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDbTableProvider<JobProcess> DbTableProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessNotificationRequest @event)
        {
            var jobProcessId = @event.JobProcessId;
            var notification = @event.Notification;
            var notificationJobData = notification.Content.ToMcmaObject<JobBase>();

            var table = new DynamoDbTable<JobProcess>(request.TableName());

            var jobProcess = await table.GetAsync(jobProcessId);

            // not updating job if it already was marked as completed or failed.
            if (jobProcess.Status == JobStatus.Completed || jobProcess.Status == JobStatus.Failed)
            {
                Logger.Warn("Ignoring update of job process that tried to change state from " + jobProcess.Status + " to " + notificationJobData.Status);
                return;
            }

            jobProcess.Status = notificationJobData.Status;
            jobProcess.StatusMessage = notificationJobData.StatusMessage;
            jobProcess.Progress = notificationJobData.Progress;
            jobProcess.JobOutput = notificationJobData.JobOutput;
            jobProcess.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobProcessId, jobProcess);

            var resourceManager = ResourceManagerProvider.Get(request);

            await resourceManager.SendNotificationAsync(jobProcess, jobProcess.NotificationEndpoint);
        }
    }
}
