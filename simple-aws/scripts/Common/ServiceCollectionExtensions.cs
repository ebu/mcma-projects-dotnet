using Mcma.Aws.Client;
using Mcma.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mcma.Aws.Sample.Scripts.Common
{
    public static class ServiceCollectionExtensions
    {
        public static McmaClientBuilder ConfigureForScripts(this McmaClientBuilder builder)
        {
            builder.Services
                   .AddSingleton<AwsCredentials>()
                   .AddSingleton<TerraformOutput>()
                   .AddSingleton<IConfigureOptions<Aws4AuthenticatorFactoryOptions>, ConfigureAws4AuthDefaults>()
                   .AddSingleton<IConfigureOptions<ResourceManagerProviderOptions>, ConfigureResourceManagerDefaults>();

            builder.Auth.AddAws4Auth();
            
            return builder;
        }
    }
}