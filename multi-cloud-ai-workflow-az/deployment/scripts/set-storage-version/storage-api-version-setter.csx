#load "../terraform-output.csx"

#r "nuget:Microsoft.Azure.Storage.Blob, 11.0.0"

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

public class StorageApiVersionSetter
{
    public void SetDefaultServiceVersion(string storageConnectionString)
    {
        var cloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);
        var blobClient = cloudStorageAccount.CreateCloudBlobClient();
        var properties = blobClient.GetServiceProperties();
        properties.DefaultServiceVersion = "2013-08-15";
        blobClient.SetServiceProperties(properties);
    }
}