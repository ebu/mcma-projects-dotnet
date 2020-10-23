using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Logic;

namespace Mcma.Azure.JobProcessor.Common
{
    public class LogicAppWorkflowCheckerTrigger : IJobCheckerTrigger, IDisposable
    {
        public LogicAppWorkflowCheckerTrigger()
        {
            LogicManagementClient =
                new LogicManagementClient(EnvironmentVariables.Instance.AzureCredentials())
                {
                    SubscriptionId = EnvironmentVariables.Instance.AzureSubscriptionId()
                };
            
            ResourceGroupName = EnvironmentVariables.Instance.AzureResourceGroupName();
            WorkflowName = EnvironmentVariables.Instance.JobCheckerWorkflowName();
        }

        private LogicManagementClient LogicManagementClient { get; }
        
        private string ResourceGroupName { get; }
        
        private string WorkflowName { get; }

        public Task EnableAsync()
            => LogicManagementClient.Workflows.EnableAsync(ResourceGroupName, WorkflowName);

        public Task DisableAsync()
            => LogicManagementClient.Workflows.DisableAsync(ResourceGroupName, WorkflowName);

        public void Dispose()
            => LogicManagementClient?.Dispose();
    }
}