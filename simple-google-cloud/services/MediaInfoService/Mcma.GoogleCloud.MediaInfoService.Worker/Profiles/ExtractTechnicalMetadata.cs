using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Mcma.GoogleCloud.Storage;
using Mcma.GoogleCloud.Storage.Proxies;
using Mcma.Worker;

namespace Mcma.GoogleCloud.MediaInfoService.Worker.Profiles
{
    internal class ExtractTechnicalMetadata : IJobProfile<AmeJob>
    {
        public ExtractTechnicalMetadata(StorageClient storageClient)
        {
            StorageClient = storageClient ?? throw new ArgumentNullException(nameof(storageClient));
        }
        
        private StorageClient StorageClient { get; }
        
        public string Name => nameof(ExtractTechnicalMetadata);

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<AmeJob> jobAssignmentHelper, McmaWorkerRequestContext requestContext)
        {
            var logger = jobAssignmentHelper.RequestContext.Logger;
            
            CloudStorageFileLocator inputFile;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as CloudStorageFileLocator");

            CloudStorageFolderLocator outputLocation;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as CloudStorageFolderLocator");

            var localFileName = "/tmp/" + Guid.NewGuid() + ".txt";

            await StorageClient.DownloadLocatorToFileAsync(inputFile, localFileName);

            logger.Debug("Running MediaInfo against " + localFileName);
            var mediaInfoProcess = await MediaInfoProcess.RunAsync(logger, "--Output=EBUCore_JSON", localFileName);

            File.Delete(localFileName);

            if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var outputFileName = Guid.NewGuid() + ".json";

            logger.Debug($"Writing MediaInfo output to bucket {outputLocation.Bucket} with path {outputFileName}...");
            var outputFile = await StorageClient.UploadTextToFolderAsync(outputLocation, outputFileName, mediaInfoProcess.StdOut);

            jobAssignmentHelper.JobOutput.Set(nameof(outputFile), outputFile);

            await jobAssignmentHelper.CompleteAsync();
        }
    }
}
