using Amazon;
using Amazon.Runtime;
using Mcma.Aws.Client;
using Newtonsoft.Json.Linq;

public class AwsCredentials
{
    private AwsCredentials(JObject json)
    {
        Credentials = new BasicAWSCredentials(json["accessKeyId"].Value<string>(), json["secretAccessKey"].Value<string>());
        Region = RegionEndpoint.GetBySystemName(json["region"].Value<string>());
        AuthContext =
            new AwsV4AuthContext(
                json["accessKeyId"].Value<string>(),
                json["secretAccessKey"].Value<string>(),
                json["region"].Value<string>()
            );
    }

    private JObject Json { get; }

    public AWSCredentials Credentials { get; }

    public RegionEndpoint Region { get; }

    public AwsV4AuthContext AuthContext { get; } 

    public static AwsCredentials Load(string terraformOutputPath)
        => new AwsCredentials(JObject.Parse(File.ReadAllText(terraformOutputPath)));
}