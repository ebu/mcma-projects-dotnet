using Mcma.Core;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.AzureAiService.Worker
{
    internal static class ContextVariableProviderExtensions
    {
        private const string Prefix = "Azure";

        public static string ApiUrl(this IContextVariableProvider contextVariableProvider) => contextVariableProvider.Get(nameof(ApiUrl));

        public static string Location(this IContextVariableProvider contextVariableProvider) => contextVariableProvider.Get(nameof(Location));

        public static string AccountID(this IContextVariableProvider contextVariableProvider) => contextVariableProvider.Get(nameof(AccountID));

        public static string SubscriptionKey(this IContextVariableProvider contextVariableProvider) => contextVariableProvider.Get(nameof(SubscriptionKey));

        private static string Get(this IContextVariableProvider contextVariableProvider, string name)
            => contextVariableProvider.GetRequiredContextVariable(Prefix + name);
    }
}
