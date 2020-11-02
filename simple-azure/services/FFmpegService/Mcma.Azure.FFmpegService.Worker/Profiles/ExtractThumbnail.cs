using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Worker;
using Microsoft.Extensions.Options;

namespace Mcma.Azure.FFmpegService.Worker
{
    internal class ExtractThumbnail : IJobProfile<TransformJob>
    {
        public ExtractThumbnail(IFFmpegProcess ffmpegProcess, IOptions<FFmpegServiceWorkerOptions> options)
        {
            FFmpegProcess = ffmpegProcess ?? throw new ArgumentNullException(nameof(ffmpegProcess));
            Options = options.Value ?? new FFmpegServiceWorkerOptions();
        }
        
        private IFFmpegProcess FFmpegProcess { get; }

        private FFmpegServiceWorkerOptions Options { get; }

        public string Name => nameof(ExtractThumbnail);


        public async Task ExecuteAsync(ProcessJobAssignmentHelper<TransformJob> jobAssignmentHelper, McmaWorkerRequestContext requestContext)
        {
            BlobStorageFileLocator inputFile;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            BlobStorageFolderLocator outputLocation;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Invalid or missing output location.");

            var inputFilePath = @"D:\local\temp\" + Guid.NewGuid() + ".mp4";
            
            await using (var fileStream = File.Open(inputFilePath, FileMode.CreateNew))
                await inputFile.Proxy(Options.MediaStorageConnectionString).GetAsync(fileStream);

            var outputFilePath = @"D:\local\temp\" + Guid.NewGuid() + ".png";
            
            await FFmpegProcess.RunAsync(
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

            await using (var outputFileStream = File.Open(outputFilePath, FileMode.Open))
                jobAssignmentHelper.JobOutput["outputFile"] =
                    await outputLocation.Proxy(Options.MediaStorageConnectionString)
                                        .PutAsync(Path.GetFileName(outputFilePath),
                                                  outputFileStream,
                                                  new BlobHttpHeaders {ContentType = "image/png"});

            await jobAssignmentHelper.CompleteAsync();
        }
    }
}
