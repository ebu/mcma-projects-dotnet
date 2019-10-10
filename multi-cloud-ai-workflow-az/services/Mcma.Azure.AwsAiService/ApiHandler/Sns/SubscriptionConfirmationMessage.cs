namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public class SubscriptionConfirmationMessage : SnsMessage
    {
        public string Token { get; set; }
    
        public string SubscribeURL { get; set; }
    }
}