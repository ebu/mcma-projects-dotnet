using Mcma.Core.Context;

namespace Mcma.Azure.AzureAiService.Worker
{
    internal static class ContextVariablesExtensions
    {
        private const string Prefix = "Azure";

        public static string ApiHandlerKey(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(ApiHandlerKey));

        public static string VideoIndexerApiUrl(this IContextVariables contextVariables)
            => contextVariables.GetPrefixed(nameof(VideoIndexerApiUrl));

        public static string VideoIndexerLocation(this IContextVariables contextVariables)
            => contextVariables.GetPrefixed(nameof(VideoIndexerLocation));

        public static string VideoIndexerAccountID(this IContextVariables contextVariables)
            => contextVariables.GetPrefixed(nameof(VideoIndexerAccountID));

        public static string VideoIndexerSubscriptionKey(this IContextVariables contextVariables)
            => contextVariables.GetPrefixed(nameof(VideoIndexerSubscriptionKey));

        private static string GetPrefixed(this IContextVariables contextVariables, string name)
            => contextVariables.GetRequired(Prefix + name);
    }
}
