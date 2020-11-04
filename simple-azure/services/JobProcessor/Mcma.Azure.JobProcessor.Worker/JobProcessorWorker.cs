using System;
using System.Threading.Tasks;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;

namespace Mcma.Azure.JobProcessor.Worker
{
    public class JobProcessorWorker
    {
        public JobProcessorWorker(IMcmaWorker worker)
        {
            Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }

        private IMcmaWorker Worker { get; }

        [FunctionName(nameof(JobProcessorWorker))]
        public Task ExecuteAsync(
            [QueueTrigger("job-processor-work-queue", Connection = "MCMA_WORK_QUEUE_STORAGE")] McmaWorkerRequest request,
            ExecutionContext executionContext)
        {
            return Worker.DoWorkAsync(new McmaWorkerRequestContext(request, executionContext.InvocationId.ToString()));
        }
    }
}
