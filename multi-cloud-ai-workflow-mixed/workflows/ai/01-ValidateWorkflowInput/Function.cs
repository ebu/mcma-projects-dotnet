using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
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

namespace Mcma.Aws.Workflows.Ai.ValidateWorkflowInput
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

        public async Task<S3Locator> Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = ResourceManagerProvider.Get(EnvironmentVariableProvider);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 0
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            // check the input and return mediaFileLocator which service as input for the AI workflows
            if (@event["input"] == null)
                throw new Exception("Missing workflow input");

            var input = @event["input"];

            if (input["bmContent"] == null)
                throw new Exception("Missing input.bmContent");

            if (input["bmEssence"] == null)
                throw new Exception("Missing input.bmEssence");

            var bmContent = await resourceManager.ResolveAsync<BMContent>(input["bmContent"].Value<string>());
            var bmEssence = await resourceManager.ResolveAsync<BMEssence>(input["bmEssence"].Value<string>());

            Logger.Debug(bmContent.ToMcmaJson().ToString());
            Logger.Debug(bmEssence.ToMcmaJson().ToString());

            // find the media locator in the website bucket with public httpEndpoint
            var mediaFileLocator =
                bmEssence.Locations.OfType<S3Locator>().FirstOrDefault(l => l.AwsS3Bucket == WebsiteBucket);

            if (mediaFileLocator == null)
                throw new Exception("No suitable Locator found on bmEssence");

            if (string.IsNullOrWhiteSpace(mediaFileLocator.HttpEndpoint))
                throw new Exception("Media file Locator does not have an httpEndpoint");

            return mediaFileLocator;
        }
    }
}