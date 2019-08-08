using System;
using System.Collections.Generic;
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

namespace Mcma.Aws.Workflows.Conform.CreateMediaAsset
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static readonly string RepositoryBucket = Environment.GetEnvironmentVariable(nameof(RepositoryBucket));

        private BMContent CreateBmContent(string title, string description)
            => new BMContent
            {
                ["name"] = title,
                ["description"] = description,
                ["bmEssences"] = new List<BMEssence>(),
                ["awsAiMetadata"] = default(JObject),
                ["azureAiMetadata"] = default(JObject)
            };

        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(AwsV4AuthContext.Global));

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
                    Progress = 18
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var metadata = @event["input"]["metadata"].ToMcmaObject<DescriptiveMetadata>();

            var bmc = CreateBmContent(metadata.Name, metadata.Description);

            bmc = await resourceManager.CreateAsync(bmc);

            if (bmc.Id == null)
                throw new Exception("Failed to register BMContent");
            
            return bmc.Id;
        }
    }
}