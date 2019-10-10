using Mcma.Core;

namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public class AwsEvent : McmaObject
    {
        public AwsEventRecord[] Records { get; set; }

        public class AwsEventRecord : McmaObject
        {
            public Sns Sns { get; set; }

            public S3 S3 { get; set; }
        }

        public class Sns
        {
            public string Message { get; set; }
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