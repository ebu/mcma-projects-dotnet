using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.JobProcessor.Common
{
    public static class EnvironmentVariablesExtensions
    {
        public static string FunctionName(this IEnvironmentVariables environmentVariables)
            => environmentVariables.Get("WEBSITE_SITE_NAME");
        
        public static string AzureSubscriptionId(this IEnvironmentVariables environmentVariables)
            => environmentVariables.Get(nameof(AzureSubscriptionId));

        public static string AzureTenantId(this IEnvironmentVariables environmentVariables)
            => environmentVariables.Get(nameof(AzureTenantId));

        public static string AzureResourceGroupName(this IEnvironmentVariables environmentVariables)
            => environmentVariables.Get(nameof(AzureResourceGroupName));

        public static AzureCredentials AzureCredentials(this IEnvironmentVariables environmentVariables)
            => 
                SdkContext.AzureCredentialsFactory
                          .FromMSI(new MSILoginInformation(MSIResourceType.AppService), AzureEnvironment.AzureGlobalCloud)
                          .WithDefaultSubscription(environmentVariables.AzureSubscriptionId());
        
        public static string JobCheckerWorkflowName(this IEnvironmentVariables environmentVariables)
            => environmentVariables.Get(nameof(JobCheckerWorkflowName));
    }
}