using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.Core;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Serialization;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Mcma.Core.Logging;
using Mcma.Aws.Api;

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public static class JobProcessRoutes
    {
        public static async Task GetJobProcessesAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetJobProcessesAsync));
            Logger.Debug(request.ToMcmaJson().ToString());
            
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            response.JsonBody = (await table.GetAllAsync<JobProcess>()).ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task AddJobProcessAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(AddJobProcessAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var jobProcess = request.JsonBody?.ToMcmaObject<JobProcess>();
            if (jobProcess == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var jobProcessId = request.StageVariables["PublicUrl"] + "/job-processes/" + Guid.NewGuid();
            jobProcess.Id = jobProcessId;
            jobProcess.Status = "NEW";
            jobProcess.DateCreated = DateTime.UtcNow;
            jobProcess.DateModified = jobProcess.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<JobProcess>(jobProcessId, jobProcess);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = jobProcess.Id;
            response.JsonBody = jobProcess.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());

            // invoking worker lambda function that will create a jobProcess process for this new jobProcess
            var lambdaClient = new AmazonLambdaClient();
            var invokeRequest = new InvokeRequest
            {
                FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                InvocationType = "Event",
                LogType = "None",
                Payload = new { action = "createJobAssignment", request = request, jobProcessId = jobProcessId }.ToMcmaJson().ToString()
            };

            await lambdaClient.InvokeAsync(invokeRequest);
        }
        
        public static async Task GetJobProcessAsync(ApiGatewayRequest request, McmaApiResponse response) 
        {
            Logger.Debug(nameof(GetJobProcessAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobProcessId = request.StageVariables["PublicUrl"] + request.Path;

            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);
            response.JsonBody = jobProcess != null ? jobProcess.ToMcmaJson() : null;

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }
        }
        
        public static async Task DeleteJobProcessAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(DeleteJobProcessAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobProcessId = request.StageVariables["PublicUrl"] + request.Path;

            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);
            if (jobProcess == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<JobProcess>(jobProcessId);

            // invoking worker lambda function that will delete the JobAssignment created for this JobProcess
            if (!string.IsNullOrEmpty(jobProcess.JobAssignment)) {
                var lambdaClient = new AmazonLambdaClient();
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                    InvocationType = "Event",
                    LogType = "None",
                    Payload = new { action = "deleteJobAssignment", request = request, jobAssignmentId = jobProcess.JobAssignment }.ToMcmaJson().ToString()
                };

                await lambdaClient.InvokeAsync(invokeRequest);
            }
        }

        public static async Task ProcessNotificationAsync(ApiGatewayRequest request, McmaApiResponse response)
        {
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobProcessId = request.StageVariables["PublicUrl"] + "/job-processes/" + request.PathVariables["id"];

            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);
            if (jobProcess == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'";
                return;
            }

            var notification = request.JsonBody?.ToMcmaObject<Notification>();

            if (notification == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification in request body";
                return;
            }

            if (jobProcess.JobAssignment != notification.Source)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Unexpected notification from '" + notification.Source + "'.";
                return;
            }

            var lambdaClient = new AmazonLambdaClient();
            var invokeRequest = new InvokeRequest
            {
                FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                InvocationType = "Event",
                LogType = "None",
                Payload = new { action = "processNotification", request = request, jobProcessId = jobProcessId, notification = notification }.ToMcmaJson().ToString()
            };

            await lambdaClient.InvokeAsync(invokeRequest);
        }
    }
}
