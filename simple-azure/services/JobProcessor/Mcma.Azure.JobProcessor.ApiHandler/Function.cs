using System.Threading.Tasks;
using Mcma.Api.Routes;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.FunctionsApi;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Azure.Logger;
using Mcma.Azure.WorkerInvoker;
using Mcma.Client;
using Mcma.Logging;
using Mcma.Serialization;
using Mcma.WorkerInvoker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static ILoggerProvider LoggerProvider { get; } = new AppInsightsLoggerProvider("job-processor-api-handler");

        private static DataController DataController { get; } = new DataController();

        private static IWorkerInvoker WorkerInvoker { get; } = new QueueWorkerInvoker();

        private static AzureFunctionApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(
                    new JobRoutes(DataController, new ResourceManagerProvider(new AuthProvider().AddAzureAdManagedIdentityAuth()), WorkerInvoker))
                .AddRoutes(
                    new JobExecutionRoutes(DataController, WorkerInvoker))
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("JobProcessorApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ExecutionContext executionContext)
        {
            return await ApiController.HandleRequestAsync(request, executionContext);
        }
    }
}
