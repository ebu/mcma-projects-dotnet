using System.Threading.Tasks;
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
using Mcma.Worker.Builders;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.AzureAiService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFolderLocator>().Add<BlobStorageFileLocator>();
        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAzureFunctionKeyAuth());

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        public static IWorker Worker { get; } =
            new WorkerBuilder()
                .HandleJobsOfType<AIJob>(
                    DbTableProvider,
                    ResourceManagerProvider,
                    x =>
                        x.AddProfile<TranscribeAudio>(TranscribeAudio.Name)
                         .AddProfile<TranslateText>(TranslateText.Name)
                         .AddProfile<ExtractAllAIMetadata>(ExtractAllAIMetadata.Name))
                .HandleOperation(new ProcessNotification(DbTableProvider, ResourceManagerProvider))
                .Build();

        [FunctionName("AzureAiServiceWorker")]
        public static async Task Run(
            [QueueTrigger("azure-ai-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            await Worker.DoWorkAsync(queueMessage.ToWorkerRequest());
        }
    }
}
