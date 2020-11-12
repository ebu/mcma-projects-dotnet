using System;
using System.Threading.Tasks;
using Mcma.GoogleCloud.JobProcessor.Common;
using Mcma.Client;
using Mcma.Worker;

namespace Mcma.GoogleCloud.JobProcessor.Worker
{
    internal class RestartJob : McmaWorkerOperation<JobReference>
    {
        public RestartJob(IResourceManagerProvider resourceManagerProvider, IDataController dataController, IJobCheckerTrigger jobCheckerTrigger)
        {
            ResourceManagerProvider = resourceManagerProvider ?? throw new ArgumentNullException(nameof(resourceManagerProvider));
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
            JobCheckerTrigger = jobCheckerTrigger ?? throw new ArgumentNullException(nameof(jobCheckerTrigger));
        }
        
        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDataController DataController { get; }

        private IJobCheckerTrigger JobCheckerTrigger { get; }
        
        public override string Name => nameof(RestartJob);
                
        protected override async Task ExecuteAsync(McmaWorkerRequestContext requestContext, JobReference jobReference)
        {
            var resourceManager = ResourceManagerProvider.Get(requestContext.Tracker);

            var mutex = await DataController.CreateMutexAsync(jobReference.JobId, requestContext.RequestId);

            await mutex.LockAsync();
            Job job;
            try
            {
                job = await DataController.GetJobAsync(jobReference.JobId);
                if (job == null)
                    throw new McmaException($"Job with ID '{jobReference.JobId}' not found.");
                
                var jobExecutor = new JobExecutor(DataController, resourceManager, requestContext);

                job = await jobExecutor.CancelExecutionAsync(jobReference, job);

                job = await jobExecutor.StartExecutionAsync(jobReference, job, JobCheckerTrigger);
            }
            finally
            {
                await mutex.UnlockAsync();
            }

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
        }
    }
}