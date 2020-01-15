using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Mcma.Aws.S3;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Utility;
using Mcma.Worker;

namespace Mcma.Azure.AwsAiService.Worker
{
    internal class DetectCelebrities : IJobProfile<AIJob>
    {
        public string Name => "AWS" + nameof(DetectCelebrities);

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<AIJob> jobHelper)
        {
            BlobStorageFileLocator inputFile;
            if (!jobHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            var randomBytes = new byte[16];
            new Random().NextBytes(randomBytes);
            var clientToken = randomBytes.HexEncode();

            var base64JobId = Encoding.UTF8.GetBytes(jobHelper.JobAssignmentId).HexEncode();

            // create destination locator
            var rekoInputFile = new AwsS3FileLocator
            {
                AwsS3Bucket = jobHelper.Request.AwsAiInputBucket(),
                AwsS3Key = inputFile.FilePath
            };
            
            // copy input file from Blob Storage to S3
            using (var blobDownloadStream = await inputFile.Proxy(jobHelper.Request).GetAsync())
            using (var rekoBucketClient = await rekoInputFile.GetBucketClientAsync(jobHelper.Request.AwsAccessKey(), jobHelper.Request.AwsSecretKey()))
            {
                await rekoBucketClient.UploadObjectFromStreamAsync(rekoInputFile.AwsS3Bucket, rekoInputFile.AwsS3Key, blobDownloadStream, null);
            }

            StartCelebrityRecognitionResponse response;
            using (var rekognitionClient = new AmazonRekognitionClient(jobHelper.Request.AwsCredentials(), jobHelper.Request.AwsRegion()))
                response = await rekognitionClient.StartCelebrityRecognitionAsync(
                    new StartCelebrityRecognitionRequest
                    {
                        Video = new Video
                        {
                            S3Object = new S3Object
                            {
                                Bucket = rekoInputFile.AwsS3Bucket,
                                Name = rekoInputFile.AwsS3Key
                            }
                        },
                        ClientRequestToken = clientToken,
                        JobTag = base64JobId,
                        NotificationChannel = new NotificationChannel
                        {
                            RoleArn = jobHelper.Request.AwsRekoSnsRoleArn(),
                            SNSTopicArn = jobHelper.Request.AwsAiOutputSnsTopicArn()
                        }
                    });
            
            jobHelper.Logger.Debug($"Started Rekognition job {response.JobId}.");
        }
    }
}
