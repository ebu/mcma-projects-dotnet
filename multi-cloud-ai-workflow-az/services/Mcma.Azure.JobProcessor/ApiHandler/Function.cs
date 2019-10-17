using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Api.Routes.Defaults;
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

namespace Mcma.Azure.JobProcessor.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAzureFunctionKeyAuth());

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static McmaApiRouteCollection Routes { get; } =
            new DefaultRouteCollectionBuilder<JobProcess>(DbTableProvider)
                .AddAll()
                .Route(r => r.Create).Configure(r =>
                    r.OnCompleted(
                        async (requestContext, jobProcess) =>
                        {
                            var workerInvoker = new QueueWorkerInvoker(requestContext);
                            
                            await workerInvoker.InvokeAsync(
                                requestContext.Variables.WorkerFunctionId(),
                                "CreateJobAssignment",
                                input: new { jobProcessId = jobProcess.Id });
                        }
                    )
                )
                .Route(r => r.Update).Remove()
                .Build();

        private static AzureFunctionApiController ApiController { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(Routes)
                .AddRoute(
                    HttpMethod.Post.Method,
                    "/job-processes/{id}/notifications",
                    JobProcessRoutes.ProcessNotificationAsync(DbTableProvider, ctx => new QueueWorkerInvoker(ctx)))
                .ToAzureFunctionApiController();

        [FunctionName("JobProcessorApiHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, Route = "{*resourcePath}")] HttpRequest request,
            string resourcePath,
            ILogger log,
            ExecutionContext executionContext)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            return await ApiController.HandleRequestAsync(request);
        }
    }
}
