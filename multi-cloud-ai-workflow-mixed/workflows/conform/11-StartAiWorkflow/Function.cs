using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.Client;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Azure.Client;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.StartAiWorkflow
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        
        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(
                new AuthProvider()
                    .AddAwsV4Auth(AwsV4AuthContext.Global)
                    .AddAzureFunctionKeyAuth());
        
        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = ResourceManagerProvider.Get(EnvironmentVariableProvider);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 90
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var jobProfiles = await resourceManager.GetAsync<JobProfile>(("name", "AiWorkflow"));

            var jobProfileId = jobProfiles?.FirstOrDefault()?.Id;

            if (jobProfileId == null)
                throw new Exception("JobProfile 'AiWorkflow' not found");

            var workflowJob = new WorkflowJob
            {
                JobProfile = jobProfileId,
                JobInput = new JobParameterBag
                {
                    ["bmContent"] = @event["data"]["bmContent"],
                    ["bmEssence"] =  @event["data"]["bmEssence"]
                }
            };

            workflowJob = await resourceManager.CreateAsync(workflowJob);

            return JToken.FromObject(new
            {
                aiWorkflow = workflowJob.Id,
                bmContent = @event["data"]["bmContent"],
                websiteMediaFile = @event["data"]["websiteFile"]
            });
        }
    }
}