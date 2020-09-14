using System.Threading.Tasks;
using Mcma.Api.Routes;
using Mcma.Api.Routing.Defaults;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Mcma.Azure.ServiceRegistry.ApiHandler
{
    public static class Function
    {
        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("service-registry-api-handler");

        private static IDocumentDatabaseTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderConfiguration().FromEnvironmentVariables());

        private static McmaApiRouteCollection ServiceRoutes { get; } = new DefaultRouteCollection<Service>(DbTableProvider);

        private static McmaApiRouteCollection JobProfileRoutes { get; } = new DefaultRouteCollection<JobProfile>(DbTableProvider);

        private static AzureFunctionApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(ServiceRoutes)
                .AddRoutes(JobProfileRoutes)
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("ServiceRegistryApiHandler")]
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
