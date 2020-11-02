using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.JobProcessor.Common;
using Mcma.Client;
using Mcma.Logging;
using Mcma.Worker;

namespace Mcma.Azure.JobProcessor.Worker
{
    internal class JobExecutor
    {
        public JobExecutor(IDataController dataController,
                           IResourceManager resourceManager,
                           McmaWorkerRequestContext requestContext)
        {
            DataController = dataController;
            ResourceManager = resourceManager;

            Logger = requestContext.Logger;
            JobEventLogger = new JobEventLogger(requestContext.Logger, resourceManager);
        }

        private IDataController DataController { get; }

        private IResourceManager ResourceManager { get; }

        private ILogger Logger { get; }
        
        private JobEventLogger JobEventLogger { get; }

        private async Task<Job> FailJobAsync(Job job, JobExecution jobExecution, Exception error)
        {
            jobExecution.Status = JobStatus.Failed;
            jobExecution.Error = new ProblemDetail
            {
                ProblemType = "uri://mcma.ebu.ch/rfc7807/job-processor/job-start-failure",
                Title = "Failed to start job",
                Detail = error?.Message
            };
            jobExecution.Error.Set(nameof(Exception.StackTrace), error?.StackTrace);
            await DataController.UpdateExecutionAsync(jobExecution);

            Logger.Error("Failed to start job due to error '" + error?.Message + "'");
            Logger.Error(error?.ToString());
            job.Status = jobExecution.Status;
            job.Error = jobExecution.Error;

            return await DataController.UpdateJobAsync(job);
        }
        
        public async Task<Job> StartExecutionAsync(JobReference jobReference, Job job, IJobCheckerTrigger checkerTrigger)
        {
            Logger.Info("Creating job execution");
            
            var jobExecution = new JobExecution
            {
                Status = JobStatus.Queued
            };

            jobExecution = await DataController.AddExecutionAsync(job.Id, jobExecution);

            job.Status = jobExecution.Status;
            job.Error = null;
            job.JobOutput = new JobParameterBag();
            job.Progress = null;
            job = await DataController.UpdateJobAsync(job);

            try
            {
                Logger.Info("Creating Job Assignment");
                
                // retrieving the jobProfile
                var jobProfile = await ResourceManager.GetAsync<JobProfile>(job.JobProfileId);

                // validating job.JobInput with required input parameters of jobProfile
                var jobInput = job.JobInput; //await resourceManager.ResolveAsync<JobParameterBag>(job.JobInput);
                if (jobInput == null)
                    return await FailJobAsync(job, jobExecution, new McmaException("Job is missing jobInput"));

                if (jobProfile.InputParameters != null)
                {
                    foreach (var parameter in jobProfile.InputParameters)
                        if (!jobInput.HasProperty(parameter.ParameterName))
                            throw new Exception("jobInput is missing required input parameter '" + parameter.ParameterName + "'");
                }

                // finding a service that is capable of handling the job type and job profile
                var services = await ResourceManager.QueryAsync<Service>(new (string, string)[0]);

                Service selectedService = null;
                IResourceEndpointClient jobAssignmentResourceEndpoint = null;

                foreach (var service in services)
                {
                    var serviceClient = ResourceManager.GetServiceClient(service);

                    jobAssignmentResourceEndpoint = null;

                    if (service.JobType == job.Type)
                    {
                        jobAssignmentResourceEndpoint = serviceClient.GetResourceEndpointClient<JobAssignment>();

                        if (jobAssignmentResourceEndpoint == null)
                            continue;

                        if (service.JobProfileIds != null &&
                            service.JobProfileIds.Any(serviceJobProfile => serviceJobProfile == job.JobProfileId))
                            selectedService = service;
                    }

                    if (selectedService != null)
                        break;
                }

                if (selectedService == null)
                    throw new Exception("Failed to find service that could execute the " + job.GetType().Name);

                var jobAssignment = new JobAssignment
                {
                    JobId = job.Id,
                    NotificationEndpoint = new NotificationEndpoint
                    {
                        HttpEndpoint = $"{jobExecution.Id}/notifications"
                    },
                    Tracker = job.Tracker
                };

                try
                {
                    jobAssignment = await jobAssignmentResourceEndpoint.PostAsync<JobAssignment>(jobAssignment);
                }
                catch (Exception error)
                {
                    Logger.Error(error);
                    return await FailJobAsync(job, jobExecution, new McmaException($"Failed to post JobAssignment to Service '{selectedService.Name}' at {jobAssignmentResourceEndpoint.HttpEndpoint}"));
                }

                Logger.Info(jobAssignment);

                jobExecution.Status = JobStatus.Scheduled;
                jobExecution.JobAssignmentId = jobAssignment.Id;
                jobExecution = await DataController.UpdateExecutionAsync(jobExecution);

                Logger.Info(jobExecution);

                job.Status = jobExecution.Status;
                job = await DataController.UpdateJobAsync(job);

                await JobEventLogger.LogJobEventAsync(job, jobExecution);

                await checkerTrigger.EnableAsync();

                return job;
            }
            catch (Exception error)
            {
                return await FailJobAsync(job, jobExecution, error);
            }
        }

        public async Task<Job> CancelExecutionAsync(JobReference jobReference, Job job)
        {
            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Canceled || job.Status == JobStatus.Failed)
                return job;

            var jobExecution = (await DataController.GetExecutionsAsync(job.Id)).Results.FirstOrDefault();
            if (jobExecution != null)
            {
                if (jobExecution.JobAssignmentId != null)
                {
                    try
                    {
                        var client = await ResourceManager.GetResourceEndpointClientAsync(jobExecution.JobAssignmentId);
                        await client.PostAsync(null, $"{jobExecution.JobAssignmentId}/cancel");
                    }
                    catch (Exception error)
                    {
                        Logger.Warn($"Canceling job assignment '{jobExecution.JobAssignmentId} failed");
                        Logger.Warn(error);
                    }
                }

                jobExecution.Status = JobStatus.Canceled;
                await DataController.UpdateExecutionAsync(jobExecution);
            }

            job.Status = JobStatus.Canceled;
            await DataController.UpdateJobAsync(job);

            await JobEventLogger.LogJobEventAsync(job, jobExecution);

            return job;
        }
    }
}