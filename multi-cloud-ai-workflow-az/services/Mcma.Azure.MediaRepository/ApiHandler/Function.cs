using System.Threading.Tasks;
using Mcma.Api.Routes;
using Mcma.Api.Routes.Defaults;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.MediaRepository.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("media-repository-api-handler");

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static McmaApiRouteCollection ContentRoutes { get; } =
            new DefaultRouteCollectionBuilder<BMContent>(DbTableProvider, "bm-contents").AddAll().WithSimpleQueryFiltering().Build();

        private static McmaApiRouteCollection EssenceRoutes { get; } =
            new DefaultRouteCollectionBuilder<BMEssence>(DbTableProvider, "bm-essences").AddAll().WithSimpleQueryFiltering().Build();

        private static AzureFunctionApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(ContentRoutes)
                .AddRoutes(EssenceRoutes)
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("MediaRepositoryApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log)
        {
            return await ApiController.HandleRequestAsync(request, log);
        }
    }
}
