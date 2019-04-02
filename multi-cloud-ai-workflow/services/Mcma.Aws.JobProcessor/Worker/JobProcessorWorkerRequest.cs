using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;
using Mcma.Aws.Api;

namespace Mcma.Aws.JobProcessor.Worker
{
    public class JobProcessorWorkerRequest
    {
        public string Action { get; set; }

        public string JobProcessId { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public ApiGatewayRequest Request { get; set; }
    }
}
