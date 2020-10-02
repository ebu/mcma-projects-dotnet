using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Azure.WorkerInvoker;
using Mcma.Client;
using Mcma.Context;
using Mcma.Serialization;
using Mcma.WorkerInvoker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static IContextVariableProvider EnvironmentVariableProvider { get; } = new EnvironmentVariableProvider();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("job-processor-api-handler");

        private static IAuthProvider AuthProvider { get; } = new AuthProvider().AddAzureAdManagedIdentityAuth();

        private static IResourceManagerProvider ResourceManagerProvider { get; } = new ResourceManagerProvider(AuthProvider);

        private static DataController DataController { get; } =
            new DataController(EnvironmentVariableProvider.TableName(), EnvironmentVariableProvider.PublicUrl());

        private static IWorkerInvoker WorkerInvoker { get; } = new QueueWorkerInvoker(EnvironmentVariableProvider);

        private static AzureFunctionApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(new JobRoutes(DataController, ResourceManagerProvider, EnvironmentVariableProvider, WorkerInvoker))
                .AddRoutes(new JobExecutionRoutes(DataController, EnvironmentVariableProvider, WorkerInvoker))
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("JobProcessorApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log,
            ExecutionContext executionContext)
        {
            return await ApiController.HandleRequestAsync(request, executionContext, log);
        }
    }
}
