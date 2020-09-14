using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.Functions.Worker;
using Mcma.Azure.MediaInfoService.Worker.Profiles;
using Mcma.Client;
using Mcma.Serialization;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Mcma.Azure.MediaInfoService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("mediainfo-service-worker");

        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static ProviderCollection ProviderCollection { get; } =
            new ProviderCollection(
                LoggerProvider,
                new ResourceManagerProvider(AuthProvider),
                new CosmosDbTableProvider(new CosmosDbTableProviderConfiguration().FromEnvironmentVariables()),
                AuthProvider
            );

        private static IWorker Worker { get; } =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddJobProcessing<AmeJob>(op => op.AddProfile<ExtractTechnicalMetadata>());
            
        [FunctionName("MediaInfoServiceWorker")]
        public static async Task Run(
            [QueueTrigger("mediainfo-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log,
            ExecutionContext executionContext)
        {
            var workerRequest = queueMessage.ToWorkerRequest();

            var logger = LoggerProvider.AddLogger(log, executionContext.InvocationId.ToString(), workerRequest.Tracker);

            MediaInfoProcess.HostRootDir = executionContext.FunctionAppDirectory;

            await Worker.DoWorkAsync(new WorkerRequestContext(workerRequest, executionContext.InvocationId.ToString(), logger));
        }
    }
}
