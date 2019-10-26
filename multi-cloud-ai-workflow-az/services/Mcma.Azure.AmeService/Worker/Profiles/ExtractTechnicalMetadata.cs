using System;
using System.IO;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Azure.BlobStorage;
using Mcma.Worker;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Text;
using Mcma.Azure.BlobStorage.Proxies;

namespace Mcma.Azure.AmeService.Worker
{
    internal class ExtractTechnicalMetadata : IJobProfileHandler<AmeJob>
    {
        public const string Name = nameof(ExtractTechnicalMetadata);

        public async Task ExecuteAsync(WorkerJobHelper<AmeJob> jobHelper)
        {
            BlobStorageFileLocator inputFile;
            if (!jobHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as BlobStorageFileLocator");
            if (string.IsNullOrWhiteSpace(inputFile.Container))
                throw new Exception("Input file locator does not specify container");
            if (string.IsNullOrWhiteSpace(inputFile.FilePath))
                throw new Exception("Input file locator does not specify file path.");

            BlobStorageFolderLocator outputLocation;
            if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as BlobStorageFolderLocator");

            var localFileName = @"D:\local\temp\" + Guid.NewGuid().ToString() + ".tmp";
            using (var localFileStream = File.Open(localFileName, FileMode.Create))
                await inputFile.Proxy(jobHelper.Variables).GetAsync(localFileStream);

            var fileInfo = new FileInfo(localFileName);
            jobHelper.Logger.Debug($"Local file = " + localFileName);
            jobHelper.Logger.Debug($"Local file exists = " + fileInfo.Exists);
            jobHelper.Logger.Debug($"Local file size = " + fileInfo.Length);

            jobHelper.Logger.Debug("Running MediaInfo against " + localFileName);
            var mediaInfoProcess = await MediaInfoProcess.RunAsync(jobHelper, "--Output=EBUCore_JSON", localFileName);

            // jobHelper.Logger.Debug($"MediaInfo completed. Deleting temp file {localFileName}...");
            // File.Delete(localFileName);
            // jobHelper.Logger.Debug($"Temp file {localFileName} deleted.");

            if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var outputFileName = Guid.NewGuid().ToString() + ".json";

            jobHelper.Logger.Debug($"Writing MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}...");

            var outputFile = await outputLocation.Proxy(jobHelper.Variables).PutAsync(outputFileName, new MemoryStream(Encoding.UTF8.GetBytes(mediaInfoProcess.StdOut)));

            jobHelper.Logger.Debug($"Successfully wrote MediaInfo output to container {outputLocation.Container} in folder {outputLocation.FolderPath} with file name {outputFileName}");

            jobHelper.JobOutput[nameof(outputFile)] = outputFile;

            await jobHelper.CompleteAsync();
        }
    }
}
