using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mcma.Api;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

namespace Mcma.Azure.WorkflowService.ApiHandler
{
    public static class Resources
    {
        private static HttpClient HttpClient { get; } = new HttpClient();

        private const string WorkflowCallbackUrlParamName = "workflowCallbackUrl";

        private static async Task<McmaResource> CreateResourceAsync<T>(ResourceManager resourceManager, object resource) where T : McmaResource
            => await resourceManager.CreateAsync((T)resource);

        private static MethodInfo CreateMethodGeneric { get; } = typeof(Resources).GetMethod(nameof(CreateResourceAsync), BindingFlags.Static | BindingFlags.NonPublic);

        private static Dictionary<Type, MethodInfo> CreateMethods { get; } = new Dictionary<Type, MethodInfo>();

        public static Func<McmaApiRequestContext, Task> ResourceHandler(IResourceManagerProvider resourceManagerProvider)
            =>
            async requestContext =>
            {
                var resourceJson = requestContext.GetRequestBodyJson();

                var resourceTypeString = resourceJson?["@type"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(resourceTypeString))
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, "Resource type ('@type' property) not found in request body.");
                    return;
                }

                var resourceType = McmaTypes.FindType(resourceTypeString);
                if (resourceType == null)
                {
                    requestContext.SetResponseStatusCode(HttpStatusCode.BadRequest, $"Unknown resource type '{resourceTypeString}' in request body.");
                    return;
                }

                var resourceManager = resourceManagerProvider.Get(requestContext.Variables);

                var resourceToCreate = resourceJson.ToMcmaObject(resourceType);

                if (resourceToCreate is Job job && job.NotificationEndpoint?.HttpEndpoint != null)
                    job.NotificationEndpoint.HttpEndpoint =
                        $"{requestContext.Variables.PublicUrl().TrimEnd('/')}/resource-notifications" +
                        $"?code={requestContext.Request.QueryStringParameters["code"]}" +
                        $"&{WorkflowCallbackUrlParamName}={Uri.EscapeDataString(job.NotificationEndpoint.HttpEndpoint)}";
                
                var resource = await InvokeCreateAsync(resourceManager, resourceToCreate);

                requestContext.SetResponseResourceCreated(resource);
            };

        public static Func<McmaApiRequestContext, Task> ResourceNotificationHandler()
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
        
        private static async Task<McmaResource> InvokeCreateAsync(ResourceManager resourceManager, object resource)
        {
            var resourceType = resource.GetType();

            var createMethod =
                CreateMethods.ContainsKey(resourceType) ? CreateMethods[resourceType] : CreateMethodGeneric.MakeGenericMethod(resourceType);

            return await (Task<McmaResource>)createMethod.Invoke(null, new[] { resourceManager, resource });
        }
    }
}
