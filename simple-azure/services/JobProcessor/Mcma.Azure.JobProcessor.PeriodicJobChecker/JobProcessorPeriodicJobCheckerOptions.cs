using Mcma.Utility;

namespace Mcma.Azure.JobProcessor
{
    public class JobProcessorPeriodicJobCheckerOptions
    {
        public long? DefaultJobTimeoutInMinutes { get; set; } =
            long.TryParse(McmaEnvironmentVariables.Get("DEFAULT_JOB_TIMEOUT_IN_MINUTES", false), out var tmp)
                ? tmp
                : default(long?);
    }
}