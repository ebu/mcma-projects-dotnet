using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.StartAiWorkflow
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        
        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

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