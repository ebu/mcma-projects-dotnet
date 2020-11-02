using Mcma.Utility;

namespace Mcma.Azure.JobProcessor.PeriodicJobCleanup
{
    public class JobProcessorPeriodicJobCleanupOptions
    {
        public int? JobRetentionPeriodInDays { get; set; } =
            int.TryParse(McmaEnvironmentVariables.Get("JOB_RETENTION_PERIOD_IN_DAYS", false), out var tmp)
                ? tmp
                : default(int?);
    }
}