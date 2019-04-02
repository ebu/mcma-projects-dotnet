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
using Amazon.S3.Model;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Amazon.Translate;
using Amazon.Translate.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Mcma.Core;
using Mcma.Core.Utility;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;

namespace Mcma.Aws.AwsAiService.Worker
{
    internal class AwsAiServiceWorker : Mcma.Worker.Worker<AwsAiServiceWorkerRequest>
    {
        public const string JOB_PROFILE_TRANSCRIBE_AUDIO = "AWSTranscribeAudio";
        public const string JOB_PROFILE_TRANSLATE_TEXT = "AWSTranslateText";
        public const string JOB_PROFILE_DETECT_CELEBRITIES = "AWSDetectCelebrities";

        private static string REKO_SNS_ROLE_ARN = Environment.GetEnvironmentVariable("REKO_SNS_ROLE_ARN");
        private static string SNS_TOPIC_ARN = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");

        protected override IDictionary<string, Func<AwsAiServiceWorkerRequest, Task>> Operations { get; } =
            new Dictionary<string, Func<AwsAiServiceWorkerRequest, Task>>
            {
                ["ProcessJobAssignment"] = ProcessJobAssignmentAsync,
                ["ProcessTranscribeJobResult"] = ProcessTranscribeJobResultAsync,
                ["ProcessRekognitionResult"] = ProcessRekognitionResultAsync
            };

        internal static async Task ProcessJobAssignmentAsync(AwsAiServiceWorkerRequest @event)
        {
            var resourceManager = @event.GetAwsV4ResourceManager();
            var table = new DynamoDbTable(@event.StageVariables["TableName"]);
            var jobAssignmentId = @event.JobAssignmentId;

            try
            {
                // 1. Setting job assignment status to RUNNING
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "RUNNING", null);

                // 2. Retrieving WorkflowJob
                var job = await RetrieveJobAsync(resourceManager, table, jobAssignmentId);

                // 3. Retrieve JobProfile
                var jobProfile = await RetrieveJobProfileAsync(resourceManager, job);

                // 4. Retrieve job inputParameters
                var jobInput = job.JobInput;

                // 5. Check if we support jobProfile and if we have required parameters in jobInput
                ValidateJobProfile(jobProfile, jobInput);

                S3Locator inputFile;
                if (!jobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                    throw new Exception("Invalid or missing input file.");

                S3Locator outputLocation;
                if (!jobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                switch (jobProfile.Name)
                {
                    case JOB_PROFILE_TRANSCRIBE_AUDIO:
                        string mediaFileUrl;
                        if (!string.IsNullOrWhiteSpace(inputFile.HttpEndpoint))
                        {
                            mediaFileUrl = inputFile.HttpEndpoint;
                        }
                        else
                        {
                            var bucketLocation = await inputFile.GetBucketLocationAsync();
                            var s3SubDomain = !string.IsNullOrWhiteSpace(bucketLocation) ? $"s3-{bucketLocation}" : "s3";
                            mediaFileUrl = $"https://{s3SubDomain}.amazonaws.com/{inputFile.AwsS3Bucket}/{inputFile.AwsS3Key}";
                        }

                        string mediaFormat;
                        if (mediaFileUrl.EndsWith("mp3", StringComparison.OrdinalIgnoreCase))
                            mediaFormat = "mp3";
                        else if (mediaFileUrl.EndsWith("mp4", StringComparison.OrdinalIgnoreCase))
                            mediaFormat = "mp4";
                        else if (mediaFileUrl.EndsWith("wav", StringComparison.OrdinalIgnoreCase))
                            mediaFormat = "wav";
                        else if (mediaFileUrl.EndsWith("flac", StringComparison.OrdinalIgnoreCase))
                            mediaFormat = "flac";
                        else
                            throw new Exception($"Unable to determine media format from input file '{mediaFileUrl}'");

                        var transcribeParameters = new StartTranscriptionJobRequest
                        {
                            TranscriptionJobName = "TranscriptionJob-" + jobAssignmentId.Substring(jobAssignmentId.LastIndexOf("/") + 1),
                            LanguageCode = "en-US",
                            Media = new Media { MediaFileUri = mediaFileUrl },
                            MediaFormat = mediaFormat,
                            OutputBucketName = @event.StageVariables["ServiceOutputBucket"]
                        };

                        var transcribeService = new AmazonTranscribeServiceClient();
                        
                        var startJobResponse = await transcribeService.StartTranscriptionJobAsync(transcribeParameters);
                        Logger.Debug(startJobResponse.ToMcmaJson().ToString());
                        break;
                    case JOB_PROFILE_TRANSLATE_TEXT:
                        var s3Bucket = inputFile.AwsS3Bucket;
                        var s3Key = inputFile.AwsS3Key;

                        GetObjectResponse s3Object;
                        try
                        {
                            s3Object = await inputFile.GetAsync();
                        }
                        catch (Exception error)
                        {
                            throw new Exception($"Unable to read file in bucket '{s3Bucket}' with key '{s3Key}'.", error);
                        }

                        var inputText = await new StreamReader(s3Object.ResponseStream).ReadToEndAsync();

                        var translateParameters = new TranslateTextRequest
                        {
                            SourceLanguageCode = jobInput.TryGet("sourceLanguageCode", out string srcLanguageCode) ? srcLanguageCode : "auto",
                            TargetLanguageCode = jobInput.Get<string>("targetLanguageCode"),
                            Text = inputText
                        };

                        var translateService = new AmazonTranslateClient();
                        var translateResponse = await translateService.TranslateTextAsync(translateParameters);

                        var s3Params = new PutObjectRequest
                        {
                            BucketName = outputLocation.AwsS3Bucket,
                            Key = (!string.IsNullOrWhiteSpace(outputLocation.AwsS3KeyPrefix) ? outputLocation.AwsS3Key : string.Empty) + Guid.NewGuid() + ".txt",
                            ContentBody = translateResponse.TranslatedText
                        };

                        var outputS3 = await outputLocation.GetClientAsync();
                        await outputS3.PutObjectAsync(s3Params);

                        var jobOutput = new JobParameterBag();
                        jobOutput["outputFile"] = new S3Locator
                        {
                            AwsS3Bucket = s3Params.BucketName,
                            AwsS3Key = s3Params.Key
                        };

                        Logger.Debug("Updating job assignment");
                        await UpdateJobAssignmentWithOutputAsync(table, jobAssignmentId, jobOutput);
                        await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "COMPLETED");
                        break;
                    case JOB_PROFILE_DETECT_CELEBRITIES:
                        var randomBytes = new byte[16];
                        new Random().NextBytes(randomBytes);
                        var clientToken = randomBytes.HexEncode();

                        var base64JobId = Encoding.UTF8.GetBytes(jobAssignmentId).HexEncode();

                        var rekoParams = new StartCelebrityRecognitionRequest
                        {
                            Video = new Video
                            {
                                S3Object = new Amazon.Rekognition.Model.S3Object
                                {
                                    Bucket = inputFile.AwsS3Bucket,
                                    Name = inputFile.AwsS3Key
                                }
                            },
                            ClientRequestToken = clientToken,
                            JobTag = base64JobId,
                            NotificationChannel = new NotificationChannel
                            {
                                RoleArn = REKO_SNS_ROLE_ARN,
                                SNSTopicArn = SNS_TOPIC_ARN
                            }
                        };

                        var rekognitionClient = new AmazonRekognitionClient();
                        var startCelebRecognitionResponse = await rekognitionClient.StartCelebrityRecognitionAsync(rekoParams);

                        Logger.Debug(startCelebRecognitionResponse.ToMcmaJson().ToString());
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
        }

        public static async Task ProcessTranscribeJobResultAsync(AwsAiServiceWorkerRequest @event)
        {
            var resourceManager = @event.GetAwsV4ResourceManager();
            var table = new DynamoDbTable(@event.StageVariables["TableName"]);
            var jobAssignmentId = @event.JobAssignmentId;

            var job = await RetrieveJobAsync(resourceManager, table, jobAssignmentId);
            try
            {
                var jobInput = job.JobInput;

                S3Locator outputLocation;
                if (!jobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                var copySource = Uri.EscapeDataString(@event.OutputFile.AwsS3Bucket + "/" + @event.OutputFile.AwsS3Key);

                var s3Bucket = outputLocation.AwsS3Bucket;
                var s3Key = (!string.IsNullOrWhiteSpace(outputLocation.AwsS3KeyPrefix) ? outputLocation.AwsS3KeyPrefix : string.Empty) + @event.OutputFile.AwsS3Key;

                try
                {
                    var destS3 = await outputLocation.GetClientAsync();
                    await destS3.CopyObjectAsync(new CopyObjectRequest
                    {
                        SourceBucket = @event.OutputFile.AwsS3Bucket,
                        SourceKey = @event.OutputFile.AwsS3Key,
                        DestinationBucket = s3Bucket,
                        DestinationKey = s3Key
                    });
                }
                catch (Exception error)
                {
                    throw new Exception("Unable to copy output file to bucket '" + s3Bucket + "' with key'" + s3Key + "'", error);
                }

                var jobOutput = new JobParameterBag();
                jobOutput["outputFile"] = new S3Locator
                {
                    AwsS3Bucket = s3Bucket,
                    AwsS3Key = s3Key
                };

                await UpdateJobAssignmentWithOutputAsync(table, jobAssignmentId, jobOutput);
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "COMPLETED");
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

            // Cleanup: Deleting original output file
            try
            {
                var sourceS3 = await @event.OutputFile.GetClientAsync();
                await sourceS3.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = @event.OutputFile.AwsS3Bucket,
                    Key = @event.OutputFile.AwsS3Key,
                });
            }
            catch (Exception error)
            {
                Logger.Error("Failed to cleanup transcribe output file.");
                Logger.Exception(error);
            }
        }

        public static async Task ProcessRekognitionResultAsync(AwsAiServiceWorkerRequest @event)
        {
            var resourceManager = @event.GetAwsV4ResourceManager();
            var table = new DynamoDbTable(@event.StageVariables["TableName"]);
            var jobAssignmentId = @event.JobAssignmentId;

            var job = await RetrieveJobAsync(resourceManager, table, jobAssignmentId);
            try
            {
                var jobInput = job.JobInput;

                S3Locator outputLocation;
                if (!jobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                var s3Bucket = outputLocation.AwsS3Bucket;

                var rekoJobId = @event.JobExternalInfo.RekoJobId;
                var rekoJobType = @event.JobExternalInfo.RekoJobType;
                var status = @event.JobExternalInfo.Status;

                if (status != "SUCCEEDED")
                    throw new Exception($"AI Rekognition failed job info: rekognition status:" + status);

                object data = null;
                switch (rekoJobType)
                {
                    case "StartCelebrityRecognition":
                        var getCelebRecognitionParams = new GetCelebrityRecognitionRequest
                        {
                            JobId = rekoJobId, /* required */
                            MaxResults = 1000000,
                            SortBy = "TIMESTAMP"
                        };

                        var rekognitionClient = new AmazonRekognitionClient();
                        data = await rekognitionClient.GetCelebrityRecognitionAsync(getCelebRecognitionParams);
                        break;

                    case "StartLabelDetection":
                        throw new NotImplementedException("StartLabelDetection has not yet been implemented");
                    case "StartContentModeration":
                        throw new NotImplementedException("StartContentModeration has not yet been implemented");
                    case "StartPersonTracking":
                        throw new NotImplementedException("StartPersonTracking has not yet been implemented");
                    case "StartFaceDetection":
                        throw new NotImplementedException("StartLabelDetection has not yet been implemented");
                    case "StartFaceSearch":
                        throw new NotImplementedException("StartLabelDetection has not yet been implemented");
                    default:
                        throw new Exception($"Unknown job type '{rekoJobType}'");
                }

                if (data == null)
                    throw new Exception($"No data was returned by AWS Rekognition");

                var newS3Key = $"reko_{Guid.NewGuid()}.json";
                var s3Params = new PutObjectRequest
                {
                    BucketName = outputLocation.AwsS3Bucket,
                    Key = newS3Key,
                    ContentBody = data.ToMcmaJson().ToString(),
                    ContentType = "application/json"
                };

                try
                {
                    var destS3 = await outputLocation.GetClientAsync();
                    await destS3.PutObjectAsync(s3Params);
                }
                catch (Exception error)
                {
                    Logger.Error("Unable to write output file to bucket '" + s3Bucket + "' with key '" + newS3Key + "'");
                    Logger.Exception(error);
                }

                var jobOutput = new JobParameterBag();
                jobOutput["outputFile"] = new S3Locator
                {
                    AwsS3Bucket = s3Bucket,
                    AwsS3Key = newS3Key
                };

                await UpdateJobAssignmentWithOutputAsync(table, jobAssignmentId, jobOutput);
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "COMPLETED");
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

        private static void ValidateJobProfile(JobProfile jobProfile, JobParameterBag jobInput)
        {
            if (jobProfile.Name != JOB_PROFILE_TRANSCRIBE_AUDIO &&
                jobProfile.Name != JOB_PROFILE_TRANSLATE_TEXT &&
                jobProfile.Name != JOB_PROFILE_DETECT_CELEBRITIES)
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

        private static async Task<Job> RetrieveJobAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId)
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
