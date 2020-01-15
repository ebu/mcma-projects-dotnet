using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.JobRepository.Worker
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
            var logger = ProviderCollection.LoggerProvider.Get(request.Tracker);

            var jobId = notificationRequest.JobId;
            var notification = notificationRequest.Notification;
            var notificationJob = notification.Content.ToMcmaObject<JobBase>();

            var table = ProviderCollection.DbTableProvider.Table<Job>(request.TableName());

            var job = await table.GetAsync(jobId);

            // not updating job if it already was marked as completed or failed.
            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
            {
                logger.Warn("Ignoring update of job that tried to change state from " + job.Status + " to " + notificationJob.Status);
                return;
            }

            job.Status = notificationJob.Status;
            job.StatusMessage = notificationJob.StatusMessage;
            job.Progress = notificationJob.Progress;
            job.JobOutput = notificationJob.JobOutput;
            job.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobId, job);

            var resourceManager = ProviderCollection.ResourceManagerProvider.Get(request);

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }
    }
}
