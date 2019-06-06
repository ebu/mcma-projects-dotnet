using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Aws;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Ai.ExtractSpeechToText
{
    public class Function
    {
        private const string JOB_PROFILE_NAME = "AWSTranscribeAudio";
        private const string JOB_RESULTS_PREFIX = "AIResults/";

        private static readonly string TEMP_BUCKET = Environment.GetEnvironmentVariable(nameof(TEMP_BUCKET));
        private static readonly string ACTIVITY_CALLBACK_URL = Environment.GetEnvironmentVariable(nameof(ACTIVITY_CALLBACK_URL));
        private static readonly string ACTIVITY_ARN = Environment.GetEnvironmentVariable(nameof(ACTIVITY_ARN));

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    ParallelProgress =  { ["speech-text-translate"] = 20 }
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

            var job = new AIJob
            {
                JobProfile = jobProfileId,
                JobInput = new JobParameterBag
                {
                    ["inputFile"] = @event["data"]["mediaFileLocator"],
                    ["outputLocation"] = new S3Locator
                    {
                        AwsS3Bucket = TEMP_BUCKET,
                        AwsS3KeyPrefix = JOB_RESULTS_PREFIX
                    }
                },
                NotificationEndpoint = new NotificationEndpoint
                {
                    HttpEndpoint = ACTIVITY_CALLBACK_URL + "?taskToken=" + Uri.EscapeDataString(taskToken)
                }
            };

            job = await resourceManager.CreateAsync(job);

            return job.Id;
        }
    }
}