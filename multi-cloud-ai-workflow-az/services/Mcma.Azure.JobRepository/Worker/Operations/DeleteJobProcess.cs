using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Worker;

namespace Mcma.Azure.JobRepository.Worker
{
    internal class DeleteJobProcess : WorkerOperation<DeleteJobProcessRequest>
    {
        public DeleteJobProcess(ProviderCollection providerCollection)
            : base(providerCollection)
        {
        }

        public override string Name => nameof(DeleteJobProcess);

        protected override async Task ExecuteAsync(WorkerRequest request, DeleteJobProcessRequest deleteRequest)
        {
            var logger = ProviderCollection.LoggerProvider.Get(request.Tracker);
            var jobProcessId = deleteRequest.JobProcessId;

            try
            {
                var resourceManager = ProviderCollection.ResourceManagerProvider.Get(request);

                await resourceManager.DeleteAsync<JobProcess>(jobProcessId);
            }
            catch (Exception error)
            {
                logger.Error(error);
            }
        }
    }
}
