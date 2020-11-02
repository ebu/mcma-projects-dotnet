using System;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.Sample.Scripts.Common;
using Mcma.Client;

namespace Mcma.Azure.Sample.Scripts.RunJobs
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

        public async Task<string> StartJobAsync<T>(string profile, string inputFile, string outputLocation) where T : Job, new()
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
                        [nameof(inputFile)] = new BlobStorageFileLocator
                        {
                            StorageAccountName = TerraformOutput.MediaStorageAccountName,
                            Container = TerraformOutput.UploadContainer,
                            FilePath = inputFile
                        },
                        [nameof(outputLocation)] = new BlobStorageFolderLocator
                        {
                            StorageAccountName = TerraformOutput.MediaStorageAccountName,
                            Container = TerraformOutput.OutputContainer,
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