using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Client;
using Mcma.Azure.CosmosDb;
using Mcma.Azure.Functions.Api;
using Mcma.Azure.Functions.Logging;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Serialization;
using Mcma.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using McmaLogger = Mcma.Core.Logging.Logger;

namespace Mcma.Azure.AzureAiService.ApiInsecure
{
    public static class Function
    {
        static Function() => McmaTypes.Add<BlobStorageLocator>();

        private static IResourceManagerProvider ResourceManagerProvider { get; } =
            new ResourceManagerProvider(new AuthProvider().AddAzureFunctionKeyAuth());

        private static IDbTableProvider DbTableProvider { get; } =
            new CosmosDbTableProvider(new CosmosDbTableProviderOptions().FromEnvironmentVariables());

        private static AzureFunctionApiController Controller { get; } =
            new McmaApiRouteCollection()
                .AddRoute(HttpMethod.Post, "/job-assignments/{id}/notifications", ProcessNotificationAsync)
                .ToAzureFunctionApiController();

        [FunctionName("Function")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request,
            ILogger log)
        {
            McmaLogger.Global = new MicrosoftLoggerWrapper(log);

            return await Controller.HandleRequestAsync(request);
        }

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            McmaLogger.Debug(nameof(ProcessNotificationAsync));
            McmaLogger.Debug(requestContext.Request.ToMcmaJson().ToString());
            
            var table = DbTableProvider.Table<JobAssignment>(requestContext.TableName());

            var jobAssignmentId = requestContext.PublicUrl() + "/job-assignments/" + requestContext.Request.PathVariables["id"];

            var jobAssignment = await table.GetAsync(jobAssignmentId);

            McmaLogger.Debug("jobAssignment = {0}", jobAssignment);

            if (jobAssignment == null)
            {
                requestContext.SetResponseResourceNotFound();
                return;
            }

            var notification = requestContext.Request.QueryStringParameters;
            McmaLogger.Debug("notification = {0}", notification);
            if (notification == null || !notification.Any())
            {
                requestContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                requestContext.Response.StatusMessage = "Missing notification in request Query String";
                return;
            }
            
            var workerInvoker = new QueueWorkerInvoker(requestContext);
                            
            await workerInvoker.InvokeAsync(
                requestContext.WorkerFunctionId(),
                "ProcessNotification",
                requestContext.GetAllContextVariables().ToDictionary(),
                new
                {
                    jobAssignmentId,
                    notification
                });
        }
    }
}
