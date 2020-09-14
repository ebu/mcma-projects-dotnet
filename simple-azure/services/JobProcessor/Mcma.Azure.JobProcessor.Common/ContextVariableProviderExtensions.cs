using Mcma.Context;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.JobProcessor.Common
{
    public static class ContextVariableProviderExtensions
    {
        public static string FunctionName(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable("WEBSITE_SITE_NAME");
        
        public static string AzureSubscriptionId(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureSubscriptionId));

        public static string AzureTenantId(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureTenantId));

        public static string AzureResourceGroupName(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AzureResourceGroupName));

        public static AzureCredentials AzureCredentials(this IContextVariableProvider contextVariableProvider)
            => 
                SdkContext.AzureCredentialsFactory
                          .FromMSI(new MSILoginInformation(MSIResourceType.AppService), AzureEnvironment.AzureGlobalCloud)
                          .WithDefaultSubscription(contextVariableProvider.AzureSubscriptionId());
        
        public static string JobCheckerWorkflowName(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(JobCheckerWorkflowName));
    }
}