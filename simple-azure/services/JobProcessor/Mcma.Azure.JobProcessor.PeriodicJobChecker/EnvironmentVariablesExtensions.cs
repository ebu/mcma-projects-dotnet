namespace Mcma.Azure.JobProcessor
{
    public static class EnvironmentVariablesExtensions
    {
        public static long? DefaultJobTimeoutInMinutes(this IEnvironmentVariables contextVariableProvider)
            => 
                long.TryParse(contextVariableProvider.GetOptional(nameof(DefaultJobTimeoutInMinutes)), out var tmp)
                    ? tmp
                    : default(long?);
    }
}