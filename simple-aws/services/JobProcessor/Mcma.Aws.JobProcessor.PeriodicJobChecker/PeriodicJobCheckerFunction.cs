using Amazon.Lambda.Core;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.Functions;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Aws.Lambda;
using Mcma.Aws.WorkerInvoker;
using Mcma.Utility;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.PeriodicJobChecker
{
    public class PeriodicJobCheckerFunction : McmaLambdaFunction<PeriodicJobCheckerHandler>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.Configure<PeriodicJobCheckerOptions>(
                opts =>
                    opts.DefaultJobTimeoutInMinutes =
                        long.TryParse(McmaEnvironmentVariables.Get("DEFAULT_JOB_TIMEOUT_IN_MINUTES", false), out var defaultJobTimeoutInMinutes)
                            ? defaultJobTimeoutInMinutes
                            : default(long?));

            services.AddMcmaCloudWatchLogging("job-processor-periodic-job-cleanup")
                    .AddMcmaLambdaWorkerInvoker()
                    .AddDataController()
                    .AddSingleton<IJobCheckerTrigger, CloudWatchEventsJobCheckerTrigger>();
        }
    }
}