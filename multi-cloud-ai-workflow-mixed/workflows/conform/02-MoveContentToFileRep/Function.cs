using System;
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

namespace Mcma.Aws.Workflows.Conform.MoveContentToFileRep
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        
        private static readonly string RepositoryBucket = Environment.GetEnvironmentVariable(nameof(RepositoryBucket));

        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(
                new AuthProvider()
                    .AddAwsV4Auth(AwsV4AuthContext.Global)
                    .AddAzureFunctionKeyAuth());

        private static string yyyymmdd() => DateTime.UtcNow.ToString("yyyyMMdd");

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = ResourceManagerProvider.Get(EnvironmentVariableProvider);

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

            var s3Bucket = RepositoryBucket;
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