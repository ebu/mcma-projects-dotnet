using System.Threading.Tasks;
using Mcma.Api.Routes.Defaults;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Core;
using Mcma.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.JobRepository.ApiHandler
{
    public static class Function
    {
        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static AzureFunctionApiController ApiController { get; } =
            new DefaultRouteCollectionBuilder<Job>(DbTableProvider)
                .AddAll()
                .WithSimpleQueryFiltering()
                .Build()
                .ToAzureFunctionApiController();

        [FunctionName("JobRepositoryApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            return await ApiController.HandleRequestAsync(request);
        }
    }
}
