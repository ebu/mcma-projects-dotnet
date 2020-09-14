using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.Functions.Worker;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Client;
using Mcma.Context;
using Mcma.Serialization;
using Mcma.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

using McmaLogger = Mcma.Logging.Logger;

namespace Mcma.Azure.JobProcessor.Worker
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();
        
        private static IContextVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("job-processor-worker");
            
        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static ProviderCollection ProviderCollection { get; } = new ProviderCollection(
            LoggerProvider,
            new ResourceManagerProvider(AuthProvider),
            new CosmosDbTableProvider(new CosmosDbTableProviderConfiguration().FromEnvironmentVariables()),
            AuthProvider
        );

        private static DataController DataController { get; } =
            new DataController(EnvironmentVariableProvider.TableName(), EnvironmentVariableProvider.PublicUrl());

        private static IJobCheckerTrigger JobCheckerTrigger { get; } = new LogicAppWorkflowCheckerTrigger(EnvironmentVariableProvider);

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
            ILogger log,
            ExecutionContext executionContext)
        {
            var request = queueMessage.ToWorkerRequest();

            var logger = LoggerProvider.AddLogger(log, executionContext.InvocationId.ToString(), request.Tracker);

            await Worker.DoWorkAsync(new WorkerRequestContext(request, executionContext.InvocationId.ToString(), logger));
        }
    }
}
