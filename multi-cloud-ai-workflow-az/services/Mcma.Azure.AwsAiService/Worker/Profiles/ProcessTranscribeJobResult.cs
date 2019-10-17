using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Mcma.Aws.S3;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Context;
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
                    DbTableProvider.Table<JobAssignment>(request.Variables.TableName()),
                    ResourceManagerProvider.Get(request.Variables),
                    request,
                    requestInput.JobAssignmentId);

            var transcribeOutputClient = await requestInput.OutputFile.GetBucketClientAsync(jobHelper.Variables.AwsAccessKey(), jobHelper.Variables.AwsSecretKey());

            try
            {
                try
                {
                    await jobHelper.InitializeAsync();

                    BlobStorageFolderLocator outputLocation;
                    if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                        throw new Exception("Invalid or missing output location.");

                    jobHelper.JobOutput["outputFile"] = 
                        await outputLocation.Proxy(request.Variables).PutAsync(
                            requestInput.OutputFile.Key,
                            await transcribeOutputClient.GetObjectStreamAsync(requestInput.OutputFile.Bucket, requestInput.OutputFile.Key, null));

                    await jobHelper.CompleteAsync();
                }
                catch (Exception ex)
                {
                    jobHelper.Logger.Exception(ex);
                    try
                    {
                        await jobHelper.FailAsync(ex.ToString());
                    }
                    catch (Exception innerEx)
                    {
                        jobHelper.Logger.Exception(innerEx);
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
                    jobHelper.Logger.Error("Failed to cleanup transcribe output file.");
                    jobHelper.Logger.Exception(error);
                }
            }
            finally
            {
                transcribeOutputClient.Dispose();
            }
        }
    }
}
