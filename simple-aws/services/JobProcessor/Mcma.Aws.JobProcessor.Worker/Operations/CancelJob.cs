using System;
using System.Threading.Tasks;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Client;
using Mcma.Worker;

namespace Mcma.Aws.JobProcessor.Worker
{
    internal class CancelJob : McmaWorkerOperation<JobReference>
    {
        public CancelJob(IResourceManagerProvider resourceManagerProvider, IDataController dataController)
        {
            ResourceManagerProvider = resourceManagerProvider ?? throw new ArgumentNullException(nameof(resourceManagerProvider));
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
        }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDataController DataController { get; }

        public override string Name => nameof(CancelJob);

        protected override async Task ExecuteAsync(McmaWorkerRequestContext requestContext, JobReference jobReference)
        {
            var resourceManager = ResourceManagerProvider.Get(requestContext.Tracker);

            var mutex = await DataController.CreateMutexAsync(jobReference.JobId, requestContext.RequestId);

            await mutex.LockAsync();
            try
            {
                var job = await DataController.GetJobAsync(jobReference.JobId);
                if (job == null)
                    throw new McmaException($"Job with ID '{jobReference.JobId}' not found.");

                var jobExecutor = new JobExecutor(DataController, resourceManager, requestContext);

                job = await jobExecutor.CancelExecutionAsync(jobReference, job);

                await resourceManager.SendNotificationAsync(job, job.NotificationEndpoint);
            }
            finally
            {
                await mutex.UnlockAsync();
            }
        }
    }
}