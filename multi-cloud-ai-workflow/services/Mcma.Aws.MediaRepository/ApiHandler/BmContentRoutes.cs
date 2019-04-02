using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Api;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

namespace Mcma.Aws.MediaRepository.ApiHandler
{
    public static class BmContentRoutes
    {
        public static async Task GetBmContentsAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetBmContentsAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var bmContents = await table.GetAllAsync<BMContent>();

            response.JsonBody = bmContents.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task AddBmContentAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(AddBmContentAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var bmContent = request.JsonBody?.ToMcmaObject<BMContent>();
            if (bmContent == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var bmContentId = request.StageVariables["PublicUrl"] + "/bm-contents/" + Guid.NewGuid();
            bmContent.Id = bmContentId;
            bmContent.Status = "NEW";
            bmContent.DateCreated = DateTime.UtcNow;
            bmContent.DateModified = bmContent.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<BMContent>(bmContentId, bmContent);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = bmContent.Id;
            response.JsonBody = bmContent.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task GetBmContentAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetBmContentAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var bmContentId = request.StageVariables["PublicUrl"] + request.Path;

            var bmContent = await table.GetAsync<BMContent>(bmContentId);
            response.JsonBody = bmContent != null ? bmContent.ToMcmaJson() : null;

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task PutBmContentAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(PutBmContentAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var bmContent = request.JsonBody?.ToMcmaObject<BMContent>();
            if (bmContent == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var bmContentId = request.StageVariables["PublicUrl"] + request.Path;
            bmContent.Id = bmContentId;
            bmContent.DateModified = DateTime.UtcNow;
            if (!bmContent.DateCreated.HasValue)
                bmContent.DateCreated = bmContent.DateModified;

            await table.PutAsync<BMContent>(bmContentId, bmContent);

            response.JsonBody = bmContent.ToMcmaJson();
        }

        public static async Task DeleteBmContentAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(DeleteBmContentAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var bmContentId = request.StageVariables["PublicUrl"] + request.Path;

            var bmContent = await table.GetAsync<BMContent>(bmContentId);
            if (bmContent == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<BMContent>(bmContentId);
        }
    }
}