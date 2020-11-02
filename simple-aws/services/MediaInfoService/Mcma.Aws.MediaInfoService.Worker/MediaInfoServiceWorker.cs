using Amazon.Lambda.Core;
using Mcma.Aws.Functions;
using Mcma.Aws.Functions.ApiHandler;
using Mcma.Aws.Functions.Worker;
using Mcma.Aws.Lambda;
using Mcma.Aws.MediaInfoService.Worker.Profiles;
using Mcma.Worker;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.MediaInfoService.Worker
{
    public class MediaInfoServiceWorker : McmaLambdaFunction<McmaLambdaWorker, McmaWorkerRequest>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddMcmaAwsLambdaJobAssignmentWorker<AmeJob>(
                "mediainfo-service-worker",
                x => x.AddProfile<ExtractTechnicalMetadata>());
        }
    }
}
