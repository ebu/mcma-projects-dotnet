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

namespace Mcma.Azure.AmeService.Worker
{
    internal class ExtractTechnicalMetadata : IJobProfileHandler<AmeJob>
    {
        public const string Name = nameof(ExtractTechnicalMetadata);

        public async Task ExecuteAsync(WorkerJobHelper<AmeJob> job)
        {
            var storageConnectionString = job.Request.GetRequiredContextVariable("MediaStorageConnectionString");
            if (!CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount))
                throw new Exception($"Failed to parse connection string '{storageConnectionString}' for media storage account.");

            var storageClient = storageAccount.CreateCloudBlobClient();

            BlobStorageLocator inputFile;
            if (!job.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Unable to parse input file as S3Locator");

            BlobStorageLocator outputLocation;
            if (!job.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Unable to parse output location as S3Locator");

            MediaInfoProcess mediaInfoProcess;
            if (inputFile is HttpEndpointLocator httpEndpointLocator && !string.IsNullOrWhiteSpace(httpEndpointLocator.HttpEndpoint))
            {
                Logger.Debug("Running MediaInfo against " + httpEndpointLocator.HttpEndpoint);
                mediaInfoProcess = await MediaInfoProcess.RunAsync("--Output=EBUCore_JSON", httpEndpointLocator.HttpEndpoint);
            } 
            else if (!string.IsNullOrWhiteSpace(inputFile.Container) && !string.IsNullOrWhiteSpace(inputFile.Path))
            {
                var inputFileContainerRef = storageClient.GetContainerReference(inputFile.Container);
                var inputFileBlobRef = inputFileContainerRef.GetBlockBlobReference(inputFile.Path);

                var localFileName = @"D:\local\temp\" + Guid.NewGuid().ToString() + ".tmp";
                await inputFileBlobRef.DownloadToFileAsync(localFileName, FileMode.CreateNew);

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

            var outputPath = (outputLocation.Path ?? string.Empty) + Guid.NewGuid().ToString() + ".json";

            Logger.Debug($"Writing MediaInfo output to container {outputLocation.Container} with path {outputPath}...");

            var container = storageClient.GetContainerReference(outputLocation.Container);
            var blob = container.GetBlockBlobReference(outputPath);
            await blob.UploadFromStreamAsync(new MemoryStream(Encoding.UTF8.GetBytes(mediaInfoProcess.StdOut)));

            Logger.Debug($"Successfully wrote MediaInfo output to container {outputLocation.Container} with path {outputPath}");

            job.JobOutput.Set("outputFile", new BlobStorageLocator
            {
                Container = outputLocation.Container,
                Path = outputPath
            });

            await job.CompleteAsync();
        }
    }
}
