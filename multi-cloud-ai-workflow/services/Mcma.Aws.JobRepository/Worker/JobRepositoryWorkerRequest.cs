using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;
using Mcma.Aws.Api;

namespace Mcma.Aws.JobRepository.Worker
{
    public class JobRepositoryWorkerRequest
    {
        public string Action { get; set; }

        public string JobId { get; set; }

        public string JobProcessId { get; set; }

        public Notification Notification { get; set; }

        public ApiGatewayRequest Request { get; set; }
    }
}
