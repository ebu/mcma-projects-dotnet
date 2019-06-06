using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Mcma.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Ai.RegisterSpeechTranslation
{
    public class Function
    {
        public async Task Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    ParallelProgress =  { ["speech-text-translate"] = 80 }
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            // get ai job id (first non null entry in array)
            var jobId = @event["data"]["translateJobId"]?.FirstOrDefault(id => id != null)?.Value<string>();
            if (jobId == null)
                throw new Exception("Failed to obtain TranslationJobId");
            
            Logger.Debug("[TranslationJobId]:", jobId);

            // get result of ai job
            var job = await resourceManager.ResolveAsync<AIJob>(jobId);

            S3Locator outputFile;
            if (!job.JobOutput.TryGet<S3Locator>(nameof(outputFile), false, out outputFile))
                throw new Exception($"AI job '{jobId}' does not specify an output file.");

            // get media info
            var s3Bucket = outputFile.AwsS3Bucket;
            var s3Key = outputFile.AwsS3Key;
            GetObjectResponse s3Object;
            try
            {
                var s3Client = new AmazonS3Client();
                s3Object = await s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = s3Bucket,
                    Key = s3Key,
                });
            }
            catch (Exception error)
            {
                throw new Exception("Unable to translation file in bucket '" + s3Bucket + "' with key '" + s3Key + "'", error);
            }

            var translationResult = await s3Object.ResponseStream.ReadStringFromStreamAsync();
            Logger.Debug("Translation result: {0}", translationResult);

            var bmContent = await resourceManager.ResolveAsync<BMContent>(@event["input"]["bmContent"].Value<string>());

            bmContent
                .GetOrAdd<McmaExpandoObject>("awsAiMetadata")
                    .GetOrAdd<McmaExpandoObject>("transcription")
                        .Set("translation", translationResult);

            await resourceManager.UpdateAsync(bmContent);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    ParallelProgress =  { ["speech-text-translate"] = 100 }
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }
        }
    }
}