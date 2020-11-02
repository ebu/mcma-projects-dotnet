using Amazon.Lambda.Core;
using Mcma.Aws.Functions;
using Mcma.Aws.Functions.ApiHandler;
using Mcma.Aws.Functions.Worker;
using Mcma.Aws.Lambda;
using Mcma.Worker;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.FFmpegService.Worker
{
    public class FFmpegServiceWorker : McmaLambdaFunction<McmaLambdaWorker, McmaWorkerRequest>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddMcmaAwsLambdaJobAssignmentWorker<TransformJob>(
                "ffmpeg-service-worker",
                builder => builder.AddProfile<ExtractThumbnail>());
        }
    }
}
