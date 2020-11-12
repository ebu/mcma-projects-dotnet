using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Mcma.GoogleCloud.Storage;
using Mcma.GoogleCloud.Storage.Proxies;
using Mcma.Worker;

namespace Mcma.GoogleCloud.FFmpegService.Worker
{
    internal class ExtractThumbnail : IJobProfile<TransformJob>
    {
        public ExtractThumbnail(StorageClient storageClient)
        {
            StorageClient = storageClient;
        }
        
        private StorageClient StorageClient { get; }

        public string Name => nameof(ExtractThumbnail);

        public async Task ExecuteAsync(ProcessJobAssignmentHelper<TransformJob> jobAssignmentHelper, McmaWorkerRequestContext requestContext)
        {
            var logger = jobAssignmentHelper.RequestContext.Logger;
            
            CloudStorageFileLocator inputFile;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(inputFile), out inputFile))
                throw new Exception("Invalid or missing input file.");

            CloudStorageFolderLocator outputLocation;
            if (!jobAssignmentHelper.JobInput.TryGet(nameof(outputLocation), out outputLocation))
                throw new Exception("Invalid or missing output location.");

            var tempId = Guid.NewGuid().ToString();
            var tempVideoFile = "/tmp/video_" + tempId + ".mp4";
            var tempThumbFile = "/tmp/thumb_" + tempId + ".png";

            try
            {
                logger.Info("Get video from s3 location: " + inputFile.Bucket + " " + inputFile.FilePath);

                await StorageClient.DownloadLocatorToFileAsync(inputFile, tempVideoFile);
            
                await FFmpegProcess.RunAsync(
                    logger,
                    "-i",
                    tempVideoFile,
                    "-ss",
                    "00:00:00.500",
                    "-vframes",
                    "1",
                    "-vf",
                    "scale=200:-1",
                    tempThumbFile);

                var outputFile = await StorageClient.UploadFileToFolderAsync(outputLocation, tempThumbFile);

                jobAssignmentHelper.JobOutput.Set(nameof(outputFile), outputFile);

                await jobAssignmentHelper.CompleteAsync();
            }
            finally
            {
                try
                {
                    if (File.Exists(tempVideoFile))
                        File.Delete(tempVideoFile);
                }
                catch
                {
                    // just ignore this
                }

                try
                {
                    if (File.Exists(tempThumbFile))
                        File.Delete(tempThumbFile);
                }
                catch
                {
                    // just ignore this
                }
            }
        }
    }
}
