
using Amazon.Runtime;
using Mcma.Core.ContextVariables;

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

        public static BasicAWSCredentials AwsCredentials(this IContextVariableProvider contextVariableProvider)
            => new BasicAWSCredentials(contextVariableProvider.AwsAccessKey(), contextVariableProvider.AwsSecretKey());

        public static string RekognitionRekoSnsRoleArn(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(RekognitionRekoSnsRoleArn));

        public static string AwsAiOutputSnsTopicArn(this IContextVariableProvider contextVariableProvider)
            => contextVariableProvider.GetRequiredContextVariable(nameof(AwsAiOutputSnsTopicArn));
    }
}