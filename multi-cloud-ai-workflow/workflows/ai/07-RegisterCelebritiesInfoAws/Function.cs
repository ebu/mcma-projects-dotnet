using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Ai.RegisterCelebritiesInfoAws
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
                    ParallelProgress =  { ["detect-celebrities-aws"] = 80 }
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            // get ai job id (first non null entry in array)
            var jobId = @event["data"]["awsCelebritiesJobId"]?.FirstOrDefault(id => id != null)?.Value<string>();
            if (jobId == null)
                throw new Exception("Failed to obtain awsCelebritiesJobId");
            
            Logger.Debug("[awsCelebritiesJobId]:", jobId);

            // get result of ai job
            var job = await resourceManager.ResolveAsync<AIJob>(jobId);

            S3Locator outputFile;
            if (!job.JobOutput.TryGet<S3Locator>(nameof(outputFile), false, out outputFile))
                throw new Exception($"AI job '{jobId}' does not specify an output file.");

            // get the response from Rekognition, stored as a file on S3
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
                throw new Exception("Unable to celebrities info file in bucket '" + s3Bucket + "' with key '" + s3Key + "'", error);
            }

            // read the result from the file in S3 as a Rekognition response object
            var celebritiesResult = (await s3Object.ResponseStream.ReadJsonFromStreamAsync()).ToMcmaObject<GetCelebrityRecognitionResponse>();

            var celebrityRecognitionList = new List<CelebrityRecognition>();
            var lastRecognitions = new Dictionary<string, long>();

            foreach (var celebrity in celebritiesResult.Celebrities)
            {
                // get the timestamp of the last time we hit a recognition for this celebrity (if any)
                var lastRecognized = lastRecognitions.ContainsKey(celebrity.Celebrity.Name) ? lastRecognitions[celebrity.Celebrity.Name] : default(long?);

                // we only want recognitions at 3 second intervals, and only when the confidence is at least 50%
                if ((!lastRecognized.HasValue || celebrity.Timestamp - lastRecognized.Value > 3000) && celebrity.Celebrity.Confidence > 50)
                {
                    // mark the timestamp of the last recognition for this celebrity
                    lastRecognitions[celebrity.Celebrity.Name] = celebrity.Timestamp;

                    // add to the list that we actually want to store
                    celebrityRecognitionList.Add(celebrity);
                }
            }

            // store the filtered results back on the original object
            celebritiesResult.Celebrities = celebrityRecognitionList;

            Logger.Debug("AWS Celebrities result", celebritiesResult.ToMcmaJson().ToString());

            var bmContent = await resourceManager.ResolveAsync<BMContent>(@event["input"]["bmContent"].Value<string>());

            // store the celebrity data back onto the AwsAiMetadata property on the BMContent, either using the existing object or creating a new one
            bmContent
                .GetOrAdd<McmaExpandoObject>("awsAiMetadata")
                    .Set("celebrities", celebritiesResult.ToMcmaJson(true));

            await resourceManager.UpdateAsync(bmContent);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    ParallelProgress =  { ["detect-celebrities-aws"] = 100 }
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