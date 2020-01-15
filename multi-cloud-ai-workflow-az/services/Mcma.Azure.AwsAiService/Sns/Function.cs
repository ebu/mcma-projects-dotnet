using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api.Routes;
using Mcma.Azure.AwsAiService.ApiHandler.Sns;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Core.Serialization;
using Mcma.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Mcma.Azure.AwsAiService.Sns
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("aws-ai-service-sns");

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static AzureFunctionApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoute(
                    HttpMethod.Post,
                    "sns-notifications",
                    SnsNotificationHandler.Create(DbTableProvider, LoggerProvider, reqCtx => new QueueWorkerInvoker(reqCtx)))
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("AwsAiServiceSnsHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log,
            ExecutionContext executionContext)
        {
            return await Controller.HandleRequestAsync(request, log);
        }
    }
}
