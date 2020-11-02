using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.Functions;
using Mcma.Aws.JobProcessor.Common;
using Mcma.Logging;
using Mcma.WorkerInvoker;
using Microsoft.Extensions.Options;

namespace Mcma.Aws.JobProcessor.PeriodicJobCleanup
{
    public class PeriodicJobCleanupHandler : IMcmaLambdaFunctionHandler
    {
        public PeriodicJobCleanupHandler(ILoggerProvider loggerProvider,
                                         IDataController dataController,
                                         IWorkerInvoker workerInvoker,
                                         IOptions<PeriodicJobCleanupOptions> options)
        {
            LoggerProvider = loggerProvider ?? throw new ArgumentNullException(nameof(loggerProvider));
            DataController = dataController ?? throw new ArgumentNullException(nameof(dataController));
            WorkerInvoker = workerInvoker ?? throw new ArgumentNullException(nameof(workerInvoker));
            Options = options?.Value ?? new PeriodicJobCleanupOptions();
        }

        private ILoggerProvider LoggerProvider { get; }
        
        private IDataController DataController { get; }

        private IWorkerInvoker WorkerInvoker { get; }
        
        private PeriodicJobCleanupOptions Options { get; }

        public async Task ExecuteAsync(ILambdaContext context)
        {
            var tracker = new McmaTracker
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"Periodic Job Cleanup - {DateTime.UtcNow:O}"
            };

            var logger = LoggerProvider.Get(context.AwsRequestId, tracker);
            try
            {   
                logger.Info($"Job Retention Period set to {Options.JobRetentionPeriodInDays} days");

                if (!Options.JobRetentionPeriodInDays.HasValue || Options.JobRetentionPeriodInDays.Value <= 0)
                {
                    logger.Info("Exiting");
                    return;
                }

                var retentionDateLimit = DateTime.UtcNow - TimeSpan.FromDays(Options.JobRetentionPeriodInDays.Value);

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