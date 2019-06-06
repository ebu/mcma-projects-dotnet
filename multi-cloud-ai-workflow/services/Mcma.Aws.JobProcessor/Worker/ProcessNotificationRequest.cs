using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using System.Collections.Generic;

namespace Mcma.Aws.JobProcessor.Worker
{
    public class ProcessNotificationRequest
    {
        public string JobProcessId { get; set; }

        public Notification Notification { get; set; }
    }
}
