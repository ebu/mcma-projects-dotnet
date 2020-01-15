using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.JobRepository.Worker
{
    internal class CreateJobProcess : WorkerOperation<CreateJobProcessRequest>
    {
        public CreateJobProcess(ProviderCollection providerCollection)
            : base(providerCollection)
        {
        }

        public override string Name => nameof(CreateJobProcess);

        protected override async Task ExecuteAsync(WorkerRequest request, CreateJobProcessRequest createRequest)
        {
            var logger = ProviderCollection.LoggerProvider.Get(request.Tracker);

            logger.Debug("Executing CreateJobProcess operation...");

            var jobId = createRequest.JobId;

            logger.Debug($"Getting job with id {jobId} from db...");
            var table = ProviderCollection.DbTableProvider.Table<Job>(request.TableName());
            var job = await table.GetAsync(jobId);
            logger.Debug($"Successfully retrieved job {jobId} of type {job.Type}.");

            var resourceManager = ProviderCollection.ResourceManagerProvider.Get(request);

            try
            {
                logger.Debug("Creating JobProcess...");

                var jobProcess = new JobProcess {Job = jobId, NotificationEndpoint = new NotificationEndpoint {HttpEndpoint = jobId + "/notifications"}};
                jobProcess = await resourceManager.CreateAsync(jobProcess);

                logger.Debug($"JobProcess.Id = {jobProcess.Id}");

                job.Status = "QUEUED";
                job.JobProcess = jobProcess.Id;
            }
            catch (Exception error)
            {
                logger.Error("Failed to create JobProcess.", error);

                job.Status = JobStatus.Failed;
                job.StatusMessage = $"Failed to create JobProcess due to error '{error}'";
            }

            job.DateModified = DateTime.UtcNow;

            logger.Debug($"Updating job status to {job.Status} and JobProcess ID to {job.JobProcess}...");
            await table.PutAsync(jobId, job);
            logger.Debug($"Successfully updated job. Sending notification to {job.NotificationEndpoint?.HttpEndpoint}...");

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
            logger.Debug($"Notification successfully sent to {job.NotificationEndpoint?.HttpEndpoint}.");
            
            logger.Debug("CreateJobProcess operation completed successfully.");
        }
    }
}
