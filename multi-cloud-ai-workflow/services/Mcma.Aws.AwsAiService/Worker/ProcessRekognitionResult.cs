namespace Mcma.Aws.AwsAiService.Worker
{
    public class ProcessRekognitionResult
    {
        public string JobAssignmentId { get; set; }

        public RekognitionJobInfo JobInfo { get; set; }

        public class RekognitionJobInfo
        {
            public string RekoJobId { get; set; }

            public string RekoJobType { get; set; }

            public string Status { get; set; }
        }
    }
}
