using System.Threading.Tasks;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Mcma.Utility;

namespace Mcma.Aws.JobProcessor.Common
{
    public class CloudWatchEventsJobCheckerTrigger : IJobCheckerTrigger
    {
        private AmazonCloudWatchEventsClient Client { get; } = new AmazonCloudWatchEventsClient();

        private string RuleName { get; } = McmaEnvironmentVariables.Get("CLOUDWATCH_EVENT_RULE");

        public Task EnableAsync() => Client.EnableRuleAsync(new EnableRuleRequest {Name = RuleName});

        public Task DisableAsync() => Client.DisableRuleAsync(new DisableRuleRequest {Name = RuleName});
    }
}