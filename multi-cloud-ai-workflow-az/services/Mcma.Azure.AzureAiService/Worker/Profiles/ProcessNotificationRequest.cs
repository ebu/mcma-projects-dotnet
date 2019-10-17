namespace Mcma.Azure.AzureAiService.Worker
{
    public class ProcessNotificationRequest
    {
        public class VideoIndexerNotification
        {
            public string Id { get; set; }

            public object State { get; set; } 
        }

        public string JobAssignmentId { get; set; }

        public VideoIndexerNotification Notification { get; set; }
    }
}
