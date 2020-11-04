using System.Threading.Tasks;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;

namespace Mcma.Azure.MediaInfoService.Worker
{
    public class MediaInfoServiceWorker
    {
        public MediaInfoServiceWorker(IMcmaWorker worker)
        {
            Worker = worker;
        }
        private IMcmaWorker Worker { get; }
            
        [FunctionName(nameof(MediaInfoServiceWorker))]
        public async Task ExecuteAsync(
            [QueueTrigger("mediainfo-service-work-queue", Connection = "MCMA_WORK_QUEUE_STORAGE")] McmaWorkerRequest workerRequest,
            ExecutionContext executionContext)
        {
            await Worker.DoWorkAsync(new McmaWorkerRequestContext(workerRequest, executionContext.InvocationId.ToString()));
        }
    }
}
