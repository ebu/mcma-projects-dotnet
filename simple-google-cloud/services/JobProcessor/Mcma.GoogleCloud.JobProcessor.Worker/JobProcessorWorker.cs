using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Mcma.Worker;

namespace Mcma.GoogleCloud.JobProcessor.Worker
{
    public class JobProcessorWorker : ICloudEventFunction<McmaWorkerRequest>
    {
        public JobProcessorWorker(IMcmaWorker worker)
        {
            Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }
        
        private IMcmaWorker Worker { get; }

        public Task HandleAsync(CloudEvent cloudEvent, McmaWorkerRequest request, CancellationToken cancellationToken)
            => Worker.DoWorkAsync(new McmaWorkerRequestContext(request, cloudEvent.Id));
    }
}
