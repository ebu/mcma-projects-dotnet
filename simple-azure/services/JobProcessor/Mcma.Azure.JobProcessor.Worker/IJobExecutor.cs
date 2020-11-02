using System.Threading.Tasks;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal interface IJobExecutor
    {
        Task<Job> StartExecutionAsync(McmaWorkerRequestContext requestContext, JobReference jobReference, Job job);

        Task<Job> CancelExecutionAsync(McmaWorkerRequestContext requestContext, JobReference jobReference, Job job);
    }
}