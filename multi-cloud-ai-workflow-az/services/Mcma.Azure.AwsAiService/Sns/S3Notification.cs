namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public class S3Notification
    {
        public S3Records[] Records { get; set; }

        public class S3Records
        {
            public S3 S3 { get; set; }
        }

        public class S3
        {
            public Bucket Bucket { get; set; }

            public Object Object { get; set; }
        }

        public class Bucket
        {
            public string Name { get; set; }
        }

        public class Object
        {
            public string Key { get; set; }

            public long Size { get; set; }

            public string ETag { get; set; }
        }
    }
}