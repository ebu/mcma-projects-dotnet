using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.Functions.Worker;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Mcma.Azure.AwsAiService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("aws-ai-service-worker");

        private static ProviderCollection ProviderCollection = new ProviderCollection(
            LoggerProvider,
            new ResourceManagerProvider(AuthProvider),
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables()),
            AuthProvider
        );

        private static IWorker Worker { get; } =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddJobProcessing<AIJob>(
                    x =>
                        x.AddProfile<TranscribeAudio>()
                         .AddProfile<TranslateText>()
                         .AddProfile<DetectCelebrities>())
                .AddOperation(new ProcessTranscribeJobResult(ProviderCollection))
                .AddOperation(new ProcessRekognitionResult(ProviderCollection));
                
        [FunctionName("AwsAiServiceWorker")]
        public static async Task Run(
            [QueueTrigger("aws-ai-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            var request = queueMessage.ToWorkerRequest();

            LoggerProvider.AddLogger(log, request.Tracker);

            await Worker.DoWorkAsync(request);
        }
    }
}
