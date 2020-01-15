using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Worker;
using Microsoft.Azure.Management.Logic;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.WorkflowService.Worker
{
    internal class RunWorkflow : IJobProfile<WorkflowJob>
    {
        public RunWorkflow(string workflowName)
        {
            WorkflowName = workflowName;
        }

        private HttpClient HttpClient { get; } = new HttpClient();

        private string WorkflowName { get; }

        public string Name => WorkflowName;

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<WorkflowJob> jobHelper)
        {
            var logicAppUrl = await GetWorkflowUrlAsync(jobHelper);

            var request =
                new
                {
                    input = jobHelper.JobInput,
                    resourceEndpoint = GetResourcesUrl(jobHelper),
                    resourceEndpointAudience = GetApiUrl(jobHelper),
                    jobProfilesEndpoint = jobHelper.Request.JobProfilesUrl(),
                    jobProfilesEndpointAudience = jobHelper.Request.ServiceRegistryUrl(),
                    notificationEndpoint = jobHelper.JobAssignmentId.TrimEnd('/') + "/notifications",
                    notificationEndpointAudience = GetApiUrl(jobHelper),
                }.ToMcmaJson();

            jobHelper.Logger.Debug($"Invoking Logic App at {logicAppUrl} with request body:{Environment.NewLine}{request.ToString(Formatting.Indented)}");

            var resp =
                await HttpClient.PostAsync(
                    logicAppUrl,
                    new StringContent(request.ToString(), Encoding.UTF8, "application/json"));

            resp.EnsureSuccessStatusCode();
        }

        private async Task<string> GetWorkflowUrlAsync(ProcessJobAssignmentHelper<WorkflowJob> jobHelper)
        {
            var azAuthContext =
                new AuthenticationContext(
                    $"{AzureEnvironment.AzureGlobalCloud.AuthenticationEndpoint}{jobHelper.Request.AzureTenantName()}");

            using (var logicAppsClient = new LogicManagementClient(jobHelper.Request.AzureCredentials()) { SubscriptionId = jobHelper.Request.AzureSubscriptionId() })
            {
                var workflow = await logicAppsClient.Workflows.GetAsync(jobHelper.Request.AzureResourceGroupName(), jobHelper.Profile.Name);
                if (workflow == null)
                    throw new Exception($"Workflow '{workflow.Name}' does not exist.");

                var triggerProperties = (workflow.Definition as JObject)?["triggers"]?.ToObject<JObject>().Properties().ToList();
                jobHelper.Logger.Debug($"Found {triggerProperties.Count} triggers for workflow '{workflow.Name}': {(string.Join(", ", triggerProperties.Select(p => p.Name)))}");

                var triggerName = triggerProperties.FirstOrDefault()?.Name;
                if (string.IsNullOrWhiteSpace(triggerName))
                    throw new Exception($"Definition for workflow '{workflow.Name}' does not have any triggers defined.");

                var callbackUrl = await logicAppsClient.WorkflowTriggers.ListCallbackUrlAsync(jobHelper.Request.AzureResourceGroupName(), workflow.Name, triggerName);

                return callbackUrl?.Value ?? throw new Exception($"Trigger '{triggerName}' for workflow '{workflow.Name}' does not have a callback url defined.");
            }
        }

        private string GetApiUrl(ProcessJobAssignmentHelper<WorkflowJob> jobHelper)
            => jobHelper.JobAssignmentId.Substring(0, jobHelper.JobAssignmentId.LastIndexOf("/job-assignments/"));

        private string GetResourcesUrl(ProcessJobAssignmentHelper<WorkflowJob> jobHelper)
            => $"{GetApiUrl(jobHelper)}/resources";
    }
}
