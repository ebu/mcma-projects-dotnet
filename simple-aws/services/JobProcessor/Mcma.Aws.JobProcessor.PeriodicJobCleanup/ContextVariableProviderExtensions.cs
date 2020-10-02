using Mcma.Context;

namespace Mcma.Aws.JobProcessor.PeriodicJobCleanup
{
    public static class ContextVariableProviderExtensions
    {
        public static int? JobRetentionPeriodInDays(this IContextVariableProvider contextVariableProvider)
            => 
                int.TryParse(contextVariableProvider.GetOptionalContextVariable(nameof(JobRetentionPeriodInDays)), out var tmp)
                    ? tmp
                    : default(int?);
    }
}