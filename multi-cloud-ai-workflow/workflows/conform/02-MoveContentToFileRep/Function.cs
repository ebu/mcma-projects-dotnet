using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.MoveContentToFileRep
{
    public class Function
    {
        private static readonly string REPOSITORY_BUCKET = Environment.GetEnvironmentVariable(nameof(REPOSITORY_BUCKET));
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));

        private static string yyyymmdd()
            => DateTime.UtcNow.ToString("yyyyMMdd");

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 9
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var inputFile = @event["input"]["inputFile"].ToMcmaObject<S3Locator>();

            var s3Bucket = REPOSITORY_BUCKET;
            var s3Key = yyyymmdd() + "/" + Guid.NewGuid();

            var idxLastDot = inputFile.AwsS3Key.LastIndexOf(".");
            if (idxLastDot > 0)
                s3Key += inputFile.AwsS3Key.Substring(idxLastDot);

            try
            {
                var s3Client = new AmazonS3Client();
                var destBucketLocation = await s3Client.GetBucketLocationAsync(s3Bucket);
                var regionEndpoint = RegionEndpoint.GetBySystemName(!string.IsNullOrWhiteSpace(destBucketLocation.Location) ? (string)destBucketLocation.Location : "us-east-1");
                var copyClient = new AmazonS3Client(regionEndpoint);
                await copyClient.CopyObjectAsync(new CopyObjectRequest
                {
                    SourceBucket = inputFile.AwsS3Bucket,
                    SourceKey = inputFile.AwsS3Key,
                    DestinationBucket = s3Bucket,
                    DestinationKey = s3Key
                });
            }
            catch (Exception error)
            {
                throw new Exception("Unable to read input file in bucket '" + inputFile.AwsS3Bucket + "' with key '" + inputFile.AwsS3Key + "' due to error: " + error);
            }

            return new S3Locator
            {
                AwsS3Bucket = s3Bucket,
                AwsS3Key = s3Key
            }.ToMcmaJson();
        }
    }
}