using Mcma;
using Mcma.Azure.BlobStorage;
using Mcma.Client;
using Newtonsoft.Json.Linq;

public static async Task<string> RunMediaInfoJobAsync(IResourceManager resourceManager, BlobStorageFileLocator inputFile, string uuid)
{
    var jobProfiles = (await resourceManager.QueryAsync<JobProfile>(("name", "ExtractTechnicalMetadata"))).ToArray();
    if (jobProfiles == null || jobProfiles.Length == 0)
        throw new McmaException("JobProfile with the name 'ExtractTechnicalMetadata' not found.");
    
    if (jobProfiles.Length > 1)
        throw new McmaException("Found more than one JobProfile with the name 'ExtractTechnicalMetadata'.");

    var ameJob = new AmeJob
    {
        JobProfile = jobProfiles[0].Id,
        JobInput =
            new JobParameterBag
            {
                [nameof(inputFile)] = inputFile,
                ["outputLocation"] = new BlobStorageFolderLocator
                {
                    StorageAccountName = TerraformOutput["media_storage_account_name"]["value"].Value<string>(),
                    Container = TerraformOutput["output_container"]["value"].Value<string>(),
                    FolderPath = uuid + "/metadata"
                }
            }
    };

    ameJob = await resourceManager.CreateAsync(ameJob);

    return ameJob.Id;
}