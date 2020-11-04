using Mcma.Api.Routing.Defaults;
using Mcma.Azure.Functions.ApiHandler;
using Mcma.Azure.MediaInfoService.ApiHandler;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.MediaInfoService.ApiHandler
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services.AddMcmaAzureFunctionJobAssignmentApiHandler("mediainfo-service-api-handler");
    }
}
