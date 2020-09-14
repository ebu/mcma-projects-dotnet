#r "nuget:Newtonsoft.Json, 12.0.3"
#r "nuget:Mcma.Azure.BlobStorage, 0.13.0"
#r "nuget:Mcma.Azure.Client, 0.13.0"
#r "nuget:Mcma.Core, 0.13.0"

#load "./upload-file.csx"
#load "./run-mediainfo-job.csx"
#load "./run-ffmpeg-thumbnail-job.csx"
#load "./poll-jobs-for-completion.csx"

using Mcma;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Azure.Client;
using Mcma.Azure.Client.AzureAd;
using Mcma.Client;
using Mcma.Serialization;
using Newtonsoft.Json.Linq;

var azureTenantId = Args.FirstOrDefault(x => x.StartsWith("--azureTenantId="))?.Replace("--azureTenantId=", string.Empty);
var azureClientId = Args.FirstOrDefault(x => x.StartsWith("--azureClientId="))?.Replace("--azureClientId=", string.Empty);
var azureClientSecret = Args.FirstOrDefault(x => x.StartsWith("--azureClientSecret="))?.Replace("--azureClientSecret=", string.Empty);
var testFilePath = Args.FirstOrDefault(x => x.StartsWith("--testFilePath="))?.Replace("--testFilePath=", string.Empty);

public static readonly JObject TerraformOutput = JObject.Parse(File.ReadAllText("../../deployment/terraform.output.json"));

var serviceRegistryUrl = TerraformOutput["service_registry_url"]["value"].Value<string>();

var resourceManagerProvider = 
    new ResourceManagerProvider(
        new AuthProvider().AddAzureAdConfidentialClientAuth(azureTenantId, azureClientId, azureClientSecret),
        new ResourceManagerConfig(
            serviceRegistryUrl + "services",
            AzureConstants.AzureAdAuthType,
            new AzureAdAuthContext { Scope = $"{serviceRegistryUrl}.default" }.ToMcmaJson().ToString()));

async Task ExecuteAsync()
{
    if (string.IsNullOrWhiteSpace(testFilePath))
    {
        Console.Error.WriteLine("Must provide a file to process as an argument");
        return;
    }

    var runMediaInfo = true;
    var runFFmpegThumbnail = true;

    try
    {
        var resourceManager = resourceManagerProvider.Get();

        var uploadedFileLocator = await UploadFileAsync(testFilePath);

        var uuid = Guid.NewGuid().ToString();
        var jobIds = new List<string>();

        string mediaInfoJobId = null;
        if (runMediaInfo)
        {
            Console.WriteLine("Starting MediaInfo job...");
            mediaInfoJobId = await RunMediaInfoJobAsync(resourceManager, uploadedFileLocator, uuid);
            jobIds.Add(mediaInfoJobId);
            Console.WriteLine("MediaInfo job successfully started: " + mediaInfoJobId);
        }
        
        string ffmpegThumbnailJobId = null;
        if (runFFmpegThumbnail)
        {
            Console.WriteLine("Starting FFmpeg thumbnail job...");
            ffmpegThumbnailJobId = await RunFFmpegThumbnailJobAsync(resourceManager, uploadedFileLocator, uuid);
            jobIds.Add(ffmpegThumbnailJobId);
            Console.WriteLine("FFmpeg thumbnail job successfully started: " + ffmpegThumbnailJobId);
        }

        Console.WriteLine("Polling jobs for completion...");
        var jobs = await PollJobsForCompletionAsync(resourceManager, jobIds);

        if (mediaInfoJobId != null)
        {
            var mediaInfoJob = jobs[mediaInfoJobId];
            if (mediaInfoJob.Status == JobStatus.Completed)
            {
                var fileLocator = mediaInfoJob.JobOutput.Get<BlobStorageFileLocator>("outputFile");
                var fileLocatorProxy = fileLocator.Proxy(TerraformOutput["media_storage_connection_string"]["value"].Value<string>());
                Console.WriteLine("MediaInfo output: " + fileLocatorProxy.GetPublicReadOnlyUrl());
            }
            else
                Console.WriteLine($"MediaInfo job finished with status {mediaInfoJob.Status}");
        }

        if (ffmpegThumbnailJobId != null)
        {
            var ffmpegThumbnailJob = jobs[ffmpegThumbnailJobId];
            if (ffmpegThumbnailJob.Status == JobStatus.Completed)
            {
                var fileLocator = ffmpegThumbnailJob.JobOutput.Get<BlobStorageFileLocator>("outputFile");
                var fileLocatorProxy = fileLocator.Proxy(TerraformOutput["media_storage_connection_string"]["value"].Value<string>());
                Console.WriteLine("FFmpeg thumbnail output: " + fileLocatorProxy.GetPublicReadOnlyUrl());
            }
            else
                Console.WriteLine($"FFmpeg thumbnail job finished with status {ffmpegThumbnailJob.Status}");
        }
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error.ToString());
    }
}

await ExecuteAsync();
Console.WriteLine("Done");