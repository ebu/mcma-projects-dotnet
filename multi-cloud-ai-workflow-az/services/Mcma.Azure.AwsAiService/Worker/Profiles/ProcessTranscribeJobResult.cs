using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Mcma.Aws.S3;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.ContextVariables;
using Mcma.Core.Logging;
using Mcma.Data;
using Mcma.Worker;

namespace Mcma.Azure.AwsAiService.Worker
{
    internal class ProcessTranscribeJobResult : WorkerOperationHandler<ProcessTranscribeJobResultRequest>
    {
        public ProcessTranscribeJobResult(IDbTableProvider dbTableProvider, IResourceManagerProvider resourceManagerProvider)
        {
            DbTableProvider = dbTableProvider;
            ResourceManagerProvider = resourceManagerProvider;
        }

        private IDbTableProvider DbTableProvider { get; }

        private IResourceManagerProvider ResourceManagerProvider { get; }

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessTranscribeJobResultRequest requestInput)
        {
            var jobHelper =
                new WorkerJobHelper<AIJob>(
                    DbTableProvider.Table<JobAssignment>(request.TableName()),
                    ResourceManagerProvider.Get(request),
                    request,
                    requestInput.JobAssignmentId);

            var transcribeOutputClient = await requestInput.OutputFile.GetBucketClientAsync(jobHelper.Request.AwsAccessKey(), jobHelper.Request.AwsSecretKey());

            try
            {
                try
                {
                    await jobHelper.InitializeAsync();

                    BlobStorageFolderLocator outputLocation;
                    if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                        throw new Exception("Invalid or missing output location.");

                    var outputFilePath =
                        (!string.IsNullOrWhiteSpace(outputLocation.FolderPath) ? outputLocation.FolderPath : string.Empty)
                        + requestInput.OutputFile.Key;

                    jobHelper.JobOutput["outputFile"] = 
                        await outputLocation.Proxy(request).PutAsync(
                            outputFilePath,
                            await transcribeOutputClient.GetObjectStreamAsync(requestInput.OutputFile.Bucket, requestInput.OutputFile.Key, null));

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

                try
                {
                    await transcribeOutputClient.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = requestInput.OutputFile.Bucket,
                        Key = requestInput.OutputFile.Key,
                    });
                }
                catch (Exception error)
                {
                    Logger.Error("Failed to cleanup transcribe output file.");
                    Logger.Exception(error);
                }
            }
            finally
            {
                transcribeOutputClient.Dispose();
            }
        }
    }
}
