using Mcma.Aws.S3;

namespace Mcma.Azure.AwsAiService.Worker
{
    internal class ProcessTranscribeJobResultRequest
    {
        public string JobAssignmentId { get; set; }

        public S3FileLocator OutputFile { get; set; }
    }
}
