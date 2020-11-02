using System;
using System.Threading.Tasks;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;

namespace Mcma.Azure.FFmpegService.Worker
{
    public class FFmpegServiceWorker
    {
        public FFmpegServiceWorker(IMcmaWorker worker)
        {
            Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }

        private IMcmaWorker Worker { get; }
            
        [FunctionName(nameof(FFmpegServiceWorker))]
        public async Task ExecuteAsync(
            [QueueTrigger("ffmpeg-service-work-queue", Connection = "WorkQueueStorage")] McmaWorkerRequest workerRequest,
            ExecutionContext executionContext)
        {
            await Worker.DoWorkAsync(new McmaWorkerRequestContext(workerRequest, executionContext.InvocationId.ToString()));
        }
    }
}
