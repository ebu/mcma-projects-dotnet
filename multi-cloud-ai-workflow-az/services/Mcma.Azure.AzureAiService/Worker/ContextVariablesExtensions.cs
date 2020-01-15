using Mcma.Core.Context;

namespace Mcma.Azure.AzureAiService.Worker
{
    internal static class ContextVariablesExtensions
    {
        private const string Prefix = "Azure";

        public static string NotificationsUrl(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(NotificationsUrl));

        public static string NotificationHandlerKey(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(NotificationHandlerKey));

        public static string VideoIndexerApiUrl(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetPrefixed(nameof(VideoIndexerApiUrl));

        public static string VideoIndexerLocation(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetPrefixed(nameof(VideoIndexerLocation));

        public static string VideoIndexerAccountID(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetPrefixed(nameof(VideoIndexerAccountID));

        public static string VideoIndexerSubscriptionKey(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetPrefixed(nameof(VideoIndexerSubscriptionKey));

        private static string GetPrefixed(this IContextVariableProvider contextVariableProvider, string name)
            => contextVariableProvider.GetRequiredContextVariable(Prefix + name);
    }
}
