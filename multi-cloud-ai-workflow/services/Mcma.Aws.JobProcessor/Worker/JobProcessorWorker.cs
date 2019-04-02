using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using System.Collections.Generic;

namespace Mcma.Aws.JobProcessor.Worker
{
    internal class JobProcessorWorker : Mcma.Worker.Worker<JobProcessorWorkerRequest>
    {
        protected override IDictionary<string, Func<JobProcessorWorkerRequest, Task>> Operations { get; } =
            new Dictionary<string, Func<JobProcessorWorkerRequest, Task>>
            {
                ["CreateJobAssignment"] = CreateJobAssignmentAsync,
                ["DeleteJobAssignment"] = DeleteJobAssignmentAsync,
                ["ProcessNotification"] = ProcessNotificationAsync
            };

        internal static async Task CreateJobAssignmentAsync(JobProcessorWorkerRequest @event)
        {
            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var jobProcessId = @event.JobProcessId;
            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);

            try
            {
                // retrieving the job
                var job = await resourceManager.ResolveAsync<Job>(jobProcess.Job);

                // retrieving the jobProfile
                var jobProfile = await resourceManager.ResolveAsync<JobProfile>(job.JobProfile);
                
                // validating job.JobInput with required input parameters of jobProfile
                var jobInput = job.JobInput; //await resourceManager.ResolveAsync<JobParameterBag>(job.JobInput);
                if (jobInput == null)
                    throw new Exception("Job is missing jobInput");

                if (jobProfile.InputParameters != null)
                {
                    foreach (var parameter in jobProfile.InputParameters)
                        if (!jobInput.HasProperty(parameter.ParameterName))
                            throw new Exception("jobInput is missing required input parameter '" + parameter.ParameterName + "'");
                }

                // finding a service that is capable of handling the job type and job profile
                var services = await resourceManager.GetAsync<Service>();

                Service selectedService = null;
                ResourceEndpointClient jobAssignmentResourceEndpoint = null;
                
                foreach (var service in services)
                {
                    var serviceClient = new ServiceClient(service, AwsEnvironment.GetDefaultAwsV4AuthProvider());

                    jobAssignmentResourceEndpoint = null;

                    if (service.JobType == job.Type)
                    {
                        jobAssignmentResourceEndpoint = serviceClient.GetResourceEndpoint<JobAssignment>();
                        
                        if (jobAssignmentResourceEndpoint == null)
                            continue;

                        if (service.JobProfiles != null)
                        {
                            foreach (var serviceJobProfile in service.JobProfiles)
                            {
                                if (serviceJobProfile == job.JobProfile)
                                {
                                    selectedService = service;
                                    break;
                                }
                            }
                        }
                    }

                    if (selectedService != null)
                        break;
                }

                if (jobAssignmentResourceEndpoint == null)
                    throw new Exception("Failed to find service that could execute the " + job.GetType().Name);

                var jobAssignment = new JobAssignment
                {
                    Job = jobProcess.Job,
                    NotificationEndpoint = new NotificationEndpoint
                    {
                        HttpEndpoint = jobProcessId + "/notifications"
                    }
                };

                jobAssignment = await jobAssignmentResourceEndpoint.PostAsync<JobAssignment>(jobAssignment);

                jobProcess.Status = "SCHEDULED";
                jobProcess.JobAssignment = jobAssignment.Id;
            }
            catch (Exception error)
            {
                Logger.Error("Failed to create job assignment");
                Logger.Exception(error);

                jobProcess.Status = "FAILED";
                jobProcess.StatusMessage = error.ToString();
            }

            jobProcess.DateModified = DateTime.UtcNow;

            await table.PutAsync<JobProcess>(jobProcessId, jobProcess);

            await resourceManager.SendNotificationAsync(jobProcess, jobProcess.NotificationEndpoint);
        }

        internal static async Task DeleteJobAssignmentAsync(JobProcessorWorkerRequest @event)
        {
            var jobAssignmentId = @event.JobAssignmentId;

            try
            {
                var resourceManager = @event.Request.GetAwsV4ResourceManager();
                await resourceManager.DeleteAsync<JobAssignment>(jobAssignmentId);
            }
            catch (Exception error)
            {
                Logger.Exception(error);
            }
        }

        internal static async Task ProcessNotificationAsync(JobProcessorWorkerRequest @event)
        {
            var jobProcessId = @event.JobProcessId;
            var notification = @event.Notification;
            var notificationJobData = notification.Content.ToMcmaObject<JobBase>();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);

            // not updating job if it already was marked as completed or failed.
            if (jobProcess.Status == "COMPLETED" || jobProcess.Status == "FAILED")
            {
                Logger.Warn("Ignoring update of job process that tried to change state from " + jobProcess.Status + " to " + notificationJobData.Status);
                return;
            }

            jobProcess.Status = notificationJobData.Status;
            jobProcess.StatusMessage = notificationJobData.StatusMessage;
            jobProcess.Progress = notificationJobData.Progress;
            jobProcess.JobOutput = notificationJobData.JobOutput;
            jobProcess.DateModified = DateTime.UtcNow;

            await table.PutAsync<JobProcess>(jobProcessId, jobProcess);

            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(jobProcess, jobProcess.NotificationEndpoint);
        }
    }
}
