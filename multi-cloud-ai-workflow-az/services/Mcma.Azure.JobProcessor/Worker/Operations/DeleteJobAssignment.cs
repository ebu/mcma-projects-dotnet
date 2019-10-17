using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal class DeleteJobAssignment : WorkerOperationHandler<DeleteJobAssignmentRequest>
    {
        public DeleteJobAssignment(IResourceManagerProvider resourceManagerProvider)
        {
            ResourceManagerProvider = resourceManagerProvider;
        }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, DeleteJobAssignmentRequest deleteRequest)
        {
            var jobAssignmentId = deleteRequest.JobAssignmentId;

            try
            {
                var resourceManager = ResourceManagerProvider.Get(request.Variables);

                await resourceManager.DeleteAsync<JobAssignment>(jobAssignmentId);
            }
            catch (Exception error)
            {
                request.Logger.Exception(error);
            }
        }
    }
}
