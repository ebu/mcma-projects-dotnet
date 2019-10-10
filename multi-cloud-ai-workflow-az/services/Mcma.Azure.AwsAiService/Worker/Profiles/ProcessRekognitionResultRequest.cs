namespace Mcma.Azure.AwsAiService.Worker
{
    public class ProcessRekognitionResultRequest
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
