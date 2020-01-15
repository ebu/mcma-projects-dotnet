using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal class ProcessNotification : WorkerOperation<ProcessNotificationRequest>
    {
        public ProcessNotification(ProviderCollection providerCollection)
            : base(providerCollection)
        {
        }
        
        public override string Name => nameof(ProcessNotification);

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessNotificationRequest @event)
        {
            var jobProcessId = @event.JobProcessId;
            var notification = @event.Notification;
            var notificationJobData = notification.Content.ToMcmaObject<JobBase>();

            var logger = ProviderCollection.LoggerProvider.Get(request.Tracker);

            var table = ProviderCollection.DbTableProvider.Table<JobProcess>(request.TableName());

            var jobProcess = await table.GetAsync(jobProcessId);

            // not updating job if it already was marked as completed or failed.
            if (jobProcess.Status == JobStatus.Completed || jobProcess.Status == JobStatus.Failed)
            {
                logger.Warn("Ignoring update of job process that tried to change state from " + jobProcess.Status + " to " + notificationJobData.Status);
                return;
            }

            jobProcess.Status = notificationJobData.Status;
            jobProcess.StatusMessage = notificationJobData.StatusMessage;
            jobProcess.Progress = notificationJobData.Progress;
            jobProcess.JobOutput = notificationJobData.JobOutput;
            jobProcess.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobProcessId, jobProcess);

            var resourceManager = ProviderCollection.ResourceManagerProvider.Get(request);

            await resourceManager.SendNotificationAsync(jobProcess, jobProcess.NotificationEndpoint);
        }
    }
}
