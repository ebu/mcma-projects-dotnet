using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Logic;
using Microsoft.Extensions.Options;

namespace Mcma.Azure.JobProcessor.Common
{
    public class LogicAppWorkflowCheckerTrigger : IJobCheckerTrigger, IDisposable
    {
        public LogicAppWorkflowCheckerTrigger(IOptions<LogicAppWorkflowCheckerTriggerOptions> options)
        {
            Options = options.Value ?? new LogicAppWorkflowCheckerTriggerOptions();
            
            LogicManagementClient =
                new LogicManagementClient(Options.GetManagedServiceIdentityCredentials())
                {
                    SubscriptionId = Options.AzureSubscriptionId
                };
        }

        private LogicManagementClient LogicManagementClient { get; }
        
        private LogicAppWorkflowCheckerTriggerOptions Options { get; }

        public Task EnableAsync()
            => LogicManagementClient.Workflows.EnableAsync(Options.AzureResourceGroupName, Options.JobCheckerWorkflowName);

        public Task DisableAsync()
            => LogicManagementClient.Workflows.DisableAsync(Options.AzureResourceGroupName, Options.JobCheckerWorkflowName);

        public void Dispose()
            => LogicManagementClient?.Dispose();
    }
}