using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Aws.JobRepository.Worker
{
    internal class CreateJobProcess : WorkerOperationHandler<CreateJobProcessRequest>
    {
        public CreateJobProcess(IResourceManagerProvider resourceManagerProvider, IDbTableProvider<Job> dbTableProvider)
        {
            ResourceManagerProvider = resourceManagerProvider;
            DbTableProvider = dbTableProvider;
        }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDbTableProvider<Job> DbTableProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest @event, CreateJobProcessRequest createRequest)
        {
            var jobId = createRequest.JobId;

            var table = DbTableProvider.Table(@event.TableName());
            var job = await table.GetAsync(jobId);

            var resourceManager = ResourceManagerProvider.Get(@event);

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
    }
}
