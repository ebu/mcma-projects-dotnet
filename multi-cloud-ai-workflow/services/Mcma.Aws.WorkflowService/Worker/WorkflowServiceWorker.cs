using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Newtonsoft.Json.Linq;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;

namespace Mcma.Aws.WorkflowService.Worker
{
    internal class WorkflowServiceWorker : Mcma.Worker.Worker<WorkflowServiceWorkerRequest>
    {
        private const string JOB_PROFILE_CONFORM_WORKFLOW = "ConformWorkflow";
        private const string JOB_PROFILE_AI_WORKFLOW = "AiWorkflow";

        protected override IDictionary<string, Func<WorkflowServiceWorkerRequest, Task>> Operations { get; } =
            new Dictionary<string, Func<WorkflowServiceWorkerRequest, Task>>
            {
                ["ProcessJobAssignment"] = ProcessJobAssignmentAsync,
                ["ProcessNotification"] = ProcessNotificationAsync
            };

        internal static async Task ProcessJobAssignmentAsync(WorkflowServiceWorkerRequest @event)
        {
            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);
            var jobAssignmentId = @event.JobAssignmentId;

            try
            {
                // 1. Setting job assignment status to RUNNING
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "RUNNING", null);

                // 2. Retrieving WorkflowJob
                var workflowJob = await RetrieveWorkflowJobAsync(resourceManager, table, jobAssignmentId);

                // 3. Retrieve JobProfile
                var jobProfile = await RetrieveJobProfileAsync(resourceManager, workflowJob);

                // 4. Retrieve job inputParameters
                var jobInput = workflowJob.JobInput;

                // 5. Check if we support jobProfile and if we have required parameters in jobInput
                ValidateJobProfile(jobProfile, jobInput);

                // 6. Launch the appropriate workflow
                var workflowInput = new
                {
                    Input = jobInput,
                    NotificationEndpoint = new NotificationEndpoint {HttpEndpoint = jobAssignmentId + "/notifications"}
                };

                var startExecutionRequest = new StartExecutionRequest
                {
                    Input = workflowInput.ToMcmaJson().ToString()
                };

                switch (jobProfile.Name) {
                    case JOB_PROFILE_CONFORM_WORKFLOW:
                        startExecutionRequest.StateMachineArn = @event.Request.StageVariables["ConformWorkflowId"];
                        break;
                    case JOB_PROFILE_AI_WORKFLOW:
                        startExecutionRequest.StateMachineArn = @event.Request.StageVariables["AiWorkflowId"];
                        break;
                }

                var stepFunctionClient = new AmazonStepFunctionsClient();
                var startExecutionResponse = await stepFunctionClient.StartExecutionAsync(startExecutionRequest);

                // 7. saving the executionArn on the jobAssignment
                var jobAssignment = await GetJobAssignmentAsync(table, jobAssignmentId);
                //TODO: Additional properties on JobAssignment? How to handle this?
                //jobAssignment.WorkflowExecutionId = startExecutionResponse.ExecutionArn;
                await PutJobAssignmentAsync(resourceManager, table, jobAssignmentId, jobAssignment);
            }
            catch (Exception error)
            {
                Logger.Exception(error);
                try
                {
                    await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "FAILED", error.ToString());
                }
                catch (Exception innerError)
                {
                    Logger.Exception(innerError);
                }
            }
        }

        internal static async Task ProcessNotificationAsync(WorkflowServiceWorkerRequest @event)
        {
            var jobAssignmentId = @event.JobAssignmentId;
            var notification = @event.Notification;
            var notificationJobPayload = notification.Content.ToMcmaObject<JobBase>();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var jobAssignment = await table.GetAsync<JobAssignment>(jobAssignmentId);

            jobAssignment.Status = notificationJobPayload.Status;
            jobAssignment.StatusMessage = notificationJobPayload.StatusMessage;
            if (notificationJobPayload.Progress != null)
                jobAssignment.Progress = notificationJobPayload.Progress;

            jobAssignment.JobOutput = notificationJobPayload.JobOutput;
            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync<JobAssignment>(jobAssignmentId, jobAssignment);

            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }

        private static void ValidateJobProfile(JobProfile jobProfile, JobParameterBag jobInput)
        {
            if (jobProfile.Name != JOB_PROFILE_CONFORM_WORKFLOW && jobProfile.Name != JOB_PROFILE_AI_WORKFLOW)
                throw new Exception("JobProfile '" + jobProfile.Name + "' is not supported");

            if (jobProfile.InputParameters != null)
            {
                foreach (var parameter in jobProfile.InputParameters) {
                    if (!jobInput.HasProperty(parameter.ParameterName))
                        throw new Exception("jobInput misses required input parameter '" + parameter.ParameterName + "'");
                }
            }
        }

        private static async Task<JobProfile> RetrieveJobProfileAsync(ResourceManager resourceManager, Job job)
        {
            return await RetrieveResourceAsync<JobProfile>(resourceManager, job.JobProfile, "job.jobProfile");
        }

        private static async Task<Job> RetrieveWorkflowJobAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId)
        {
            var jobAssignment = await GetJobAssignmentAsync(table, jobAssignmentId);

            return await RetrieveResourceAsync<Job>(resourceManager, jobAssignment.Job, "jobAssignment.job");
        }

        private static async Task<T> RetrieveResourceAsync<T>(ResourceManager resourceManager, string resourceId, string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new Exception($"{resourceName} does not exist");

            return await resourceManager.ResolveAsync<T>(resourceId);
        }

        private static async Task UpdateJobAssignmentWithOutputAsync(DynamoDbTable table, string jobAssignmentId, JobParameterBag jobOutput)
        {
            var jobAssignment = await GetJobAssignmentAsync(table, jobAssignmentId);
            jobAssignment.JobOutput = jobOutput;
            await PutJobAssignmentAsync(null, table, jobAssignmentId, jobAssignment);
        }

        private static async Task UpdateJobAssignmentStatusAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId, string status, string statusMessage)
        {
            var jobAssignment = await GetJobAssignmentAsync(table, jobAssignmentId);
            jobAssignment.Status = status;
            jobAssignment.StatusMessage = statusMessage;
            await PutJobAssignmentAsync(resourceManager, table, jobAssignmentId, jobAssignment);
        }

        private static async Task<JobAssignment> GetJobAssignmentAsync(DynamoDbTable table, string jobAssignmentId)
        {
            var jobAssignment = await table.GetAsync<JobAssignment>(jobAssignmentId);
            if (jobAssignment == null)
                throw new Exception("JobAssignment with id '" + jobAssignmentId + "' not found");
            return jobAssignment;
        }

        private static async Task PutJobAssignmentAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId, JobAssignment jobAssignment)
        {
            jobAssignment.DateModified = DateTime.UtcNow;
            await table.PutAsync<JobAssignment>(jobAssignmentId, jobAssignment);

            if (resourceManager != null)
                await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
