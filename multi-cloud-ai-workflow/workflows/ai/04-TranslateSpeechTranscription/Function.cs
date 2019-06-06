using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
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

namespace Mcma.Aws.Workflows.Ai.TranslateSpeechTranscription
{
    public class Function
    {
        // Environment Variable(AWS Lambda)
        private static readonly string TEMP_BUCKET = Environment.GetEnvironmentVariable(nameof(TEMP_BUCKET));
        private static readonly string ACTIVITY_CALLBACK_URL = Environment.GetEnvironmentVariable(nameof(ACTIVITY_CALLBACK_URL));
        private static readonly string ACTIVITY_ARN = Environment.GetEnvironmentVariable(nameof(ACTIVITY_ARN));

        private const string JOB_PROFILE_NAME = "AWSTranslateText";
        private const string JOB_RESULTS_PREFIX = "AIResults/";

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
                    ParallelProgress =  { ["speech-text-translate"] = 60 }
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

            // writing speech transcription to a textfile in temp bucket
            var bmContent = await resourceManager.ResolveAsync<BMContent>(@event["input"]["bmContent"].Value<string>());

            // get the transcript from the BMContent
            var transcript =
                bmContent.Get<McmaExpandoObject>("awsAiMetadata")
                         ?.Get<McmaExpandoObject>("transcription")
                         ?.Get<string>("original");

            if (transcript == null)
                throw new Exception("Missing transcription on BMContent");

            var s3Params = new PutObjectRequest
            {
                BucketName = TEMP_BUCKET,
                Key = "AiInput/" + Guid.NewGuid() + ".txt",
                ContentBody = transcript
            };

            var s3Client = new AmazonS3Client();
            await s3Client.PutObjectAsync(s3Params);

            var job = new AIJob
            {
                JobProfile = jobProfileId,
                JobInput = new JobParameterBag
                {
                    ["inputFile"] = new S3Locator
                    {
                        AwsS3Bucket = s3Params.BucketName,
                        AwsS3Key = s3Params.Key
                    },
                    ["targetLanguageCode"] = "ja",
                    ["outputLocation"] = new S3Locator
                    {
                        AwsS3Bucket = TEMP_BUCKET,
                        AwsS3Key = JOB_RESULTS_PREFIX
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