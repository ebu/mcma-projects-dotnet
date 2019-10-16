using System;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;
using Mcma.Client;
using Mcma.Azure.BlobStorage;
using Mcma.Core.Serialization;
using Mcma.Azure.BlobStorage.Proxies;

namespace Mcma.Azure.AwsAiService.Worker
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
            var jobHelper =
                new WorkerJobHelper<AIJob>(
                    DbTableProvider.Table<JobAssignment>(request.TableName()),
                    ResourceManagerProvider.Get(request),
                    request,
                    requestInput.JobAssignmentId);
            try
            {
                await jobHelper.InitializeAsync();

                var jobInput = jobHelper.JobInput;

                BlobStorageFolderLocator outputLocation;
                if (!jobInput.TryGet(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output bucket.");

                var containerName = outputLocation.Container;

                var rekoJobId = requestInput.JobInfo.RekoJobId;
                var rekoJobType = requestInput.JobInfo.RekoJobType;
                var status = requestInput.JobInfo.Status;

                if (status != "SUCCEEDED")
                    throw new Exception($"AI Rekognition failed job info: rekognition status:" + status);

                object data = null;
                switch (rekoJobType)
                {
                    case "StartCelebrityRecognition":
                        using (var rekognitionClient = new AmazonRekognitionClient(jobHelper.Request.AwsCredentials(), jobHelper.Request.AwsRegion()))
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

                jobHelper.JobOutput["outputFile"] =
                    await outputLocation.Proxy(jobHelper.Request).PutAsTextAsync($"Rekognition-{Guid.NewGuid()}.json", data.ToMcmaJson().ToString());

                await jobHelper.CompleteAsync();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                try
                {
                    await jobHelper.FailAsync(ex.ToString());
                }
                catch (Exception innerEx)
                {
                    Logger.Exception(innerEx);
                }
            }
        }
    }
}
