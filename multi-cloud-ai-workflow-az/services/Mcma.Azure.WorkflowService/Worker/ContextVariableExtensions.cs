using Mcma.Core.Context;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.WorkflowService.Worker
{
    internal static class ContextVariableExtensions
    {
        public static string AzureClientId(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureClientId));

        public static string AzureClientSecret(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureClientSecret));

        public static string AzureSubscriptionId(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureSubscriptionId));

        public static string AzureTenantId(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureTenantId));

        public static string AzureTenantName(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureTenantName));

        public static string AzureResourceGroupName(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureResourceGroupName));

        public static string LogicAppUrl(this IContextVariableProvider contextVariableProvider, string workflowName)
            => contextVariableProvider.GetRequiredContextVariable(workflowName + nameof(LogicAppUrl));

        public static AzureCredentials AzureCredentials(this IContextVariableProvider contextVariableProvider)
            => 
            SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    contextVariableProvider.AzureClientId(),
                    contextVariableProvider.AzureClientSecret(),
                    contextVariableProvider.AzureTenantId(),
                    AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(contextVariableProvider.AzureSubscriptionId());

        public static string ServiceRegistryUrl(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(ServiceRegistryUrl));
            
        public static string JobProfilesUrl(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.ServiceRegistryUrl().TrimEnd('/') + "/job-profiles";
    }
}
