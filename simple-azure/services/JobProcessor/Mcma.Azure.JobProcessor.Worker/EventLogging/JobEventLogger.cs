using System;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Logging;

namespace Mcma.Azure.JobProcessor
{
    public class JobEventLogger
    {
        public JobEventLogger(ILogger logger, IResourceManager resourceManager)
        {
            Logger = logger;
            ResourceManager = resourceManager;
        }
        
        private ILogger Logger { get; }

        private IResourceManager ResourceManager { get; }

        public async Task LogJobEventAsync(Job job, JobExecution jobExecution)
        {
            JobProfile jobProfile = null;
            try
            {
                jobProfile = await ResourceManager.GetAsync<JobProfile>(job.JobProfileId);
            }
            catch (Exception exception)
            {
                Logger.Warn("Failed to get job profile");
                Logger.Warn(exception);
            }
            
            var msg = new JobEventMessage(job, jobProfile, jobExecution);

            if (job.Status == JobStatus.Scheduled)
                Logger.JobStart(msg);
            else if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed || job.Status == JobStatus.Canceled)
                Logger.JobEnd(msg);
            else
                Logger.JobUpdate(msg);
        }
    }
}