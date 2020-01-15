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
    internal class ProcessTranscribeJobResult : WorkerOperation<ProcessTranscribeJobResultRequest>
    {
        public ProcessTranscribeJobResult(ProviderCollection providerCollection)
            : base(providerCollection)
        {
        }

        public override string Name => nameof(ProcessTranscribeJobResult);

        protected override async Task ExecuteAsync(WorkerRequest request, ProcessTranscribeJobResultRequest requestInput)
        {
            var logger = ProviderCollection.LoggerProvider.Get(request.Tracker);

            var jobHelper =
                new ProcessJobAssignmentHelper<AIJob>(
                    ProviderCollection.DbTableProvider.Table<JobAssignment>(request.TableName()),
                    ProviderCollection.ResourceManagerProvider.Get(request),
                    logger,
                    request,
                    requestInput.JobAssignmentId);

            var transcribeOutputClient = await requestInput.OutputFile.GetBucketClientAsync(request.AwsAccessKey(), request.AwsSecretKey());

            try
            {
                try
                {
                    await jobHelper.InitializeAsync();

                    BlobStorageFolderLocator outputLocation;
                    if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                        throw new Exception("Invalid or missing output location.");

                    jobHelper.JobOutput["outputFile"] = 
                        await outputLocation.Proxy(request).PutAsync(
                            requestInput.OutputFile.AwsS3Key,
                            await transcribeOutputClient.GetObjectStreamAsync(requestInput.OutputFile.AwsS3Bucket, requestInput.OutputFile.AwsS3Key, null));

                    await jobHelper.CompleteAsync();
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    try
                    {
                        await jobHelper.FailAsync(ex.ToString());
                    }
                    catch (Exception innerEx)
                    {
                        logger.Error(innerEx);
                    }
                }

                try
                {
                    await transcribeOutputClient.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = requestInput.OutputFile.AwsS3Bucket,
                        Key = requestInput.OutputFile.AwsS3Key,
                    });
                }
                catch (Exception error)
                {
                    logger.Error("Failed to cleanup transcribe output file.", error);
                }
            }
            finally
            {
                transcribeOutputClient.Dispose();
            }
        }
    }
}
