using System.Threading.Tasks;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Client;
using Mcma.Worker;

namespace Mcma.Aws.JobProcessor.Worker
{
    internal class CancelJob : WorkerOperation<JobReference>
    {
        private DataController DataController { get; }

        public CancelJob(ProviderCollection providerCollection, DataController dataController)
            : base(providerCollection)
        {
            DataController = dataController;
        }

        public override string Name => nameof(CancelJob);

        protected override async Task ExecuteAsync(WorkerRequestContext requestContext, JobReference jobReference)
        {
            var resourceManager = ProviderCollection.ResourceManagerProvider.Get(requestContext);

            var mutex = await DataController.CreateMutexAsync(jobReference.JobId, requestContext.RequestId);

            await mutex.LockAsync();
            try
            {
                var job = await DataController.GetJobAsync(jobReference.JobId);
                if (job == null)
                    throw new McmaException($"Job with ID '{jobReference.JobId}' not found.");

                var jobExecutor = new JobExecutor(DataController, resourceManager, requestContext);

                job = await jobExecutor.CancelExecutionAsync(jobReference, job);

                await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint, job.Tracker);
            }
            finally
            {
                await mutex.UnlockAsync();
            }
        }
    }
}