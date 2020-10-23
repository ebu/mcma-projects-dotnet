using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.Logger;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Azure.WorkerInvoker;
using Mcma.Logging;
using Mcma.WorkerInvoker;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Mcma.Azure.JobProcessor.PeriodicJobCleanup
{
    public class Function
    {
        private static ILoggerProvider LoggerProvider { get; } = new AppInsightsLoggerProvider("job-processor-periodic-job-checker");
        
        private static DataController DataController { get; } = new DataController();

        private static IWorkerInvoker WorkerInvoker { get; } = new QueueWorkerInvoker();

        [FunctionName("JobProcessorPeriodicJobCleanup")]
        public static async Task Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
            ExecutionContext executionContext)
        {
            var tracker = new McmaTracker
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"Periodic Job Cleanup - {DateTime.UtcNow:O}"
            };

            var logger = LoggerProvider.Get(executionContext.InvocationId.ToString(), tracker);
            try
            {
                var jobRetentionPeriodInDays = EnvironmentVariables.Instance.JobRetentionPeriodInDays();
                
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
            await WorkerInvoker.InvokeAsync(EnvironmentVariables.Instance.WorkerFunctionId(),
                                            "DeleteJob",
                                            new
                                            {
                                                jobId = job.Id
                                            },
                                            job.Tracker);
        }
    }
}