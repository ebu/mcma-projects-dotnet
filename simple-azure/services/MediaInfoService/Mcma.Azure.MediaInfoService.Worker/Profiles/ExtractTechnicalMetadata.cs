using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Worker;

namespace Mcma.Azure.MediaInfoService.Worker.Profiles
{
    internal class ExtractTechnicalMetadata : IJobProfile<AmeJob>
    {
        public string Name => nameof(ExtractTechnicalMetadata);

        public async Task ExecuteAsync(ProviderCollection providerCollection, ProcessJobAssignmentHelper<AmeJob> workerJobHelper, WorkerRequestContext requestContext)
        {
            BlobStorageFileLocator inputFile;
            if (!workerJobHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as BlobStorageFileLocator");
            if (string.IsNullOrWhiteSpace(inputFile.Container))
                throw new Exception("Input file locator does not specify container");
            if (string.IsNullOrWhiteSpace(inputFile.FilePath))
                throw new Exception("Input file locator does not specify file path.");

            BlobStorageFolderLocator outputLocation;
            if (!workerJobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as BlobStorageFolderLocator");

            var localFileName = @"D:\local\temp\" + Guid.NewGuid() + ".tmp";
            using (var localFileStream = File.Open(localFileName, FileMode.Create))
                await inputFile.Proxy(workerJobHelper.RequestContext).GetAsync(localFileStream);

            workerJobHelper.Logger.Debug("Running MediaInfo against " + localFileName);
            var mediaInfoProcess = await MediaInfoProcess.RunAsync(workerJobHelper.Logger, "--Output=EBUCore_JSON", localFileName);

            if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var outputFileName = Guid.NewGuid() + ".json";

            workerJobHelper.Logger.Debug($"Writing MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}...");

            var outputFile = await outputLocation.Proxy(workerJobHelper.RequestContext).PutAsync(outputFileName, new MemoryStream(Encoding.UTF8.GetBytes(mediaInfoProcess.StdOut)));

            workerJobHelper.Logger.Debug($"Successfully wrote MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}");

            workerJobHelper.JobOutput[nameof(outputFile)] = outputFile;

            await workerJobHelper.CompleteAsync();
        }
    }
}
