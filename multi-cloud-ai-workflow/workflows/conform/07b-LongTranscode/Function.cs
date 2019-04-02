using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.LongTranscode
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        private static readonly string ACTIVITY_ARN = Environment.GetEnvironmentVariable(nameof(ACTIVITY_ARN));
        private static readonly string ACTIVITY_CALLBACK_URL = Environment.GetEnvironmentVariable(nameof(ACTIVITY_CALLBACK_URL));
        private static readonly string TEMP_BUCKET = Environment.GetEnvironmentVariable(nameof(TEMP_BUCKET));
        private static readonly string REPOSITORY_BUCKET = Environment.GetEnvironmentVariable(nameof(REPOSITORY_BUCKET));
        private static readonly string WEBSITE_BUCKET = Environment.GetEnvironmentVariable(nameof(WEBSITE_BUCKET));

        private const string JOB_PROFILE_NAME = "CreateProxyEC2";
        
        public async Task Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 54
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var stepFunction = new AmazonStepFunctionsClient();
            var data = await stepFunction.GetActivityTaskAsync(new GetActivityTaskRequest
            {
                ActivityArn = ACTIVITY_ARN
            });

            var taskToken = data.TaskToken;
            if (taskToken == null)
                throw new Exception("Failed to obtain activity task");

            @event = JToken.Parse(data.Input);

            var jobProfiles = await resourceManager.GetAsync<JobProfile>(("name", JOB_PROFILE_NAME));

            var jobProfileId = jobProfiles?.FirstOrDefault()?.Id;

            if (jobProfileId == null)
                throw new Exception($"JobProfile '{JOB_PROFILE_NAME}' not found");

            var createProxyJob = new TransformJob
            {
                JobProfile = jobProfileId,
                JobInput = new JobParameterBag
                {
                    ["inputFile"] = @event["data"]["repositoryFile"],
                    ["outputLocation"] = new S3Locator
                    {
                        AwsS3Bucket = REPOSITORY_BUCKET,
                        AwsS3KeyPrefix = "TransformJobResults/"
                    }
                },
                NotificationEndpoint = new NotificationEndpoint
                {
                    HttpEndpoint = ACTIVITY_CALLBACK_URL + "?taskToken=" + Uri.EscapeDataString(taskToken)
                }
            };

            createProxyJob = await resourceManager.CreateAsync(createProxyJob);
        }
    }
}