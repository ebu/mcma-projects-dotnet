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
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.WorkflowService.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAzureFunctionKeyAuth());

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static IWorker Worker =
            new Mcma.Worker.Builders.WorkerBuilder()
                .HandleJobsOfType<WorkflowJob>(
                    DbTableProvider,
                    ResourceManagerProvider,
                    x =>
                        x.AddProfile<RunWorkflow>("ConformWorkflow")
                         .AddProfile<RunWorkflow>("AiWorkflow"))
                .HandleOperation(new ProcessNotification(ResourceManagerProvider, DbTableProvider))
                .Build();

        [FunctionName("WorkflowServiceWorker")]
        public static async Task Run(
            [QueueTrigger("workflow-service-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ILogger log)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            await Worker.DoWorkAsync(queueMessage.ToWorkerRequest());
        }
    }
}
