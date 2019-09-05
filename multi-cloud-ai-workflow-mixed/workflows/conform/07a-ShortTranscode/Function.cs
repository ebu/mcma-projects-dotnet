using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
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

namespace Mcma.Aws.Workflows.Conform.ShortTranscode
{
    public class Function
    {
        static Function() => McmaTypes.Add<S3Locator>();
        private static readonly string ActivityArn = Environment.GetEnvironmentVariable(nameof(ActivityArn));
        private static readonly string ActivityCallbackUrl = Environment.GetEnvironmentVariable(nameof(ActivityCallbackUrl));
        private static readonly string TempBucket = Environment.GetEnvironmentVariable(nameof(TempBucket));
        private static readonly string RepositoryBucket = Environment.GetEnvironmentVariable(nameof(RepositoryBucket));
        private static readonly string WebsiteBucket = Environment.GetEnvironmentVariable(nameof(WebsiteBucket));

        private const string JOB_PROFILE_NAME = "CreateProxyLambda";

        private static EnvironmentVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(
                new AuthProvider()
                    .AddAwsV4Auth(AwsV4AuthContext.Global)
                    .AddAzureFunctionKeyAuth());

        public async Task<string> Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = ResourceManagerProvider.Get(EnvironmentVariableProvider);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 54
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var stepFunction = new AmazonStepFunctionsClient();
            var data = await stepFunction.GetActivityTaskAsync(new GetActivityTaskRequest
            {
                ActivityArn = ActivityArn
            });

            var taskToken = data.TaskToken;
            if (taskToken == null)
                throw new Exception("Failed to obtain activity task");

            @event = JToken.Parse(data.Input);

            var jobProfiles = await resourceManager.GetAsync<JobProfile>(("name", JOB_PROFILE_NAME));

            var jobProfileId = jobProfiles?.FirstOrDefault()?.Id;

            if (jobProfileId == null)
                throw new Exception($"JobProfile '{JOB_PROFILE_NAME}' not found");

            var createProxyJob = new TransformJob
            {
                JobProfile = jobProfileId,
                JobInput = new JobParameterBag
                {
                    ["inputFile"] = @event["data"]["repositoryFile"],
                    ["outputLocation"] = new S3Locator
                    {
                        AwsS3Bucket = RepositoryBucket,
                        AwsS3KeyPrefix = "TransformJobResults/"
                    }
                },
                NotificationEndpoint = new NotificationEndpoint
                {
                    HttpEndpoint = ActivityCallbackUrl + "?taskToken=" + Uri.EscapeDataString(taskToken)
                }
            };

            createProxyJob = await resourceManager.CreateAsync(createProxyJob);

            return createProxyJob.Id;
        }
    }
}