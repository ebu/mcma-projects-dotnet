using System.Threading.Tasks;
using Google.Cloud.Scheduler.V1;
using Mcma.Utility;

namespace Mcma.GoogleCloud.JobProcessor.Common
{
    public class CloudSchedulerJobCheckerTrigger : IJobCheckerTrigger
    {
        private CloudSchedulerClient CloudSchedulerClient { get; } = CloudSchedulerClient.Create();

        public Task EnableAsync() => CloudSchedulerClient.ResumeJobAsync(McmaEnvironmentVariables.Get("CLOUD_SCHEDULER_JOB_NAME"));

        public Task DisableAsync() => CloudSchedulerClient.PauseJobAsync(McmaEnvironmentVariables.Get("CLOUD_SCHEDULER_JOB_NAME"));
    }
}