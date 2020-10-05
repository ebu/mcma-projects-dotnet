#r "nuget:Mcma.Aws.Client, 0.13.14"
#r "nuget:Mcma.Aws.S3, 0.13.14"

#load "../aws-creds.csx"
#load "./upload-file.csx"
#load "./run-mediainfo-job.csx"
#load "./run-ffmpeg-thumbnail-job.csx"
#load "./poll-jobs-for-completion.csx"

using Mcma;
using Mcma.Aws.S3;
using Mcma.Aws.Client;
using Mcma.Client;
using Mcma.Serialization;
using Newtonsoft.Json.Linq;

const string AwsCredentialsPath = "../../deployment/aws-credentials.json";
const string TerraformOutputPath = "../../deployment/terraform.output.json";

var terraformOutput = JObject.Parse(File.ReadAllText(TerraformOutputPath));
var awsCreds = AwsCredentials.Load(AwsCredentialsPath);

var serviceRegistryUrl = terraformOutput["service_registry_url"]["value"].Value<string>();
var resourceManagerProvider =
    new ResourceManagerProvider(
        new AuthProvider().AddAwsV4Auth(awsCreds.AuthContext),
        new ResourceManagerConfig(serviceRegistryUrl.TrimEnd('/') + "/services", AwsConstants.AWS4));

async Task ExecuteAsync()
{
    var testFilePath = Args.FirstOrDefault(x => x.StartsWith("--testFilePath="))?.Replace("--testFilePath=", string.Empty);
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

        Console.WriteLine("Uploading test file...");
        var uploadedFileLocator = await UploadFileAsync(testFilePath, terraformOutput, awsCreds);

        var uuid = Guid.NewGuid().ToString();
        var jobIds = new List<string>();

        string mediaInfoJobId = null;
        if (runMediaInfo)
        {
            Console.WriteLine("Starting MediaInfo job...");
            mediaInfoJobId = await RunMediaInfoJobAsync(resourceManager, terraformOutput, uploadedFileLocator, uuid);
            jobIds.Add(mediaInfoJobId);
            Console.WriteLine("MediaInfo job successfully started: " + mediaInfoJobId);
        }
        
        string ffmpegThumbnailJobId = null;
        if (runFFmpegThumbnail)
        {
            Console.WriteLine("Starting FFmpeg thumbnail job...");
            ffmpegThumbnailJobId = await RunFFmpegThumbnailJobAsync(resourceManager, terraformOutput, uploadedFileLocator, uuid);
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
                var fileLocator = mediaInfoJob.JobOutput.Get<AwsS3FileLocator>("outputFile");
                Console.WriteLine("MediaInfo output: " + fileLocator.Url);
            }
            else
                Console.WriteLine($"MediaInfo job finished with status {mediaInfoJob.Status}");
        }

        if (ffmpegThumbnailJobId != null)
        {
            var ffmpegThumbnailJob = jobs[ffmpegThumbnailJobId];
            if (ffmpegThumbnailJob.Status == JobStatus.Completed)
            {
                var fileLocator = ffmpegThumbnailJob.JobOutput.Get<AwsS3FileLocator>("outputFile");
                Console.WriteLine("FFmpeg thumbnail output: " + fileLocator.Url);
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