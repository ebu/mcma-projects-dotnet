#load "../terraform-output.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"
#r "nuget:Mcma.Core, 0.8.6-beta5"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcma.Azure.Client;
using Mcma.Azure.Client.AzureAd;
using Mcma.Client;
using Mcma.Core;
using Mcma.Core.Serialization;

public class ServiceRegistryPopulator
{
    private const string ServiceRegistryJson = @"{
    ""@type"": ""Service"",
    ""name"": ""Service Registry"",
    ""resources"": [
        {
            ""@type"": ""ResourceEndpoint"",
            ""resourceType"": ""Service"",
            ""httpEndpoint"": ""/services""
        },
        {
            ""@type"": ""ResourceEndpoint"",
            ""resourceType"": ""JobProfile"",
            ""httpEndpoint"": ""/job-profiles""
        }
    ]
}";

    private const string CurDir = "./deployment/scripts/update-service-registry";

    private static readonly JObject ServicesJson = JObject.Parse(File.ReadAllText(CurDir + "/services.json"));

    private static readonly JArray JobProfilesJson = JArray.Parse(File.ReadAllText(CurDir + "/profiles.json"));

    public ServiceRegistryPopulator(TerraformOutput terraformOutput)
    {
        TerraformOutput = terraformOutput;

        ServiceRegistry = GetServiceFromJson(JToken.Parse(ServiceRegistryJson), TerraformOutput.ServiceRegistryUrl);

        JobProfiles = JobProfilesJson.Select(j => j.ToMcmaObject<JobProfile>()).ToArray();
    }

    private TerraformOutput TerraformOutput { get; }

    public Service ServiceRegistry { get; }

    public JobProfile[] JobProfiles { get; }

    public Service[] Services { get; private set; }

    public void LoadServicesWithJobProfileIds()
    {
        Services =
            TerraformOutput
                .ServiceUrls
                .Select(serviceUrl => new { ServiceJson = ServicesJson[serviceUrl.Key], ServiceUrl = serviceUrl.Value })
                .Where(x => x.ServiceJson != null)
                .Select(x => GetServiceFromJson(x.ServiceJson, x.ServiceUrl))
                .ToArray();
    }

    private Service GetServiceFromJson(JToken serviceJson, string url)
    {
        var resourceArray = serviceJson["resources"];
        if (resourceArray != null)
            foreach (var resourceJson in resourceArray)
                resourceJson["httpEndpoint"] = url.TrimEnd('/') + "/" + resourceJson["httpEndpoint"].Value<string>().TrimStart('/');

        var jobProfileArray = serviceJson["jobProfiles"] as JArray;
        if (jobProfileArray != null)
        {
            for (var i = 0; i < jobProfileArray.Count; i++)
            {
                var jobProfileName = jobProfileArray[i].Value<string>();

                var jobProfileId = JobProfiles.FirstOrDefault(p => p.Name.Equals(jobProfileName, StringComparison.OrdinalIgnoreCase))?.Id;
                if (jobProfileId == null)
                    throw new Exception($"Service {serviceJson["name"]} references job profile '{jobProfileName}', but the profile has not been defined.");
                
                jobProfileArray[i] = jobProfileId;
            }
        }

        serviceJson["authType"] = AzureConstants.AzureAdAuthType;
        serviceJson["authContext"] = new AzureAdAuthContext { Scope = $"{url}.default" }.ToMcmaJson().ToString();

        return serviceJson.ToMcmaObject<Service>();
    }

    public ResourceManager GetResourceManager()
    {
        var resourceManagerProvider =
            new ResourceManagerProvider(
                new AuthProvider().AddAzureAdConfidentialClientAuth(
                    (string)TaskRunner.Inputs.azureTenantId,
                    (string)TaskRunner.Inputs.azureClientId,
                    (string)TaskRunner.Inputs.azureClientSecret));

        return resourceManagerProvider.Get(TerraformOutput.ServicesUrl, ServiceRegistry.AuthType, ServiceRegistry.AuthContext);
    }
}