using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.AmeService.Worker
{
    internal class ExtractTechnicalMetadata : IJobProfileHandler<AmeJob>
    {
        public const string Name = nameof(ExtractTechnicalMetadata);

        public async Task ExecuteAsync(WorkerJobHelper<AmeJob> job)
        {
            S3Locator inputFile;
            if (!job.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as S3Locator");

            S3Locator outputLocation;
            if (!job.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as S3Locator");

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

            job.JobOutput.Set("outputFile", new S3Locator
            {
                AwsS3Bucket = s3Params.BucketName,
                AwsS3Key = s3Params.Key
            });

            await job.CompleteAsync();
        }
    }
}
