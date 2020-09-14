using System.Threading.Tasks;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Client;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal class RestartJob : WorkerOperation<JobReference>
    {
        public RestartJob(ProviderCollection providerCollection, DataController dataController, IJobCheckerTrigger jobCheckerTrigger)
            : base(providerCollection)
        {
            DataController = dataController;
            JobCheckerTrigger = jobCheckerTrigger;
        }

        private DataController DataController { get; }

        private IJobCheckerTrigger JobCheckerTrigger { get; }
        
        public override string Name => nameof(RestartJob);
                
        protected override async Task ExecuteAsync(WorkerRequestContext requestContext, JobReference jobReference)
        {
            var resourceManager = ProviderCollection.ResourceManagerProvider.Get(requestContext);

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

            await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint, job.Tracker);
        }
    }
}