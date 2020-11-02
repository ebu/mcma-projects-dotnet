using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Worker;
using Microsoft.Extensions.Options;

namespace Mcma.Azure.MediaInfoService.Worker.Profiles
{
    internal class ExtractTechnicalMetadata : IJobProfile<AmeJob>
    {
        public ExtractTechnicalMetadata(IMediaInfoProcess mediaInfoProcess, IOptions<MediaInfoServiceWorkerOptions> options)
        {
            MediaInfoProcess = mediaInfoProcess ?? throw new ArgumentNullException(nameof(mediaInfoProcess));
            Options = options.Value ?? new MediaInfoServiceWorkerOptions();
        }

        public string Name => nameof(ExtractTechnicalMetadata);
        
        private IMediaInfoProcess MediaInfoProcess { get; }
        
        private MediaInfoServiceWorkerOptions Options { get; }

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<AmeJob> processJobAssignmentHelper, McmaWorkerRequestContext requestContext)
        {
            BlobStorageFileLocator inputFile;
            if (!processJobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as BlobStorageFileLocator");
            if (string.IsNullOrWhiteSpace(inputFile.Container))
                throw new Exception("Input file locator does not specify container");
            if (string.IsNullOrWhiteSpace(inputFile.FilePath))
                throw new Exception("Input file locator does not specify file path.");

            BlobStorageFolderLocator outputLocation;
            if (!processJobAssignmentHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as BlobStorageFolderLocator");

            var localFileName = @"D:\local\temp\" + Guid.NewGuid() + ".tmp";
            await using (var localFileStream = File.Open(localFileName, FileMode.Create))
                await inputFile.Proxy(Options.MediaStorageConnectionString).GetAsync(localFileStream);

            processJobAssignmentHelper.RequestContext.Logger.Debug("Running MediaInfo against " + localFileName);
            var (stdOut, _) = await MediaInfoProcess.RunAsync("--Output=EBUCore_JSON", localFileName);

            if (string.IsNullOrWhiteSpace(stdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var outputFileName = Guid.NewGuid() + ".json";

            processJobAssignmentHelper.RequestContext.Logger.Debug(
                $"Writing MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}...");

            var outputFile = await outputLocation.Proxy(Options.MediaStorageConnectionString)
                                                 .PutAsTextAsync(outputFileName,
                                                                 stdOut,
                                                                 new BlobHttpHeaders {ContentType = "application/json"});

            processJobAssignmentHelper.RequestContext.Logger.Debug(
                $"Successfully wrote MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}");

            processJobAssignmentHelper.JobOutput[nameof(outputFile)] = outputFile;

            await processJobAssignmentHelper.CompleteAsync();
        }
    }
}
