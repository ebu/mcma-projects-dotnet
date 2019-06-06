using Mcma.Core;

namespace Mcma.Aws.JobRepository.Worker
{
    public class ProcessNotificationRequest
    {
        public string JobId { get; set; }

        public Notification Notification { get; set; }
    }
}
