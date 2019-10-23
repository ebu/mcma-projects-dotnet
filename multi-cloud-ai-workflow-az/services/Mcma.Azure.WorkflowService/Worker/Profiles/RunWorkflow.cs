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
    internal class RunWorkflow : IJobProfileHandler<WorkflowJob>
    {
        private HttpClient HttpClient { get; } = new HttpClient();

        public async Task ExecuteAsync(WorkerJobHelper<WorkflowJob> jobHelper)
        {
            var logicAppUrl = await GetWorkflowUrlAsync(jobHelper);

            var request =
                new
                {
                    input = jobHelper.JobInput,
                    resourceEndpoint = GetResourcesUrl(jobHelper),
                    jobProfilesEndpoint = jobHelper.Variables.JobProfilesUrl(),
                    notificationEndpoint = jobHelper.JobAssignmentId.TrimEnd('/') + "/notifications?code=" + jobHelper.Variables.ApiHandlerKey()
                }.ToMcmaJson();

            Logger.Debug($"Invoking Logic App at {logicAppUrl} with request body:{Environment.NewLine}{request.ToString(Formatting.Indented)}");

            var resp =
                await HttpClient.PostAsync(
                    logicAppUrl,
                    new StringContent(request.ToString(), Encoding.UTF8, "application/json"));

            resp.EnsureSuccessStatusCode();
        }

        private async Task<string> GetWorkflowUrlAsync(WorkerJobHelper<WorkflowJob> jobHelper)
        {
            var azAuthContext =
                new AuthenticationContext(
                    $"{AzureEnvironment.AzureGlobalCloud.AuthenticationEndpoint}{jobHelper.Variables.AzureTenantName()}");

            using (var logicAppsClient = new LogicManagementClient(jobHelper.Variables.AzureCredentials()) { SubscriptionId = jobHelper.Variables.AzureSubscriptionId() })
            {
                var workflow = await logicAppsClient.Workflows.GetAsync(jobHelper.Variables.AzureResourceGroupName(), jobHelper.Profile.Name);
                if (workflow == null)
                    throw new Exception($"Workflow '{workflow.Name}' does not exist.");

                var triggerProperties = (workflow.Definition as JObject)?["triggers"]?.ToObject<JObject>().Properties().ToList();
                Logger.Debug($"Found {triggerProperties.Count} triggers for workflow '{workflow.Name}': {(string.Join(", ", triggerProperties.Select(p => p.Name)))}");

                var triggerName = triggerProperties.FirstOrDefault()?.Name;
                if (string.IsNullOrWhiteSpace(triggerName))
                    throw new Exception($"Definition for workflow '{workflow.Name}' does not have any triggers defined.");

                var callbackUrl = await logicAppsClient.WorkflowTriggers.ListCallbackUrlAsync(jobHelper.Variables.AzureResourceGroupName(), workflow.Name, triggerName);

                return callbackUrl?.Value ?? throw new Exception($"Trigger '{triggerName}' for workflow '{workflow.Name}' does not have a callback url defined.");
            }
        }

        private string GetResourcesUrl(WorkerJobHelper<WorkflowJob> jobHelper)
            => $"{jobHelper.JobAssignmentId.Substring(0, jobHelper.JobAssignmentId.LastIndexOf("/job-assignments/"))}/resources?code={jobHelper.Variables.ApiHandlerKey()}";
    }
}
