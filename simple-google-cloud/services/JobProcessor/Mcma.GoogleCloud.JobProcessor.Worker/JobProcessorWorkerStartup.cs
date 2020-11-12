using Mcma.GoogleCloud.Functions.Worker;
using Mcma.GoogleCloud.JobProcessor.Common;
using Mcma.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.JobProcessor.Worker
{
    public class JobProcessorWorkerStartup : McmaWorkerStartup
    {
        protected override string ApplicationName => "job-processor-worker";
        
        protected override IServiceCollection ConfigureAdditionalServices(IServiceCollection services)
            => services
                .AddDataController()
                .AddSingleton<IJobCheckerTrigger, CloudSchedulerJobCheckerTrigger>();

        protected override void BuildWorker(McmaWorkerBuilder workerBuilder)
            =>
                workerBuilder
                    .AddOperation<StartJob>()
                    .AddOperation<CancelJob>()
                    .AddOperation<RestartJob>()
                    .AddOperation<FailJob>()
                    .AddOperation<DeleteJob>()
                    .AddOperation<ProcessNotification>();
    }
}