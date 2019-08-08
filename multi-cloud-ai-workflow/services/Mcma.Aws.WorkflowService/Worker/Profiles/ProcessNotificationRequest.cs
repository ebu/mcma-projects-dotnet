using Mcma.Core;

namespace Mcma.Aws.WorkflowService.Worker
{
    internal class ProcessNotificationRequest
    {
        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }
    }
}
