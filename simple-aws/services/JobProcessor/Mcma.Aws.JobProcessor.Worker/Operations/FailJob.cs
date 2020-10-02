using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Client;
using Mcma.Worker;

namespace Mcma.Aws.JobProcessor.Worker
{
    internal class FailJob : WorkerOperation<JobFailure>
    {
        public FailJob(ProviderCollection providerCollection, DataController dataController)
            : base(providerCollection)
        {
            DataController = dataController;
        }

        private DataController DataController { get; }

        public override string Name => nameof(FailJob);

        protected override async Task ExecuteAsync(WorkerRequestContext requestContext, JobFailure jobFailure)
        {
            var logger = requestContext.Logger;
            var resourceManager = ProviderCollection.ResourceManagerProvider.Get(requestContext);
            var jobEventLogger = new JobEventLogger(logger, resourceManager);

            var mutex = await DataController.CreateMutexAsync(jobFailure.JobId, requestContext.RequestId);

            await mutex.LockAsync();
            Job job;
            try
            {
                job = await DataController.GetJobAsync(jobFailure.JobId);
                if (job == null)
                    throw new McmaException($"Job with ID '{jobFailure.JobId}' not found.");

                if (job.Status == JobStatus.Completed || job.Status == JobStatus.Canceled || job.Status == JobStatus.Failed)
                    return;

                var jobExecution = (await DataController.GetExecutionsAsync(job.Id)).Results.FirstOrDefault();
                if (jobExecution == null)
                {
                    requestContext.Logger.Warn($"Job with ID '{jobFailure.JobId}' does not have any active executions.");
                    return;
                }

                if (jobExecution.JobAssignmentId != null)
                {
                    try
                    {
                        var client = await resourceManager.GetResourceEndpointAsync(jobExecution.JobAssignmentId);
                        await client.PostAsync(null, $"{jobExecution.JobAssignmentId}/cancel");
                    }
                    catch (Exception error)
                    {
                        requestContext.Logger.Warn($"Canceling job assignment '{jobExecution.JobAssignmentId} failed");
                        requestContext.Logger.Warn(error);
                    }
                }

                if (!jobExecution.ActualEndDate.HasValue)
                    jobExecution.ActualEndDate = DateTime.UtcNow;

                jobExecution.ActualDuration =
                    jobExecution.ActualStartDate.HasValue
                        ? (long)(jobExecution.ActualEndDate.Value - jobExecution.ActualStartDate.Value).TotalMilliseconds
                        : 0;

                jobExecution.Status = JobStatus.Failed;
                jobExecution.Error = jobFailure.Error;
                await DataController.UpdateExecutionAsync(jobExecution);

                job.Status = JobStatus.Failed;
                job.Error = jobFailure.Error;
                await DataController.UpdateJobAsync(job);

                await jobEventLogger.LogJobEventAsync(job, jobExecution);
            }
            finally
            {
                await mutex.UnlockAsync();
            }

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint, job.Tracker);
        }
    }
}