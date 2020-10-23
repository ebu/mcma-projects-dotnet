using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Worker;

namespace Mcma.Azure.MediaInfoService.Worker.Profiles
{
    internal class ExtractTechnicalMetadata : IJobProfile<AmeJob>
    {
        public string Name => nameof(ExtractTechnicalMetadata);

        public async Task ExecuteAsync(ProviderCollection providerCollection, ProcessJobAssignmentHelper<AmeJob> processJobAssignmentHelper, WorkerRequestContext requestContext)
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
            using (var localFileStream = File.Open(localFileName, FileMode.Create))
                await inputFile.Proxy(processJobAssignmentHelper.RequestContext.EnvironmentVariables).GetAsync(localFileStream);

            processJobAssignmentHelper.Logger.Debug("Running MediaInfo against " + localFileName);
            var mediaInfoProcess = await MediaInfoProcess.RunAsync(processJobAssignmentHelper.Logger, "--Output=EBUCore_JSON", localFileName);

            if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var outputFileName = Guid.NewGuid() + ".json";

            processJobAssignmentHelper.Logger.Debug(
                $"Writing MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}...");

            var outputFile = await outputLocation.Proxy(processJobAssignmentHelper.RequestContext.EnvironmentVariables)
                                                 .PutAsTextAsync(outputFileName,
                                                                 mediaInfoProcess.StdOut,
                                                                 new BlobHttpHeaders {ContentType = "application/json"});

            processJobAssignmentHelper.Logger.Debug(
                $"Successfully wrote MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}");

            processJobAssignmentHelper.JobOutput[nameof(outputFile)] = outputFile;

            await processJobAssignmentHelper.CompleteAsync();
        }
    }
}
