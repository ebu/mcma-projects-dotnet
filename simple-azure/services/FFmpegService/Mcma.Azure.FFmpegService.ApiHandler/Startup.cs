using Mcma.Api.Routing.Defaults;
using Mcma.Azure.Functions.ApiHandler;
using Mcma.Azure.FFmpegService.ApiHandler;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.FFmpegService.ApiHandler
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddMcmaAzureFunctionApiHandler(
                          "ffmpeg-service-api-handler",
                          apiBuilder => apiBuilder.AddDefaultJobAssignmentRoutes());
    }
}
