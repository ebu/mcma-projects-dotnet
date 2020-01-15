
using Amazon;
using Amazon.Runtime;
using Mcma.Core.Context;

namespace Mcma.Azure.AwsAiService
{
    public static class AwsContextVariableProviderExtensions
    {
        public static string AwsAiInputBucket(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AwsAiInputBucket));

        public static string AwsAiOutputBucket(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AwsAiOutputBucket));

        public static string AwsAccessKey(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AwsAccessKey));

        public static string AwsSecretKey(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AwsSecretKey));

        public static RegionEndpoint AwsRegion(this IContextVariableProvider contextVariableProvider)
            => RegionEndpoint.GetBySystemName(contextVariableProvider.GetRequiredContextVariable(nameof(AwsRegion)));

        public static string AwsRekoSnsRoleArn(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AwsRekoSnsRoleArn));

        public static string AwsAiOutputSnsTopicArn(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AwsAiOutputSnsTopicArn));

        public static BasicAWSCredentials AwsCredentials(this IContextVariableProvider contextVariableProvider)
            => new BasicAWSCredentials(contextVariableProvider.AwsAccessKey(), contextVariableProvider.AwsSecretKey());
    }
}