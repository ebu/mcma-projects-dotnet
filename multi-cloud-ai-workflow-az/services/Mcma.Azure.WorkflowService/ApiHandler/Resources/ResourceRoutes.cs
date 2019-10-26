using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Api.Routes;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.WorkflowService.ApiHandler
{
    internal static class ResourceRoutes
    {
        private static HttpClient HttpClient { get; } = new HttpClient();

        private const string WorkflowCallbackUrlParamName = "workflowCallbackUrl";

        private static (Type, object) GetResourceAndType(McmaApiRequestContext requestContext)
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

            return (resourceType, resourceJson.ToMcmaObject(resourceType));
        }

        public static McmaApiRouteCollection Get(IResourceManagerProvider resourceManagerProvider) =>
            new McmaApiRouteCollection()
                .AddRoute(HttpMethod.Post, "/resources", CreateResourceHandler(resourceManagerProvider))
                .AddRoute(HttpMethod.Get, "/resources/{resourceType}", ResolveResourceHandler(resourceManagerProvider))
                .AddRoute(HttpMethod.Put, "/resources", UpdateResourceHandler(resourceManagerProvider))
                .AddRoute(HttpMethod.Post, "/resources/notifications", ResourceNotificationHandler());

        private static Func<McmaApiRequestContext, Task> CreateResourceHandler(IResourceManagerProvider resourceManagerProvider)
            =>
            async requestContext =>
            {
                var (resourceType, resourceToCreate) = GetResourceAndType(requestContext);
                if (resourceType == null)
                    return;

                if (resourceToCreate is Job job && job.NotificationEndpoint?.HttpEndpoint != null)
                    job.NotificationEndpoint.HttpEndpoint =
                        $"{requestContext.Variables.PublicUrl().TrimEnd('/')}/resource-notifications" +
                        $"?code={requestContext.Request.QueryStringParameters["code"]}" +
                        $"&{WorkflowCallbackUrlParamName}={Uri.EscapeDataString(job.NotificationEndpoint.HttpEndpoint)}";

                var resourceManager = resourceManagerProvider.Get(requestContext.Variables);
                
                var resource = await resourceManager.CreateResourceAsync(resourceType, resourceToCreate);

                requestContext.SetResponseResourceCreated(resource);
            };
        
        private static Func<McmaApiRequestContext, Task> ResolveResourceHandler(IResourceManagerProvider resourceManagerProvider)
            =>
            async requestContext =>
            {
                var resourceTypeName =
                    !requestContext.Request.PathVariables.ContainsKey("resourceType")
                        ? requestContext.Request.PathVariables["resourceType"]?.ToString()
                        : null;
                
                if (string.IsNullOrWhiteSpace(resourceTypeName))
                {
                    requestContext.SetResponseResourceNotFound();
                    return;
                }

                var resourceType = McmaTypes.FindType(resourceTypeName);
                if (resourceType == null)
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, $"Unknown resource type '{resourceTypeName}' in path.");
                    return;
                }

                var resourceId =
                    requestContext.Request.QueryStringParameters.ContainsKey("resourceUrl")
                        ? Uri.UnescapeDataString(requestContext.Request.QueryStringParameters["resourceUrl"])
                        : null;

                if (string.IsNullOrWhiteSpace(resourceId))
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Resource ID must be provided as a query parameter.");
                    return;
                }

                var resourceManager = resourceManagerProvider.Get(requestContext.Variables);

                requestContext.SetResponseBody(await resourceManager.CreateResourceAsync(resourceType, resourceId));
            };
        
        private static Func<McmaApiRequestContext, Task> UpdateResourceHandler(IResourceManagerProvider resourceManagerProvider)
            =>
            async requestContext =>
            {
                var (resourceType, resourceToCreate) = GetResourceAndType(requestContext);
                if (resourceType == null)
                    return;

                var resourceManager = resourceManagerProvider.Get(requestContext.Variables);
                
                requestContext.SetResponseBody(await resourceManager.UpdateResourceAsync(resourceType, resourceToCreate));
            };

        private static Func<McmaApiRequestContext, Task> ResourceNotificationHandler()
            =>
            async requestContext =>
            {
                var notification = requestContext.GetRequestBody<Notification>();
                var job = notification.Content.ToMcmaObject<Job>();

                // ignore notifications prior to the job finishing
                if (job.Status != JobStatus.Completed && job.Status != JobStatus.Failed)
                    return;

                if (!requestContext.Request.QueryStringParameters.ContainsKey(WorkflowCallbackUrlParamName))
                {
                    requestContext.Logger.Warn($"Received request without a {WorkflowCallbackUrlParamName} query parameter.");
                    return;
                }
                
                var workflowCallbackUrl =
                    Uri.UnescapeDataString(requestContext.Request.QueryStringParameters[WorkflowCallbackUrlParamName]);

                var resp = await HttpClient.PostAsync(workflowCallbackUrl, new StringContent(job.ToMcmaJson().ToString(), Encoding.UTF8, "application/json"));
                resp.EnsureSuccessStatusCode();
            };
    }
}
