using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal class DeleteJobAssignment : WorkerOperation<DeleteJobAssignmentRequest>
    {
        public DeleteJobAssignment(ProviderCollection providerCollection)
            : base(providerCollection)
        {
        }

        public override string Name => nameof(DeleteJobAssignment);

        protected override async Task ExecuteAsync(WorkerRequest request, DeleteJobAssignmentRequest deleteRequest)
        {
            var logger = ProviderCollection.LoggerProvider.Get(request.Tracker);
            var jobAssignmentId = deleteRequest.JobAssignmentId;

            try
            {
                var resourceManager = ProviderCollection.ResourceManagerProvider.Get(request);

                await resourceManager.DeleteAsync<JobAssignment>(jobAssignmentId);
            }
            catch (Exception error)
            {
                logger.Error(error);
            }
        }
    }
}
