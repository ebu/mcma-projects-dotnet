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
using Amazon.S3.Model;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.TransformService.Worker
{
    internal class CreateProxyLambda : IJobProfileHandler<TransformJob>
    {
        public const string Name = nameof(CreateProxyLambda);

        public async Task ExecuteAsync(WorkerJobHelper<TransformJob> job)
        {
            S3Locator inputFile;
            if (!job.JobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            S3Locator outputLocation;
            if (!job.JobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                throw new Exception("Invalid or missing output location.");
            
            if (string.IsNullOrWhiteSpace(inputFile.AwsS3Bucket) || string.IsNullOrWhiteSpace(inputFile.AwsS3Key))
                throw new Exception("Not able to obtain input file");

            var data = await inputFile.GetAsync();
            
            var localFileName = "/tmp/" + Guid.NewGuid();
            await data.WriteResponseStreamToFileAsync(localFileName, true, CancellationToken.None);
            
            var tempFileName = "/tmp/" + Guid.NewGuid() + ".mp4";
            var ffmpegParams = new[] {"-y", "-i", localFileName, "-preset", "ultrafast", "-vf", "scale=-1:360", "-c:v", "libx264", "-pix_fmt", "yuv420p", tempFileName};
            var ffmpegProcess = await FFmpegProcess.RunAsync(ffmpegParams);

            File.Delete(localFileName);

            var s3Params = new PutObjectRequest
            {
                BucketName = outputLocation.AwsS3Bucket,
                Key = (outputLocation.AwsS3KeyPrefix ?? string.Empty) + Guid.NewGuid().ToString() + ".mp4",
                FilePath = tempFileName,
                ContentType = "video/mp4"
            };

            var outputS3 = await outputLocation.GetClientAsync();
            var putResp = await outputS3.PutObjectAsync(s3Params);

            job.JobOutput["outputFile"] = new S3Locator
            {
                AwsS3Bucket = s3Params.BucketName,
                AwsS3Key = s3Params.Key
            };

            await job.CompleteAsync();
        }
    }
}
