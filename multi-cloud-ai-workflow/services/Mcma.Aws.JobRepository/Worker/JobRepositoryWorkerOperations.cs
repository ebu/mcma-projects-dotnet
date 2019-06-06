using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using Mcma.Aws.DynamoDb;
using Mcma.Worker;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.JobRepository.Worker
{
    internal static class JobRepositoryWorkerOperations
    {
        public const string CreateJobProcessOperationName = "CreateJobProcess";
        public const string DeleteJobProcessOperationName = "DeleteJobProcess";
        public const string ProcessNotificationOperationName = "ProcessNotification";

        internal static async Task CreateJobProcessAsync(WorkerRequest @event, CreateJobProcessRequest createRequest)
        {
            var jobId = createRequest.JobId;

            var table = new DynamoDbTable<Job>(@event.TableName());
            var job = await table.GetAsync(jobId);

            var resourceManager = @event.GetAwsV4ResourceManager();

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

                job.Status = JobStatus.Failed;
                job.StatusMessage = $"Failed to create JobProcess due to error '{error}'";
            }

            job.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobId, job);

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }

        internal static async Task DeleteJobProcessAsync(WorkerRequest @event, DeleteJobProcessRequest deleteRequest)
        {
            var jobProcessId = deleteRequest.JobProcessId;

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

        internal static async Task ProcessNotificationAsync(WorkerRequest @event, ProcessNotificationRequest notificationRequest)
        {
            var jobId = notificationRequest.JobId;
            var notification = notificationRequest.Notification;
            var notificationJob = notification.Content.ToMcmaObject<JobBase>();

            var table = new DynamoDbTable<Job>(@event.TableName());

            var job = await table.GetAsync(jobId);

            // not updating job if it already was marked as completed or failed.
            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
            {
                Logger.Warn("Ignoring update of job that tried to change state from " + job.Status + " to " + notificationJob.Status);
                return;
            }

            job.Status = notificationJob.Status;
            job.StatusMessage = notificationJob.StatusMessage;
            job.Progress = notificationJob.Progress;
            job.JobOutput = notificationJob.JobOutput;
            job.DateModified = DateTime.UtcNow;

            await table.PutAsync(jobId, job);

            var resourceManager = @event.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }
    }
}
