using Mcma.Client;
using Microsoft.Extensions.Options;

namespace Mcma.GoogleCloud.Sample.Scripts.Common
{
    public class ConfigureResourceManagerDefaults : IConfigureOptions<ResourceManagerProviderOptions>
    {
        public ConfigureResourceManagerDefaults(TerraformOutput terraformOutput)
        {
            TerraformOutput = terraformOutput;
        }

        private TerraformOutput TerraformOutput { get; }

        public void Configure(ResourceManagerProviderOptions options)
        {
            options.DefaultOptions = new ResourceManagerOptions
            {
                ServicesUrl = TerraformOutput.ServicesUrl,
                ServicesAuthType = TerraformOutput.ServiceRegistryAuthType,
                ServicesAuthContext = TerraformOutput.ServiceRegistryAuthContext
            };
        }
    }
}