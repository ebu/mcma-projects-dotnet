using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Logger;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Client;
using Mcma.Logging;
using Mcma.Serialization;
using Mcma.Worker;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.JobProcessor.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static ILoggerProvider LoggerProvider { get; } = new AppInsightsLoggerProvider("job-processor-worker");

        private static ProviderCollection ProviderCollection { get; } =
            new ProviderCollection(
                LoggerProvider,
                new ResourceManagerProvider(new AuthProvider().AddAzureAdManagedIdentityAuth()),
                new CosmosDbTableProvider());

        private static DataController DataController { get; } = new DataController();

        private static IJobCheckerTrigger JobCheckerTrigger { get; } = new LogicAppWorkflowCheckerTrigger();

        private static IWorker Worker { get; } =
            new Mcma.Worker.Worker(ProviderCollection)
                .AddOperation(new StartJob(ProviderCollection, DataController, JobCheckerTrigger))
                .AddOperation(new CancelJob(ProviderCollection, DataController))
                .AddOperation(new RestartJob(ProviderCollection, DataController, JobCheckerTrigger))
                .AddOperation(new FailJob(ProviderCollection, DataController))
                .AddOperation(new DeleteJob(ProviderCollection, DataController))
                .AddOperation(new ProcessNotification(ProviderCollection, DataController));

        [FunctionName("JobProcessorWorker")]
        public static async Task Run(
            [QueueTrigger("job-processor-work-queue", Connection = "WorkQueueStorage")] CloudQueueMessage queueMessage,
            ExecutionContext executionContext)
        {
            var request = JObject.Parse(queueMessage.AsString).ToMcmaObject<WorkerRequest>();
            var logger = LoggerProvider.Get(executionContext.InvocationId.ToString(), request.Tracker);

            await Worker.DoWorkAsync(new WorkerRequestContext(request, executionContext.InvocationId.ToString(), logger));
        }
    }
}
