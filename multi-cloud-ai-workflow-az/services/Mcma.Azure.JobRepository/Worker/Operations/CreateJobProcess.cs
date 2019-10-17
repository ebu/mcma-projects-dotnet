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
    internal class CreateJobProcess : WorkerOperationHandler<CreateJobProcessRequest>
    {
        public CreateJobProcess(IResourceManagerProvider resourceManagerProvider, IDbTableProvider dbTableProvider)
        {
            ResourceManagerProvider = resourceManagerProvider;
            DbTableProvider = dbTableProvider;
        }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDbTableProvider DbTableProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, CreateJobProcessRequest createRequest)
        {
            request.Logger.Debug("Executing CreateJobProcess operation...");

            var jobId = createRequest.JobId;

            request.Logger.Debug($"Getting job with id {jobId} from db...");
            var table = DbTableProvider.Table<Job>(request.Variables.TableName());
            var job = await table.GetAsync(jobId);
            request.Logger.Debug($"Successfully retrieved job {jobId} of type {job.Type}.");

            var resourceManager = ResourceManagerProvider.Get(request.Variables);

            try
            {
                request.Logger.Debug("Creating JobProcess...");

                var jobProcess = new JobProcess {Job = jobId, NotificationEndpoint = new NotificationEndpoint {HttpEndpoint = jobId + "/notifications"}};
                jobProcess = await resourceManager.CreateAsync(jobProcess);

                request.Logger.Debug($"JobProcess.Id = {jobProcess.Id}");

                job.Status = "QUEUED";
                job.JobProcess = jobProcess.Id;
            }
            catch (Exception error)
            {
                request.Logger.Error("Failed to create JobProcess.");
                request.Logger.Exception(error);

                job.Status = JobStatus.Failed;
                job.StatusMessage = $"Failed to create JobProcess due to error '{error}'";
            }

            job.DateModified = DateTime.UtcNow;

            request.Logger.Debug($"Updating job status to {job.Status} and JobProcess ID to {job.JobProcess}...");
            await table.PutAsync(jobId, job);
            request.Logger.Debug($"Successfully updated job. Sending notification to {job.NotificationEndpoint?.HttpEndpoint}...");

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
            request.Logger.Debug($"Notification successfully sent to {job.NotificationEndpoint?.HttpEndpoint}.");
            
            request.Logger.Debug("CreateJobProcess operation completed successfully.");
        }
    }
}
