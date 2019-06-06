using Mcma.Aws.S3;

namespace Mcma.Aws.AwsAiService.Worker
{
    internal class ProcessTranscribeJobResult
    {
        public const string OperationName = nameof(ProcessTranscribeJobResult);

        public string JobAssignmentId { get; set; }

        public S3Locator OutputFile { get; set; }
    }
}
