using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Aws.S3;
using Mcma.Aws.DynamoDb;
using Mcma.Worker;
using Mcma.Data;
using Mcma.Aws.Worker;
using Mcma.Core.ContextVariables;

namespace Mcma.Aws.AwsAiService.Worker
{
    internal class ProcessTranscribeJobResultHandler : WorkerOperationHandler<ProcessTranscribeJobResult>
    {
        public const string OperationName = "ProcessTranscribeJobResult";

        public ProcessTranscribeJobResultHandler(IDbTableProvider<JobAssignment> dbTableProvider, IWorkerResourceManagerProvider resourceManagerProvider)
        {
            DbTableProvider = dbTableProvider;
            ResourceManagerProvider = resourceManagerProvider;
        }

        private IDbTableProvider<JobAssignment> DbTableProvider { get; }

        private IWorkerResourceManagerProvider ResourceManagerProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessTranscribeJobResult requestInput)
        {
            var workerJobHelper =
                new WorkerJobHelper<AIJob>(
                    DbTableProvider.Table(request.TableName()),
                    ResourceManagerProvider.GetResourceManager(request),
                    request,
                    requestInput.JobAssignmentId);
            try
            {
                await workerJobHelper.InitializeAsync();

                S3Locator outputLocation;
                if (!workerJobHelper.JobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                var copySource = Uri.EscapeDataString(requestInput.OutputFile.AwsS3Bucket + "/" + requestInput.OutputFile.AwsS3Key);

                var s3Bucket = outputLocation.AwsS3Bucket;
                var s3Key =
                    (!string.IsNullOrWhiteSpace(outputLocation.AwsS3KeyPrefix) ? outputLocation.AwsS3KeyPrefix : string.Empty)
                    + requestInput.OutputFile.AwsS3Key;

                try
                {
                    var destS3 = await outputLocation.GetClientAsync();
                    await destS3.CopyObjectAsync(new CopyObjectRequest
                    {
                        SourceBucket = requestInput.OutputFile.AwsS3Bucket,
                        SourceKey = requestInput.OutputFile.AwsS3Key,
                        DestinationBucket = s3Bucket,
                        DestinationKey = s3Key
                    });
                }
                catch (Exception error)
                {
                    throw new Exception("Unable to copy output file to bucket '" + s3Bucket + "' with key'" + s3Key + "'", error);
                }

                workerJobHelper.JobOutput["outputFile"] = new S3Locator
                {
                    AwsS3Bucket = s3Bucket,
                    AwsS3Key = s3Key
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

            // Cleanup: Deleting original output file
            try
            {
                var sourceS3 = await requestInput.OutputFile.GetClientAsync();
                await sourceS3.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = requestInput.OutputFile.AwsS3Bucket,
                    Key = requestInput.OutputFile.AwsS3Key,
                });
            }
            catch (Exception error)
            {
                Logger.Error("Failed to cleanup transcribe output file.");
                Logger.Exception(error);
            }
        }
    }
}
