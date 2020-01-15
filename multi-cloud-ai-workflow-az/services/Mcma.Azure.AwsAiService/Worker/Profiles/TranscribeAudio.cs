using System;
using System.Threading.Tasks;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Mcma.Core;
using Mcma.Worker;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Core.Logging;
using Mcma.Aws.S3;

namespace Mcma.Azure.AwsAiService.Worker
{
    internal class TranscribeAudio : IJobProfile<AIJob>
    {
        public string Name => "AWS" + nameof(TranscribeAudio);

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<AIJob> jobHelper)
        {
            BlobStorageFileLocator inputFile;
            if (!jobHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            var publicUri = new Uri(inputFile.Proxy(jobHelper.Request).GetPublicReadOnlyUrl());

            string mediaFormat;
            if (publicUri.AbsolutePath.EndsWith("mp3", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "mp3";
            else if (publicUri.AbsolutePath.EndsWith("mp4", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "mp4";
            else if (publicUri.AbsolutePath.EndsWith("wav", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "wav";
            else if (publicUri.AbsolutePath.EndsWith("flac", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "flac";
            else
                throw new Exception($"Unable to determine media format from input file '{inputFile.Url}'");

            // create destination locator
            var transcribeInputFile = new AwsS3FileLocator
            {
                AwsS3Bucket = jobHelper.Request.AwsAiInputBucket(),
                AwsS3Key = inputFile.FilePath
            };
            
            // copy input file from Blob Storage to S3
            using (var blobDownloadStream = await inputFile.Proxy(jobHelper.Request).GetAsync())
            using (var rekoBucketClient = await transcribeInputFile.GetBucketClientAsync(jobHelper.Request.AwsAccessKey(), jobHelper.Request.AwsSecretKey()))
            {
                await rekoBucketClient.UploadObjectFromStreamAsync(transcribeInputFile.AwsS3Bucket, transcribeInputFile.AwsS3Key, blobDownloadStream, null);
            }

            using (var transcribeService = new AmazonTranscribeServiceClient(jobHelper.Request.AwsCredentials(), jobHelper.Request.AwsRegion()))
                await transcribeService.StartTranscriptionJobAsync(
                    new StartTranscriptionJobRequest
                    {
                        TranscriptionJobName = "TranscriptionJob-" + jobHelper.JobAssignmentId.Substring(jobHelper.JobAssignmentId.LastIndexOf("/") + 1),
                        LanguageCode = "en-US",
                        Media = new Media { MediaFileUri = transcribeInputFile.Url },
                        MediaFormat = mediaFormat,
                        OutputBucketName = jobHelper.Request.AwsAiOutputBucket()
                    });
        }
    }
}
