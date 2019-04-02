using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.CreateMediaAsset
{
    public class Function
    {
        private static readonly string REPOSITORY_BUCKET = Environment.GetEnvironmentVariable(nameof(REPOSITORY_BUCKET));
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));

        private BMContent CreateBmContent(string title, string description)
            => new BMContent
            {
                ["name"] = title,
                ["description"] = description,
                ["bmEssences"] = new List<BMEssence>(),
                ["awsAiMetadata"] = default(JObject),
                ["azureAiMetadata"] = default(JObject)
            };

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

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