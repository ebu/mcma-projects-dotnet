
using Amazon;
using Amazon.Runtime;
using Mcma.Core.Context;

namespace Mcma.Azure.AwsAiService
{
    public static class AwscontextVariablesExtensions
    {
        public static string AwsAiInputBucket(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AwsAiInputBucket));

        public static string AwsAiOutputBucket(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AwsAiOutputBucket));

        public static string AwsAccessKey(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AwsAccessKey));

        public static string AwsSecretKey(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AwsSecretKey));

        public static RegionEndpoint AwsRegion(this IContextVariables contextVariables)
            => RegionEndpoint.GetBySystemName(contextVariables.GetRequired(nameof(AwsRegion)));

        public static string AwsRekoSnsRoleArn(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AwsRekoSnsRoleArn));

        public static string AwsAiOutputSnsTopicArn(this IContextVariables contextVariables)
            => contextVariables.GetRequired(nameof(AwsAiOutputSnsTopicArn));

        public static BasicAWSCredentials AwsCredentials(this IContextVariables contextVariables)
            => new BasicAWSCredentials(contextVariables.AwsAccessKey(), contextVariables.AwsSecretKey());
    }
}