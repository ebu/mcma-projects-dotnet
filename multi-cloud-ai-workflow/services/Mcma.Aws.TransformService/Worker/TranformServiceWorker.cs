using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Mcma.Core;
using Mcma.Core.Serialization;
using Amazon.S3.Model;
using Mcma.Core.Logging;

namespace Mcma.Aws.TransformService.Worker
{
    internal class TransformServiceWorker : Mcma.Worker.Worker<TransformServiceWorkerRequest>
    {
        public const string JOB_PROFILE_CREATE_PROXY_LAMBDA = "CreateProxyLambda";
        public const string JOB_PROFILE_CREATE_PROXY_EC2 = "CreateProxyEC2";

        protected override IDictionary<string, Func<TransformServiceWorkerRequest, Task>> Operations { get; } =
            new Dictionary<string, Func<TransformServiceWorkerRequest, Task>>
            {
                ["ProcessJobAssignment"] = ProcessJobAssignmentAsync,
                ["ProcessNotification"] = ProcessNotificationAsync
            };

        internal static async Task ProcessJobAssignmentAsync(TransformServiceWorkerRequest @event)
        {
            var resourceManager = @event.Request.GetAwsV4ResourceManager();
            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);
            var jobAssignmentId = @event.JobAssignmentId;

            try
            {
                // 1. Setting job assignment status to RUNNING
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "RUNNING", null);

                // 2. Retrieving WorkflowJob
                var transformJob = await RetrieveTransformJobAsync(resourceManager, table, jobAssignmentId);

                // 3. Retrieve JobProfile
                var jobProfile = await RetrieveJobProfileAsync(resourceManager, transformJob);

                // 4. Retrieve job inputParameters
                var jobInput = transformJob.JobInput;

                // 5. Check if we support jobProfile and if we have required parameters in jobInput
                ValidateJobProfile(jobProfile, jobInput);

                switch (jobProfile.Name)
                {
                    case JOB_PROFILE_CREATE_PROXY_LAMBDA:
                        S3Locator inputFile;
                        if (!jobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                            throw new Exception("Invalid or missing input file.");

                        S3Locator outputLocation;
                        if (!jobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                            throw new Exception("Invalid or missing output location.");
                        
                        if (string.IsNullOrWhiteSpace(inputFile.AwsS3Bucket) || string.IsNullOrWhiteSpace(inputFile.AwsS3Key))
                            throw new Exception("Not able to obtain input file");

                        var data = await inputFile.GetAsync();
                        
                        var localFileName = "/tmp/" + Guid.NewGuid();
                        await data.WriteResponseStreamToFileAsync(localFileName, true, CancellationToken.None);
                        
                        var tempFileName = "/tmp/" + Guid.NewGuid() + ".mp4";
                        var ffmpegParams = new[] {"-y", "-i", localFileName, "-preset", "ultrafast", "-vf", "scale=-1:360", "-c:v", "libx264", "-pix_fmt", "yuv420p", tempFileName};
                        var ffmpegProcess = await FFmpegProcess.RunAsync(ffmpegParams);

                        File.Delete(localFileName);

                        var s3Params = new PutObjectRequest
                        {
                            BucketName = outputLocation.AwsS3Bucket,
                            Key = (outputLocation.AwsS3KeyPrefix ?? string.Empty) + Guid.NewGuid().ToString() + ".mp4",
                            FilePath = tempFileName,
                            ContentType = "video/mp4"
                        };

                        var outputS3 = await outputLocation.GetClientAsync();
                        var putResp = await outputS3.PutObjectAsync(s3Params);

                        var jobOutput = new JobParameterBag();
                        jobOutput["outputFile"] = new S3Locator
                        {
                            AwsS3Bucket = s3Params.BucketName,
                            AwsS3Key = s3Params.Key
                        };

                        await UpdateJobAssignmentWithOutputAsync(table, jobAssignmentId, jobOutput);
                        await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "COMPLETED");

                        break;
                    case JOB_PROFILE_CREATE_PROXY_EC2:
                        var ec2hostname = @event.Request.StageVariables["HostnameInstanceEC2"];

                        var ec2Url = "http://" + ec2hostname + "/new-transform-job";

                        var message = new
                        {
                            input = jobInput,
                            notificationEndpoint = new NotificationEndpoint {HttpEndpoint = jobAssignmentId + "/notifications"}
                        };

                        Logger.Debug("Sending to", ec2Url, "message", message);
                        var mcmaHttp = new McmaHttpClient();
                        await mcmaHttp.PostAsJsonAsync(ec2Url, message);
                        Logger.Debug("Done");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);

                try
                {
                    await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "FAILED", ex.ToString());
                }
                catch (Exception innerEx)
                {
                    Logger.Exception(innerEx);
                }
            }
            finally
            {
                Logger.Debug("Exiting TransformServiceWorker.ProcessJobAssignmentAsync");
            }
        }

        internal static async Task ProcessNotificationAsync(TransformServiceWorkerRequest @event)
        {
            var jobAssignmentId = @event.JobAssignmentId;
            var notification = @event.Notification;

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var jobAssignment = await table.GetAsync<JobAssignment>(jobAssignmentId);

            var notificationJobAssignment = notification.Content.ToMcmaObject<JobAssignment>();
            jobAssignment.Status = notificationJobAssignment.Status;
            jobAssignment.StatusMessage = notificationJobAssignment.StatusMessage;
            if (notificationJobAssignment.Progress.HasValue)
                jobAssignment.Progress = notificationJobAssignment.Progress;
            jobAssignment.JobOutput = notificationJobAssignment.JobOutput;
            jobAssignment.DateModified = DateTime.UtcNow;

            await table.PutAsync<JobAssignment>(jobAssignmentId, jobAssignment);

            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }

        private static void ValidateJobProfile(JobProfile jobProfile, JobParameterBag jobInput)
        {
            if (jobProfile.Name != JOB_PROFILE_CREATE_PROXY_LAMBDA && 
                jobProfile.Name != JOB_PROFILE_CREATE_PROXY_EC2)
                throw new Exception("JobProfile '" + jobProfile.Name + "' is not supported");

            if (jobProfile.InputParameters != null)
                foreach (var parameter in jobProfile.InputParameters)
                    if (!jobInput.HasProperty(parameter.ParameterName))
                        throw new Exception("jobInput misses required input parameter '" + parameter.ParameterName + "'");
        }

        private static async Task<JobProfile> RetrieveJobProfileAsync(ResourceManager resourceManager, Job job)
        {
            return await RetrieveResourceAsync<JobProfile>(resourceManager, job.JobProfile, "job.jobProfile");
        }

        private static async Task<Job> RetrieveTransformJobAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId)
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

        private static async Task UpdateJobAssignmentStatusAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId, string status, string statusMessage = null)
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
