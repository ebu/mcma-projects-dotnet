using System.Collections.Generic;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;

namespace Mcma.Aws.AwsAiService.Worker
{
    public class AwsAiServiceWorkerRequest : IStageVariableProvider
    {
        public string Action { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public IDictionary<string, string> StageVariables { get; set; }

        public S3Locator OutputFile { get; set; }

        public JobExternalInfo JobExternalInfo { get; set; }
    }
}
