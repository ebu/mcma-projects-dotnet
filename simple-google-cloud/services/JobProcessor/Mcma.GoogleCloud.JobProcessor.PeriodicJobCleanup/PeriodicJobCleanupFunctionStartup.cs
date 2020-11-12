using Google.Cloud.Functions.Hosting;
using Mcma.GoogleCloud.JobProcessor.Common;
using Mcma.GoogleCloud.Logger;
using Mcma.GoogleCloud.PubSubWorkerInvoker;
using Mcma.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Mcma.GoogleCloud.JobProcessor.PeriodicJobCleanup
{
    public class PeriodicJobCleanupFunctionStartup : FunctionsStartup
    {
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.Configure<PeriodicJobCleanupOptions>(
                opts =>
                    opts.JobRetentionPeriodInDays =
                        int.TryParse(McmaEnvironmentVariables.Get("JOB_RETENTION_PERIOD_IN_DAYS", false), out var jobRetentionPeriodInDays)
                            ? jobRetentionPeriodInDays
                            : default(int?));
                
            services
                .AddMcmaCloudLogging("job-processor-periodic-job-cleanup")
                .AddDataController()
                .AddMcmaPubSubWorkerInvoker();
        }
    }
}