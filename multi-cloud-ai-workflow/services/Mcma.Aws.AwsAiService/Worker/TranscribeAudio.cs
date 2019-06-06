using System;
using System.Threading.Tasks;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Mcma.Core;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.AwsAiService.Worker
{
    internal class TranscribeAudio : IJobProfileHandler<AIJob>
    {
        public const string Name = "AWSTranscribeAudio";

        public async Task ExecuteAsync(WorkerJobHelper<AIJob> jobHelper)
        {
            S3Locator inputFile;
            if (!jobHelper.JobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            string mediaFileUrl;
            if (!string.IsNullOrWhiteSpace(inputFile.HttpEndpoint))
            {
                mediaFileUrl = inputFile.HttpEndpoint;
            }
            else
            {
                var bucketLocation = await inputFile.GetBucketLocationAsync();
                var s3SubDomain = !string.IsNullOrWhiteSpace(bucketLocation) ? $"s3-{bucketLocation}" : "s3";
                mediaFileUrl = $"https://{s3SubDomain}.amazonaws.com/{inputFile.AwsS3Bucket}/{inputFile.AwsS3Key}";
            }

            string mediaFormat;
            if (mediaFileUrl.EndsWith("mp3", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "mp3";
            else if (mediaFileUrl.EndsWith("mp4", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "mp4";
            else if (mediaFileUrl.EndsWith("wav", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "wav";
            else if (mediaFileUrl.EndsWith("flac", StringComparison.OrdinalIgnoreCase))
                mediaFormat = "flac";
            else
                throw new Exception($"Unable to determine media format from input file '{mediaFileUrl}'");

            var transcribeParameters = new StartTranscriptionJobRequest
            {
                TranscriptionJobName = "TranscriptionJob-" + jobHelper.JobAssignmentId.Substring(jobHelper.JobAssignmentId.LastIndexOf("/") + 1),
                LanguageCode = "en-US",
                Media = new Media { MediaFileUri = mediaFileUrl },
                MediaFormat = mediaFormat,
                OutputBucketName = jobHelper.Request.GetRequiredContextVariable("ServiceOutputBucket")
            };

            using (var transcribeService = new AmazonTranscribeServiceClient())
                await transcribeService.StartTranscriptionJobAsync(transcribeParameters);
        }
    }
}
