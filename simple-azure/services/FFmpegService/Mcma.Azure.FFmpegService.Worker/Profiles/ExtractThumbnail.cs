using System;
using System.IO;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;

using Mcma.Worker;

namespace Mcma.Azure.FFmpegService.Worker
{
    internal class ExtractThumbnail : IJobProfile<TransformJob>
    {
        public string Name => nameof(ExtractThumbnail);

        public async Task ExecuteAsync(ProviderCollection providerCollection,
                                       ProcessJobAssignmentHelper<TransformJob> jobHelper,
                                       WorkerRequestContext requestContext)
        {
            BlobStorageFileLocator inputFile;
            if (!jobHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            BlobStorageFolderLocator outputLocation;
            if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Invalid or missing output location.");

            var inputFilePath = @"D:\local\temp\" + Guid.NewGuid() + ".mp4";
            using (var fileStream = File.Open(inputFilePath, FileMode.CreateNew))
                await inputFile.Proxy(requestContext).GetAsync(fileStream);

            var outputFilePath = @"D:\local\temp\" + Guid.NewGuid() + ".png";
            
            await FFmpegProcess.RunAsync(
                jobHelper.Logger,
                "-i",
                inputFilePath,
                "-ss",
                "00:00:00.500",
                "-vframes",
                "1",
                "-vf",
                "scale=200:-1",
                outputFilePath);

            File.Delete(inputFilePath);

            using (var outputFileStream = File.Open(outputFilePath, FileMode.Open))
                jobHelper.JobOutput["outputFile"] =
                    await outputLocation.Proxy(requestContext).PutAsync(Path.GetFileName(outputFilePath), outputFileStream);

            await jobHelper.CompleteAsync();
        }
    }
}
