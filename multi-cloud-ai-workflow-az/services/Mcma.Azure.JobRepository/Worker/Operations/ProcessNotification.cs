using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.JobRepository.Worker
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
            var jobId = notificationRequest.JobId;
            var notification = notificationRequest.Notification;
            var notificationJob = notification.Content.ToMcmaObject<JobBase>();

            var table = DbTableProvider.Table<Job>(request.Variables.TableName());

            var job = await table.GetAsync(jobId);

            // not updating job if it already was marked as completed or failed.
            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
            {
                request.Logger.Warn("Ignoring update of job that tried to change state from " + job.Status + " to " + notificationJob.Status);
                return;
            }

            job.Status = notificationJob.Status;
            job.StatusMessage = notificationJob.StatusMessage;
            job.Progress = notificationJob.Progress;
            job.JobOutput = notificationJob.JobOutput;
            job.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobId, job);

            var resourceManager = ResourceManagerProvider.Get(request.Variables);

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }
    }
}
