namespace Mcma.Aws.AzureAiService.Worker
{
    internal class AzureConfig
    {
        public AzureConfig(AzureAiServiceWorkerRequest @event)
        {
            ApiUrl = @event.StageVariables["AzureApiUrl"];
            Location = @event.StageVariables["AzureLocation"];
            AccountID = @event.StageVariables["AzureAccountID"];
            SubscriptionKey = @event.StageVariables["AzureSubscriptionKey"];
        }

        public string ApiUrl { get; }

        public string Location { get; }

        public string AccountID { get; }

        public string SubscriptionKey { get; }
    }
}
