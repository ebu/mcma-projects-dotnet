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
using Mcma.Worker.Builders;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.JobRepository.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageLocator>();
            
        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureFunctionKeyAuth();

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static IResourceManagerProvider ResourceManagerProvider { get; } = new ResourceManagerProvider(AuthProvider);

        private static IWorker Worker =
            new WorkerBuilder()
                .HandleOperation(new CreateJobProcess(ResourceManagerProvider, DbTableProvider))
                .HandleOperation(new DeleteJobProcess(ResourceManagerProvider))
                .HandleOperation(new ProcessNotification(ResourceManagerProvider, DbTableProvider))
                .Build();
            
        [FunctionName("JobRepositoryWorker")]
        public static async Task Run(
            [QueueTrigger("job-repository-work-queue", Connection = "")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            await Worker.DoWorkAsync(queueMessage.ToWorkerRequest());
        }
    }
}