using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.Functions.Worker;
using Mcma.Client;
using Mcma.Core.Serialization;
using Mcma.Data;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.JobProcessor.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("job-processor-worker");
            
        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static ProviderCollection ProviderCollection { get; } = new ProviderCollection(
            LoggerProvider,
            new ResourceManagerProvider(AuthProvider),
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables()),
            AuthProvider
        );

        private static IWorker Worker =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddOperation(new CreateJobAssignment(ProviderCollection))
                .AddOperation(new DeleteJobAssignment(ProviderCollection))
                .AddOperation(new ProcessNotification(ProviderCollection));


        [FunctionName("JobProcessorWorker")]
        public static async Task Run(
            [QueueTrigger("job-processor-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            var request = queueMessage.ToWorkerRequest();

            LoggerProvider.AddLogger(log, request.Tracker);

            await Worker.DoWorkAsync(request);
        }
    }
}
