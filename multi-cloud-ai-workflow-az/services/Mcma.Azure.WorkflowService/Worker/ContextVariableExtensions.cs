using Mcma.Core.Context;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.WorkflowService.Worker
{
    internal static class ContextVariableExtensions
    {
        public static string AzureClientId(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AzureClientId));

        public static string AzureClientSecret(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AzureClientSecret));

        public static string AzureSubscriptionId(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AzureSubscriptionId));

        public static string AzureTenantId(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AzureTenantId));

        public static string AzureTenantName(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AzureTenantName));

        public static string AzureResourceGroupName(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AzureResourceGroupName));

        public static string ApiHandlerKey(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(ApiHandlerKey));

        public static string LogicAppUrl(this IContextVariables contextVariables, string workflowName)
            => contextVariables.GetRequired(workflowName + nameof(LogicAppUrl));

        public static AzureCredentials AzureCredentials(this IContextVariables contextVariables)
            => 
            SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    contextVariables.AzureClientId(),
                    contextVariables.AzureClientSecret(),
                    contextVariables.AzureTenantId(),
                    AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(contextVariables.AzureSubscriptionId());

        public static string ServiceRegistryUrl(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(ServiceRegistryUrl));

        public static string ServiceRegistryKey(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(ServiceRegistryKey));

        public static string JobProfilesUrl(this IContextVariables contextVariables)
            => contextVariables.ServiceRegistryUrl().TrimEnd('/') + "/job-profiles?code=" + contextVariables.ServiceRegistryKey();
    }
}
