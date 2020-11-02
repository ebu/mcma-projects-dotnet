using Amazon.Lambda.Core;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.Functions;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Aws.Lambda;
using Mcma.Aws.WorkerInvoker;
using Mcma.Utility;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.PeriodicJobCleanup
{
    public class PeriodicJobCleanupFunction : McmaLambdaFunction<PeriodicJobCleanupHandler>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.Configure<PeriodicJobCleanupOptions>(
                opts =>
                    opts.JobRetentionPeriodInDays =
                        int.TryParse(McmaEnvironmentVariables.Get("JOB_RETENTION_PERIOD_IN_DAYS", false), out var jobRetentionPeriodInDays)
                            ? jobRetentionPeriodInDays
                            : default(int?));
                
            services
                .AddMcmaCloudWatchLogging("job-processor-periodic-job-cleanup")
                .AddDataController()
                .AddMcmaLambdaWorkerInvoker();
        }
    }
}