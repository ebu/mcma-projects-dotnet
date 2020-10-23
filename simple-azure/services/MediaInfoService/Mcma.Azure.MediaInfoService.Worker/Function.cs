using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Logger;
using Mcma.Azure.MediaInfoService.Worker.Profiles;
using Mcma.Client;
using Mcma.Logging;
using Mcma.Serialization;
using Mcma.Worker;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.MediaInfoService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static ILoggerProvider LoggerProvider { get; } = new AppInsightsLoggerProvider("mediainfo-service-worker");

        private static IWorker Worker { get; } =
            new Mcma.Worker.Worker(
                    new ProviderCollection(
                        LoggerProvider,
                        new ResourceManagerProvider(new AuthProvider().AddAzureAdManagedIdentityAuth()),
                        new CosmosDbTableProvider()))
                .AddJobProcessing<AmeJob>(op => op.AddProfile<ExtractTechnicalMetadata>());
            
        [FunctionName("MediaInfoServiceWorker")]
        public static async Task Run(
            [QueueTrigger("mediainfo-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ExecutionContext executionContext)
        {
            var request = JObject.Parse(queueMessage.AsString).ToMcmaObject<WorkerRequest>();
            var logger = LoggerProvider.Get(executionContext.InvocationId.ToString(), request.Tracker);

            MediaInfoProcess.HostRootDir = executionContext.FunctionAppDirectory;

            await Worker.DoWorkAsync(new WorkerRequestContext(request, executionContext.InvocationId.ToString(), logger));
        }
    }
}
