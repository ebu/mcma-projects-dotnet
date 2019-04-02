
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Serialization;
using System.Net;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public static class JobProfileRoutes
    {
        public static async Task GetJobProfilesAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetJobProfilesAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobProfiles = await table.GetAllAsync<JobProfile>();

            if (request.QueryStringParameters.Any())
            {
                Logger.Debug(
                    "Applying job profile filter from query string: " + string.Join(", ", request.QueryStringParameters.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                jobProfiles.Filter(request.QueryStringParameters);
            }

            response.JsonBody = jobProfiles.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task AddJobProfileAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(AddJobProfileAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var jobProfile = request.JsonBody?.ToMcmaObject<JobProfile>();
            if (jobProfile == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var jobProfileId = request.StageVariables["PublicUrl"] + "/job-profiles/" + Guid.NewGuid();
            
            jobProfile.Id = jobProfileId;
            jobProfile.DateCreated = DateTime.UtcNow;
            jobProfile.DateModified = jobProfile.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<JobProfile>(jobProfileId, jobProfile);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = jobProfile.Id;
            response.JsonBody = jobProfile.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task GetJobProfileAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetJobProfileAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobProfileId = request.StageVariables["PublicUrl"] + request.Path;

            response.JsonBody = (await table.GetAsync<JobProfile>(jobProfileId)).ToMcmaJson();

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }
        }

        public static async Task PutJobProfileAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(PutJobProfileAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var jobProfile = request.JsonBody?.ToMcmaObject<JobProfile>();
            if (jobProfile == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var table = new DynamoDbTable(request.StageVariables["TableName"]);
            
            var jobProfileId = request.StageVariables["PublicUrl"] + request.Path;
            jobProfile.Id = jobProfileId;
            jobProfile.DateModified = DateTime.UtcNow;
            if (!jobProfile.DateCreated.HasValue)
                jobProfile.DateCreated = jobProfile.DateModified;

            await table.PutAsync<JobProfile>(jobProfileId, jobProfile);

            response.JsonBody = jobProfile.ToMcmaJson();
        }

        public static async Task DeleteJobProfileAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(DeleteJobProfileAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobProfileId = request.StageVariables["PublicUrl"] + request.Path;

            var jobProfile = await table.GetAsync<JobProfile>(jobProfileId);
            if (jobProfile == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<JobProfile>(jobProfileId);
        }
    }
}
