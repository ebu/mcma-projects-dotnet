using System.Threading.Tasks;
using Mcma.Api.Routes.Defaults;
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

namespace Mcma.Azure.AmeService.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("ame-service-worker");

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static AzureFunctionApiController Controller { get; } =
            DefaultRoutes.ForJobAssignments(DbTableProvider, (reqCtx, _) => new QueueWorkerInvoker(reqCtx))
                // .Route(r => r.Create).Configure(configure =>
                //     configure
                //         .OnStarted(reqCtx =>
                //         {
                //             var logger = LoggerProvider.Get(reqCtx.GetTracker());
                //             logger.Info("AmeService OnCreate. WorkerFunctionId = " + reqCtx.WorkerFunctionId());
                //             return Task.CompletedTask;
                //         })
                //         .OnCompleted((reqCtx, jobAssignment) =>
                //         {
                //             var logger = LoggerProvider.Get(reqCtx.GetTracker());
                //             logger.Info("AmeService OnCompleted. JobAssignmentId = " + jobAssignment.Id);
                //             return Task.CompletedTask;
                //         }))
                .Build()
                .ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("AmeServiceApiHandler")]
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
