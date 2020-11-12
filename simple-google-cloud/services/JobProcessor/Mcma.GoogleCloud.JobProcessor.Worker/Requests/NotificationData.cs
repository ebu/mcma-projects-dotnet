

namespace Mcma.GoogleCloud.JobProcessor.Worker
{
    public class NotificationData
    {
        public string JobId { get; set; }
        
        public string JobExecutionId { get; set; }

        public Notification Notification { get; set; }
    }
}
