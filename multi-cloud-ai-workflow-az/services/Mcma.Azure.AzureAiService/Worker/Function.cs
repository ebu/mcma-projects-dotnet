using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.Functions.Worker;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Mcma.Azure.AzureAiService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFolderLocator>().Add<BlobStorageFileLocator>();

        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("azure-ai-service-worker");

        private static ProviderCollection ProviderCollection { get; } = new ProviderCollection(
            LoggerProvider,
            new ResourceManagerProvider(AuthProvider),
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables()),
            AuthProvider
        );

        public static IWorker Worker { get; } =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddJobProcessing<AIJob>(
                    x =>
                        x.AddProfile<TranscribeAudio>()
                         .AddProfile<TranslateText>()
                         .AddProfile<ExtractAllAIMetadata>())
                .AddOperation(new ProcessNotification(ProviderCollection));

        [FunctionName("AzureAiServiceWorker")]
        public static async Task Run(
            [QueueTrigger("azure-ai-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            var request = queueMessage.ToWorkerRequest();

            LoggerProvider.AddLogger(log, request.Tracker);

            await Worker.DoWorkAsync(request);
        }
    }
}
