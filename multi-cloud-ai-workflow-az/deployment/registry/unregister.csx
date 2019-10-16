#load "../../build/build.csx"
#load "../../build/task.csx"

#r "nuget:AWSSDK.Core, 3.3.103.20"
#r "nuget:Newtonsoft.Json, 11.0.2"

#r "nuget:Mcma.Core, 0.5.5.50"
#r "nuget:Mcma.Client, 0.5.5.50"
#r "nuget:Mcma.Azure.Client, 0.5.5.50"

using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Mcma.Core;
using Mcma.Client;
using Mcma.Azure.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ClearServiceRegistry : BuildTask
{
    private static readonly JObject AwsCredentialsJson = JObject.Parse(File.ReadAllText("./deployment/aws-credentials.json"));
    private static readonly AWSCredentials AwsCredentials = new BasicAWSCredentials(AwsCredentialsJson["accessKeyId"].Value<string>(), AwsCredentialsJson["secretAccessKey"].Value<string>());
    private static readonly RegionEndpoint AwsRegion = RegionEndpoint.GetBySystemName(AwsCredentialsJson["region"].Value<string>());

    private static AwsV4AuthContext ServicesAuthContext { get; } =
        new AwsV4AuthContext(
            AwsCredentialsJson["accessKeyId"].Value<string>(),
            AwsCredentialsJson["secretAccessKey"].Value<string>(),
            AwsCredentialsJson["region"].Value<string>()
        );

    private static IResourceManagerProvider ResourceManagerProvider { get; } =
        new ResourceManagerProvider(new AuthProvider().AddAwsV4Auth(ServicesAuthContext));

    private IDictionary<string, string> ParseContent(string content)
    {
        var serviceUrls = new Dictionary<string, string>();

        foreach (var line in content.Split('\n'))
        {
            var parts = line.Split(new[] {" = "}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
                serviceUrls[parts[0]] = parts[1].Trim();
        }

        return serviceUrls;
    }

    protected override async Task<bool> ExecuteTask()
    {
        var content = File.ReadAllText($"{Build.Dirs.Deployment.TrimEnd('/')}/terraform.output");
        var terraformOutput = ParseContent(content);
        
        var servicesUrl = terraformOutput["services_url"];

        var resourceManager = ResourceManagerProvider.Get(new ResourceManagerConfig(servicesUrl));
        await resourceManager.InitAsync();

        foreach (var jobProfile in await resourceManager.GetAsync<JobProfile>())
            await resourceManager.DeleteAsync(jobProfile);
        
        foreach (var service in  await resourceManager.GetAsync<Service>())
            await resourceManager.DeleteAsync(service);

        return true;
    }
}