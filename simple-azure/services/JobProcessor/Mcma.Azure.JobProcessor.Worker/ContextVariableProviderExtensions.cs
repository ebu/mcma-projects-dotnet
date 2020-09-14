using Mcma.Context;

namespace Mcma.Azure.JobProcessor.Worker
{
    public static class ContextVariableProviderExtensions
    {
        public static string PublicUrl(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(PublicUrl));
    }
}