using System;
using System.IO;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Core;
using Mcma.Worker;

namespace Mcma.Azure.TransformService.Worker
{
    internal class CreateProxyLambda : IJobProfileHandler<TransformJob>
    {
        public const string Name = nameof(CreateProxyLambda);

        public async Task ExecuteAsync(WorkerJobHelper<TransformJob> jobHelper)
        {
            BlobStorageFileLocator inputFile;
            if (!jobHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            BlobStorageFolderLocator outputLocation;
            if (!jobHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Invalid or missing output location.");

            var inputFilePath = @"D:\local\temp\" + Guid.NewGuid();
            using (var fileStream = File.Open(inputFilePath, FileMode.CreateNew))
                await inputFile.Proxy(jobHelper.Variables).GetAsync(fileStream);
            
            var outputFilePath = @"D:\local\temp\" + Guid.NewGuid() + ".mp4";
            var ffmpegParams = new[] {"-y", "-i", inputFilePath, "-preset", "ultrafast", "-vf", "scale=-1:360", "-c:v", "libx264", "-pix_fmt", "yuv420p", outputFilePath};
            var ffmpegProcess = await FFmpegProcess.RunAsync(jobHelper, ffmpegParams);

            File.Delete(inputFilePath);

            using (var outputFileStream = File.Open(outputFilePath, FileMode.Open))
                jobHelper.JobOutput["outputFile"] = outputLocation.Proxy(jobHelper.Variables).PutAsync(Path.GetFileName(outputFilePath), outputFileStream);

            await jobHelper.CompleteAsync();
        }
    }
}
