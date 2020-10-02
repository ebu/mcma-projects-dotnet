using Mcma.Context;

namespace Mcma.Aws.JobProcessor.Worker
{
    public static class ContextVariableProviderExtensions
    {
        public static string PublicUrl(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(PublicUrl));
    }
}