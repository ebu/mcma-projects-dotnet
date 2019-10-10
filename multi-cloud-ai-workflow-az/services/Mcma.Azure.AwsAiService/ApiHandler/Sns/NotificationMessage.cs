namespace Mcma.Azure.AwsAiService.ApiHandler.Sns
{
    public class NotificationMessage : SnsMessage
    {
        public string UnsubscribeURL { get; set; }
    }
}