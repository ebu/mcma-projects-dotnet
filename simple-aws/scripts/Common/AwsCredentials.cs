using System.IO;
using Amazon;
using Amazon.Runtime;
using Mcma.Aws.Client;
using Newtonsoft.Json.Linq;

namespace Mcma.Aws.Sample.Scripts.Common
{
    public class AwsCredentials
    {
        private const string AwsCredentialsPath = "../deployment/aws-credentials.json";

        public AwsCredentials()
        {
            var json = JObject.Parse(File.ReadAllText(AwsCredentialsPath));

            Credentials = new BasicAWSCredentials(json["accessKeyId"]?.Value<string>(), json["secretAccessKey"]?.Value<string>());
            Region = RegionEndpoint.GetBySystemName(json["region"]?.Value<string>());
            AuthContext =
                new Aws4AuthContext(
                    json["accessKeyId"]?.Value<string>(),
                    json["secretAccessKey"]?.Value<string>(),
                    json["region"]?.Value<string>()
                );
        }

        public AWSCredentials Credentials { get; }

        public RegionEndpoint Region { get; }

        public Aws4AuthContext AuthContext { get; }
    }
}