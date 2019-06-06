using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Mcma.Core;
using Mcma.Core.Utility;
using Mcma.Aws.S3;
using Mcma.Worker;

namespace Mcma.Aws.AwsAiService.Worker
{
    internal class DetectCelebrities : IJobProfileHandler<AIJob>
    {
        public const string Name = "AWS" + nameof(DetectCelebrities);

        public async Task ExecuteAsync(WorkerJobHelper<AIJob> jobHelper)
        {
            S3Locator inputFile;
            if (!jobHelper.JobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            var randomBytes = new byte[16];
            new Random().NextBytes(randomBytes);
            var clientToken = randomBytes.HexEncode();

            var base64JobId = Encoding.UTF8.GetBytes(jobHelper.JobAssignmentId).HexEncode();

            var rekoParams = new StartCelebrityRecognitionRequest
            {
                Video = new Video
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = inputFile.AwsS3Bucket,
                        Name = inputFile.AwsS3Key
                    }
                },
                ClientRequestToken = clientToken,
                JobTag = base64JobId,
                NotificationChannel = new NotificationChannel
                {
                    RoleArn = Environment.GetEnvironmentVariable("REKO_SNS_ROLE_ARN"),
                    SNSTopicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN")
                }
            };

            using (var rekognitionClient = new AmazonRekognitionClient())
                await rekognitionClient.StartCelebrityRecognitionAsync(rekoParams);
        }
    }
}
