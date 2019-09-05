using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Mcma.Aws.S3;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;
using Mcma.Client;

namespace Mcma.Aws.AwsAiService.Worker
{
    internal class ProcessRekognitionResult : WorkerOperationHandler<ProcessRekognitionResultRequest>
    {
        public ProcessRekognitionResult(IDbTableProvider dbTableProvider, IResourceManagerProvider resourceManagerProvider)
        {
            DbTableProvider = dbTableProvider;
            ResourceManagerProvider = resourceManagerProvider;
        }

        private IDbTableProvider DbTableProvider { get; }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessRekognitionResultRequest requestInput)
        {
            var workerJobHelper =
                new WorkerJobHelper<AIJob>(
                    DbTableProvider.Table<JobAssignment>(request.TableName()),
                    ResourceManagerProvider.Get(request),
                    request,
                    requestInput.JobAssignmentId);
            try
            {
                await workerJobHelper.InitializeAsync();

                var jobInput = workerJobHelper.JobInput;

                S3Locator outputLocation;
                if (!jobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                var s3Bucket = outputLocation.AwsS3Bucket;

                var rekoJobId = requestInput.JobInfo.RekoJobId;
                var rekoJobType = requestInput.JobInfo.RekoJobType;
                var status = requestInput.JobInfo.Status;

                if (status != "SUCCEEDED")
                    throw new Exception($"AI Rekognition failed job info: rekognition status:" + status);

                object data = null;
                switch (rekoJobType)
                {
                    case "StartCelebrityRecognition":
                        using (var rekognitionClient = new AmazonRekognitionClient())
                            data = await rekognitionClient.GetCelebrityRecognitionAsync(new GetCelebrityRecognitionRequest
                            {
                                JobId = rekoJobId, /* required */
                                MaxResults = 1000000,
                                SortBy = "TIMESTAMP"
                            });
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

                workerJobHelper.JobOutput["outputFile"] = new S3Locator
                {
                    AwsS3Bucket = s3Bucket,
                    AwsS3Key = newS3Key
                };

                await workerJobHelper.CompleteAsync();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                try
                {
                    await workerJobHelper.FailAsync(ex.ToString());
                }
                catch (Exception innerEx)
                {
                    Logger.Exception(innerEx);
                }
            }
        }
    }
}
