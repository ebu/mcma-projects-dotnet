using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes.Defaults;
using Mcma.Azure.AwsAiService.ApiHandler.Sns;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.AwsAiService.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAzureFunctionKeyAuth());

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static IWorkerInvoker WorkerInvoker { get; } = new QueueWorkerInvoker();

        private static AzureFunctionApiController Controller { get; } =
            new DefaultRouteCollectionBuilder<JobAssignment>(DbTableProvider)
                .ForJobAssignments((ctx, _) => new QueueWorkerInvoker(ctx))
                .AddRoute(HttpMethod.Post, "sns-notifications", SnsNotificationHandler.Create(ResourceManagerProvider, DbTableProvider, WorkerInvoker))
                .ToAzureFunctionApiController();

        [FunctionName("AwsAiServiceApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log,
            ExecutionContext executionContext)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            return await Controller.HandleRequestAsync(request);
        }
    }
}
