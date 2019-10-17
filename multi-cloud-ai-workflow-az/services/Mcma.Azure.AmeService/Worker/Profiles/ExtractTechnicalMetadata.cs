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
                throw new Exception("Unable to parse input file as S3Locator");

            BlobStorageFolderLocator outputLocation;
            if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as S3Locator");

            MediaInfoProcess mediaInfoProcess;
            if (inputFile is IUrlLocator urlLocator && !string.IsNullOrWhiteSpace(urlLocator.Url))
            {
                jobHelper.Logger.Debug("Running MediaInfo against " + urlLocator.Url);
                mediaInfoProcess = await MediaInfoProcess.RunAsync(jobHelper, "--Output=EBUCore_JSON", urlLocator.Url);
            } 
            else if (!string.IsNullOrWhiteSpace(inputFile.Container) && !string.IsNullOrWhiteSpace(inputFile.FilePath))
            {
                var inputFileStream = await inputFile.Proxy(jobHelper.Variables).GetAsync();

                var localFileName = @"D:\local\temp\" + Guid.NewGuid().ToString() + ".tmp";
                using (var localFileStream = File.Open(localFileName, FileMode.Create))
                    await inputFileStream.CopyToAsync(localFileStream);

                jobHelper.Logger.Debug("Running MediaInfo against " + localFileName);
                mediaInfoProcess = await MediaInfoProcess.RunAsync(jobHelper, "--Output=EBUCore_JSON", localFileName);

                jobHelper.Logger.Debug($"MediaInfo completed. Deleting temp file {localFileName}...");
                File.Delete(localFileName);
                jobHelper.Logger.Debug($"Temp file {localFileName} deleted.");
            }
            else
                throw new Exception("Not able to obtain input file");

            if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var outputPath = (outputLocation.FolderPath ?? string.Empty) + Guid.NewGuid().ToString() + ".json";

            jobHelper.Logger.Debug($"Writing MediaInfo output to container {outputLocation.Container} with path {outputPath}...");

            var outputFile = await outputLocation.Proxy(jobHelper.Variables).PutAsync(outputPath, new MemoryStream(Encoding.UTF8.GetBytes(mediaInfoProcess.StdOut)));

            jobHelper.Logger.Debug($"Successfully wrote MediaInfo output to container {outputLocation.Container} with path {outputPath}");

            jobHelper.JobOutput[nameof(outputFile)] = outputFile;

            await jobHelper.CompleteAsync();
        }
    }
}
