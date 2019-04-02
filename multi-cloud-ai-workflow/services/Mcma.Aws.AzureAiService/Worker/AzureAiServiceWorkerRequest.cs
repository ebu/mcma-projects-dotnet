using System.Collections.Generic;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;
using Mcma.Aws.Api;

namespace Mcma.Aws.AzureAiService.Worker
{
    public class AzureAiServiceWorkerRequest : IStageVariableProvider
    {
        public string Action { get; set; }

        public string JobAssignmentId { get; set; }

        public IDictionary<string, string> StageVariables { get; set; }

        public S3Locator OutputFile { get; set; }

        public AzureNotification Notification { get; set; }
    }
}
