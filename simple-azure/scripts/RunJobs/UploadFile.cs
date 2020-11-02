using System.IO;
using System.Threading.Tasks;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Mcma.Azure.Sample.Scripts.Common;

namespace Mcma.Azure.Sample.Scripts.RunJobs
{
    public class FileUploader
    {
        public FileUploader(AzureADCredentials azureADCredentials, TerraformOutput terraformOutput, ExecutionIdProvider executionIdProvider)
        {
            AzureADCredentials = azureADCredentials;
            TerraformOutput = terraformOutput;
            ExecutionIdProvider = executionIdProvider;
        }
     
        private AzureADCredentials AzureADCredentials { get; }

        private TerraformOutput TerraformOutput { get; }

        private ExecutionIdProvider ExecutionIdProvider { get; }

        public async Task<string> UploadFileAsync(string localFilePath)
        {
            if (!File.Exists(localFilePath))
                throw new McmaException($"Local file not found at provided path '{localFilePath}'");

            var uploadFileName = ExecutionIdProvider.Id + "/" + Path.GetFileName(localFilePath);

            var containerLocator = new BlobStorageFolderLocator
            {
                StorageAccountName = TerraformOutput.MediaStorageAccountName,
                Container = TerraformOutput.UploadContainer,
                FolderPath = string.Empty
            };

            var containerProxy = containerLocator.Proxy(TerraformOutput.MediaStorageConnectionString);

            await using var localFileStream = File.OpenRead(localFilePath);
            await containerProxy.PutAsync(uploadFileName, localFileStream);

            return uploadFileName;
        }
    }
}