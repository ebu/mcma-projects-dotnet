using Mcma.Utility;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Mcma.Azure.JobProcessor.Common
{
    public class LogicAppWorkflowCheckerTriggerOptions
    {
        public string AzureSubscriptionId { get; set; } = McmaEnvironmentVariables.Get("AZURE_SUBSCRIPTION_ID");

        public string AzureResourceGroupName { get; set; } = McmaEnvironmentVariables.Get("AZURE_RESOURCE_GROUP_NAME");
        
        public string JobCheckerWorkflowName { get; set; } = McmaEnvironmentVariables.Get("JOB_CHECKER_WORKFLOW_NAME");

        public AzureCredentials GetManagedServiceIdentityCredentials()
            => 
                SdkContext.AzureCredentialsFactory
                          .FromMSI(new MSILoginInformation(MSIResourceType.AppService), AzureEnvironment.AzureGlobalCloud)
                          .WithDefaultSubscription(AzureSubscriptionId);
    }
}