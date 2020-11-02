using Mcma.Azure.Functions.Worker;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Azure.JobProcessor.Worker;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Mcma.Azure.JobProcessor.Worker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
            => builder.Services
                      .AddDataController()
                      .AddSingleton<IJobCheckerTrigger, LogicAppWorkflowCheckerTrigger>()
                      .AddMcmaAzureFunctionWorker("job-processor-worker",
                                                  workerBuilder =>
                                                      workerBuilder
                                                          .AddOperation<StartJob>()
                                                          .AddOperation<CancelJob>()
                                                          .AddOperation<RestartJob>()
                                                          .AddOperation<FailJob>()
                                                          .AddOperation<DeleteJob>()
                                                          .AddOperation<ProcessNotification>());
    }
}