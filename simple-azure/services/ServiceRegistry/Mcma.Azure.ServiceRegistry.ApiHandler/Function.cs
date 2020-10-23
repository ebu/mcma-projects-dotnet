using System.Threading.Tasks;
using Mcma.Api.Routes;
using Mcma.Api.Routing.Defaults;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.FunctionsApi;
using Mcma.Azure.Logger;
using Mcma.Data;
using Mcma.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Mcma.Azure.ServiceRegistry.ApiHandler
{
    public static class Function
    {
        private static ILoggerProvider LoggerProvider { get; } = new AppInsightsLoggerProvider("service-registry-api-handler");

        private static IDocumentDatabaseTableProvider DbTableProvider { get; } = new CosmosDbTableProvider();
        
        private static AzureFunctionApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(new DefaultRouteCollection<Service>(DbTableProvider))
                .AddRoutes(new DefaultRouteCollection<JobProfile>(DbTableProvider))
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("ServiceRegistryApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ExecutionContext executionContext)
        {
            return await ApiController.HandleRequestAsync(request, executionContext);
        }
    }
}
