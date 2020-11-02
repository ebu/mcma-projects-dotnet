using System.Linq;

namespace Mcma.Azure.Sample.Scripts.Common
{
    public class AzureADCredentials
    {
        public AzureADCredentials(params string[] args)
        {            
            TenantId = args.FirstOrDefault(x => x.StartsWith("--azureTenantId="))?.Replace("--azureTenantId=", string.Empty);
            ClientId = args.FirstOrDefault(x => x.StartsWith("--azureClientId="))?.Replace("--azureClientId=", string.Empty);
            ClientSecret = args.FirstOrDefault(x => x.StartsWith("--azureClientSecret="))?.Replace("--azureClientSecret=", string.Empty);
        }

        public string TenantId { get; }
        
        public string ClientId { get; }
        
        public string ClientSecret { get; }
    }
}