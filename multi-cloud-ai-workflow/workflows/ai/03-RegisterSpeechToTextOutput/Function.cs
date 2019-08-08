using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws.Client;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Ai.RegisterSpeechToTextOutput
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global));

        public async Task Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = ResourceManagerProvider.Get(EnvironmentVariableProvider);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    ParallelProgress =  { ["speech-text-translate"] = 40 }
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            // get ai job id (first non null entry in array)
            var jobId = @event["data"]["transcribeJobId"]?.FirstOrDefault(id => id != null)?.Value<string>();
            if (jobId == null)
                throw new Exception("Failed to obtain TranscribeJobId");
            
            Logger.Debug("[TranscribeJobId]:", jobId);

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
                throw new Exception("Unable to media info file in bucket '" + s3Bucket + "' with key '" + s3Key + "'", error);
            }

            var transcriptionResult = await s3Object.ResponseStream.ReadJsonFromStreamAsync();
            Logger.Debug("Transcription result: {0}", transcriptionResult.ToString(Formatting.Indented));

            var transcripts = transcriptionResult["results"]["transcripts"];
            Logger.Debug(transcripts.ToString(Formatting.Indented));

            var transcript = string.Empty;
            foreach (var ts in transcripts)
                transcript += ts["transcript"].Value<string>();

            var bmContent = await resourceManager.ResolveAsync<BMContent>(@event["input"]["bmContent"].Value<string>());
            
            bmContent
                .GetOrAdd<McmaExpandoObject>("awsAiMetadata")
                    .GetOrAdd<McmaExpandoObject>("transcription")
                        .Set("original", transcript);

            await resourceManager.UpdateAsync(bmContent);
        }
    }
}