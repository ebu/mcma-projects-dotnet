using System;
using System.Threading.Tasks;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;

namespace Mcma.Aws.JobProcessor.Common
{
    public class CloudWatchEventsJobCheckerTrigger : IJobCheckerTrigger
    {
        private AmazonCloudWatchEventsClient Client { get; } = new AmazonCloudWatchEventsClient();

        private string RuleName { get; } = Environment.GetEnvironmentVariable("CloudwatchEventRule");

        public Task EnableAsync() => Client.EnableRuleAsync(new EnableRuleRequest {Name = RuleName});

        public Task DisableAsync() => Client.DisableRuleAsync(new DisableRuleRequest {Name = RuleName});
    }
}