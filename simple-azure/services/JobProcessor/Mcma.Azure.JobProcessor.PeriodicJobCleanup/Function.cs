using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Azure.WorkerInvoker;
using Mcma.Context;
using Mcma.WorkerInvoker;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Mcma.Azure.JobProcessor.PeriodicJobCleanup
{
    public class Function
    {
        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("job-processor-periodic-job-checker");

        private static IContextVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();
        
        private static DataController DataController { get; } =
            new DataController(EnvironmentVariableProvider.TableName(), EnvironmentVariableProvider.GetRequiredContextVariable("PublicUrl"));

        private static IWorkerInvoker WorkerInvoker { get; } = new QueueWorkerInvoker(EnvironmentVariableProvider);

        [FunctionName("JobProcessorPeriodicJobCleanup")]
        public static async Task Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
            ILogger log,
            ExecutionContext executionContext)
        {
            var tracker = new McmaTracker
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"Periodic Job Cleanup - {DateTime.UtcNow:O}"
            };

            var logger = LoggerProvider.AddLogger(log, executionContext.InvocationId.ToString(), tracker);
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
                logger.FunctionEnd(executionContext.InvocationId.ToString());
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