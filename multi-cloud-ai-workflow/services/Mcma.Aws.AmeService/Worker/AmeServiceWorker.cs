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

namespace Mcma.Aws.AmeService.Worker
{
    internal class AmeServiceWorker : Mcma.Worker.Worker<AmeServiceWorkerRequest>
    {
        public const string JOB_PROFILE_EXTRACT_TECHNICAL_METADATA = "ExtractTechnicalMetadata";

        protected override IDictionary<string, Func<AmeServiceWorkerRequest, Task>> Operations { get; } =
            new Dictionary<string, Func<AmeServiceWorkerRequest, Task>>
            {
                ["ProcessJobAssignment"] = ProcessJobAssignmentAsync
            };

        internal static async Task ProcessJobAssignmentAsync(AmeServiceWorkerRequest @event)
        {
            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);
            var jobAssignmentId = @event.JobAssignmentId;

            try
            {
                // 1. Setting job assignment status to RUNNING
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "RUNNING", null);

                // 2. Retrieving WorkflowJob
                var ameJob = await RetrieveAmeJobAsync(resourceManager, table, jobAssignmentId);

                // 3. Retrieve JobProfile
                var jobProfile = await RetrieveJobProfileAsync(resourceManager, ameJob);

                // 4. Retrieve job inputParameters
                var jobInput = ameJob.JobInput;

                // 5. Check if we support jobProfile and if we have required parameters in jobInput
                ValidateJobProfile(jobProfile, jobInput);

                S3Locator inputFile;
                if (!jobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                    throw new Exception("Invalid or missing input file.");

                S3Locator outputLocation;
                if (!jobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                MediaInfoProcess mediaInfoProcess;
                if (inputFile is HttpEndpointLocator httpEndpointLocator && !string.IsNullOrWhiteSpace(httpEndpointLocator.HttpEndpoint))
                {
                    Logger.Debug("Running MediaInfo against " + httpEndpointLocator.HttpEndpoint);
                    mediaInfoProcess = await MediaInfoProcess.RunAsync("--Output=EBUCore_JSON", httpEndpointLocator.HttpEndpoint);
                } 
                else if (inputFile is S3Locator s3Locator && !string.IsNullOrWhiteSpace(s3Locator.AwsS3Bucket) && !string.IsNullOrWhiteSpace(s3Locator.AwsS3Key))
                {
                    var s3GetResponse = await (await s3Locator.GetClientAsync()).GetObjectAsync(s3Locator.AwsS3Bucket, s3Locator.AwsS3Key);

                    var localFileName = "/tmp/" + Guid.NewGuid().ToString();
                    await s3GetResponse.WriteResponseStreamToFileAsync(localFileName, false, CancellationToken.None);

                    Logger.Debug("Running MediaInfo against " + localFileName);
                    mediaInfoProcess = await MediaInfoProcess.RunAsync("--Output=EBUCore_JSON", localFileName);

                    File.Delete(localFileName);
                }
                else
                    throw new Exception("Not able to obtain input file");

                if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                    throw new Exception("Failed to obtain mediaInfo output");

                var s3Params = new PutObjectRequest
                {
                    BucketName = outputLocation.AwsS3Bucket,
                    Key = (outputLocation.AwsS3KeyPrefix ?? string.Empty) + Guid.NewGuid().ToString() + ".json",
                    ContentBody = mediaInfoProcess.StdOut,
                    ContentType = "application/json"
                };

                Logger.Debug($"Writing MediaInfo output to bucket {s3Params.BucketName} with key {s3Params.Key}...");
                var outputS3 = await outputLocation.GetClientAsync();
                Logger.Debug($"Got client for bucket {s3Params.BucketName}. Submitting put request...");
                var putResp = await outputS3.PutObjectAsync(s3Params);
                Logger.Debug($"Put request completed with status code {putResp.HttpStatusCode}. Setting job output...");

                var jobOutput = new JobParameterBag();
                jobOutput["outputFile"] = new S3Locator
                {
                    AwsS3Bucket = s3Params.BucketName,
                    AwsS3Key = s3Params.Key
                };

                Logger.Debug("Updating job assignment");
                await UpdateJobAssignmentWithOutputAsync(table, jobAssignmentId, jobOutput);
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "COMPLETED");
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
                Logger.Debug("Exiting AmeServiceWorker.ProcessJobAssignmentAsync");
            }
        }

        private static void ValidateJobProfile(JobProfile jobProfile, JobParameterBag jobInput)
        {
            if (jobProfile.Name != JOB_PROFILE_EXTRACT_TECHNICAL_METADATA)
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

        private static async Task<Job> RetrieveAmeJobAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId)
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
