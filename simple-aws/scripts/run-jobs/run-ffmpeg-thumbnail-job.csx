using Mcma;
using Mcma.Aws.S3;
using Mcma.Client;
using Newtonsoft.Json.Linq;

public static async Task<string> RunFFmpegThumbnailJobAsync(IResourceManager resourceManager, JObject terraformOutput, AwsS3FileLocator inputFile, string uuid)
{
    var jobProfiles = (await resourceManager.QueryAsync<JobProfile>(("name", "ExtractThumbnail"))).ToArray();
    if (jobProfiles == null || jobProfiles.Length == 0)
        throw new McmaException("JobProfile with the name 'ExtractThumbnail' not found.");
    
    if (jobProfiles.Length > 1)
        throw new McmaException("Found more than one JobProfile with the name 'ExtractThumbnail'.");

    var transformJob = new TransformJob
    {
        JobProfileId = jobProfiles[0].Id,
        JobInput =
            new JobParameterBag
            {
                [nameof(inputFile)] = inputFile,
                ["outputLocation"] = new AwsS3FolderLocator
                {
                    Bucket = terraformOutput["output_bucket"]["value"].Value<string>(),
                    KeyPrefix = uuid + "/thumbnail/"
                }
            }
    };

    transformJob = await resourceManager.CreateAsync(transformJob);

    return transformJob.Id;
}