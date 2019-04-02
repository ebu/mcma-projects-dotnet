using System;
using System.Collections.Generic;
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
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.ExtractTechnicalMetadata
{
    public class Function
    {
        private static readonly string TEMP_BUCKET = Environment.GetEnvironmentVariable(nameof(TEMP_BUCKET));
        private static readonly string ACTIVITY_CALLBACK_URL = Environment.GetEnvironmentVariable(nameof(ACTIVITY_CALLBACK_URL));
        private static readonly string ACTIVITY_ARN = Environment.GetEnvironmentVariable(nameof(ACTIVITY_ARN));

        public async Task<string> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 27
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var stepFunction = new AmazonStepFunctionsClient();
            Logger.Debug($"Getting Activity Task with ARN {ACTIVITY_ARN}...");
            var data = await stepFunction.GetActivityTaskAsync(new GetActivityTaskRequest
            {
                ActivityArn = ACTIVITY_ARN
            });

            var taskToken = data.TaskToken;
            if (taskToken == null)
                throw new Exception("Failed to obtain activity task");
            
            Logger.Debug($"Activity Task token is {taskToken}");

            @event = JToken.Parse(data.Input);
            
            Logger.Debug($"Getting job profile 'ExtractTechnicalMetadata'...");

            var jobProfiles = await resourceManager.GetAsync<JobProfile>(("name", "ExtractTechnicalMetadata"));

            var jobProfileId = jobProfiles?.FirstOrDefault()?.Id;

            if (jobProfileId == null)
                throw new Exception("JobProfile 'ExtractTechnicalMetadata' not found");

            var ameJob = new AmeJob
            {
                JobProfile = jobProfileId,
                JobInput = new JobParameterBag
                {
                    ["inputFile"] = @event["data"]["repositoryFile"],
                    ["outputLocation"] = new S3Locator
                    {
                        AwsS3Bucket = TEMP_BUCKET,
                        AwsS3KeyPrefix = "AmeJobResults/"
                    }
                },
                NotificationEndpoint = new NotificationEndpoint
                {
                    HttpEndpoint = ACTIVITY_CALLBACK_URL + "?taskToken=" + Uri.EscapeDataString(taskToken)
                }
            };
            
            Logger.Debug($"Submitting AME job...");

            ameJob = await resourceManager.CreateAsync(ameJob);
            
            Logger.Debug($"Successfully created AME job {ameJob.Id}.");

            return ameJob.Id;
        }
    }
}