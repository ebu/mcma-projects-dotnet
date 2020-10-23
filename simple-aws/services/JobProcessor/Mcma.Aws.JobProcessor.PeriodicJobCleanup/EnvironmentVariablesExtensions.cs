namespace Mcma.Aws.JobProcessor.PeriodicJobCleanup
{
    public static class EnvironmentVariablesExtensions
    {
        public static int? JobRetentionPeriodInDays(this IEnvironmentVariables environmentVariables)
            => 
                int.TryParse(environmentVariables.GetOptional(nameof(JobRetentionPeriodInDays)), out var tmp)
                    ? tmp
                    : default(int?);
    }
}