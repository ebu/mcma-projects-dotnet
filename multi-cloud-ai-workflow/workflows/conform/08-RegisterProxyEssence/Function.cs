using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Mcma.Aws.Client;
using Mcma.Aws.Lambda;
using Mcma.Aws.S3;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.RegisterProxyEssence
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global));

        private string GetTransformJobId(JToken @event)
            => @event["data"]["transformJob"]?.FirstOrDefault()?.Value<string>();

        private BMEssence CreateBmEssence(BMContent bmContent, S3Locator location)
            => new BMEssence
            {
                BmContent = bmContent.Id,
                Locations = new Locator[] {location}
            };

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
                    Progress = 63
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }
            
            // get transform job id
            var transformJobId = GetTransformJobId(@event);

            // in case we did not do a transcode, just return the existing essence ID
            if (transformJobId == null)
                return @event["data"]["bmEssence"].Value<string>();

            // 
            var transformJob = await resourceManager.ResolveAsync<TransformJob>(transformJobId);

            S3Locator outputFile;
            if (!transformJob.JobOutput.TryGet<S3Locator>(nameof(outputFile), false, out outputFile))
                throw new Exception("Unable to get outputFile from AmeJob output.");

            var s3Bucket = outputFile.AwsS3Bucket;
            var s3Key = outputFile.AwsS3Key;

            var bmc = await resourceManager.ResolveAsync<BMContent>(@event["data"]["bmContent"]?.ToString());

            var locator = new S3Locator
            {
                AwsS3Bucket = s3Bucket,
                AwsS3Key = s3Key
            };

            var bme = CreateBmEssence(bmc, locator);

            bme = await resourceManager.CreateAsync(bme);
            if (bme?.Id == null)
                throw new Exception("Failed to register BMEssence.");

            bmc.BmEssences.Add(bme.Id);

            bmc = await resourceManager.UpdateAsync(bmc);

            return bme.Id;
        }
    }
}