using Mcma.Azure.Functions.ApiHandler;
using Mcma.Azure.JobProcessor.ApiHandler;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Azure.WorkerInvoker;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddMcmaQueueWorkerInvoker()
                      .AddDataController()
                      .AddMcmaAzureFunctionApiHandler(
                          "job-processor-api-handler",
                          apiBuilder =>
                              apiBuilder.AddRouteCollection<JobRoutes>()
                                        .AddRouteCollection<JobExecutionRoutes>());
    }
}
