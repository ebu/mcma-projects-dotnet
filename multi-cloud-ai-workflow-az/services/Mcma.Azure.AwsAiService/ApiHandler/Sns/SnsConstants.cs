namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public static class SnsConstants
    {
        public const string MessageTypeHeader = "x-amz-sns-message-type";

        public static class MessageTypes
        {
            public const string SubscriptionConfirmation = nameof(SubscriptionConfirmation);

            public const string Notification = nameof(Notification);
        }
    }
}