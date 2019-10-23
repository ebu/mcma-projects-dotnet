using Newtonsoft.Json.Linq;

namespace Mcma.Azure.WorkflowService.Worker
{
    internal class WorkflowState
    {
        public string Status { get; set; }

        public double? Progress { get; set; }

        public JObject Output { get; set; }

        public JArray Errors { get; set; }
    }
}
