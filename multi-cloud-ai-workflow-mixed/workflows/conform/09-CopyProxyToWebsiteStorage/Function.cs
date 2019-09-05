using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
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

namespace Mcma.Aws.Workflows.Conform.CopyProxyToWebsiteStorage
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static readonly string WebsiteBucket = Environment.GetEnvironmentVariable(nameof(WebsiteBucket));
        
        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(
                new AuthProvider()
                    .AddAwsV4Auth(AwsV4AuthContext.Global)
                    .AddAzureFunctionKeyAuth());
        
        private string GetTransformJobId(JToken @event)
            => @event["data"]["transformJob"]?.FirstOrDefault()?.ToString();

        public async Task<S3Locator> Handler(JToken @event, ILambdaContext context)
        {

            var resourceManager = ResourceManagerProvider.Get(EnvironmentVariableProvider);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 72
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var transformJobId = GetTransformJobId(@event);

            S3Locator outputFile;
            if (transformJobId == null)
            {
                Logger.Debug("Transform job ID is null. Transform was not done. Using original essence as proxy.");

                var bme = await resourceManager.ResolveAsync<BMEssence>(@event["data"]["bmEssence"]?.ToString());

                outputFile = (S3Locator)bme.Locations[0];
            }
            else
            {
                Logger.Debug($"Getting proxy location from transform job {transformJobId}.");

                var transformJob = await resourceManager.ResolveAsync<TransformJob>(transformJobId);

                outputFile = transformJob.JobOutput.Get<S3Locator>(nameof(outputFile));
            }

            var s3Bucket = WebsiteBucket;
            var s3Key = "media/" + Guid.NewGuid();

            var idxLastDot = outputFile.AwsS3Key.LastIndexOf(".");
            if (idxLastDot > 0)
                s3Key += outputFile.AwsS3Key.Substring(idxLastDot);

            var s3 = new AmazonS3Client();
            var data = await s3.GetBucketLocationAsync(s3Bucket);
            try
            {
                var copyParams = new CopyObjectRequest
                {
                    SourceBucket = outputFile.AwsS3Bucket,
                    SourceKey = outputFile.AwsS3Key,
                    DestinationBucket = s3Bucket,
                    DestinationKey = s3Key
                };
                var regionEndpoint = RegionEndpoint.GetBySystemName(!string.IsNullOrWhiteSpace(data.Location) ? (string)data.Location : "us-east-1");
                var destS3 = new AmazonS3Client(regionEndpoint);
                await destS3.CopyObjectAsync(copyParams);
            }
            catch (Exception error)
            {
                throw new Exception("Unable to read input file in bucket '" + s3Bucket + "' with key '" + s3Key + "' due to error: " + error);
            }

            var s3SubDomain = !string.IsNullOrWhiteSpace(data.Location) ? $"s3-{data.Location}" : "s3";
            var httpEndpoint = "https://" + s3SubDomain + ".amazonaws.com/" + s3Bucket + "/" + s3Key;

            return new S3Locator
            {
                AwsS3Bucket = s3Bucket,
                AwsS3Key = s3Key,
                HttpEndpoint = httpEndpoint
            };
        }
    }
}