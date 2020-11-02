using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.FFmpegService.Worker
{
    internal class ExtractThumbnail : IJobProfile<TransformJob>
    {
        public string Name => nameof(ExtractThumbnail);

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<TransformJob> jobAssignmentHelper, McmaWorkerRequestContext requestContext)
        {
            var logger = jobAssignmentHelper.RequestContext.Logger;
            
            AwsS3FileLocator inputFile;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            AwsS3FolderLocator outputLocation;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Invalid or missing output location.");

            var tempId = Guid.NewGuid().ToString();
            var tempVideoFile = "/tmp/video_" + tempId + ".mp4";
            var tempThumbFile = "/tmp/thumb_" + tempId + ".png";

            try
            {
                logger.Info("Get video from s3 location: " + inputFile.Bucket + " " + inputFile.Key);

                var data = await inputFile.GetAsync();
            
                await data.WriteResponseStreamToFileAsync(tempVideoFile, true, CancellationToken.None);
            
                await FFmpegProcess.RunAsync(
                    logger,
                    "-i",
                    tempVideoFile,
                    "-ss",
                    "00:00:00.500",
                    "-vframes",
                    "1",
                    "-vf",
                    "scale=200:-1",
                    tempThumbFile);

                var s3Params = new PutObjectRequest
                {
                    BucketName = outputLocation.Bucket,
                    Key = (outputLocation.KeyPrefix ?? string.Empty) + Guid.NewGuid() + ".png",
                    FilePath = tempThumbFile,
                    ContentType = "image/png"
                };

                using (var outputS3 = await outputLocation.GetBucketClientAsync())
                    await outputS3.PutObjectAsync(s3Params);

                jobAssignmentHelper.JobOutput.Set(
                    "outputFile",
                    new AwsS3FileLocator
                    {
                        Bucket = s3Params.BucketName,
                        Key = s3Params.Key
                    });

                await jobAssignmentHelper.CompleteAsync();
            }
            finally
            {
                try
                {
                    if (File.Exists(tempVideoFile))
                        File.Delete(tempVideoFile);
                }
                catch
                {
                    // just ignore this
                }

                try
                {
                    if (File.Exists(tempThumbFile))
                        File.Delete(tempThumbFile);
                }
                catch
                {
                    // just ignore this
                }
            }
        }
    }
}
