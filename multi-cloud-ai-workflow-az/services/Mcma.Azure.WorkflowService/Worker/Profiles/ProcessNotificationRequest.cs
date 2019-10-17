using Mcma.Core;

namespace Mcma.Azure.WorkflowService.Worker
{
    internal class ProcessNotificationRequest
    {
        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }
    }
}
