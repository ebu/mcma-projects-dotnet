#load "../../build/task.csx"
#load "../../build/build.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"
#r "nuget:Mcma.Core, 0.5.5.31"
#r "nuget:Mcma.Client, 0.5.5.31"
#r "nuget:Mcma.Azure.Client, 0.5.5.31"

using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Client;
using Mcma.Azure.Client;

public class UpdateServiceRegistry : BuildTask
{
    private static readonly JObject Services = JObject.Parse(File.ReadAllText("./deployment/registry/services.json"));

    private static readonly JObject JobProfiles = JObject.Parse(File.ReadAllText("./deployment/registry/profiles.json"));

    private static readonly string PrivateEncryptionKeyJson = File.ReadAllText("./deployment/private-key.json");

    private static readonly string PublicEncryptionKeyJson = File.ReadAllText("./deployment/public-key.json");

    private static IResourceManagerProvider ResourceManagerProvider { get; } =
        new ResourceManagerProvider(new AuthProvider().AddAzureFunctionKeyAuth(PrivateEncryptionKeyJson));

    // private async Task ConfigureCognito(IDictionary<string, string> terraformOutput, string servicesUrl)
    // {
    //     // 2. Uploading configuration to website
    //     Console.WriteLine("Uploading deployment configuration to website");
    //     var config = JObject.FromObject(new
    //     {
    //         resourceManager = new
    //         {
    //             servicesUrl = terraformOutput["services_url"],
    //             servicesAuthType = terraformOutput["services_auth_type"]
    //         },
    //         aws = new
    //         {
    //             region = terraformOutput["aws_region"],
    //             s3 = new
    //             {
    //                 uploadBucket = terraformOutput["upload_bucket"]
    //             }
    //         }
    //     });

    //     var s3Params = new PutObjectRequest
    //     {
    //         BucketName = terraformOutput["website_bucket"],
    //         Key = "config.json",
    //         ContentBody = config.ToString(),
    //         ContentType = "application/json"
    //     };

    //     try
    //     {
    //         var s3 = new AmazonS3Client(AwsCredentials, AwsRegion);
    //         await s3.PutObjectAsync(s3Params);
    //     }
    //     catch (Exception error)
    //     {
    //         Console.WriteLine(error);
    //         return;
    //     }
    // }

    protected override async Task<bool> ExecuteTask()
    {
        var content = File.ReadAllText($"{Build.Dirs.Deployment.TrimEnd('/')}/terraform.output");
        var terraformOutput = ParseContent(content);
        var serviceUrlsAndKeys = GetServiceUrlsAndKeys(terraformOutput);
        
        var serviceRegistryUrl = terraformOutput["service_registry_url"];
        var serviceRegistryKey = terraformOutput["service_registry_key"];

        var servicesUrl = $"{serviceRegistryUrl}services";
        var jobProfilesUrl = $"{serviceRegistryUrl}job-profiles";

        var serviceRegistry = new Service
        {
            Name = "Service Registry",
            Resources = new[]
            {
                new ResourceEndpoint {ResourceType = nameof(Service), HttpEndpoint = servicesUrl},
                new ResourceEndpoint {ResourceType = nameof(JobProfile), HttpEndpoint = jobProfilesUrl}
            },
            AuthType = AzureConstants.AzureFunctionKeyAuth,
            AuthContext =
                new AzureFunctionKeyAuthContext(EncryptionHelper.Encrypt(serviceRegistryKey, PublicEncryptionKeyJson))
                    .ToMcmaJson()
                    .ToString()
        };

        var resourceManager = ResourceManagerProvider.Get(servicesUrl, serviceRegistry.AuthType, serviceRegistry.AuthContext);
        
        Console.WriteLine("Getting existing services...");
        var retrievedServices = await resourceManager.GetAsync<Service>();
        
        foreach (var retrievedService in retrievedServices)
        {
            if (retrievedService.Name == "Service Registry")
            {
                if (serviceRegistry.Id == null)
                {
                    serviceRegistry.Id = retrievedService.Id;

                    Console.WriteLine("Updating Service Registry");
                    await resourceManager.UpdateAsync(serviceRegistry);
                }
                else
                {
                    Console.WriteLine("Removing duplicate Service Registry '" + retrievedService.Id + "'");
                    await resourceManager.DeleteAsync(retrievedService);
                }
            }
        }

        if (serviceRegistry.Id == null)
        {
            Console.WriteLine("Inserting Service Registry");
            serviceRegistry = await resourceManager.CreateAsync(serviceRegistry);
        }

        await resourceManager.InitAsync();

        var retrievedJobProfiles = await resourceManager.GetAsync<JobProfile>();

        foreach (var retrievedJobProfile in retrievedJobProfiles)
        {
            var jobProfileJson = JobProfiles[retrievedJobProfile.Name];
            if (jobProfileJson != null)
            {
                jobProfileJson["id"] = retrievedJobProfile.Id;
                var jobProfile = jobProfileJson.ToMcmaObject<JobProfile>();

                Console.WriteLine("Updating JobProfile '" + jobProfile.Name + "'");
                await resourceManager.UpdateAsync(jobProfile);
            }
            else
            {
                Console.WriteLine("Removing JobProfile '" + retrievedJobProfile.Name + "'");
                await resourceManager.DeleteAsync(retrievedJobProfile);
            }
        }

        foreach (var jobProfileName in JobProfiles.Properties().Select(p => p.Name).ToList())
        {
            var jobProfileJson = JobProfiles[jobProfileName];
            if (jobProfileJson["id"] == null)
            {
                var jobProfile = jobProfileJson.ToMcmaObject<JobProfile>();
                Console.WriteLine("Inserting JobProfile '" + jobProfile.Name + "'");
                jobProfile = await resourceManager.CreateAsync(jobProfile);
                jobProfileJson["id"] = jobProfile.Id;
            }
        }

        var services = CreateServices(serviceUrlsAndKeys);

        retrievedServices = await resourceManager.GetAsync<Service>();

        foreach (var retrievedService in retrievedServices.ToList())
        {
            if (retrievedService.Name == serviceRegistry.Name)
                continue;

            if (services.ContainsKey(retrievedService.Name))
            {
                var service = services[retrievedService.Name];
                service.Id = retrievedService.Id;

                Console.WriteLine("Updating Service '" + service.Name + "'");
                await resourceManager.UpdateAsync(service);
            }
            else
            {
                Console.WriteLine("Removing Service '" + retrievedService.Name + "'");
                await resourceManager.DeleteAsync(retrievedService);
            }
        }

        foreach (var serviceName in services.Keys.ToList())
        {
            var service = services[serviceName];
            if (service.Id == null)
            {
                Console.WriteLine("Inserting Service '" + service.Name + "'");
                services[serviceName] = await resourceManager.CreateAsync(service);
            }
        }

        return true;
    }

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

    private IDictionary<string, (string, string)> GetServiceUrlsAndKeys(IDictionary<string, string> terraformOutput)
        =>
            terraformOutput
                .Where(x => x.Key.EndsWith("_url") || x.Key.EndsWith("_key"))
                .GroupBy(x => x.Key.Replace("_url", "").Replace("_key", ""))
                .Where(
                    x =>
                        x.Count() == 2 &&
                        x.Any(y => y.Key.EndsWith("_url")) &&
                        x.Any(y => y.Key.EndsWith("_key")))
                .ToDictionary(
                    x => x.First(y => y.Key.EndsWith("_url")).Key,
                    x =>
                    (
                        x.First(y => y.Key.EndsWith("_url")).Value,
                        x.First(y => y.Key.EndsWith("_key")).Value
                    )
                );

    private static IDictionary<string, Service> CreateServices(IDictionary<string, (string, string)> serviceUrlsAndKeys)
    {
        var serviceList = new List<Service>();

        foreach (var prop in serviceUrlsAndKeys.Keys)
        {
            var serviceJson = Services[prop];
            if (serviceJson == null)
                continue;

            var (url, key) = serviceUrlsAndKeys[prop];

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

                    var jobProfileId = JobProfiles[jobProfileName]?["id"];
                    if (jobProfileId == null)
                        throw new Exception($"Service {serviceJson["name"]} references job profile '{jobProfileName}', but the profile has not been defined.");
                    
                    jobProfileArray[i] = jobProfileId;
                }
            }

            serviceJson["authType"] = AzureConstants.AzureFunctionKeyAuth;
            serviceJson["authContext"] =
                new AzureFunctionKeyAuthContext(EncryptionHelper.Encrypt(key, PublicEncryptionKeyJson))
                    .ToMcmaJson()
                    .ToString();

            serviceList.Add(serviceJson.ToMcmaObject<Service>());
        }

        return serviceList.ToDictionary(service => service.Name, service => service);
    }
}