namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public class RekognitionNotification
    {
        public string JobId { get; set; }

        public string Status { get; set; }

        public string API { get; set; }

        public string JobTag { get; set; }

        public long Timestamp { get; set; }

        public RekoVideo Video { get; set;}

        public class RekoVideo
        {
            public string S3Bucket { get; set; }

            public string S3ObjectName { get; set; }
        }
    }
}