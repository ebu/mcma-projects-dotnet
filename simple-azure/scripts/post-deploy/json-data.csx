using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcma;
using Mcma.Azure.Client;
using Mcma.Azure.Client.AzureAd;
using Mcma.Client;
using Mcma.Serialization;

public class JsonData
{
    private static readonly JObject ServicesJson = JObject.Parse(File.ReadAllText("./services.json"));

    private static readonly JArray JobProfilesJson = JArray.Parse(File.ReadAllText("./profiles.json"));

    public JsonData(JObject terraformOutput)
    {
        Services =
            new Lazy<Service[]>(
                () => ServicesJson.Properties().Select(p => GetServiceFromJson(p.Value, terraformOutput[p.Name]["value"].ToString())).ToArray());

        JobProfiles =
            new Lazy<JobProfile[]>(
                () => JobProfilesJson.Select(j => j.ToMcmaObject<JobProfile>()).ToArray());
    }

    public Lazy<JobProfile[]> JobProfiles { get; }

    public Lazy<Service[]> Services { get; }

    public Service ServiceRegistry => Services.Value[0];

    public string ServicesUrl => ServiceRegistry.Resources.FirstOrDefault(r => r.ResourceType == nameof(Service))?.HttpEndpoint;

    public static JsonData Load(string terraformOutputPath)
        => new JsonData(JObject.Parse(File.ReadAllText(terraformOutputPath)));

    public void SetJobProfileIds()
    {
        foreach (var service in Services.Value.Where(s => s.JobProfileIds != null && s.JobProfileIds.Length > 0))
        {
            for (var i = 0; i < service.JobProfileIds.Length; i++)
            {
                var jobProfileName = service.JobProfileIds[i];

                var jobProfileId = JobProfiles.Value.FirstOrDefault(p => p.Name.Equals(jobProfileName, StringComparison.OrdinalIgnoreCase))?.Id;
                if (jobProfileId == null)
                    throw new Exception($"Service {service.Name} references job profile '{jobProfileName}', but the profile has not been defined.");
                
                service.JobProfileIds[i] = jobProfileId;
            }
        }
    }

    private Service GetServiceFromJson(JToken serviceJson, string url)
    {
        var service = serviceJson.ToMcmaObject<Service>();

        foreach (var resourceEndpoint in service.Resources)
            resourceEndpoint.HttpEndpoint = url.TrimEnd('/') + "/" + resourceEndpoint.HttpEndpoint.TrimStart('/');

        service.AuthType = AzureConstants.AzureAdAuthType;
        service.AuthContext = new AzureAdAuthContext { Scope = $"{url}.default" }.ToMcmaJson().ToString();

        return service;
    }
}