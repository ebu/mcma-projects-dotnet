using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Mcma.GoogleCloud.Functions;
using Mcma.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.MediaInfoService.Worker
{
    public class MediaInfoServiceWorker : ICloudEventFunction<McmaWorkerRequest>
    {
        public MediaInfoServiceWorker(IMcmaWorker worker)
        {
            Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }

        private IMcmaWorker Worker { get; }

        public Task HandleAsync(CloudEvent cloudEvent, McmaWorkerRequest request, CancellationToken cancellationToken)
            => Worker.DoWorkAsync(new McmaWorkerRequestContext(request, cloudEvent.Id));
    }
}
