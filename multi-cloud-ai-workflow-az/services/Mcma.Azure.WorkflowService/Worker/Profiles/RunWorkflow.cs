using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Worker;

namespace Mcma.Azure.WorkflowService.Worker
{
    internal class RunWorkflow : IJobProfileHandler<WorkflowJob>
    {
        public async Task ExecuteAsync(WorkerJobHelper<WorkflowJob> jobHelper)
        {
            
        }
    }
}
