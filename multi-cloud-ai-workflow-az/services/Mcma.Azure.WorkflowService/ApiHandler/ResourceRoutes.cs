using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.WorkflowService.ApiHandler
{
    internal static class ResourceRoutes
    {
        private static HttpClientHandler HttpClientHandler { get; } = new HttpClientHandler { AllowAutoRedirect = false };

        private static HttpClient HttpClient { get; } = new HttpClient(HttpClientHandler);

        private const string WorkflowCallbackUrlParamName = "workflowCallbackUrl";

        private static (Type, McmaResource) GetResourceAndType(McmaApiRequestContext requestContext)
        {
            var resourceJson = requestContext.GetRequestBodyJson();

            var resourceTypeString = resourceJson?["@type"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(resourceTypeString))
            {
                requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Resource type ('@type' property) not found in request body.");
                return (null, null);
            }

            var resourceType = McmaTypes.FindType(resourceTypeString);
            if (resourceType == null)
            {
                requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, $"Unknown resource type '{resourceTypeString}' in request body.");
                return (null, null);
            }

            return (resourceType, (McmaResource) resourceJson.ToMcmaObject(resourceType));
        }

        public static McmaApiRouteCollection Get(ILoggerProvider loggerProvider, IResourceManagerProvider resourceManagerProvider) =>
            new McmaApiRouteCollection()
                .AddRoute(HttpMethod.Post, "/resources", CreateResourceHandler(loggerProvider, resourceManagerProvider))
                .AddRoute(HttpMethod.Get, "/resources", GetResourceHandler(loggerProvider, resourceManagerProvider))
                .AddRoute(HttpMethod.Put, "/resources", UpdateResourceHandler(loggerProvider, resourceManagerProvider))
                .AddRoute(HttpMethod.Post, "/resource-notifications", ResourceNotificationHandler(loggerProvider));

        private static Func<McmaApiRequestContext, Task> CreateResourceHandler(ILoggerProvider loggerProvider, IResourceManagerProvider resourceManagerProvider)
            =>
            async requestContext =>
            {
                var logger = loggerProvider.Get(requestContext.GetTracker());
                logger.Info("Start CreateResource");
                
                var (resourceType, resourceToCreate) = GetResourceAndType(requestContext);
                if (resourceType == null)
                    return;
                
                logger.Info($"Creating resource of type {resourceType.Name}...");

                if (resourceToCreate is Job job && job.NotificationEndpoint?.HttpEndpoint != null)
                {
                    job.NotificationEndpoint.HttpEndpoint =
                        $"{requestContext.PublicUrl().TrimEnd('/')}/resource-notifications" +
                        $"?{WorkflowCallbackUrlParamName}={Uri.EscapeDataString(job.NotificationEndpoint.HttpEndpoint)}";
                    
                    logger.Info($"Set notification endpoint for job {job.NotificationEndpoint.HttpEndpoint}");
                }
                    
                logger.Info($"Getting resource manager");

                var resourceManager = resourceManagerProvider.Get(HttpClient, requestContext.GetResourceManagerConfig());
                
                logger.Info("Sending create request via resource manager...");
                
                var resource = await resourceManager.CreateAsync(resourceType, resourceToCreate);
                
                logger.Info($"Successfully created resource: {resource.Id}");

                requestContext.SetResponseResourceCreated(resource);
            };
        
        private static Func<McmaApiRequestContext, Task> GetResourceHandler(ILoggerProvider loggerProvider, IResourceManagerProvider resourceManagerProvider)
            =>
            async requestContext =>
            {
                var logger = loggerProvider.Get(requestContext.GetTracker());
                
                var resourceTypeName =
                    requestContext.Request.QueryStringParameters.ContainsKey("resourceType")
                        ? requestContext.Request.QueryStringParameters["resourceType"]
                        : null;
                
                if (string.IsNullOrWhiteSpace(resourceTypeName))
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Resource type not specified.");
                    return;
                }

                var resourceType = McmaTypes.FindType(resourceTypeName);
                if (resourceType == null)
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, $"Unknown resource type '{resourceTypeName}' in path.");
                    return;
                }

                var resourceId =
                    requestContext.Request.QueryStringParameters.ContainsKey("resourceId")
                        ? Uri.UnescapeDataString(requestContext.Request.QueryStringParameters["resourceId"])
                        : null;

                if (string.IsNullOrWhiteSpace(resourceId))
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Resource ID must be provided as a query parameter.");
                    return;
                }

                var resourceManager = resourceManagerProvider.Get(requestContext);

                requestContext.SetResponseBody(await resourceManager.GetAsync(resourceType, resourceId));
            };
        
        private static Func<McmaApiRequestContext, Task> UpdateResourceHandler(ILoggerProvider loggerProvider, IResourceManagerProvider resourceManagerProvider)
            =>
            async requestContext =>
            {
                var (resourceType, resourceToCreate) = GetResourceAndType(requestContext);
                if (resourceType == null)
                    return;

                var resourceManager = resourceManagerProvider.Get(requestContext);
                
                requestContext.SetResponseBody(await resourceManager.UpdateAsync(resourceType, resourceToCreate));
            };

        private static Func<McmaApiRequestContext, Task> ResourceNotificationHandler(ILoggerProvider loggerProvider)
            =>
            async requestContext =>
            {
                var logger = loggerProvider.Get(requestContext.GetTracker());
                var notification = requestContext.GetRequestBody<Notification>();
                var job = notification.Content.ToMcmaObject<Job>();

                // ignore notifications prior to the job finishing
                if (job.Status != JobStatus.Completed && job.Status != JobStatus.Failed)
                    return;

                if (!requestContext.Request.QueryStringParameters.ContainsKey(WorkflowCallbackUrlParamName))
                {
                    logger.Warn($"Received request without a {WorkflowCallbackUrlParamName} query parameter.");
                    return;
                }
                
                var workflowCallbackUrl =
                    Uri.UnescapeDataString(requestContext.Request.QueryStringParameters[WorkflowCallbackUrlParamName]);

                var resp = await HttpClient.PostAsync(workflowCallbackUrl, new StringContent(job.ToMcmaJson().ToString(), Encoding.UTF8, "application/json"));
                resp.EnsureSuccessStatusCode();
            };
    }
}
