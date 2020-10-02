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

namespace Mcma.Aws.JobProcessor.PeriodicJobChecker
{
    public class Function
    {
        private static AwsCloudWatchLoggerProvider LoggerProvider { get; } = new AwsCloudWatchLoggerProvider("job-processor-periodic-job-checker", Environment.GetEnvironmentVariable("LogGroupName"));

        private static IContextVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static DataController DataController { get; } =
            new DataController(EnvironmentVariableProvider.TableName(), EnvironmentVariableProvider.GetRequiredContextVariable("PublicUrl"));

        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker(EnvironmentVariableProvider);

        private static IJobCheckerTrigger CheckerTrigger { get; } = new CloudWatchEventsJobCheckerTrigger();

        public static async Task Handler(ILambdaContext context)
        {
            var tracker = new McmaTracker
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"Periodic Job Checker - {DateTime.UtcNow:O}"
            };

            var logger = LoggerProvider.Get(context.AwsRequestId, tracker);
            try
            {
                await CheckerTrigger.DisableAsync();
                
                var newJobs = await DataController.QueryJobsAsync(new JobResourceQueryParameters {Status = JobStatus.New});
                var queuedJobs = await DataController.QueryJobsAsync(new JobResourceQueryParameters {Status = JobStatus.Queued});
                var scheduledJobs = await DataController.QueryJobsAsync(new JobResourceQueryParameters {Status = JobStatus.Scheduled});
                var runningJobs = await DataController.QueryJobsAsync(new JobResourceQueryParameters {Status = JobStatus.Running});

                var jobs = newJobs.Results.Concat(queuedJobs.Results).Concat(scheduledJobs.Results).Concat(runningJobs.Results).ToArray();

                logger.Info($"Found {jobs.Length} active jobs");

                var activeJobsCount = 0;
                var failedJobsCount = 0;
                var now = DateTime.UtcNow;

                foreach (var job in jobs)
                {
                    var deadlinePassed = false;
                    var timeoutPassed = false;

                    var defaultTimeout = EnvironmentVariableProvider.DefaultJobTimeoutInMinutes();

                    if (job.Deadline != null)
                    {
                        defaultTimeout = null;
                        if (job.Deadline < now)
                            deadlinePassed = true;
                    }

                    var timeout = job.Timeout ?? defaultTimeout;
                    if (timeout.HasValue)
                    {
                        var jobExecution = (await DataController.GetExecutionsAsync(job.Id)).Results.FirstOrDefault();

                        var startDate = jobExecution?.ActualStartDate ?? jobExecution?.DateCreated ?? job.DateCreated;

                        var timePassedInMinutes = (now - startDate)?.TotalMinutes;
                        if (timePassedInMinutes > timeout)
                            timeoutPassed = true;
                    }

                    if (deadlinePassed)
                    {
                        await FailJobAsync(job,
                                           new ProblemDetail
                                           {
                                               ProblemType = "uri://mcma.ebu.ch/rfc7807/job-processor/job-deadline-passed",
                                               Title = "Job failed to complete before deadline",
                                               Detail = $"Job missed deadline of {job.Deadline:O}"
                                           });
                        failedJobsCount++;
                    }
                    else if (timeoutPassed)
                    {
                        await FailJobAsync(job,
                                           new ProblemDetail
                                           {
                                               ProblemType = "uri://mcma.ebu.ch/rfc7807/job-processor/job-timeout-passed",
                                               Title = "Job failed to complete before timeout limit",
                                               Detail = $"Job timed out after {timeout} minutes"
                                           });
                        failedJobsCount++;
                    }
                    else
                        activeJobsCount++;
                }

                logger.Info($"Failed {failedJobsCount} jobs due to deadline or timeout constraints");

                if (activeJobsCount > 0)
                {
                    logger.Info($"There are {activeJobsCount} active jobs remaining");
                    await CheckerTrigger.EnableAsync();
                }
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

        private static async Task FailJobAsync(Job job, ProblemDetail error)
        {
            await WorkerInvoker.InvokeAsync(EnvironmentVariableProvider.GetRequiredContextVariable("WorkerFunctionId"),
                                            "FailJob",
                                            input: new
                                            {
                                                jobId = job.Id,
                                                error
                                            },
                                            tracker: job.Tracker);
        }
    }
}