using Mcma.Aws.S3;

namespace Mcma.Azure.AwsAiService.Worker
{
    internal class ProcessTranscribeJobResultRequest
    {
        public string JobAssignmentId { get; set; }

        public AwsS3FileLocator OutputFile { get; set; }
    }
}
