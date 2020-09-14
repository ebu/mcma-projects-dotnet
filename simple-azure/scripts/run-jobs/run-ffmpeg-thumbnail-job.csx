using Mcma;
using Mcma.Azure.BlobStorage;
using Mcma.Client;
using Newtonsoft.Json.Linq;

public static async Task<string> RunFFmpegThumbnailJobAsync(IResourceManager resourceManager, BlobStorageFileLocator inputFile, string uuid)
{
    var jobProfiles = (await resourceManager.QueryAsync<JobProfile>(("name", "ExtractThumbnail"))).ToArray();
    if (jobProfiles == null || jobProfiles.Length == 0)
        throw new McmaException("JobProfile with the name 'ExtractThumbnail' not found.");
    
    if (jobProfiles.Length > 1)
        throw new McmaException("Found more than one JobProfile with the name 'ExtractThumbnail'.");

    var transformJob = new TransformJob
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
                    FolderPath = uuid + "/thumbnails"
                }
            }
    };

    transformJob = await resourceManager.CreateAsync(transformJob);

    return transformJob.Id;
}