using Mcma.Azure.Functions.Worker;
using Mcma.Azure.FFmpegService.Worker;
using Mcma.Worker;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.FFmpegService.Worker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddSingleton<IFFmpegProcess, FFmpegProcess>()
                      .AddMcmaAzureFunctionWorker(
                          "ffmpeg-service-worker",
                          workerBuilder =>
                              workerBuilder.AddProcessJobAssignmentOperation<TransformJob>(
                                  x =>
                                      x.AddProfile<ExtractThumbnail>()));
    }
}
