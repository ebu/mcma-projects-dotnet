#load "./terraform-output.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"
#r "nuget:Microsoft.Azure.Storage.Blob, 11.0.0"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

public class WebsiteConfigUploader
{
    private static JObject ConfigJson = JObject.Parse(File.ReadAllText("./website/src/config.json"));

    public WebsiteConfigUploader(TerraformOutput terraformOutput)
    {
        TerraformOutput = terraformOutput;
    }

    private TerraformOutput TerraformOutput { get; }

    public async Task UploadConfigAsync()
    {
        Console.WriteLine("Uploading deployment configuration to website");

        // set the services url
        ConfigJson["resourceManager"]["servicesUrl"] = TerraformOutput.ServicesUrl;

        var azureConfig = (JObject)ConfigJson["azure"];
        
        // configure authentication
        var azureAdConfig = (JObject)azureConfig["ad"];
        azureAdConfig["config"]["auth"]["clientId"] = TerraformOutput.WebsiteClientId;
        azureAdConfig["config"]["tenant"] = Build.Inputs.azureTenantName;
        azureAdConfig["scopes"] = new JArray(new[]
            {
                TerraformOutput.ServiceRegistryUrl + TerraformOutput.ServiceRegistryScope,
                TerraformOutput.JobRepositoryUrl + TerraformOutput.JobRepositoryScope
            });

        // configure storage
        var storageConfig = (JObject)azureConfig["storage"];
        storageConfig["websiteStorageAccountName"] = TerraformOutput.WebsiteStorageAccountName;
        storageConfig["mediaStorageAccountName"] = TerraformOutput.MediaStorageAccountName;
        storageConfig["uploadContainer"] = TerraformOutput.UploadContainer;

        try
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(TerraformOutput.WebsiteStorageConnectionString);
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(TerraformOutput.WebsiteContainer);
            var blob = container.GetBlockBlobReference("config.json");

            await blob.UploadTextAsync(ConfigJson.ToString(Formatting.Indented));
        }
        catch (Exception error)
        {
            Console.WriteLine(error);
            return;
        }
    }
}