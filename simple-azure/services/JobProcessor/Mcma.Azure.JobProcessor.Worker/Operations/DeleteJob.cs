using System;
using System.Threading.Tasks;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Client;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal class DeleteJob : McmaWorkerOperation<JobReference>
    {
        public DeleteJob(IResourceManagerProvider resourceManagerProvider, IDataController dataController)
        {
            ResourceManagerProvider = resourceManagerProvider ?? throw new ArgumentNullException(nameof(resourceManagerProvider));
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
        }
        
        private IResourceManagerProvider ResourceManagerProvider { get; }

        private IDataController DataController { get; }

        public override string Name => nameof(DeleteJob);

        protected override async Task ExecuteAsync(McmaWorkerRequestContext requestContext, JobReference jobReference)
        {
            var jobId = jobReference.JobId;
            
            var logger = requestContext.Logger;
            var resourceManager = ResourceManagerProvider.Get(requestContext.Tracker);

            var mutex = await DataController.CreateMutexAsync(jobReference.JobId, requestContext.RequestId);

            await mutex.LockAsync();
            try
            {
                var job = await DataController.GetJobAsync(jobId);
                if (job == null)
                    throw new McmaException($"Job with ID '{jobId}' not found.");

                var executions = await DataController.GetExecutionsAsync(jobId);

                foreach (var execution in executions.Results)
                {
                    if (execution.JobAssignmentId != null)
                    {
                        try
                        {
                            await resourceManager.DeleteAsync<JobAssignment>(execution.JobAssignmentId);
                        }
                        catch (Exception error)
                        {
                            logger.Warn($"Failed to delete job assignment {execution.JobAssignmentId}");
                            logger.Warn(error);
                        }
                    }
                    await DataController.DeleteExecutionAsync(execution.Id);
                }

                await DataController.DeleteJobAsync(job.Id);
            }
            finally
            {
                await mutex.UnlockAsync();
            }
        }
    }
}