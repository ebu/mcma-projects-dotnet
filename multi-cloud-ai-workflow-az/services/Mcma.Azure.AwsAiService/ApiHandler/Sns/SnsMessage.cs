using System;

namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public abstract class SnsMessage
    {
        public string MessageId { get; set; }
        public string TopicArn { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public int SignatureVersion { get; set; }
        public string Signature { get; set; }
        public string SigningCertURL { get; set; }
    }
}