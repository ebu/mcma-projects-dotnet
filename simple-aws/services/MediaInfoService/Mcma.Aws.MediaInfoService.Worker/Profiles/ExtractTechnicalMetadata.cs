using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.MediaInfoService.Worker.Profiles
{
    internal class ExtractTechnicalMetadata : IJobProfile<AmeJob>
    {
        public string Name => nameof(ExtractTechnicalMetadata);

        public async Task ExecuteAsync(ProviderCollection providerCollection, ProcessJobAssignmentHelper<AmeJob> jobAssignmentHelper, WorkerRequestContext requestContext)
        {
            var logger = jobAssignmentHelper.Logger;
            
            AwsS3FileLocator inputFile;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as AwsS3FileLocator");

            AwsS3FolderLocator outputLocation;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as AwsS3FolderLocator");

            var localFileName = "/tmp/" + Guid.NewGuid() + ".txt";
            using (var s3Client = await inputFile.GetBucketClientAsync())
            {
                var s3GetResponse = await s3Client.GetObjectAsync(inputFile.Bucket, inputFile.Key);

                await s3GetResponse.WriteResponseStreamToFileAsync(localFileName, false, CancellationToken.None);
            }

            logger.Debug("Running MediaInfo against " + localFileName);
            var mediaInfoProcess = await MediaInfoProcess.RunAsync(logger, "--Output=EBUCore_JSON", localFileName);

            File.Delete(localFileName);

            if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var s3Params = new PutObjectRequest
            {
                BucketName = outputLocation.Bucket,
                Key = (outputLocation.KeyPrefix ?? string.Empty) + Guid.NewGuid() + ".json",
                ContentBody = mediaInfoProcess.StdOut,
                ContentType = "application/json"
            };

            logger.Debug($"Writing MediaInfo output to bucket {s3Params.BucketName} with key {s3Params.Key}...");
            using (var outputS3 = await outputLocation.GetBucketClientAsync())
            {
                logger.Debug($"Got client for bucket {s3Params.BucketName}. Submitting put request...");
                await outputS3.PutObjectAsync(s3Params);
                logger.Debug("Put request completed. Setting job output...");
            }

            jobAssignmentHelper.JobOutput.Set("outputFile", new AwsS3FileLocator
            {
                Bucket = s3Params.BucketName,
                Key = s3Params.Key
            });

            await jobAssignmentHelper.CompleteAsync();
        }
    }
}
