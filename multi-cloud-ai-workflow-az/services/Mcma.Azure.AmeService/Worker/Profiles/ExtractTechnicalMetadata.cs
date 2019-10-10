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

        public async Task ExecuteAsync(WorkerJobHelper<AmeJob> job)
        {
            BlobStorageFileLocator inputFile;
            if (!job.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as S3Locator");

            BlobStorageFolderLocator outputLocation;
            if (!job.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as S3Locator");

            MediaInfoProcess mediaInfoProcess;
            if (inputFile is IUrlLocator urlLocator && !string.IsNullOrWhiteSpace(urlLocator.Url))
            {
                Logger.Debug("Running MediaInfo against " + urlLocator.Url);
                mediaInfoProcess = await MediaInfoProcess.RunAsync("--Output=EBUCore_JSON", urlLocator.Url);
            } 
            else if (!string.IsNullOrWhiteSpace(inputFile.Container) && !string.IsNullOrWhiteSpace(inputFile.FilePath))
            {
                var inputFileStream = await inputFile.Proxy(job.Request).GetAsync();

                var localFileName = @"D:\local\temp\" + Guid.NewGuid().ToString() + ".tmp";
                using (var localFileStream = File.Open(localFileName, FileMode.Create))
                    await inputFileStream.CopyToAsync(localFileStream);

                Logger.Debug("Running MediaInfo against " + localFileName);
                mediaInfoProcess = await MediaInfoProcess.RunAsync("--Output=EBUCore_JSON", localFileName);

                Logger.Debug($"MediaInfo completed. Deleting temp file {localFileName}...");
                File.Delete(localFileName);
                Logger.Debug($"Temp file {localFileName} deleted.");
            }
            else
                throw new Exception("Not able to obtain input file");

            if (string.IsNullOrWhiteSpace(mediaInfoProcess.StdOut))
                throw new Exception("Failed to obtain mediaInfo output");

            var outputPath = (outputLocation.FolderPath ?? string.Empty) + Guid.NewGuid().ToString() + ".json";

            Logger.Debug($"Writing MediaInfo output to container {outputLocation.Container} with path {outputPath}...");

            var container = outputLocation.Proxy(job.Request);
            await container.PutAsync(outputPath, new MemoryStream(Encoding.UTF8.GetBytes(mediaInfoProcess.StdOut)));

            Logger.Debug($"Successfully wrote MediaInfo output to container {outputLocation.Container} with path {outputPath}");

            job.JobOutput.Set("outputFile", new BlobStorageFileLocator
            {
                Container = outputLocation.Container,
                FilePath = outputPath
            });

            await job.CompleteAsync();
        }
    }
}
