using Mcma.Core;

namespace Mcma.Aws.JobProcessor.Worker
{
    public class ProcessNotificationRequest
    {
        public string JobProcessId { get; set; }

        public Notification Notification { get; set; }
    }
}
