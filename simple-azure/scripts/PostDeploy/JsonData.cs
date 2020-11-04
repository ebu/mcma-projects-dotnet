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
        public JsonData(TerraformOutput terraformOutput)
        {
            Services =
                JArray.Parse(ReplaceServiceJsonTokens(terraformOutput))
                       .Select(j => j.ToMcmaObject<Service>())
                       .ToArray();

            JobProfiles = 
                JArray.Parse(File.ReadAllText("./PostDeploy/profiles.json"))
                      .Select(j => j.ToMcmaObject<JobProfile>())
                      .ToArray();

            ServiceRegistry = Services.FirstOrDefault(s => s.Name == "Service Registry");
        }

        public JobProfile[] JobProfiles { get; }

        public Service[] Services { get; }

        public Service ServiceRegistry { get; }

        private static string ReplaceServiceJsonTokens(TerraformOutput terraformOutput)
            => terraformOutput.Properties()
                              .Where(p => p.Name.Contains("_url"))
                              .Aggregate(File.ReadAllText("./PostDeploy/services.json"),
                                         (json, curProp) => json.Replace("{{{" + curProp.Name + "}}}", curProp.Value["value"].Value<string>()));
    }
    
}