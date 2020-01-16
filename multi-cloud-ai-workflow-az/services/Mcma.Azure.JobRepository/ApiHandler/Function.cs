using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api;
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
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.JobRepository.ApiHandler
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageFileLocator>().Add<BlobStorageFolderLocator>();

        private static MicrosoftLoggerProvider LoggerProvider { get; } = new MicrosoftLoggerProvider("job-repository-api-handler");

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static McmaApiRouteCollection DefaultRoutes { get; } =
            new DefaultRouteCollectionBuilder<Job>(DbTableProvider)
                .AddAll()
                .Route(r => r.Create).Configure(r =>
                    r.OnCompleted(
                        async (ctx, job) =>
                        {
                            var logger = LoggerProvider.Get(ctx.GetTracker());

                            logger.Info("Invoking worker at {0}", ctx.WorkerFunctionId());

                            var workerInvoker = new QueueWorkerInvoker(ctx);

                            await workerInvoker.InvokeAsync(
                                ctx.WorkerFunctionId(),
                                "CreateJobProcess",
                                input: new { jobId = job.Id }
                            );
                        }
                    )
                )
                .Route(r => r.Delete).Configure(r =>
                    r.OnCompleted(
                        async (ctx, job) =>
                        {
                            var workerInvoker = new QueueWorkerInvoker(ctx);

                            if (!string.IsNullOrWhiteSpace(job.JobProcess))
                                await workerInvoker.InvokeAsync(
                                    ctx.WorkerFunctionId(),
                                    "DeleteJobProcess",
                                    input: new { jobProcessId = job.JobProcess }
                                );
                        }
                    )
                )
                .WithSimpleQueryFiltering()
                .Build();
                

        private static McmaApiRouteCollection AllRoutes { get; } =
            new McmaApiRouteCollection()
                .AddRoutes(DefaultRoutes)
                .AddRoute(HttpMethod.Post, "/jobs/{id}/stop", Actions.StopHandler())
                .AddRoute(HttpMethod.Post, "/jobs/{id}/cancel", Actions.CancelHandler())
                .AddRoute(HttpMethod.Post, "/jobs/{id}/notifications", Notifications.Handler(DbTableProvider, reqCtx => new QueueWorkerInvoker(reqCtx)));
        
        private static AzureFunctionApiController Controller { get; } = AllRoutes.ToAzureFunctionApiController(LoggerProvider);

        [FunctionName("JobRepositoryApiHandler")]
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
