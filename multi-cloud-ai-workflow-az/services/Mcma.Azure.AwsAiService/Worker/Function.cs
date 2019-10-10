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
using Mcma.Worker.Builders;
using Microsoft.WindowsAzure.Storage.Queue;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.AwsAiService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageLocator>();
        
        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAzureFunctionKeyAuth());

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static IWorker Worker { get; } =
            new WorkerBuilder()
                .HandleJobsOfType<AIJob>(
                    DbTableProvider,
                    ResourceManagerProvider,
                    x =>
                        x.AddProfile<TranscribeAudio>(TranscribeAudio.Name)
                         .AddProfile<TranslateText>(TranslateText.Name)
                         .AddProfile<DetectCelebrities>(DetectCelebrities.Name))
                .HandleOperation(new ProcessTranscribeJobResult(DbTableProvider, ResourceManagerProvider))
                .HandleOperation(new ProcessRekognitionResult(DbTableProvider, ResourceManagerProvider))
                .Build();
                
        [FunctionName("AwsAiServiceWorker")]
        public static async Task Run(
            [QueueTrigger("aws-ai-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            await Worker.DoWorkAsync(queueMessage.ToWorkerRequest());
        }
    }
}
