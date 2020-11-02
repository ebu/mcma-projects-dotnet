using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Client;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal class FailJob : McmaWorkerOperation<JobFailure>
    {
        public FailJob(IResourceManagerProvider resourceManagerProvider, IDataController dataController)
        {
            ResourceManagerProvider = resourceManagerProvider ?? throw new ArgumentNullException(nameof(resourceManagerProvider));
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
        }
        
        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDataController DataController { get; }

        public override string Name => nameof(FailJob);

        protected override async Task ExecuteAsync(McmaWorkerRequestContext requestContext, JobFailure jobFailure)
        {
            var logger = requestContext.Logger;
            var resourceManager = ResourceManagerProvider.Get(requestContext.Tracker);
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
                        var client = await resourceManager.GetResourceEndpointClientAsync(jobExecution.JobAssignmentId);
                        await client.PostAsync(null, $"{jobExecution.JobAssignmentId}/cancel");
                    }
                    catch (Exception error)
                    {
                        requestContext.Logger.Warn($"Canceling job assignment '{jobExecution.JobAssignmentId} failed");
                        requestContext.Logger.Warn(error);
                    }
                }

                if (!jobExecution.ActualEndDate.HasValue)
                    jobExecution.ActualEndDate = DateTimeOffset.UtcNow;

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

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }
    }
}