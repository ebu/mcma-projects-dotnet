using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.Functions.Worker;
using Mcma.Client;
using Mcma.Serialization;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

using McmaLogger = Mcma.Logging.Logger;

namespace Mcma.Azure.FFmpegService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("ffmpeg-service-worker");

        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static ProviderCollection ProviderCollection { get; } = new ProviderCollection(
            LoggerProvider,
            new ResourceManagerProvider(AuthProvider),
            new CosmosDbTableProvider(new CosmosDbTableProviderConfiguration().FromEnvironmentVariables()),
            AuthProvider
        );

        private static IWorker Worker { get; } =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddJobProcessing<TransformJob>(x => x.AddProfile<ExtractThumbnail>());
            
        [FunctionName("FFmpegServiceWorker")]
        public static async Task Run(
            [QueueTrigger("ffmpeg-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log,
            ExecutionContext executionContext)
        {
            var request = queueMessage.ToWorkerRequest();

            FFmpegProcess.HostRootDir = executionContext.FunctionAppDirectory;

            var logger = LoggerProvider.AddLogger(log, executionContext.InvocationId.ToString(), request.Tracker);

            await Worker.DoWorkAsync(new WorkerRequestContext(request, executionContext.InvocationId.ToString(), logger));
        }
    }
}
