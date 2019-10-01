using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.JobRepository.Worker
{
    internal class CreateJobProcess : WorkerOperationHandler<CreateJobProcessRequest>
    {
        public CreateJobProcess(IResourceManagerProvider resourceManagerProvider, IDbTableProvider dbTableProvider)
        {
            ResourceManagerProvider = resourceManagerProvider;
            DbTableProvider = dbTableProvider;
        }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDbTableProvider DbTableProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest @event, CreateJobProcessRequest createRequest)
        {
            Logger.Debug("Executing CreateJobProcess operation...");

            var jobId = createRequest.JobId;

            Logger.Debug($"Getting job with id {jobId} from db...");
            var table = DbTableProvider.Table<Job>(@event.TableName());
            var job = await table.GetAsync(jobId);
            Logger.Debug($"Successfully retrieved job {jobId} of type {job.Type}.");

            var resourceManager = ResourceManagerProvider.Get(@event);

            try
            {
                Logger.Debug("Creating JobProcess...");

                var jobProcess = new JobProcess {Job = jobId, NotificationEndpoint = new NotificationEndpoint {HttpEndpoint = jobId + "/notifications"}};
                jobProcess = await resourceManager.CreateAsync(jobProcess);

                Logger.Debug($"JobProcess.Id = {jobProcess.Id}");

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

            Logger.Debug($"Updating job status to {job.Status} and JobProcess ID to {job.JobProcess}...");
            await table.PutAsync(jobId, job);
            Logger.Debug($"Successfully updated job. Sending notification to {job.NotificationEndpoint?.HttpEndpoint}...");

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
            Logger.Debug($"Notification successfully sent to {job.NotificationEndpoint?.HttpEndpoint}.");
            
            Logger.Debug("CreateJobProcess operation completed successfully.");
        }
    }
}
