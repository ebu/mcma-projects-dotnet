using Mcma.Aws.Client;
using Microsoft.Extensions.Options;

namespace Mcma.Aws.Sample.Scripts.Common
{
    public class ConfigureAws4AuthDefaults : IConfigureOptions<Aws4AuthenticatorFactoryOptions>
    {
        public ConfigureAws4AuthDefaults(AwsCredentials awsCredentials)
        {
            AwsCredentials = awsCredentials;
        }

        private AwsCredentials AwsCredentials { get; }

        public void Configure(Aws4AuthenticatorFactoryOptions options)
        {
            options.DefaultAuthContext = AwsCredentials.AuthContext;
        }
    }
}