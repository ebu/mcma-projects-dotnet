using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Mcma.Aws.JobRepository.Worker
{
    internal class JobRepositoryWorker : Mcma.Worker.Worker<JobRepositoryWorkerRequest>
    {
        protected override IDictionary<string, Func<JobRepositoryWorkerRequest, Task>> Operations { get; } =
            new Dictionary<string, Func<JobRepositoryWorkerRequest, Task>>
            {
                ["CreateJobProcess"] = CreateJobProcessAsync,
                ["DeleteJobProcess"] = DeleteJobProcessAsync,
                ["ProcessNotification"] = ProcessNotificationAsync
            };

        internal static async Task CreateJobProcessAsync(JobRepositoryWorkerRequest @event)
        {
            var jobId = @event.JobId;

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);
            var job = await table.GetAsync<Job>(jobId);

            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            try
            {
                var jobProcess = new JobProcess {Job = jobId, NotificationEndpoint = new NotificationEndpoint {HttpEndpoint = jobId + "/notifications"}};
                jobProcess = await resourceManager.CreateAsync(jobProcess);

                job.Status = "QUEUED";
                job.JobProcess = jobProcess.Id;
            }
            catch (Exception error)
            {
                Logger.Error("Failed to create JobProcess.");
                Logger.Exception(error);

                job.Status = "FAILED";
                job.StatusMessage = $"Failed to create JobProcess due to error '{error}'";
            }

            job.DateModified = DateTime.UtcNow;

            await table.PutAsync<Job>(jobId, job);

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }

        internal static async Task DeleteJobProcessAsync(JobRepositoryWorkerRequest @event)
        {
            var jobProcessId = @event.JobProcessId;

            try
            {
                var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

                await resourceManager.DeleteAsync<JobProcess>(jobProcessId);
            }
            catch (Exception error)
            {
                Logger.Exception(error);
            }
        }

        internal static async Task ProcessNotificationAsync(JobRepositoryWorkerRequest @event)
        {
            var jobId = @event.JobId;
            var notification = @event.Notification;
            var notificationJob = notification.Content.ToMcmaObject<JobBase>();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var job = await table.GetAsync<Job>(jobId);

            // not updating job if it already was marked as completed or failed.
            if (job.Status == "COMPLETED" || job.Status == "FAILED")
            {
                Logger.Warn("Ignoring update of job that tried to change state from " + job.Status + " to " + notificationJob.Status);
                return;
            }

            job.Status = notificationJob.Status;
            job.StatusMessage = notificationJob.StatusMessage;
            job.Progress = notificationJob.Progress;
            job.JobOutput = notificationJob.JobOutput;
            job.DateModified = DateTime.UtcNow;

            await table.PutAsync<Job>(jobId, job);

            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }
    }
}
