using Mcma.Azure.Client;
using Mcma.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Mcma.Azure.Sample.Scripts.Common
{
    public static class ServiceCollectionExtensions
    {
        public static McmaClientBuilder ConfigureForScripts(this McmaClientBuilder builder, params string[] args)
        {
            builder.Services
                   .AddSingleton(new AzureADCredentials(args))
                   .AddSingleton<TerraformOutput>()
                   .AddSingleton<IConfigureOptions<ConfidentialClientApplicationOptions>, ConfigureAzureADConfidentialClientOptions>()
                   .AddSingleton<IConfigureOptions<ResourceManagerProviderOptions>, ConfigureResourceManagerDefaults>();

            builder.Auth.AddAzureADConfidentialClientAuth();
            
            return builder;
        }
    }
}