using Mcma.Core;

namespace Mcma.Aws.TransformService.Worker
{
    public class ProcessNotificationRequest
    {
        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }
    }
}
