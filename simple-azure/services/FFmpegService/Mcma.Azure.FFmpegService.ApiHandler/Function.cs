using System.Threading.Tasks;
using Mcma.Api.Routing.Defaults;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Azure.WorkerInvoker;
using Mcma.Context;
using Mcma.Data;
using Mcma.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Mcma.Azure.FFmpegService.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("ffmpeg-service-api-handler");

        private static IDocumentDatabaseTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderConfiguration().FromEnvironmentVariables());

        private static AzureFunctionApiController Controller { get; } =
            new DefaultJobRouteCollection(DbTableProvider, new QueueWorkerInvoker(new EnvironmentVariableProvider()))
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("FFmpegServiceApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log,
            ExecutionContext executionContext)
        {
            return await Controller.HandleRequestAsync(request, executionContext, log);
        }
    }
}
