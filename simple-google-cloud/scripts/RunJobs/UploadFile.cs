using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Mcma.GoogleCloud.Sample.Scripts.Common;
using Mcma.GoogleCloud.Storage;
using Mcma.GoogleCloud.Storage.Proxies;

namespace Mcma.GoogleCloud.Sample.Scripts.RunJobs
{
    public class FileUploader
    {
        public FileUploader(TerraformOutput terraformOutput, ExecutionIdProvider executionIdProvider, StorageClient storageClient)
        {
            TerraformOutput = terraformOutput ?? throw new ArgumentNullException(nameof(terraformOutput));
            ExecutionIdProvider = executionIdProvider ?? throw new ArgumentNullException(nameof(executionIdProvider));
            StorageClient = storageClient ?? throw new ArgumentNullException(nameof(storageClient));
        }

        private TerraformOutput TerraformOutput { get; }

        private ExecutionIdProvider ExecutionIdProvider { get; }

        private StorageClient StorageClient { get; }

        public async Task<Locator> UploadFileAsync(string localFilePath)
        {
            if (!File.Exists(localFilePath))
                throw new McmaException($"Local file not found at provided path '{localFilePath}'");

            var folderLocator = new CloudStorageFolderLocator {Bucket = TerraformOutput.UploadBucket, FolderPath = ExecutionIdProvider.Id};

            return await StorageClient.UploadFileToFolderAsync(folderLocator, localFilePath);
        }
    }
}