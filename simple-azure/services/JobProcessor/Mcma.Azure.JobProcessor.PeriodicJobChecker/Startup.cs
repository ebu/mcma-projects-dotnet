using Mcma.Azure.JobProcessor.Common;
using Mcma.Azure.JobProcessor.PeriodicJobChecker;
using Mcma.Azure.Logger;
using Mcma.Azure.WorkerInvoker;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.JobProcessor.PeriodicJobChecker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddMcmaAppInsightsLogging("job-processor-periodic-job-cleanup")
                      .AddMcmaQueueWorkerInvoker()
                      .AddDataController()
                      .AddSingleton<IJobCheckerTrigger, LogicAppWorkflowCheckerTrigger>();
    }
}
