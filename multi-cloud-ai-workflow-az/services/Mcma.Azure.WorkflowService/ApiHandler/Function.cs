using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api.Routes.Defaults;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Client;
using Mcma.Core.Serialization;
using Mcma.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.WorkflowService.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("workflow-service-api-handler");

        private static IResourceManagerProvider ResourceManagerProvider { get; }
            = new ResourceManagerProvider(new AuthProvider().AddAzureAdManagedIdentityAuth());

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static AzureFunctionApiController Controller { get; } =
            DefaultRoutes.ForJobAssignments(DbTableProvider, (ctx, _) => new QueueWorkerInvoker(ctx))
                .AddAdditionalRoute(
                    HttpMethod.Post,
                    "/job-assignments/{id}/notifications",
                    Notifications.Handler(LoggerProvider, DbTableProvider, reqCtx => new QueueWorkerInvoker(reqCtx)))
                .AddAdditionalRoutes(ResourceRoutes.Get(LoggerProvider, ResourceManagerProvider))
                .Build()
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("WorkflowServiceApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log,
            ExecutionContext executionContext)
        {
            return await Controller.HandleRequestAsync(request, log);
        }
    }
}
