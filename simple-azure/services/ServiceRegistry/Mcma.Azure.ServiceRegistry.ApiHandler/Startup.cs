using Mcma.Api.Routing.Defaults.Routes;
using Mcma.Azure.Functions.ApiHandler;
using Mcma.Azure.ServiceRegistry.ApiHandler;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.ServiceRegistry.ApiHandler
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddMcmaAzureFunctionApiHandler(
                          "service-registry-api-handler",
                          apiBuilder =>
                              apiBuilder.AddDefaultRoutes<Service>()
                                        .AddDefaultRoutes<JobProfile>());
    }
}
