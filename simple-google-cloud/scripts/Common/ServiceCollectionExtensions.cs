using Mcma.Client;
using Mcma.GoogleCloud.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mcma.GoogleCloud.Sample.Scripts.Common
{
    public static class ServiceCollectionExtensions
    {
        public static McmaClientBuilder ConfigureForScripts(this McmaClientBuilder builder)
        {
            builder.Services
                   .AddSingleton<TerraformOutput>()
                   .AddSingleton<IConfigureOptions<ResourceManagerProviderOptions>, ConfigureResourceManagerDefaults>();

            builder.Auth.AddGoogleAuth(opts => opts.KeyFile = "../google-cloud-credentials");
            
            return builder;
        }
    }
}