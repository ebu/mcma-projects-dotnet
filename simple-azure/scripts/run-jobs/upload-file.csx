using Mcma;
using Mcma.Azure.BlobStorage;
using Mcma.Azure.BlobStorage.Proxies;
using Newtonsoft.Json.Linq;

public static async Task<BlobStorageFileLocator> UploadFileAsync(string localFilePath)
{
    if (!File.Exists(localFilePath))
        throw new McmaException($"Local file not found at provided path '{localFilePath}'");

    var uploadFileName = Path.GetFileNameWithoutExtension(localFilePath) + "-" + Guid.NewGuid() + Path.GetExtension(localFilePath);

    var containerLocator = new BlobStorageFolderLocator
    {
        StorageAccountName = TerraformOutput["media_storage_account_name"]["value"].Value<string>(),
        Container = TerraformOutput["upload_container"]["value"].Value<string>(),
        FolderPath = string.Empty
    };

    var containerProxy = containerLocator.Proxy(TerraformOutput["media_storage_connection_string"]["value"].Value<string>());

    using (var localFileStream = File.OpenRead(localFilePath))
        return await containerProxy.PutAsync(uploadFileName, localFileStream);
}