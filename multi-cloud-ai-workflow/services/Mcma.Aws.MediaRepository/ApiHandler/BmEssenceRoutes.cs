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
    public static class BmEssenceRoutes
    {
        public static async Task GetBmEssencesAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetBmEssencesAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var essences = await table.GetAllAsync<BMEssence>();

            response.JsonBody = essences.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task AddBmEssenceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(AddBmEssenceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var bmEssence = request.JsonBody?.ToMcmaObject<BMEssence>();
            if (bmEssence == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var bmEssenceId = request.StageVariables["PublicUrl"] + "/bm-essences/" + Guid.NewGuid();
            bmEssence.Id = bmEssenceId;
            bmEssence.Status = "NEW";
            bmEssence.DateCreated = DateTime.UtcNow;
            bmEssence.DateModified = bmEssence.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<BMEssence>(bmEssenceId, bmEssence);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = bmEssence.Id;
            response.JsonBody = bmEssence.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task GetBmEssenceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetBmEssenceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var bmEssenceId = request.StageVariables["PublicUrl"] + request.Path;

            var bmEssence = await table.GetAsync<BMEssence>(bmEssenceId);
            response.JsonBody = bmEssence != null ? bmEssence.ToMcmaJson() : null;

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }
        }

        public static async Task PutBmEssenceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(PutBmEssenceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var bmEssence = request.JsonBody?.ToMcmaObject<BMEssence>();
            if (bmEssence == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var bmEssenceId = request.StageVariables["PublicUrl"] + request.Path;
            bmEssence.Id = bmEssenceId;
            bmEssence.DateModified = DateTime.UtcNow;
            if (!bmEssence.DateCreated.HasValue)
                bmEssence.DateCreated = bmEssence.DateModified;

            await table.PutAsync<BMEssence>(bmEssenceId, bmEssence);

            response.JsonBody = bmEssence.ToMcmaJson();
        }

        public static async Task DeleteBmEssenceAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(DeleteBmEssenceAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var bmEssenceId = request.StageVariables["PublicUrl"] + request.Path;

            var bmEssence = await table.GetAsync<BMEssence>(bmEssenceId);
            if (bmEssence == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<BMEssence>(bmEssenceId);
        }
    }
}