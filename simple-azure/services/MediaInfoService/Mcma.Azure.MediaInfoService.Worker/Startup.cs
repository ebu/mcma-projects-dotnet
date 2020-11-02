using Mcma.Azure.Functions.Worker;
using Mcma.Azure.MediaInfoService.Worker;
using Mcma.Azure.MediaInfoService.Worker.Profiles;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.MediaInfoService.Worker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddSingleton<IMediaInfoProcess, MediaInfoProcess>()
                      .AddMcmaAzureFunctionJobAssignmentWorker<AmeJob>(
                          "mediainfo-service-worker",
                          x => x.AddProfile<ExtractTechnicalMetadata>());
    }
}