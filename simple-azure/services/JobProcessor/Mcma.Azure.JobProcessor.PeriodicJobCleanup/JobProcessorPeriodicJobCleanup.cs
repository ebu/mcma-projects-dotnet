﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Logging;
using Mcma.WorkerInvoker;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Mcma.Azure.JobProcessor.PeriodicJobCleanup
{
    public class JobProcessorPeriodicJobCleanup
    {
        public JobProcessorPeriodicJobCleanup(ILoggerProvider loggerProvider,
                                              IDataController dataController,
                                              IWorkerInvoker workerInvoker,
                                              IOptions<JobProcessorPeriodicJobCleanupOptions> options)
        {
            LoggerProvider = loggerProvider ?? throw new ArgumentNullException(nameof(loggerProvider));
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
            WorkerInvoker = workerInvoker ?? throw new ArgumentNullException(nameof(workerInvoker));
            Options = options.Value ?? new JobProcessorPeriodicJobCleanupOptions();
        }

        private ILoggerProvider LoggerProvider { get; }
        
        private IDataController DataController { get; }

        private IWorkerInvoker WorkerInvoker { get; }
        
        private JobProcessorPeriodicJobCleanupOptions Options { get; }

        [FunctionName(nameof(JobProcessorPeriodicJobCleanup))]
        public async Task Run(
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
                var jobRetentionPeriodInDays = Options.JobRetentionPeriodInDays;
                
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

        private async Task DeleteJobAsync(Job job)
        {
            await WorkerInvoker.InvokeAsync("DeleteJob",
                                            new
                                            {
                                                jobId = job.Id
                                            },
                                            job.Tracker);
        }
    }
}