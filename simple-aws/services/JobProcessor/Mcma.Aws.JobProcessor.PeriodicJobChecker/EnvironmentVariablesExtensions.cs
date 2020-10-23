namespace Mcma.Aws.JobProcessor
{
    public static class EnvironmentVariablesExtensions
    {
        public static long? DefaultJobTimeoutInMinutes(this IEnvironmentVariables environmentVariables)
            => 
                long.TryParse(environmentVariables.GetOptional(nameof(DefaultJobTimeoutInMinutes)), out var tmp)
                    ? tmp
                    : default(long?);
    }
}