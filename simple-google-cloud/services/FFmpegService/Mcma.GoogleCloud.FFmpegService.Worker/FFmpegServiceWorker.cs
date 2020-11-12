using System;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Mcma.GoogleCloud.Functions.Worker;
using Mcma.Worker;

namespace Mcma.GoogleCloud.FFmpegService.Worker
{
    public class FFmpegServiceWorkerStartup : McmaJobAssignmentWorkerStartup<TransformJob>
    {
        protected override string ApplicationName => "ffmpeg-service-worker";

        protected override void AddProfiles(ProcessJobAssignmentOperationBuilder<TransformJob> builder)
            => builder.AddProfile<ExtractThumbnail>();
    }

    [FunctionsStartup(typeof(FFmpegServiceWorkerStartup))]
    public class FFmpegServiceWorker : ICloudEventFunction<McmaWorkerRequest>
    {
        public FFmpegServiceWorker(IMcmaWorker worker)
        {
            Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }
     
        private IMcmaWorker Worker { get; }

        public Task HandleAsync(CloudEvent cloudEvent, McmaWorkerRequest workerRequest, CancellationToken cancellationToken)
            => Worker.DoWorkAsync(new McmaWorkerRequestContext(workerRequest, cloudEvent.Id));
    }
}
