using Mcma.Context;

namespace Mcma.Aws.JobProcessor
{
    public static class ContextVariableProviderExtensions
    {
        public static long? DefaultJobTimeoutInMinutes(this IContextVariableProvider contextVariableProvider)
            => 
                long.TryParse(contextVariableProvider.GetOptionalContextVariable(nameof(DefaultJobTimeoutInMinutes)), out var tmp)
                    ? tmp
                    : default(long?);
    }
}