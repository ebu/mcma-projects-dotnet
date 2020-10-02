using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.CloudWatch;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Aws.Lambda;
using Mcma.Context;
using Mcma.WorkerInvoker;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.PeriodicJobCleanup
{
    public class Function
    {
        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } = new AwsCloudWatchLoggerProvider("job-processor-periodic-job-checker", Environment.GetEnvironmentVariable("LogGroupName"));

        private static IContextVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();
        
        private static DataController DataController { get; } =
            new DataController(EnvironmentVariableProvider.TableName(), EnvironmentVariableProvider.GetRequiredContextVariable("PublicUrl"));

        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker(EnvironmentVariableProvider);

        public static async Task Handler(ILambdaContext context)
        {
            var tracker = new McmaTracker
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"Periodic Job Cleanup - {DateTime.UtcNow:O}"
            };

            var logger = LoggerProvider.Get(context.AwsRequestId, tracker);
            try
            {
                var jobRetentionPeriodInDays = EnvironmentVariableProvider.JobRetentionPeriodInDays();
                
                logger.Info($"Job Retention Period set to {jobRetentionPeriodInDays} days");

                if (!jobRetentionPeriodInDays.HasValue || jobRetentionPeriodInDays.Value <= 0)
                {
                    logger.Info("Exiting");
                    return;
                }

                var retentionDateLimit = DateTime.UtcNow - TimeSpan.FromDays(jobRetentionPeriodInDays.Value);

                var completedJobs = await DataController.QueryJobsAsync(new JobResourceQueryParameters {Status = JobStatus.Completed});
                var failedJobs = await DataController.QueryJobsAsync(new JobResourceQueryParameters {Status = JobStatus.Failed});
                var canceledJobs = await DataController.QueryJobsAsync(new JobResourceQueryParameters {Status = JobStatus.Canceled});

                var jobs = completedJobs.Results.Concat(failedJobs.Results).Concat(canceledJobs.Results).ToArray();

                logger.Info($"Deleting {jobs.Length} jobs older than {retentionDateLimit:O}");

                foreach (var job in jobs)
                    await DeleteJobAsync(job);
            }
            catch (Exception error)
            {
                logger.Error(error);
                throw;
            }
            finally
            {
                logger.FunctionEnd(context.AwsRequestId);
            }
        }

        private static async Task DeleteJobAsync(Job job)
        {
            await WorkerInvoker.InvokeAsync(EnvironmentVariableProvider.GetRequiredContextVariable("WorkerFunctionId"),
                                            "DeleteJob",
                                            input: new
                                            {
                                                jobId = job.Id
                                            },
                                            tracker: job.Tracker);
        }
    }
}