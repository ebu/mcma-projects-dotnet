using Google.Cloud.Functions.Hosting;
using Mcma.GoogleCloud.JobProcessor.Common;
using Mcma.GoogleCloud.Logger;
using Mcma.GoogleCloud.PubSubWorkerInvoker;
using Mcma.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.JobProcessor.PeriodicJobChecker
{
    public class PeriodicJobCheckerFunctionStartup : FunctionsStartup
    {
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.Configure<PeriodicJobCheckerOptions>(
                opts =>
                    opts.DefaultJobTimeoutInMinutes =
                        long.TryParse(McmaEnvironmentVariables.Get("DEFAULT_JOB_TIMEOUT_IN_MINUTES", false), out var defaultJobTimeoutInMinutes)
                            ? defaultJobTimeoutInMinutes
                            : default(long?));

            services.AddMcmaCloudLogging("job-processor-periodic-job-cleanup")
                    .AddMcmaPubSubWorkerInvoker()
                    .AddDataController()
                    .AddSingleton<IJobCheckerTrigger, CloudSchedulerJobCheckerTrigger>();
        }
    }
}