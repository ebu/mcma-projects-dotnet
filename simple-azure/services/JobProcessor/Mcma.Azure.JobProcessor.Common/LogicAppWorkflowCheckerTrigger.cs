using System;
using System.Threading.Tasks;
using Mcma.Context;
using Microsoft.Azure.Management.Logic;

namespace Mcma.Azure.JobProcessor.Common
{
    public class LogicAppWorkflowCheckerTrigger : IJobCheckerTrigger, IDisposable
    {
        public LogicAppWorkflowCheckerTrigger(IContextVariableProvider contextVariableProvider)
        {
            LogicManagementClient =
                new LogicManagementClient(contextVariableProvider.AzureCredentials())
                {
                    SubscriptionId = contextVariableProvider.AzureSubscriptionId()
                };
            
            ResourceGroupName = contextVariableProvider.AzureResourceGroupName();
            WorkflowName = contextVariableProvider.JobCheckerWorkflowName();
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