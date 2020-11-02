using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Mcma.Azure.Sample.Scripts.Common;
using Mcma.Serialization;

namespace Mcma.Azure.Sample.Scripts.PostDeploy
{
    public class JsonData
    {
        private static readonly JObject ServicesJson = JObject.Parse(File.ReadAllText("./PostDeploy/services.json"));

        private static readonly JArray JobProfilesJson = JArray.Parse(File.ReadAllText("./PostDeploy/profiles.json"));

        public JsonData(TerraformOutput terraformOutput)
        {
            Services =
                new Lazy<Service[]>(
                    () => ServicesJson.Properties().Select(p => GetServiceFromJson(p.Value, terraformOutput[p.Name]?["value"]?.ToString())).ToArray());

            JobProfiles =
                new Lazy<JobProfile[]>(
                    () => JobProfilesJson.Select(j => j.ToMcmaObject<JobProfile>()).ToArray());
        }

        public Lazy<JobProfile[]> JobProfiles { get; }

        public Lazy<Service[]> Services { get; }

        public Service ServiceRegistry => Services.Value.FirstOrDefault(s => s.Name == "Service Registry");

        public string ServicesUrl => ServiceRegistry.Resources.FirstOrDefault(r => r.ResourceType == nameof(Service))?.HttpEndpoint;

        private Service GetServiceFromJson(JToken serviceJson, string url)
        {
            var service = serviceJson.ToMcmaObject<Service>();

            foreach (var resourceEndpoint in service.Resources)
                resourceEndpoint.HttpEndpoint = url.TrimEnd('/') + "/" + resourceEndpoint.HttpEndpoint.TrimStart('/');

            return service;
        }
    }
    
}