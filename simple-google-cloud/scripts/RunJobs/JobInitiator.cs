using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.GoogleCloud.Sample.Scripts.Common;
using Mcma.GoogleCloud.Storage;

namespace Mcma.GoogleCloud.Sample.Scripts.RunJobs
{
    public class JobInitiator
    {
        public JobInitiator(IResourceManager resourceManager, TerraformOutput terraformOutput)
        {
            ResourceManager = resourceManager;
            TerraformOutput = terraformOutput;
        }
        
        private IResourceManager ResourceManager { get; }

        private TerraformOutput TerraformOutput { get; }

        public async Task<string> StartJobAsync<T>(string profile, Locator inputFile, string outputLocation) where T : Job, new()
        {
            Console.WriteLine($"Starting {profile} {typeof(T).Name}...");
            
            var jobProfiles = (await ResourceManager.QueryAsync<JobProfile>(("name", profile))).ToArray();
            if (jobProfiles == null || jobProfiles.Length == 0)
                throw new McmaException($"JobProfile with the name '{profile}' not found.");
    
            if (jobProfiles.Length > 1)
                throw new McmaException($"Found more than one JobProfile with the name '{profile}'.");

            var job = new T
            {
                JobProfileId = jobProfiles[0].Id,
                JobInput =
                    new JobParameterBag
                    {
                        [nameof(inputFile)] = inputFile,
                        [nameof(outputLocation)] = new CloudStorageFolderLocator
                        {
                            Bucket = TerraformOutput.OutputBucket,
                            FolderPath = outputLocation
                        }
                    }
            };

            job = await ResourceManager.CreateAsync(job);
            
            Console.WriteLine($"{profile} {typeof(T).Name} job successfully started: {job.Id}");

            return job.Id;
        }
    }
}