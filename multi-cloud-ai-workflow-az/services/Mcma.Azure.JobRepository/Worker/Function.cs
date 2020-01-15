using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.Functions.Worker;
using Mcma.Client;
using Mcma.Core.Serialization;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Mcma.Azure.JobRepository.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("job-repository-worker");
            
        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static ProviderCollection ProviderCollection { get; } = new ProviderCollection(
            LoggerProvider,
            new ResourceManagerProvider(AuthProvider),
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables()),
            AuthProvider
        );

        private static IWorker Worker =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddOperation(new CreateJobProcess(ProviderCollection))
                .AddOperation(new DeleteJobProcess(ProviderCollection))
                .AddOperation(new ProcessNotification(ProviderCollection));
            
        [FunctionName("JobRepositoryWorker")]
        public static async Task Run(
            [QueueTrigger("job-repository-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            var request = queueMessage.ToWorkerRequest();

            LoggerProvider.AddLogger(log, request.Tracker);

            await Worker.DoWorkAsync(request);
        }
    }
}
