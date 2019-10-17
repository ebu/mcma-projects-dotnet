using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Worker;

namespace Mcma.Azure.JobRepository.Worker
{
    internal class DeleteJobProcess : WorkerOperationHandler<DeleteJobProcessRequest>
    {
        public DeleteJobProcess(IResourceManagerProvider resourceManagerProvider)
        {
            ResourceManagerProvider = resourceManagerProvider;
        }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, DeleteJobProcessRequest deleteRequest)
        {
            var jobProcessId = deleteRequest.JobProcessId;

            try
            {
                var resourceManager = ResourceManagerProvider.Get(request.Variables);

                await resourceManager.DeleteAsync<JobProcess>(jobProcessId);
            }
            catch (Exception error)
            {
                request.Logger.Exception(error);
            }
        }
    }
}
