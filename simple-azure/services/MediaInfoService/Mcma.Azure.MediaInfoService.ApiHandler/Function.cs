using System.Threading.Tasks;
using Mcma.Api.Routing.Defaults;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.FunctionsApi;
using Mcma.Azure.Logger;
using Mcma.Azure.WorkerInvoker;
using Mcma.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Mcma.Azure.MediaInfoService.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();
        
        private static AzureFunctionApiController Controller { get; } =
            new DefaultJobRouteCollection(new CosmosDbTableProvider(), new QueueWorkerInvoker())
                .ToAzureFunctionApiController(new AppInsightsLoggerProvider("mediainfo-service-worker"));

        [FunctionName("MediaInfoServiceApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ExecutionContext executionContext)
        {
            return await Controller.HandleRequestAsync(request, executionContext);
        }
    }
}
