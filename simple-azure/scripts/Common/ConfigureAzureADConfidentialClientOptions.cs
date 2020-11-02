using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Mcma.Azure.Sample.Scripts.Common
{
    public class ConfigureAzureADConfidentialClientOptions : IConfigureOptions<ConfidentialClientApplicationOptions>
    {
        public ConfigureAzureADConfidentialClientOptions(AzureADCredentials azureADCredentials)
        {
            AzureADCredentials = azureADCredentials;
        }
        
        private AzureADCredentials AzureADCredentials { get; }

        public void Configure(ConfidentialClientApplicationOptions options)
        {
            options.TenantId = AzureADCredentials.TenantId;
            options.ClientId = AzureADCredentials.ClientId;
            options.ClientSecret = AzureADCredentials.ClientSecret;
        }
    }
}