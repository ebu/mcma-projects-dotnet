using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Api;
using Amazon.Lambda.Core;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public static class ServiceRoutes
    {
        public static async Task GetServicesAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetServicesAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var services = await table.GetAllAsync<Service>();

            if (request.QueryStringParameters.Any())
                services.Filter(request.QueryStringParameters);

            response.JsonBody = services.ToMcmaJson();
            
            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task AddServiceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(AddServiceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var service = request.JsonBody?.ToMcmaObject<Service>();
            if (service == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var serviceId = request.StageVariables["PublicUrl"] + "/services/" + Guid.NewGuid();
            
            service.Id = serviceId;
            service.DateCreated = DateTime.UtcNow;
            service.DateModified = service.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<Service>(serviceId, service);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = service.Id;
            response.JsonBody = service.ToMcmaJson();
            
            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task GetServiceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetServiceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var serviceId = request.StageVariables["PublicUrl"] + request.Path;

            response.JsonBody = (await table.GetAsync<Service>(serviceId))?.ToMcmaJson();

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }
        }

        public static async Task PutServiceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(PutServiceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var service = request.JsonBody?.ToMcmaObject<Service>();
            if (service == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var table = new DynamoDbTable(request.StageVariables["TableName"]);
            
            var serviceId = request.StageVariables["PublicUrl"] + request.Path;
            service.Id = serviceId;
            service.DateModified = DateTime.UtcNow;
            if (!service.DateCreated.HasValue)
                service.DateCreated = service.DateModified;

            await table.PutAsync<Service>(serviceId, service);

            response.JsonBody = service.ToMcmaJson();
        }

        public static async Task DeleteServiceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(DeleteServiceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var serviceId = request.StageVariables["PublicUrl"] + request.Path;

            var service = await table.GetAsync<Service>(serviceId);
            if (service == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<Service>(serviceId);
        }
    }
}
